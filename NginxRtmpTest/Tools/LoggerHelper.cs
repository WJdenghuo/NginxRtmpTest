using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NginxRtmpTest.Tools
{
    public class LoggerHelper
    {
        private static LoggerHelper _loggerHelper;
        private static object _lockObj = new object();
        static FileInfo logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");
        private LoggerHelper()
        {
            XmlConfigurator.ConfigureAndWatch(logCfg);
            Log = LogManager.GetLogger(typeof(LoggerHelper));
        }
        public static LoggerHelper GetInstance()
        {
            if (_loggerHelper==null)
            {
                lock (_lockObj)
                {
                    if (_loggerHelper==null)
                    {
                        _loggerHelper = new LoggerHelper();
                    }
                }
            }
            return _loggerHelper;
        }
        public ILog Log { get; set; }
    }
}