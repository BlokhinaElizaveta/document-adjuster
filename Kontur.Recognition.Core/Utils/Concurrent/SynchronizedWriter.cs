using System;
using System.IO;
using System.Text;

namespace Kontur.Recognition.Utils.Concurrent
{
	internal class SynchronizedWriter : TextWriter
	{
		private readonly TextWriter wrappedWriter;
		private readonly object lockObject;

		public SynchronizedWriter(TextWriter wrappedWriter, object lockObject)
		{
			this.wrappedWriter = wrappedWriter;
			this.lockObject = lockObject;
		}

		public override Encoding Encoding
		{
			get { return wrappedWriter.Encoding; }
		}

		public override void Close()
		{
			wrappedWriter.Close();
		}

		protected override void Dispose(bool disposing)
		{
			wrappedWriter.Dispose();
		}

		public override void Flush()
		{
			wrappedWriter.Flush();
		}

		public override void Write(char value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}				
		}

		public override void Write(char[] buffer)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(buffer);
			}
		}

		public override void Write(char[] buffer, int index, int count)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(buffer, index, count);
			}
		}

		public override void Write(bool value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(int value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(uint value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(long value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(ulong value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(float value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(double value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(decimal value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(string value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(object value)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(value);
			}
		}

		public override void Write(string format, object arg0)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(format, arg0);
			}
		}

		public override void Write(string format, object arg0, object arg1)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(format, arg0, arg1);
			}
		}

		public override void Write(string format, object arg0, object arg1, object arg2)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(format, arg0, arg1, arg2);
			}
		}

		public override void Write(string format, params object[] arg)
		{
			lock (lockObject)
			{
				wrappedWriter.Write(format, arg);
			}
		}

		public override void WriteLine()
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine();
			}
		}

		public override void WriteLine(char value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(char[] buffer)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(buffer);
			}
		}

		public override void WriteLine(char[] buffer, int index, int count)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(buffer, index, count);
			}
		}

		public override void WriteLine(bool value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(int value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(uint value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(long value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(ulong value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(float value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(double value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(decimal value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(string value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(object value)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(value);
			}
		}

		public override void WriteLine(string format, object arg0)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(format, arg0);
			}
		}

		public override void WriteLine(string format, object arg0, object arg1)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(format, arg0, arg1);
			}
		}

		public override void WriteLine(string format, object arg0, object arg1, object arg2)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(format, arg0, arg1, arg2);
			}
		}

		public override void WriteLine(string format, params object[] arg)
		{
			lock (lockObject)
			{
				wrappedWriter.WriteLine(format, arg);
			}
		}

		public override string NewLine { get { return wrappedWriter.NewLine; }}

		public override IFormatProvider FormatProvider { get {return wrappedWriter.FormatProvider; }}
	}
}