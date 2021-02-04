using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace logcsharp
{
    public static class Log
    {

        private static Thread LogWriteThread;
        private static BlockingCollection<LogAtom> logfifo;
        private static Stopwatch logtime;
        private static DateTime logstarttime;

        private static string currentLogFileName;
        private static string baseLogFileName;
        private static int max_rotate;
        private static int cur_rotate;
        private static long max_rotatesize;
        private static bool isAppendLogging;
        private static Level minLevel;

        private class LogAtom
        {
            private Level lvl;
            private string body;
            private long currentTime;
            private string source;
            private string member;
            private int lineno;

            public LogAtom(Level _lvl, string _bd, string _sr, string _mn, int _ln)
            {
                this.lvl = _lvl;
                this.body = _bd;
                this.currentTime = DateTime.Now.Ticks;
                this.source = _sr;
                this.member = _mn;
                this.lineno = _ln;
            }

            public string Contents
            {
                get
                {
                    DateTime t = new DateTime(this.currentTime);
                    if ( string.IsNullOrEmpty(this.source) )
                    {
                        return $"{ t.ToString("yyyy-MM-dd_HH:mm:ss.fff") } | [{this.lvl.ToString()}] | {this.body}\n";
                    }
                    else
                    {
                        string[] ss = this.source.Split('\\');
                        return $"{ t.ToString("yyyy-MM-dd_HH:mm:ss.fff") } | [{this.lvl.ToString()}] {ss[^1]}::{this.member}({this.lineno}) | {this.body}\n";
                    }
                }
            }

            public bool IsTerminate
            {
                get
                {
                    return this.lineno < 0;
                }
            }
        }

        private static void LogWriteProc()
        {
            LogAtom la;
            do
            {
                if (logfifo.TryTake(out la, 1) == false)
                {
                    continue;
                }
                if (la.IsTerminate)
                {
                    break;
                }
                System.IO.File.AppendAllText(currentLogFileName, la.Contents);
            } while (true);
            System.IO.File.AppendAllText(currentLogFileName, la.Contents);
        }

        public enum Level
        {
            FATAL,
            ERROR,
            WARNING,
            INFO,
            DEBUG,
            DETAIL,
        }

        public static void Setup(string basefilename, Level outputLevel, bool isAppend = false, int rotatenum = 0, long rotatesize = 10*1024*1024)
        {
            minLevel = outputLevel;
            isAppendLogging = isAppend;
            max_rotate = rotatenum;
            max_rotatesize = rotatesize;
            if ( rotatenum > 0 )
            {
                string[] lgbase = basefilename.Split('.');
                string[] basenames = lgbase[0..^1];
                baseLogFileName = $"{string.Join('.', basenames)}_{{0}}.{lgbase[^1]}";
                cur_rotate = 0;
                currentLogFileName = string.Format(baseLogFileName, cur_rotate);
            } else
            {
                baseLogFileName = basefilename;
                currentLogFileName = baseLogFileName;
            }
            logfifo = new BlockingCollection<LogAtom>(1000);
            LogWriteThread = new Thread(LogWriteProc);
            LogWriteThread.Start();
            logfifo.Add(new LogAtom(Level.FATAL, "Log Start.", string.Empty, string.Empty, 0));
        }

        public static void Terminate()
        {
            // ログ終了の通知
            logfifo.Add(new LogAtom(Level.FATAL, "Log Terminated.", string.Empty, string.Empty, -1));
            LogWriteThread.Join();
            logfifo.Dispose();
        }

        public static void WriteFATAL(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            logfifo.Add(new LogAtom(Level.FATAL, body, _f, _m, _l));
        }

        public static void WriteERROR(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.ERROR)
            {
                logfifo.Add(new LogAtom(Level.ERROR, body, _f, _m, _l));
            }
        }

        public static void WriteWARNING(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.WARNING)
            {
                logfifo.Add(new LogAtom(Level.WARNING, body, _f, _m, _l));
            }
        }

        public static void WriteINFO(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.INFO)
            {
                logfifo.Add(new LogAtom(Level.INFO, body, _f, _m, _l));
            }
        }

        public static void WriteDEBUG(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.DEBUG)
            {
                logfifo.Add(new LogAtom(Level.DEBUG, body, _f, _m, _l));
            }
        }

        public static void WriteDETAIL(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.DETAIL)
            {
                logfifo.Add(new LogAtom(Level.DETAIL, body, _f, _m, _l));
            }
        }

    }
}
