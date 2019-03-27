using System;

namespace Kontur.Recognition.Utils.Logging
{
	public static class Log
	{
		private static readonly LogEngine engine = new LogEngine();

		public static ILogger Logger()
		{
			return engine;
		}

		public static ILogEngine LogEngine()
		{
			return engine;
		}

		public static void DebugFormat<TLogger>(string message, params object[] args)
		{
			engine.DebugFormat<TLogger>(message, args);
		}

		public static void DebugFormat(Type loggerType, string message, params object[] args)
		{
			engine.DebugFormat(loggerType, message, args);
		}

		public static void ErrorFormat<TLogger>(string message, params object[] args)
		{
			engine.ErrorFormat<TLogger>(message, args);
		}

		public static void ErrorFormat(Type loggerType, string message, params object[] args)
		{
			engine.ErrorFormat(loggerType, message, args);
		}

		public static void InfoFormat<TLogger>(string message, params object[] args)
		{
			engine.InfoFormat<TLogger>(message, args);
		}

		public static void InfoFormat(Type loggerType, string message, params object[] args)
		{
			engine.InfoFormat(loggerType, message, args);
		}

	}
}