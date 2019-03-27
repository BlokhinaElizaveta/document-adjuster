namespace Kontur.Recognition.Utils.Monitor
{
	public class ProgressMonitorFake : IProgressMonitor
	{
		public int StepsTotal { get { return 0; } }
		public int StepsDone { get { return 0; } }
		public bool IsFinished { get { return false; } }
		public double DonePart { get { return 0; } }

		public void StepFinished()
		{
			// do nothing
		}

		public void StepsFinished(int stepsCount)
		{
			// do nothing
		}

		public void AddStepsCount(int i)
		{
			// do nothing
		}

		public IProgressMonitor SubTask(double weight)
		{
			return new ProgressMonitorFake();
		}

		public IProgressMonitor SubTask()
		{
			return new ProgressMonitorFake();
		}

		public void Dispose()
		{
			// do nothing
		}
	}
}