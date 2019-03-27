using System;

namespace Kontur.Recognition.Utils.Monitor
{
	public interface IProgressMonitor : IDisposable
	{
		/// <summary>
		/// Returns total number of steps to be done. May return 0 if information on progress is not available
		/// </summary>
		int StepsTotal { get; }

		/// <summary>
		/// Returns number of completed steps in range from 0 to StepsTotal.
		/// </summary>
		int StepsDone { get; }
		
		/// <summary>
		/// Returns whether the task is finished. Note: in case when the task gets finished due to error this flag will return
		/// true while the value returned by DonePart is less than 1 (in this case it indicates estimate on what part of the task 
		/// was completed by the time an error has occurred).
		/// </summary>
		bool IsFinished { get; }

		/// <summary>
		/// Returns finished part of the task (number between 0 and 1)
		/// </summary>
		double DonePart { get; }

		/// <summary>
		/// Invoke this method to report on finished step
		/// </summary>
		void StepFinished();

		/// <summary>
		/// Invoke this method to report on finished steps
		/// </summary>
		void StepsFinished(int stepsCount);

		/// <summary>
		/// Invoke this method to increase number of steps to perform
		/// </summary>
		void AddStepsCount(int numberOfSteps);

		/// <summary>
		/// Returns child monitor to serve subtask with specified weight in the whole process
		/// </summary>
		/// <param name="weight">The weigth of subtask in range between 0 and 1 (which part of the whole process this task makes)</param>
		/// <returns></returns>
		IProgressMonitor SubTask(double weight);

		/// <summary>
		/// Returns child monitor to serve subtask for the remainder of the process (equivalent to SubTask(1 - DonePart))
		/// </summary>
		/// <returns></returns>
		IProgressMonitor SubTask();
	}
}