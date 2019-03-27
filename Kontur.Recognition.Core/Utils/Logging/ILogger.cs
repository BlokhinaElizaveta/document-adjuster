using System;

namespace Kontur.Recognition.Utils.Logging
{
	public interface ILogger
	{
		void Log(LogLevel level, string logCategory, MessageProducer message, Exception ex = null);
	}
}