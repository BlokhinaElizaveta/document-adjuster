using System;
using System.Collections.Generic;

namespace Kontur.Recognition.Utils.Logging
{
	public interface ILoggingModule
	{
		void Start(LogEngine engine, Dictionary<string, string> parameters);
		void Stop();
		void OnLogEvent(LogLevel level, string logCategory, DateTime eventTime, MessageProducer message, Exception exceptionToTrace = null);
	}
}