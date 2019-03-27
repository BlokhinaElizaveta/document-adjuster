using System;
using System.Diagnostics;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Ref
{

	/// <summary>
	/// Implements smart handle to control disposal of disposable object in concurrent environment
	/// </summary>
	public static class SharedHandle
	{
		/// <summary>
		/// Produces new handle to specified object using provided disposer to control lifecycle of the object
		/// </summary>
		/// <typeparam name="TRefType">The type of referenced object</typeparam>
		/// <param name="obj">The referenced object</param>
		/// <param name="disposer">The disposer to use</param>
		/// <returns></returns>
		[NotNull]
		public static SharedHandle<TRefType> NewHandle<TRefType>(TRefType obj, Action<TRefType> disposer) where TRefType : class
		{
			return new SharedHandle<TRefType>(new SharedDisposableObject<TRefType>(obj, new ActionDisposer<TRefType>(disposer)));
		}

		/// <summary>
		/// Produces new handle to specified object using provided disposer to control lifecycle of the object
		/// </summary>
		/// <typeparam name="TRefType">The type of referenced object</typeparam>
		/// <param name="obj">The referenced object</param>
		/// <param name="disposer">The disposer to use</param>
		/// <returns></returns>
		[NotNull]
		public static SharedHandle<TRefType> NewHandle<TRefType>(TRefType obj, IDisposer<TRefType> disposer) where TRefType : class
		{
			return new SharedHandle<TRefType>(new SharedDisposableObject<TRefType>(obj, disposer));
		}

		/// <summary>
		/// Produces new handle to specified disposable object.
		/// </summary>
		/// <typeparam name="TRefType">The type of referenced object</typeparam>
		/// <param name="obj">The referenced object</param>
		/// <returns></returns>
		[NotNull]
		public static SharedHandle<TRefType> NewHandle<TRefType>(TRefType obj) where TRefType : class, IDisposable
		{
			return new SharedHandle<TRefType>(new SharedDisposableObject<TRefType>(obj, new DefaultDisposer<TRefType>()));
		}

		/// <summary>
		/// Adapter to invoke standard IDisposable implementation via IDisposer interface
		/// </summary>
		/// <typeparam name="TRelatedType"></typeparam>
		private class DefaultDisposer<TRelatedType> : IDisposer<TRelatedType> where TRelatedType : IDisposable
		{
			public void Dispose(TRelatedType obj)
			{
				obj.Dispose();
			}
		}

		/// <summary>
		/// Adapter to invoke action via IDisposer interface
		/// </summary>
		/// <typeparam name="TRelatedType"></typeparam>
		private class ActionDisposer<TRelatedType> : IDisposer<TRelatedType>
		{
			private readonly Action<TRelatedType> action;

			internal ActionDisposer(Action<TRelatedType> action)
			{
				this.action = action;
			}

			public void Dispose(TRelatedType obj)
			{
				action(obj);
			}
		}
	}

	/// <summary>
	/// Handle to access shared object (i.e. object with distributed ownership).
	/// The handle references shared reference counter thus providing a way to coordinate object disposal 
	/// in concurrent environment. <br/>
	/// 
	/// To duplicate handles use method Duplicate() or Clone(). <br/>
	/// 
	/// General contract of this class usage is the following: <br/>
	/// 
	/// 1. If needed, this class can be supclassed to provide specialized constructor and 
	/// accessor method. Still this class can be used as is (as generic one) <br/> 
	///  
	/// 2. To create disposable object with shared ownership create 
	/// first handle to it by using appropriate factory method. <br/>
	/// 
	/// 3. To create subsequent handles use methods Duplicate() or Clone() <br/>
	/// 
	/// 4. To provide correct functioning of handles three rules MUST be fulfilled: <br/>
	/// - each handle must be disposed in the same method where it was created with exception
	/// of the handle returned as method's result; <br/>
	/// - if some method returns handle, then this handle MUST be disposed in invoking method; <br/>
	/// - if some method gets handle as input and want to keep reference to the same object, 
	/// it MUST duplicate handle as according to the rules above input handle will be 
	/// disposed by invoking code.  
	/// </summary>
	public class SharedHandle<TType> : ICloneable, IDisposable where TType : class
	{
		// Flag to enable stack trace for specific generic type
		private static volatile bool enableStackTrace = false;

		/// <summary>
		/// Reference to underlying shared object
		/// </summary>
		private volatile SharedDisposableObject<TType> sharedObject;

		private readonly StackTrace stackTrace;

		internal SharedHandle(SharedDisposableObject<TType> sharedObj)
		{
			var local = sharedObj;
			local.use();
			sharedObject = local;
			stackTrace = enableStackTrace ? new StackTrace(true) : null;
		}

		~SharedHandle()
		{
			if (sharedObject != null)
			{
				// Activate full tracing as we have detected handle leackage
				enableStackTrace = true; 

				// It means that this handle was not properly closed
				Log.ErrorFormat(GetType(), "The handle which has not been disposed is being destructed");
				if (stackTrace != null)
				{
					Log.ErrorFormat(GetType(), "Handle creation point: {0}", stackTrace);
				}
				Dispose(false);
			}
		}

		/// <summary>
		/// Returns new weak handle to target shared object
		/// </summary>
		/// <returns></returns>
		public WeakHandle<TType> GetWeakHandle()
		{
			return GetSharedObject().NewWeakHandle();
		}

		/// <summary>
		/// Disposes this handle releasing underlying handle to target object. If this handle is the last handle to the target object, then 
		/// the target object will be disposed as well. Call to this method is idempotent, i.e. it is safe to call this method multiple times 
		/// (all but the first call will be just ignored).
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes this handle releasing underlying handle to target object. If this handle is the last handle to the target object, then 
		/// the target object will be disposed as well. Call to this method is idempotent, i.e. it is safe to call this method multiple times 
		/// (all but the first call will be just ignored).
		/// <param name="disposing">Value of "false" means that the method is being called from the finalizer</param>
		/// </summary>
		protected virtual void Dispose(bool disposing)
		{
			SharedDisposableObject<TType> local;
			lock (this)
			{
				local = sharedObject;
				if (local != null)
				{
					// Lock access to referenced object
					sharedObject = null;
				}
			}
			if (local != null)
			{
				// Reduce reference counter
				local.unuse(!disposing);
			}
		}

		/// <summary>
		/// Duplicates this handle
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public SharedHandle<TType> Duplicate()
		{
			lock (this)
			{
				var handle = (SharedHandle<TType>)MemberwiseClone();
				// Increment of usage count should be made only in case of valid handle 
				if (handle.sharedObject != null)
				{
					handle.sharedObject.use();
				}
				return handle;
			}
		}

		/// <summary>
		/// Duplicates this handle
		/// </summary>
		/// <returns></returns>
		public object Clone()
		{
			return Duplicate();
		}

		/// <summary>
		/// Internal method to provide access to underlying shared object
		/// </summary>
		/// <returns></returns>
		[NotNull]
		private SharedDisposableObject<TType> GetSharedObject()
		{
			var local = sharedObject;
			if (local != null)
			{
				return local;
			}
			throw new InvalidOperationException("Attempt to use disposed handle has been detected");
		}

		/// <summary>
		/// Returns the reference to target object
		/// </summary>
		/// <returns>Returns the reference to target object</returns>
		[NotNull]
		public TType GetTarget()
		{
			var result = GetSharedObject().GetReferent();
			if (result != null)
			{
				return result;
			}
			throw new InvalidOperationException("Attempt to use disposed handle has been detected");
		}

		/// <summary>
		/// Attempts to return the reference to target object (if it has not been disposed already)
		/// </summary>
		/// <returns>Returns the reference to target object or null if the object has been disposed</returns>
		[CanBeNull]
		public TType TryGetTarget()
		{
			var sharedObj = sharedObject;
			if (sharedObj != null)
			{
				return sharedObj.GetReferent();
			}
			return null;
		}
	}
}
