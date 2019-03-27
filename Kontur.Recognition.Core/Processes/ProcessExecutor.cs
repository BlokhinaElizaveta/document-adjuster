using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Processes
{
	/// <summary>
	/// Implements logic of execution of external process. Allows to fetch output of the process being run.
	/// </summary>
	public static class ProcessExecutor 
	{
		/// <summary>
		/// Executes specified external executable with specified arguments
		/// </summary>
		/// <param name="path">The executable to execute</param>
		/// <param name="args">The arguments to pass to executable</param>
		/// <param name="outputProcessor">The processor to react on output of the process being run</param>
		/// <returns>Result of execution (content of redirected output and error streams along with exit code)</returns>
		/// <exception cref="System.InvalidOperationException">Thrown in case when no path to execute is given</exception>
		public static ProcessExecutionResult ExecuteProcess(string path, string args,
			[CanBeNull] IOutputProcessor outputProcessor = null)
		{
			return ExecuteProcessWithEnvironment(path, args, Enumerable.Empty<KeyValuePair<string, string>>(), outputProcessor);
		}

		/// <summary>
		/// Executes specified external executable with specified arguments
		/// </summary>
		/// <param name="path">The executable to execute</param>
		/// <param name="args">The arguments to pass to executable</param>
		/// <param name="environment">The dictionary of environment variables which should be set before execution of the process</param>
		/// <param name="outputProcessor">The processor to react on output of the process being run</param>
		/// <returns>Result of execution (content of redirected output and error streams along with exit code)</returns>
		/// <exception cref="System.InvalidOperationException">Thrown in case when no path to execute is given</exception>
		public static ProcessExecutionResult ExecuteProcessWithEnvironment(string path, string args, [CanBeNull] IEnumerable<KeyValuePair<string, string>> environment, [CanBeNull] IOutputProcessor outputProcessor = null)
		{
			try
			{
				if (outputProcessor == null)
				{
					outputProcessor = new OutputProcessorNull();
				}
				var outputLines = new ConcurrentQueue<string>();
				var errorLines = new ConcurrentQueue<string>();

				var startInfo = new ProcessStartInfo
				{
					FileName = path,
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					CreateNoWindow = true,
				};
				if (environment != null)
				{
					foreach (var entry in environment)
					{
						var key = entry.Key;
						var value = entry.Value;
						if (!string.IsNullOrEmpty(key))
						{
							if (!string.IsNullOrEmpty(value))
							{
								startInfo.EnvironmentVariables.Add(key, value);
							}
							else
							{
								startInfo.EnvironmentVariables.Remove(key);
							}
						}
					}
				}

				using (var process = new Process {StartInfo = startInfo})
				{
					process.ErrorDataReceived += (sender, evArgs) =>
					                             {
						                             if (evArgs.Data == null) return;
						                             foreach (var line in evArgs.Data.Split('\r', '\n'))
						                             {
							                             if (!outputProcessor.OnErrorLinePrinted(line))
								                             errorLines.Enqueue(line);
						                             }
					                             };
					process.OutputDataReceived += (sender, evArgs) =>
					                              {
						                              if (evArgs.Data == null) return;
						                              foreach (var line in evArgs.Data.Split('\r', '\n'))
						                              {
							                              if (!outputProcessor.OnOutputLinePrinted(line))
								                              outputLines.Enqueue(line);
						                              }
					                              };

					process.Start();
					process.BeginErrorReadLine();
					process.BeginOutputReadLine();

					process.WaitForExit();
					//				while (!process.WaitForExit(100))
					//				{
					//					Thread.Sleep(50);
					//				}

					var output = new StringBuilder();
					foreach (var outputLine in outputLines)
						output.AppendLine(outputLine).Append('\n');

					var error = new StringBuilder();
					foreach (var errorLine in errorLines)
						error.AppendLine(errorLine).Append('\n');

					return new ProcessExecutionResult(process.ExitCode, output.ToString(), error.ToString());
				}
			}
			catch (Win32Exception ex)
			{
				return new ProcessExecutionResult(9999, "", ex.Message);
			}
		}
	}
}