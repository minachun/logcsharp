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
        private static long adjustLogTicks;

        private static string currentLogFileName;
        private static string baseLogFileName;
        private static int max_rotate;
        private static int cur_rotate;
        private static long max_rotatesize;
        private static long current_size;
        private static bool isAppendLogging;
        private static Level minLevel;
        private static uint failcount;
        private static bool isAppendFile;

        // ログコンテナクラス
        private class LogAtom
        {
            private Level lvl;
            private string body;
            private long currentTime;
            private long currentTick;
            private string source;
            private string member;
            private int lineno;

            public LogAtom(Level _lvl, string _bd, string _sr, string _mn, int _ln)
            {
                this.lvl = _lvl;
                this.body = _bd;
                this.currentTime = DateTime.Now.Ticks;
                this.currentTick = Log.logtime.ElapsedTicks;
                this.source = _sr;
                this.member = _mn;
                this.lineno = _ln;
            }

            public string Contents
            {
                get
                {
                    DateTime t = new DateTime(this.currentTime);
                    long usecs = ((this.currentTick - Log.adjustLogTicks) % Stopwatch.Frequency) * 10_000_000 / Stopwatch.Frequency;
                    if ( string.IsNullOrEmpty(this.source) )
                    {
                        return $"{ t.ToString("yyyy-MM-dd_HH:mm:ss") }.{usecs:D7} | [{this.lvl.ToString()}] | {this.body}";
                    }
                    else
                    {
                        string[] ss = this.source.Split('\\');
                        // ログのフォーマットはここで決める
                        return $"{ t.ToString("yyyy-MM-dd_HH:mm:ss") }.{usecs:D7} | [{this.lvl.ToString()}] {ss[^1]}::{this.member}({this.lineno}) | {this.body}";
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
            bool isT = false;
            StringBuilder logb = new StringBuilder();
            do
            {
                // 溜まっているかチェック
                if ( logfifo.Count == 0 )
                {
                    // 溜まっていないので小休止して
                    Thread.Sleep(1);
                    continue;
                }
                lock(logfifo)
                {
                    // 溜まっている分は全部吐き出す
                    while (logfifo.Count > 0)
                    {
                        la = logfifo.Take();
                        logb.AppendLine(la.Contents);
                        isT = isT || la.IsTerminate;
                    }
                }
                // ファイルへ出力
                System.IO.File.AppendAllText(currentLogFileName, logb.ToString());
                if ( max_rotate > 0 )
                {
                    current_size += logb.Length;
                    if (current_size >= max_rotatesize )
                    {
                        // ファイル名をローテート
                        Log.RotateFilename();
                    }
                }
                // バッファをクリア
                logb.Clear();
            } while (isT == false);
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

        public static void Setup(string basefilename, Level outputLevel, bool isAppend = false, int rotatenum = 0, long rotatesize = 10*1024*1024, int fifosize = 128*1024)
        {
            // 時刻合わせ
            Log.logtime = new Stopwatch();
            Log.logtime.Start();
            int _s = System.DateTime.Now.Second;
            while (_s == System.DateTime.Now.Second) ;
            Log.adjustLogTicks = Log.logtime.ElapsedTicks;

            minLevel = outputLevel;
            isAppendLogging = isAppend;
            max_rotate = rotatenum;
            max_rotatesize = rotatesize;
            if ( rotatenum > 0 )
            {
                string[] lgbase = basefilename.Split('.');
                baseLogFileName = $"{string.Join('.', lgbase[0..^1])}_{{0}}.{lgbase[^1]}";
                cur_rotate = 0;
                currentLogFileName = string.Format(baseLogFileName, cur_rotate);
            } else
            {
                baseLogFileName = basefilename;
                currentLogFileName = baseLogFileName;
            }
            // ログバッファを確保
            logfifo = new BlockingCollection<LogAtom>(fifosize);
            failcount = 0;

            isAppendFile = isAppend;
            if (isAppendFile == false )
            {
                // すぐに書いて新規作成にする
                System.IO.File.AppendText(currentLogFileName).Close();
            }
            var fi = new System.IO.FileInfo(currentLogFileName);
            current_size = fi.Length;
            logfifo.Add(new LogAtom(Level.FATAL, "Log Start.", string.Empty, string.Empty, 0));
            LogWriteThread = new Thread(LogWriteProc);
            LogWriteThread.Start();
        }

        private static void RotateFilename()
        {
            cur_rotate++;
            if ( cur_rotate >= max_rotate )
            {
                cur_rotate = 0;
            }
            currentLogFileName = string.Format(baseLogFileName, cur_rotate);
            if (isAppendFile == false)
            {
                // すぐに書いて新規作成にする
                System.IO.File.CreateText(currentLogFileName).Close();
            }
            var fi = new System.IO.FileInfo(currentLogFileName);
            current_size = fi.Length;
        }

        public static void Terminate()
        {
            // ログ終了の通知
            logfifo.Add(new LogAtom(Level.FATAL, $"Log Terminated. fail={Log.failcount}", string.Empty, string.Empty, -1));
            LogWriteThread.Join();
            logfifo.Dispose();
        }

        public static void WriteFATAL(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            bool _b;
            lock(logfifo)
            {
                _b = logfifo.TryAdd(new LogAtom(Level.FATAL, body, _f, _m, _l), 1);
            }
            if ( !_b )
            {
                failcount++;
                System.Diagnostics.Debug.WriteLine("[FATAL] Can't write the log.");
            }
        }

        public static void WriteERROR(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.ERROR)
            {
                bool _b;
                lock(logfifo)
                {
                    _b = logfifo.TryAdd(new LogAtom(Level.ERROR, body, _f, _m, _l), 1);
                }
                if (!_b)
                {
                    failcount++;
                    System.Diagnostics.Debug.WriteLine("[ERROR] Can't write the log.");
                }
            }
        }

        public static void WriteWARNING(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.WARNING)
            {
                bool _b;
                lock(logfifo)
                {
                    _b = logfifo.TryAdd(new LogAtom(Level.WARNING, body, _f, _m, _l), 1);
                }
                if (!_b)
                {
                    failcount++;
                    System.Diagnostics.Debug.WriteLine("[WARNING] Can't write the log.");
                }
            }
        }

        public static void WriteINFO(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.INFO)
            {
                bool _b;
                lock(logfifo)
                {
                    _b = logfifo.TryAdd(new LogAtom(Level.INFO, body, _f, _m, _l), 1);
                }
                if (!_b)
                {
                    failcount++;
                    System.Diagnostics.Debug.WriteLine("[INFO] Can't write the log.");
                }
            }
        }

        public static void WriteDEBUG(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.DEBUG)
            {
                bool _b;
                lock(logfifo)
                {
                    _b = logfifo.TryAdd(new LogAtom(Level.DEBUG, body, _f, _m, _l), 1);
                }
                if (!_b)
                {
                    failcount++;
                    System.Diagnostics.Debug.WriteLine("[DEBUG] Can't write the log.");
                }
            }
        }

        public static void WriteDETAIL(string body, [CallerFilePath] string _f = "", [CallerMemberName] string _m = "", [CallerLineNumber] int _l = 0)
        {
            if (minLevel >= Level.DETAIL)
            {
                bool _b;
                lock(logfifo)
                {
                    _b = logfifo.TryAdd(new LogAtom(Level.DETAIL, body, _f, _m, _l), 1);
                }
                if (!_b)
                {
                    failcount++;
                    System.Diagnostics.Debug.WriteLine("[DETAIL] Can't write the log.");
                }
            }
        }

    }
}
