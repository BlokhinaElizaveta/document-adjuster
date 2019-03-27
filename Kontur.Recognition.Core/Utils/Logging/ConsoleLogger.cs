using System;
using System.Collections.Generic;

namespace Kontur.Recognition.Utils.Logging
{
	public class ConsoleLogger : ILoggingModule
	{
		private LogLevel logLevel;

		public ConsoleLogger(LogLevel level)
		{
			logLevel = level;
		}

		public void Start(LogEngine engine, Dictionary<string, string> parameters)
		{
			OnLogEvent(LogLevel.Critical, "ConsoleLogger", DateTime.UtcNow, () => "Console Logger started.");
		}

		public void Stop()
		{
			OnLogEvent(LogLevel.Critical, "ConsoleLogger", DateTime.UtcNow, () => "Console Logger stopped.");
		}

		public void OnLogEvent(LogLevel level, string logCategory, DateTime eventTime, MessageProducer message, Exception exceptionToTrace = null)
		{
			if (level <= logLevel)
			{
				var messageText = message();
				var prefix = string.Format("{0} ({1}): ", eventTime, logCategory);
				foreach (var line in messageText.Split('\r', '\n'))
				{
					Console.Out.Write(prefix);
					Console.Out.WriteLine(line);
				}
				Console.Out.Flush();
			}
		}
	}
}