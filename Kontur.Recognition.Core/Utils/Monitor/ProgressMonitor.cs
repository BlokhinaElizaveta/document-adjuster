using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.Utils.Monitor
{
	public class ProgressMonitor : IProgressMonitor
	{
		private volatile int stepsTotal;
		private volatile int stepsDone;
		private volatile bool isFinished;
		private readonly List<SubtaskInfo> subtasks = new List<SubtaskInfo>();
		private readonly Action<IProgressMonitor> onStatusChangeAction;
		private readonly object lockObject = new Object();

		public ProgressMonitor(Action<IProgressMonitor> onStatusChangeAction = null)
		{
			this.onStatusChangeAction = onStatusChangeAction ?? (monitor => { });
		}

		public int StepsTotal { 
			get
			{
				lock (lockObject)
				{
					// In case when subtasks are present we will emulate 100000 steps per the whole task
					return subtasks.Any() ? 100000 : stepsTotal;
				}
			} }

		public int StepsDone
		{
			get
			{
				if (subtasks.Any())
				{
					// In case when subtasks are present we will emulate 100000 steps per the whole task
					return (int)Math.Ceiling(100000 * DonePart);
				}
				return stepsDone;
			}
		}

		public double DonePart
		{
			get
			{
				lock (lockObject)
				{
					var subtaskPart = subtasks.Sum(subtaskInfo => subtaskInfo.Monitor.DonePart*subtaskInfo.Weight);
					return subtaskPart + ((stepsTotal > 0) ? (RemainingWeight * stepsDone / stepsTotal) : 0);
				}
			}
		}

		public bool IsFinished
		{
			get
			{
				lock (lockObject)
				{
					return isFinished && subtasks.All(subtaskInfo => subtaskInfo.Monitor.IsFinished);
				}
			}
		}


		public void StepFinished()
		{
			lock (lockObject)
			{
				stepsDone++;
				if (stepsDone >= stepsTotal)
				{
					stepsDone = stepsTotal;
					isFinished = true;
				}
			}
			PublishStatusChangeEvent();
		}

		public void StepsFinished(int stepsCount)
		{
			lock (lockObject)
			{
				stepsDone += stepsCount;
				if (stepsDone >= stepsTotal)
				{
					stepsDone = stepsTotal;
					isFinished = true;
				}
			}
			PublishStatusChangeEvent();
		}

		public void AddStepsCount(int numberOfSteps)
		{
			lock (lockObject)
			{
				stepsTotal += numberOfSteps;
			}
			PublishStatusChangeEvent();
		}

		/// <summary>
		/// This method MUST be invoked OUTSIDE synchronization context only to avoid deadlocks
		/// </summary>
		private void PublishStatusChangeEvent()
		{
			onStatusChangeAction(this);
		}

		private IProgressMonitor SubTaskImpl(double weight)
		{
			var subtask = new ProgressMonitor(monitor => PublishStatusChangeEvent());
			subtasks.Add(new SubtaskInfo(subtask, weight));
			return subtask;
		}

		public IProgressMonitor SubTask(double weight)
		{
			lock (lockObject)
			{
				if (weight < 0 || weight > RemainingWeight)
				{
					throw new ArgumentException(string.Format("Subtask weight is too large. Maximum allowed weight is {0}", RemainingWeight));
				}
				return SubTaskImpl(weight);
			}
		}

		public IProgressMonitor SubTask()
		{
			lock (lockObject)
			{
				return SubTaskImpl(RemainingWeight);
			}
		}

		private double RemainingWeight
		{
			get
			{
				lock (lockObject)
				{
					return 1 - subtasks.Sum(subtaskInfo => subtaskInfo.Weight);
				}
			}
		}

		public void Dispose()
		{
			lock (this)
			{
				isFinished = true;
				foreach (var subtask in subtasks)
				{
					subtask.Monitor.Dispose();
				}
			}
		}

		class SubtaskInfo
		{
			public IProgressMonitor Monitor { get; private set; }
			public double Weight { get; private set; }

			public SubtaskInfo(IProgressMonitor monitor, double weight)
			{
				Monitor = monitor;
				Weight = weight;
			}
		}

	}
}