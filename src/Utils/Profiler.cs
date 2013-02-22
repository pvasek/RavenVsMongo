using System;
using System.Diagnostics;

namespace RavenVsMongo.Utils
{
    public class Profiler: IDisposable
    {
        private readonly Stopwatch _watch;
        private readonly string _message;
        private readonly object[] _args;

        private Profiler(string message, object[] args)
        {
            _message = message;
            _args = args;
            _watch = Stopwatch.StartNew();
            System.Console.WriteLine("START - {0}", message);
        }

        public static long LastTimeMs { get; private set; }

        public static Profiler Start(string message, params object[] args)
        {
            return new Profiler(message, args);
        }

        public static T Measure<T>(string message, Func<T> func)
        {
            T result;
            using (Start(message))
            {
                result = func();
            }
            return result;
        }

        public void Dispose()
        {
            _watch.Stop();
            LastTimeMs = _watch.ElapsedMilliseconds;
            System.Console.WriteLine("END ({0} ms) - {1}", _watch.ElapsedMilliseconds, String.Format(_message, _args));
        }
    }
}