﻿using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace NzbDrone.Common
{
    public class EnviromentProvider
    {
        public const string IIS_FOLDER_NAME = "iisexpress";

        public const string NZBDRONE_PATH = "NZBDRONE_PATH";
        public const string NZBDRONE_PID = "NZBDRONE_PID";

#if DEBUG
        private static readonly bool isInDebug = true;
#else
        private static readonly bool isInDebug = false; 
#endif

        private static readonly string processName = Process.GetCurrentProcess().ProcessName.ToLower();

        public static bool IsProduction
        {
            get
            {
                if (isInDebug || Debugger.IsAttached) return false;

                Console.WriteLine(processName);
                if (processName.Contains("nunit")) return false;
                if (processName.Contains("jetbrain")) return false;
                if (processName.Contains("resharper")) return false;

                return true;
            }
        }

        public virtual bool IsUserInteractive
        {
            get { return Environment.UserInteractive; }
        }

        public virtual string ApplicationPath
        {
            get
            {
                var dir = new DirectoryInfo(Environment.CurrentDirectory);

                while (!ContainsIIS(dir))
                {
                    if (dir.Parent == null) break;
                    dir = dir.Parent;
                }

                if (ContainsIIS(dir)) return dir.FullName;

                dir = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory;

                while (!ContainsIIS(dir))
                {
                    if (dir.Parent == null) throw new ApplicationException("Can't fine IISExpress folder.");
                    dir = dir.Parent;
                }

                return dir.FullName;
            }
        }


        public virtual string StartUpPath
        {
            get
            {
                return new FileInfo(Assembly.GetExecutingAssembly().Location).Directory.FullName;
            }
        }

        public virtual String SystemTemp
        {
            get
            {
                return Path.GetTempPath();
            }
        }

        public virtual Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public virtual DateTime BuildDateTime
        {
            get
            {
                var fileLocation = Assembly.GetCallingAssembly().Location;
                return new FileInfo(fileLocation).CreationTime;
            }
        }

        public virtual int NzbDroneProcessIdFromEnviroment
        {
            get
            {
                var id = Convert.ToInt32(Environment.GetEnvironmentVariable(NZBDRONE_PID));

                if (id == 0)
                    throw new InvalidOperationException("NZBDRONE_PID isn't a valid environment variable.");

                return id;
            }
        }

        private static bool ContainsIIS(DirectoryInfo dir)
        {
            return dir.GetDirectories(IIS_FOLDER_NAME).Length != 0;
        }
    }
}