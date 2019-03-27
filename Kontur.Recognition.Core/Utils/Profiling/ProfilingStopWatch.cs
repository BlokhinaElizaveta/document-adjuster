using System;

namespace Kontur.Recognition.Utils.Profiling
{
	public class ProfilingStopWatch
	{
		private DateTime lastTimeMark;

		public ProfilingStopWatch() : this(true)
		{
		}

		public ProfilingStopWatch(bool startImmediately)
		{
			if (startImmediately)
			{
				Start();
			}
		}

		public void Start()
		{
			lastTimeMark = DateTime.UtcNow;
		}

		public TimeSpan NextTimeSpan()
		{
			lock (this)
			{
				var now = DateTime.UtcNow;
				var result = now - lastTimeMark;
				lastTimeMark = now;
				return result;
			}
		}

		public string NextTimeSpanMessage(string messageTemplate, params object[] values)
		{
			var message = string.Format(messageTemplate, values);
			return string.Format("{0} ({1:N3} seconds)", message, NextTimeSpan().TotalSeconds);
		}
	}
}