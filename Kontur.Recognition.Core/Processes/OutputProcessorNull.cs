namespace Kontur.Recognition.Processes
{
	public class OutputProcessorNull : IOutputProcessor
	{
		public bool OnOutputLinePrinted(string line)
		{
			// Does nothing. Just informs that the line was not processed
			return false; 
		}

		public bool OnErrorLinePrinted(string line)
		{
			// Does nothing. Just informs that the line was not processed
			return false;
		}
	}
}