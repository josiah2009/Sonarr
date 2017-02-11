using System;
using System.Collections.Generic;
using System.Linq;
using Nancy;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using Sonarr.Api.V3.Episodes;
using Sonarr.Api.V3.Series;
using Sonarr.Http;
using Sonarr.Http.Extensions;
using Sonarr.Http.REST;

namespace Sonarr.Api.V3.History
{
    public class HistoryModule : SonarrRestModule<HistoryResource>
    {
        private readonly IHistoryService _historyService;
        private readonly IQualityUpgradableSpecification _qualityUpgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;

        public HistoryModule(IHistoryService historyService,
                             IQualityUpgradableSpecification qualityUpgradableSpecification,
                             IFailedDownloadService failedDownloadService)
        {
            _historyService = historyService;
            _qualityUpgradableSpecification = qualityUpgradableSpecification;
            _failedDownloadService = failedDownloadService;
            GetResourcePaged = GetHistory;

            Get["/since"] = x => GetHistorySince();
            Post["/failed"] = x => MarkAsFailed();
        }

        protected HistoryResource MapToResource(NzbDrone.Core.History.History model, bool includeSeries, bool includeEpisode)
        {
            var resource = model.ToResource();

            if (includeSeries)
            {
                resource.Series = model.Series.ToResource();
            }

            if (includeEpisode)
            {
                resource.Episode = model.Episode.ToResource();
            }

            if (model.Series != null)
            {
                resource.QualityCutoffNotMet = _qualityUpgradableSpecification.CutoffNotMet(model.Series.Profile.Value, model.Quality);
            }

            return resource;
        }

        private PagingResource<HistoryResource> GetHistory(PagingResource<HistoryResource> pagingResource)
        {
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, NzbDrone.Core.History.History>("date", SortDirection.Descending);
            var includeSeries = Request.GetBooleanQueryParameter("includeSeries");
            var includeEpisode = Request.GetBooleanQueryParameter("includeEpisode");

            if (pagingResource.FilterKey == "eventType")
            {
                var filterValue = (HistoryEventType)Convert.ToInt32(pagingResource.FilterValue);
                pagingSpec.FilterExpression = v => v.EventType == filterValue;
            }

            if (pagingResource.FilterKey == "episodeId")
            {
                int episodeId = Convert.ToInt32(pagingResource.FilterValue);
                pagingSpec.FilterExpression = h => h.EpisodeId == episodeId;
            }

            return ApplyToPage(_historyService.Paged, pagingSpec, h => MapToResource(h, includeSeries, includeEpisode));
        }

        private List<HistoryResource> GetHistorySince()
        {
            var queryDate = Request.Query.Date;
            var queryEventType = Request.Query.EventType;
            
            if (!queryDate.HasValue)
            {
                throw new BadRequestException("date is missing");
            }

            DateTime date = DateTime.Parse(queryDate.Value);
            HistoryEventType? eventType = null;
            var includeSeries = Request.GetBooleanQueryParameter("includeSeries");
            var includeEpisode = Request.GetBooleanQueryParameter("includeEpisode");

            if (queryEventType.HasValue)
            {
                eventType = (HistoryEventType)Convert.ToInt32(queryEventType.Value);
            }

            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeSeries, includeEpisode)).ToList();
        }

        private Response MarkAsFailed()
        {
            var id = (int)Request.Form.Id;
            _failedDownloadService.MarkAsFailed(id);
            return new object().AsResponse();
        }
    }
}
