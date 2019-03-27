using System;
using System.Threading;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;
using Kontur.Recognition.Utils.Structures;

namespace Kontur.Recognition.Utils.Concurrent
{
	public class Timer
	{
		public static readonly Timer DefaultInstance = new Timer("default-timer");

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly Thread timerThread;
		private readonly Heap<TimerTaskDescriptor> tasksHeap = new Heap<TimerTaskDescriptor>(TimerTaskComparer);
		private volatile bool isRunning;
		private static readonly TimeSpan defaultIdleSleepTime = TimeSpan.FromMinutes(5);

		public Timer([CanBeNull] string name = null)
		{
			isRunning = true;
			timerThread = new Thread(Run)
			{
				IsBackground = true,
				Name = string.Format("timer-{0}", name ?? "unnamed")
			};
			timerThread.Start();
		}

		private static int TimerTaskComparer([NotNull]TimerTaskDescriptor task1, [NotNull] TimerTaskDescriptor task2)
		{
			return task2.StartTime.CompareTo(task1.StartTime);
		}

		public void ScheduleTask(TimerTask task, TimeSpan delay, TimeSpan repeatInterval)
		{
			ScheduleTask(new TimerTaskDescriptor(task, DateTime.UtcNow + delay, repeatInterval));
		}

		public void StartImmediately(TimerTask task, TimeSpan iRepeatInterval)
		{
			ScheduleTask(task, TimeSpan.Zero, iRepeatInterval);
		}

		public void ScheduleTask(Action toExecute, TimeSpan delay, TimeSpan repeatInterval)
		{
			var taskDescriptor = new TimerTaskDescriptor(new TimerActionTask(toExecute), DateTime.UtcNow + delay, repeatInterval);
			ScheduleTask(taskDescriptor);
		}

		private void ScheduleTask(TimerTaskDescriptor task)
		{
			lock (tasksHeap)
			{
				tasksHeap.AddElement(task);
				System.Threading.Monitor.Pulse(tasksHeap);
			}
		}

		public void Cancel()
		{
			lock (tasksHeap)
			{
				isRunning = false;
				System.Threading.Monitor.Pulse(tasksHeap);
			}
		}

		private void Run()
		{
			TimerTaskDescriptor taskToProcess = null;
			while (isRunning)
			{
				TimeSpan delay;
				lock (tasksHeap)
				{
					TimerTaskDescriptor task;
					if (tasksHeap.PollTopElement(out task))
					{
						var taskTime = task.StartTime;
						var currentTime = DateTime.UtcNow;
						if (taskTime <= currentTime)
						{
							taskToProcess = task;
							delay = TimeSpan.Zero;
						}
						else
						{
							tasksHeap.AddElement(task);
							delay = taskTime - currentTime;
						}
					}
					else
					{
						delay = defaultIdleSleepTime;
					}
				}
				if (taskToProcess != null)
				{
					try
					{
						taskToProcess.DoAction();
					}
					catch (Exception ex)
					{
						Log.ErrorFormat(GetType(), "The processing of the scheduled task has failed: {0}", ex);
					}
					if (taskToProcess.RepeatInterval > TimeSpan.Zero && !taskToProcess.IsCancelled())
					{
						ReScheduleTask(taskToProcess);
					}
					taskToProcess = null;
				}
				if (delay <= TimeSpan.Zero) continue;
				lock (tasksHeap)
				{
					if (isRunning)
					{
						try
						{
							System.Threading.Monitor.Wait(tasksHeap, delay);
						}
						catch (ThreadInterruptedException)
						{
							// Thread interrupt is just ignored
						}
					}
				}
			}
		}

		private void ReScheduleTask(TimerTaskDescriptor taskToProcess)
		{
			var when = taskToProcess.StartTime + taskToProcess.RepeatInterval;
			ScheduleTask(new TimerTaskDescriptor(taskToProcess.Task, when, taskToProcess.RepeatInterval));
		}

		private class TimerTaskDescriptor
		{
			private readonly DateTime startTime;
			private readonly TimeSpan repeatInterval;

			[NotNull]
			private readonly TimerTask task;

			public DateTime StartTime
			{
				get { return startTime; }
			}

			public TimeSpan RepeatInterval { get { return repeatInterval; } }

			public TimerTask Task { get { return task; } }

			public TimerTaskDescriptor([NotNull] TimerTask task, DateTime startTime, TimeSpan repeatInterval)
			{
				this.startTime = startTime;
				this.repeatInterval = repeatInterval;
				this.task = task;
			}

			public void DoAction()
			{
				task.DoAction();
			}

			public bool IsCancelled()
			{
				return task.IsCancelled;
			}
		}

		public abstract class TimerTask
		{
			private volatile bool isCancelled;

			public bool IsCancelled { get { return isCancelled; } }

			protected void CancelTask()
			{
				isCancelled = true;
			}

			public abstract void DoAction();
		}

		private class TimerActionTask : TimerTask
		{
			private readonly Action act;

			public TimerActionTask(Action action)
			{
				act = action;
			}

			public override void DoAction()
			{
				act();
			}
		}
	}
}
