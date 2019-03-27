namespace Kontur.Recognition.Processes
{
	/// <summary>
	/// Call back interface to provide a way to setup reaction on what process is printing to the console
	/// </summary>
	public interface IOutputProcessor
	{
		/// <summary>
		/// This method is invoked for each line of standard output. 
		/// </summary>
		/// <param name="line">The line being printed</param>
		/// <returns>true if this line is processed and should be removed from process's output</returns>
		bool OnOutputLinePrinted(string line);
		/// <summary>
		/// This method is invoked for each line of error output. 
		/// </summary>
		/// <param name="line">The line being printed</param>
		/// <returns>true if this line is processed and should be removed from process's output</returns>
		bool OnErrorLinePrinted(string line);
	}
}