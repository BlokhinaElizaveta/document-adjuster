using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Kontur.Recognition.Utils.Concurrent
{
	/// <summary>
	/// Splits output to several writers
	/// </summary>
	public class MultipleStreamWriter : TextWriter
	{
		private readonly TextWriter[] writers;

		public MultipleStreamWriter(params TextWriter[] writers)
		{
			this.writers = writers;
			if (writers.Any(w => w == null))
				throw new ArgumentException("All writers must be non-null");
		}

		public override void WriteLine(String value)
		{
			foreach (var w in writers)
			{
				w.WriteLine(value);
			}
		}

		public override void WriteLine(String format, Object arg)
		{
			foreach (var w in writers)
			{
				w.WriteLine(format, arg);
			}
		}

		public override void WriteLine(String format, Object arg0, Object arg1)
		{
			foreach (var w in writers)
			{
				w.WriteLine(format, arg0, arg1);
			}
		}

		public override void WriteLine(String format, Object arg0, Object arg1, Object arg2)
		{
			foreach (var w in writers)
			{
				w.WriteLine(format, arg0, arg1, arg2);
			}
		}

		public override void WriteLine(String format, params Object[] arg)
		{
			foreach (var w in writers)
			{
				w.WriteLine(format, arg);
			}
		}

		public override Encoding Encoding
		{
			get { return writers[0].Encoding; }
		}

		public override void Close()
		{
			foreach (var w in writers)
			{
				w.Close();
			}
		}

		protected override void Dispose(bool disposing)
		{
			foreach (var w in writers)
			{
				w.Dispose();
			}
		}

		public override void Flush()
		{
			foreach (var w in writers)
			{
				w.Flush();
			}
		}
	}
}