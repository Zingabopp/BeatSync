using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BeatSync.Playlists;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace BeatSyncTests
{
    [TestClass]
    public class PlaylistTests
    {
        [TestMethod]
        public void TestMethod1()
        {
            var playlists = PlaylistManager.DefaultPlaylists;
            var song1 = new PlaylistSong("63F2998EDBCE2D1AD31917E4F4D4F8D66348105D", "Sun Pluck", "3a9b", "ruckus");
            StackTest();
            foreach (var playlist in playlists.Values)
            {
                
            }
        }

        public void StackTest()
        {
            Stopwatch sw;
            // warm up
            for (int i = 0; i < 100000; i++)
            {
                TraceCall();
            }

            // call 100K times, tracing *disabled*, passing method name
            sw = Stopwatch.StartNew();
            traceCalls = false;
            for (int i = 0; i < 100000; i++)
            {
                TraceCall(MethodBase.GetCurrentMethod());
            }
            sw.Stop();
            Console.WriteLine("Tracing Disabled, passing Method Name: {0}ms"
                             , sw.ElapsedMilliseconds);

            // call 100K times, tracing *enabled*, passing method name
            sw = Stopwatch.StartNew();
            traceCalls = true;
            for (int i = 0; i < 100000; i++)
            {
                TraceCall(MethodBase.GetCurrentMethod());
            }
            sw.Stop();
            Console.WriteLine("Tracing Enabled, passing Method Name: {0}ms"
                             , sw.ElapsedMilliseconds);

            // call 100K times, tracing *disabled*, determining method name
            sw = Stopwatch.StartNew();
            traceCalls = false;
            for (int i = 0; i < 100000; i++)
            {
                Debug(string.Empty);
            }
            sw.Stop();
            var timeSpan = new TimeSpan(sw.Elapsed.Ticks / 100000);
            Console.WriteLine("Tracing Disabled, looking up Method Name: {0}ms, {1}us per call"
                       , sw.ElapsedMilliseconds, timeSpan.TotalMilliseconds*1000);

            // call 100K times, tracing *enabled*, determining method name
            sw = Stopwatch.StartNew();
            traceCalls = true;
            for (int i = 0; i < 100000; i++)
            {
                TraceCall();
            }
            Console.WriteLine("Tracing Enabled, looking up Method Name: {0}ms"
                       , sw.ElapsedMilliseconds);
        }

        public static void Debug(string message, [CallerFilePath] string file = "", [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
        {
            
        }

        private static void TraceCall()
        {
            if (traceCalls)
            {
                StackFrame stackFrame = new StackFrame(1);
                TraceCall(stackFrame.GetMethod().Name);
            }
        }

        private static void TraceCall(MethodBase method)
        {
            if (traceCalls)
            {
                TraceCall(method.Name);
            }
        }
        static bool traceCalls;
        private static void TraceCall(StackFrame frame)
        {
            // Write to log
        }
        private static void TraceCall(string methodName)
        {
            // Write to log
        }

        [TestMethod]
        public void ConvertLegacy_Test()
        {
            PlaylistManager.ConvertLegacyPlaylists();
        }
    }
}
