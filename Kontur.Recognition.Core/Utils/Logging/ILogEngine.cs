namespace Kontur.Recognition.Utils.Logging
{
	public interface ILogEngine : ILogger
	{
		void AddLoggingModule(ILoggingModule module);
		void RemoveLoggingModule(ILoggingModule module);
	}
}