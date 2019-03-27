using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Concurrent
{
	/// <summary>
	/// Contains utility methods to manipulate OS processes
	/// </summary>
	public static class Win32ProcessHelper
	{
		private static readonly Timer timer = new Timer("processKiller");

		[DllImport("user32.dll")]
		private static extern int GetWindowThreadProcessId(int hWnd, out int lpdwProcessId);

		/// <summary>
		/// Returns process id and thread id of the process and the thread which created the window with the specified handle
		/// </summary>
		/// <param name="hWnd">Window handle</param>
		/// <param name="threadId">Id of the thread which created the window</param>
		/// <param name="processId">Id of the process which created the window</param>
		public static void GetWindowThreadAndProcessId(int hWnd, out int threadId, out int processId)
		{
			threadId = GetWindowThreadProcessId(hWnd, out processId);
		}

		/// <summary>
		/// Returns process id of the process which created the window with the specified handle
		/// </summary>
		/// <param name="hWnd">Window handle</param>
		/// <returns>The id of the process which created the window</returns>
		public static int GetWindowProcessId(int hWnd)
		{
			int processId;
			GetWindowThreadProcessId(hWnd, out processId);
			return processId;
		}

		/// <summary>
		/// Requests delayed kill of the process with the given process id after specified time has pasased
		/// </summary>
		/// <param name="processId">The id of the process to kill</param>
		/// <param name="delay">The delay to wait before doing the attempt to kill the process</param>
		public static void ScheduleDelayedProcessKill(int processId, TimeSpan delay)
		{
			timer.ScheduleTask(() =>
			{
				TryKillProcess(processId);
			}, delay, TimeSpan.Zero);
		}

		/// <summary>
		/// Returns the process descriptor by given process id.
		/// </summary>
		/// <param name="processId">The process id</param>
		/// <returns>The process identifier</returns>
		[CanBeNull]
		public static Process GetProcessById(int processId)
		{
			return Process.GetProcesses().FirstOrDefault(p => p.Id == processId);			
		}

		public static bool TryKillProcess(int processId)
		{
			return TryKillProcess(GetProcessById(processId));
		}

		public static bool TryStopProcess(int processId)
		{
			return TryStopProcess(GetProcessById(processId));
		}

		/// <summary>
		/// Does an attpemt to kill the specified process. Exceptions are suppressed! 
		/// Returns true on successfull invocation of kill
		/// </summary>
		/// <param name="process"></param>
		/// <returns></returns>
		public static bool TryKillProcess([CanBeNull] Process process)
		{
			if (process != null && !process.HasExited)
			{
				try
				{
					process.Kill();
					return true;
				}
				catch (Win32Exception)
				{
					// Intentionally ignored
					return false;
				}
				catch (InvalidOperationException)
				{
					// Intentionally ignored
					return false;
				}
				catch
				{
					return false;
				}
			}
			return true;
		}

		public static bool TryStopProcess([CanBeNull] Process process)
		{
			if (process != null && !process.HasExited)
			{
				try
				{
					return process.CloseMainWindow();
				}
				catch (Win32Exception)
				{
					// Intentionally ignored
					return false;
				}
				catch (PlatformNotSupportedException)
				{
					// Intentionally ignored
					return false;
				}
				catch (InvalidOperationException)
				{
					// Intentionally ignored
					return false;
				}
				catch
				{
					return false;
				}
			}
			return true;
		}

		


		/// <summary>
		/// Waits for specified amount of time until specified processes terminate 
		/// </summary>
		/// <param name="processNames">The names of the processes to monitor</param>
		/// <param name="timeout">The timeout to wait</param>
		public static void WaitForProcessToExit(IEnumerable<string> processNames, TimeSpan timeout)
		{
			var names = new HashSet<string>(processNames.Select(s => s.ToLowerInvariant()));
			var processes = Process.GetProcesses().Where(p =>
			                                             {
				                                             var fileName = "";
				                                             try
				                                             {
					                                             var mainModule = p.MainModule;
																 // MainModule CAN be null
					                                             // ReSharper disable once ConditionIsAlwaysTrueOrFalse
					                                             if (mainModule != null)
					                                             {
																	 fileName = Path.GetFileNameWithoutExtension(mainModule.ModuleName);
																	 fileName = Path.GetFileName(fileName) ?? "";
					                                             }
				                                             }
				                                             catch (Win32Exception)
				                                             {
					                                             // ignored
				                                             }
				                                             catch (InvalidOperationException)
				                                             {
					                                             // ignored
				                                             }
				                                             var processName = p.ProcessName;
				                                             return names.Contains(
					                                             (Path.GetFileName(fileName) ?? "").ToLowerInvariant())
				                                                    || names.Contains(processName.ToLowerInvariant());
			                                             }).ToArray();
			WaitForProcessToExit(processes, timeout);
		}

		private static void WaitForProcessToExit(Process[] processes, TimeSpan timeout)
		{
			var keepWaiting = processes.Any();
			var waitStop = DateTime.UtcNow + timeout;
			while (keepWaiting)
			{
				keepWaiting = processes.Any(p =>
				{
					try
					{
						return !p.WaitForExit(100);
					}
					catch (Win32Exception)
					{
						return false;
					}
					catch (SystemException)
					{
						return false;
					}
				});
				if (keepWaiting && (DateTime.UtcNow >= waitStop))
				{
					throw new TimeoutException();
				}
			}
		}

	}
}
