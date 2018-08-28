using System;
using System.Collections.Generic;
using System.Linq;

namespace Magnus
{
    class Profiler
    {
        public static readonly Profiler Instance = new Profiler();

        private DateTime lastTime, frameStartTime;

        private int prevFrames = 0, currentFrames = 0;
        private Dictionary<string, double> prevStats = new Dictionary<string, double>(), currentStats = new Dictionary<string, double>();

        public int FPS => prevFrames;
        public IEnumerable<KeyValuePair<string, double>> TotalStats => prevStats;
        public IEnumerable<KeyValuePair<string, double>> AverageStats => prevFrames == 0 ? new KeyValuePair<string, double>[0] : TotalStats.Select(pair => new KeyValuePair<string, double>(pair.Key, pair.Value / prevFrames));

        private Profiler()
        {
            lastTime = frameStartTime = DateTime.Now;
        }

        public void LogEvent(string eventName)
        {
            var now = DateTime.Now;
            if (!currentStats.ContainsKey(eventName))
            {
                currentStats[eventName] = 0;
            }
            currentStats[eventName] += (now - lastTime).TotalMilliseconds;
            lastTime = now;
        }

        public void LogFrameStart()
        {
            LogEvent("Before frame");
            var now = lastTime;
            if (now.Second != frameStartTime.Second)
            {
                prevFrames = currentFrames;
                currentFrames = 0;
                prevStats = currentStats;
                currentStats = new Dictionary<string, double>();
            }
            ++currentFrames;
            frameStartTime = now;
        }
    }
}
