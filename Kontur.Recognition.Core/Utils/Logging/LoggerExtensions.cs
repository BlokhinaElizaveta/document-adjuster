using System;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Logging
{
	public static class LoggerExtensions
	{
		public static void DebugFormat([NotNull] this ILogger logger, [NotNull] string logCategory, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Debug, logCategory, () => string.Format(msgTemplate, args));
		}

		public static void ErrorFormat([NotNull] this ILogger logger, [NotNull] string logCategory, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Error, logCategory, () => string.Format(msgTemplate, args));
		}

		public static void InfoFormat([NotNull] this ILogger logger, [NotNull] string logCategory, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Info, logCategory, () => string.Format(msgTemplate, args));
		}

		public static void DebugMsg([NotNull] this ILogger logger, [NotNull] string logCategory, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Debug, logCategory, message, ex);
		}

		public static void ErrorMsg([NotNull] this ILogger logger, [NotNull] string logCategory, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Error, logCategory, message, ex);
		}

		public static void InfoMsg([NotNull] this ILogger logger, [NotNull] string logCategory, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Info, logCategory, message, ex);
		}


		public static void DebugFormat<TType>([NotNull] this ILogger logger, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Debug, typeof(TType).FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}

		public static void ErrorFormat<TType>([NotNull] this ILogger logger, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Error, typeof(TType).FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}

		public static void InfoFormat<TType>([NotNull] this ILogger logger, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Info, typeof(TType).FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}

		public static void DebugFormat([NotNull] this ILogger logger, [NotNull] Type loggingType, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Debug, loggingType.FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}

		public static void ErrorFormat([NotNull] this ILogger logger, [NotNull] Type loggingType, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Error, loggingType.FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}

		public static void InfoFormat([NotNull] this ILogger logger, [NotNull] Type loggingType, [NotNull] string msgTemplate, [NotNull] params object[] args)
		{
			logger.Log(LogLevel.Info, loggingType.FullName, () => string.Format(msgTemplate, args), ExtractException(args));
		}


		public static void DebugMsg<TType>([NotNull] this ILogger logger, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Debug, typeof(TType).FullName, message, ex);
		}

		public static void ErrorMsg<TType>([NotNull] this ILogger logger, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Error, typeof(TType).FullName, message, ex);
		}

		public static void InfoMsg<TType>([NotNull] this ILogger logger, [NotNull] string logCategory, MessageProducer message, Exception ex = null)
		{
			logger.Log(LogLevel.Info, typeof(TType).FullName, message, ex);
		}

		private static Exception ExtractException(IEnumerable<object> args)
		{
			return args.OfType<Exception>().FirstOrDefault();
		}

	}
}