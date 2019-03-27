using System;
using System.Threading;

namespace Kontur.Recognition.Utils.Ref
{
	/// <summary>
	/// Collection to store multiple disposable objects and then dispose them collectively
	/// </summary>
	public class DisposableFuture<T> : IDisposable where T : class, IDisposable
	{
		/// <summary>
		/// List to store objects
		/// </summary>
		private volatile T storedValue;

		/// <summary>
		/// Wrapped reference
		/// </summary>
		public T Value
		{
			get { return storedValue; }
			set { storedValue = value; }
		}

		/// <summary>
		/// Returns true when this wrapper stores a non-null value
		/// </summary>
		public bool HasValue { get { return storedValue != null; } }

		/// <summary>
		/// Creates a new empty holder
		/// </summary>
		public DisposableFuture()
		{
		}

		/// <summary>
		/// Create a new holder with a specified stored value.
		/// </summary>
		/// <param name="value">The value to put into the holder</param>
		public DisposableFuture(T value)
		{
			storedValue = value;
		}

		public void Dispose()
		{
			// ReSharper disable once CSharpWarnings::CS0420
			var valueToDispose = Interlocked.Exchange(ref storedValue, null);
			if (valueToDispose != null)
			{
				valueToDispose.Dispose();
			}
		}
	}
}
