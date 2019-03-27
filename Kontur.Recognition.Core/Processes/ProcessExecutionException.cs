using System;

namespace Kontur.Recognition.Processes
{
	public class ProcessExecutionException : Exception
	{
		public ProcessExecutionException(string programName, string programArgs, int exitCode, string output, string error)
		{
			ProgramName = programName;
			ProgramArgs = programArgs;
			ExitCode = exitCode;
			Output = output;
			Error = error;
		}

		public override string Message
		{
			get
			{
				return string.Format("Error code {2} while attempting to execute \"{0}\" with arguments \"{1}\":\n{3}\n{4}", ProgramName, ProgramArgs, ExitCode, Output, Error);
			}
		}

		public int ExitCode { get; private set; }
		public string Output { get; private set; }
		public string Error { get; private set; }
		public string ProgramName { get; private set; }
		public string ProgramArgs { get; private set; }
	}
}