using System.IO;

namespace Kontur.Recognition.Utils.Concurrent
{
	/// <summary>
	/// Provides access to console in thread-safe manner (all operations on Out and Error streams are
	/// synchronized)
	/// </summary>
	public static class SynchronizedConsole
	{
		private static readonly object lockObject = new object();

		private static readonly SynchronizedWriter outWriter = new SynchronizedWriter(System.Console.Out, lockObject);
		/// <summary>
		/// Use this writer to print text to console (replacement to System.Console.Out)
		/// </summary>
		public static TextWriter Out { get { return outWriter; } }

		private static readonly SynchronizedWriter errWriter = new SynchronizedWriter(System.Console.Error, lockObject);
		/// <summary>
		/// Use this writer to print text to console (replacement to System.Console.Error)
		/// </summary>
		public static TextWriter Error { get { return errWriter; } }
	}
}
