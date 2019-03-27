using System;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Ref
{
	/// <summary>
	/// Handle to keep weak reference to target disposable object. The handle allows to
	/// reach the object (if possible) while not preventing it from being disposed.
	/// </summary>
	public class WeakHandle<TType> where TType : class
	{
		/// <summary>
		/// Reference to underlying shared object
		/// </summary>
		private readonly WeakReference sharedObject;

		internal WeakHandle(SharedDisposableObject<TType> sharedObj)
		{
			sharedObject = new WeakReference(sharedObj);
		}

		/// <summary>
		/// Tries to produce new shared handle to target object provided that it is not disposed yet.
		/// Returns null if target object was disposed and or finalized
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		public SharedHandle<TType> GetHandle()
		{
			var local = (SharedDisposableObject<TType>)sharedObject.Target;
			if (local != null)
			{
				return local.NewHandle();
			}
			return null;
		}
	}
}
