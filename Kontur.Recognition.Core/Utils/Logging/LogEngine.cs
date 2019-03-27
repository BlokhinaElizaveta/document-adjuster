using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.Utils.Logging
{
	public class LogEngine : ILogEngine
	{
		private readonly HashSet<ILoggingModule> logModules = new HashSet<ILoggingModule>();
		private ILoggingModule[] activeModules = new ILoggingModule[0];

		public void AddLoggingModule(ILoggingModule module)
		{
			lock (logModules)
			{
				if (logModules.Add(module))
				{
					var modules = logModules.ToArray();
					activeModules = modules;
				}
			}
		}

		public void RemoveLoggingModule(ILoggingModule module)
		{
			lock (logModules)
			{
				if (logModules.Remove(module))
				{
					var modules = logModules.ToArray();
					activeModules = modules;
				}
			}
		}

		public void Log(LogLevel level, string logCategory, MessageProducer messageProducer, Exception exceptionToTrace = null)
		{
			var now = DateTime.UtcNow;
			var modules = activeModules;

			string message = null;
			MessageProducer producer =
				() => message ?? (message = messageProducer());

			foreach (var module in modules)
			{
				try
				{
					module.OnLogEvent(level, logCategory, now, producer, exceptionToTrace);
				}
				catch (Exception ex)
				{
					// Prevent failures in external modules
					LogInternalError(module, ex);
				}
			}
		}

		private void LogInternalError(ILoggingModule module, Exception exception)
		{
			Console.Error.WriteLine("An internal problem detected in logger module {0}: {1}", module.GetType().FullName, exception.Message);
		}
	}
}