using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyLog
{
    
    class Log
    {
        public const int LEVEL_WARN = 1;
        public const int LEVEL_LOG = 2; 
        private string logPathPrefix;
        private string logPath;
        private static object locker = new object();
        public Log(string prefix)
        {
            this.logPathPrefix = prefix;
            DateTime now = DateTime.Now;
            this.logPath = this.logPathPrefix + "\\log\\";
            if (!Directory.Exists(this.logPath)) Directory.CreateDirectory(this.logPath);
            this.logPath = this.logPath + "\\" + now.ToString("yyyyMMdd") + ".log";
            if (!File.Exists(this.logPath)) File.Create(this.logPath).Close();
        }
        public bool WriteLog(string msg, int logLevel=2)
        {
            DateTime now = DateTime.Now;
            string time = now.ToString("yyyy/MM/dd HH:mm:ss");
            if (logLevel == LEVEL_LOG)
            {
                msg = "[log]["+time+"] " + msg;
            }
            if (logLevel == LEVEL_WARN)
            {
                msg = "[warn]["+time+"] " + msg;
            }
            lock (locker)
            {
                Console.WriteLine(msg);
                File.AppendAllText(this.logPath, msg + "\n");
                return true;
            }
            
        }
    }
}
