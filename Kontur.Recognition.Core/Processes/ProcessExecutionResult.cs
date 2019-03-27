using System;
using Kontur.Recognition.Utils;

namespace Kontur.Recognition.Processes
{
	public class ProcessExecutionResult : ProcessExecutionResult<int>
	{
		public ProcessExecutionResult(int exitCode, string output, string error)
			: base(exitCode, output, error)
		{
		}

		public ProcessExecutionResult<TExitCodeType> AsTypedResult<TExitCodeType>() where TExitCodeType : struct, IConvertible
		{
			TExitCodeType typedExitCode;
			if (EnumUtils.TryGetEnumValue(ExitCode, out typedExitCode))
			{
				return new ProcessExecutionResult<TExitCodeType>(this, typedExitCode);
			}
			return null;
		}
	}
	
	public class ProcessExecutionResult<TExitCodeType>
	{
		public ProcessExecutionResult(ProcessExecutionResult untypedResult, TExitCodeType exitCode)
		{
			ExitCode = exitCode;
			Output = untypedResult.Output;
			Error = untypedResult.Error;
		}

		public ProcessExecutionResult(TExitCodeType exitCode, string output, string error)
		{
			ExitCode = exitCode;
			Output = output;
			Error = error;
		}

		public TExitCodeType ExitCode { get; private set; }
		public string Output { get; private set; }
		public string Error { get; private set; }
	}

}