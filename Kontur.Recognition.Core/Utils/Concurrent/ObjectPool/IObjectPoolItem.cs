using System;

namespace Kontur.Recognition.Utils.Concurrent.ObjectPool
{
	/// <summary>
	/// Generic interface to obtain pooled object and operate with the pool itself about validity of the object
	/// </summary>
	/// <typeparam name="TObject"></typeparam>
	public interface IObjectPoolItem<out TObject> : IDisposable
	{
		/// <summary>
		/// Provides access to the pooled object
		/// </summary>
		TObject Item { get; }

		/// <summary>
		/// Call this method to inform the pool that the object is in broken state and, thus, can not be used futhermore
		/// </summary>
		void MarkAsInvalid();
	}
}