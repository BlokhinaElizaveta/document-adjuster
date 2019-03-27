using System;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Ref
{
	/// <summary>
	/// Holds reference to some disposable object and performs reference counting (counts the number of active handles).
	/// When the number of active handles reaches zero, the referenced object will be automatically disposed
	/// by invocation of appropriate disposer. The link to the referenced object will be destroyed as well thus 
	/// making that object available for garbage collection.
	/// </summary>
	public sealed class SharedDisposableObject <TType> where TType : class
	{
		/// <summary>
		/// The referenced object
		/// </summary>
		private TType referent;
    
		/// <summary>
		/// The disposer for referenced object
		/// </summary>
		private readonly IDisposer<TType> disposer;
    
		/// <summary>
		/// The number of active handles to this object
		/// </summary>
		private int refCount;

		/// <summary>
		/// Creates new instance of reference counting wrapper for given object.
		/// To end the lifecycle of the object the given disposer will be used.
		/// This class should not be used directly, only via SharedHandle instances
		/// </summary>
		/// <param name="obj">The object to wrap</param>
		/// <param name="disposer">The disposer to use for the object</param>
		internal SharedDisposableObject(TType obj, IDisposer<TType> disposer)
		{
			referent = obj;
			this.disposer = disposer;
			refCount = 0;
		}
    
		/// <summary>
		/// Increases reference counter. Must not be used outside of SharedHandle or SharedDisposableObject.
		/// </summary>
		internal void use()
		{
			lock(this)
			{
				CheckAccess();
				refCount++;
			}
		}

		/// <summary>
		/// Decreases reference counter. Must not be used outside of SharedHandle or SharedDisposableObject.  
		/// </summary>
		internal void unuse(bool callFromFinalizer)
		{
			TType objectToDestroy = null;
			lock (this)
			{
				CheckAccess();
				refCount--;
				if (refCount <= 0)
				{
					// It means, that now object is being destroyed and access should be locked                
					refCount = -1;
					// Keep reference to call destructor outside of synchronization block
					// thus avoiding possible deadlocks
					objectToDestroy = referent;
					referent = null;
				}
			}
			// If we are here with intention to dispose the controlled object
			// and the call is made from the finalizer then it means that it was the last
			// reference to the controlled object, so it is also marked for garbage collection
			// and we have no guarantee that it has not been garbage collected already.
			// So we have to ignore disposal in such cases (the controlled object itself
			// has to provide correct logic to release resources when being finalized)
			if ((objectToDestroy != null) && !callFromFinalizer)
			{
				try
				{
					disposer.Dispose(objectToDestroy);
				}
				catch (Exception ex)
				{
					Log.DebugFormat(GetType(), "Exception during object disposal: {0}", ex.Message);
				}
			}
		}
    
		/// <summary>
		/// Validates the state of this reference counter to prevent access 
		/// to disposed referenced object
		/// </summary>
		private void CheckAccess()
		{
			if (refCount < 0)
			{
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Returns new weak handle to this shared object
		/// </summary>
		/// <returns></returns>
		public WeakHandle<TType> NewWeakHandle()
		{
			return new WeakHandle<TType>(this);
		}

		/// <summary>
		/// Returns new handle to this shared object provided that it has not been disposed yet
		/// </summary>
		/// <returns></returns>
		public SharedHandle<TType> NewHandle()
		{
			lock (this)
			{
				// It is allowed to produce new handles only while the object is not disposed
				if (refCount > 0)
				{
					return new SharedHandle<TType>(this);
				}
			}
			return null;
		}

		/// <summary>
		/// Returns the reference to the wrapped object
		/// </summary>
		/// <returns></returns>
		[CanBeNull]
		internal TType GetReferent()
		{
			return referent;
		}
	}
}
