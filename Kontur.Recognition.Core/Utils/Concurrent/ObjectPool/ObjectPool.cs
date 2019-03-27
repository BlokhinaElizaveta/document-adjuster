using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Concurrent.ObjectPool
{
	/// <summary>
	/// Provides factory methods for convenient creation of typed ObjectPool
	/// </summary>
	public static class ObjectPool 
	{
		public static ObjectPool<T> Create<T>(Func<T> producer, Action<T> disposer, TimeSpan objectTtl, int poolSize = int.MaxValue) where T : class
		{
			return new ObjectPool<T>(producer, disposer, objectTtl, poolSize);
		}

		public static ObjectPool<T> Create<T>(Func<T> producer, TimeSpan objectTtl, int poolSize = int.MaxValue) where T : class, IDisposable
		{
			Action<T> disposer = obj => obj.Dispose();
			return new ObjectPool<T>(producer, disposer, objectTtl, poolSize);
		}
	}

	/// <summary>
	/// Implements pooling logic for objects
	/// </summary>
	/// <typeparam name="TObject"></typeparam>
	public class ObjectPool<TObject> : IDisposable where TObject : class
	{
		private readonly TimeSpan objectTtl;
		private readonly int maxPoolSize;

		private readonly object lockObject = new object();

		private readonly Queue<PooledObject> availableObjects;
		private readonly HashSet<PooledObject> pooledObjects;
		private volatile int currentPoolSize;
		private volatile int pendingNewObjectsRequests;

		private volatile bool poolActive;

		private readonly SingleThreadObjectFactory<TObject> objectFactory;
		private volatile Func<TObject, bool> pooledObjectValidator;

		public int MaxPoolSize { get { return maxPoolSize;} }

		public int CurrentPoolSize { get { return currentPoolSize;} }
		
		private ObjectPool([CanBeNull] Func<TObject> producer, [CanBeNull] Action<TObject> disposer, int maxPoolSize, TimeSpan objectTtl)
		{
			if (maxPoolSize < 1)
			{
				throw new InvalidOperationException("Pool size must be at least 1");
			}

			this.objectTtl = objectTtl;
			this.maxPoolSize = maxPoolSize;
			poolActive = true;
			availableObjects = new Queue<PooledObject>();
			pooledObjects = new HashSet<PooledObject>();
			currentPoolSize = 0;
			SetPooledObjectValidator(null);
			objectFactory = new SingleThreadObjectFactory<TObject>(producer ?? CreateObject, disposer ?? DisposeObject);
		}

		/// <summary>
		/// Inializes object pool with provided object creation and object destruction logic which is provided via delegates
		/// </summary>
		/// <param name="producer">Function to produce new object when the pool decides to add one</param>
		/// <param name="disposer">Action to dispose an object when the pool decides to get rid of it</param>
		/// <param name="objectTtl">How long object may remain in the pool before renewal</param>
		/// <param name="maxPoolSize">Maximal number of objects in the pool</param>
		public ObjectPool([NotNull] Func<TObject> producer, [CanBeNull] Action<TObject> disposer, TimeSpan objectTtl, int maxPoolSize = Int32.MaxValue)
			:this(producer, disposer, maxPoolSize, objectTtl)
		{
		}

		/// <summary>
		/// Constructor to use in subclasses which provide object creation/destruction logic via overriden methods
		/// </summary>
		/// <param name="objectTtl">How long object may remain in the pool before renewal</param>
		/// <param name="maxPoolSize">Maximal number of objects in the pool</param>
		protected ObjectPool(TimeSpan objectTtl, int maxPoolSize = Int32.MaxValue)
			: this(null, null, maxPoolSize, objectTtl)
		{
		}

		~ObjectPool()
		{
			Dispose(false);
		}

		public void SetPooledObjectValidator([CanBeNull] Func<TObject, bool> newPooledObjectValidator)
		{
			if (newPooledObjectValidator == null)
			{
				newPooledObjectValidator = obj => true;
			}
			pooledObjectValidator = newPooledObjectValidator;
		}

		protected virtual TObject CreateObject()
		{
			throw new InvalidOperationException("Override this method to provide object creation logic");
		}

		protected virtual void DisposeObject(TObject obj)
		{
			// Override this method to provide object destruction logic
			// By default does nothing
			var asDisposable = obj as IDisposable;
			if (asDisposable != null)
			{
				asDisposable.Dispose();
			}
		}

		/// <summary>
		/// Dosposes the pool and validates that pooled objects are not in use
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Disposes the pool and validates that pooled objects are not in use
		/// </summary>
		public virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				StopPool(true);
			}
		}

		/// <summary>
		/// Stops the pool and disposes all pooled objects regardless of whether they are in use or not
		/// </summary>
		public void ForcePoolStop()
		{
			StopPool(false);
		}

		private void StopPool(bool checkThatNotUsed)
		{
			List<PooledObject> objectsToDispose = null;

			lock (lockObject)
			{
				if (!poolActive)
				{
					return;
				}

				if (checkThatNotUsed && (availableObjects.Count < pooledObjects.Count))
				{
					// It means some objects are not returned to the pool
					throw new InvalidOperationException("An attempt to stop the pool in use has been detected");
				}
				poolActive = false;
				availableObjects.Clear();
				objectsToDispose = pooledObjects.ToList();
				pooledObjects.Clear();
				currentPoolSize = pooledObjects.Count;
			}

			foreach (var pooledObject in objectsToDispose)
			{
				pooledObject.Dispose();
			}

			// This informs object factory that it must terminate service thread when all scheduled operations are finished
			objectFactory.Stop(TimeSpan.FromSeconds(30));
		}

		/// <summary>
		/// Requests that the pool dispose all available objects
		/// </summary>
		public void ShrinkPool()
		{
			List<PooledObject> objectsToDispose;

			lock (lockObject)
			{
				if (!poolActive)
				{
					return;
				}

				objectsToDispose = availableObjects.ToList();
				availableObjects.Clear();
				foreach (var pooledObject in objectsToDispose)
				{
					pooledObjects.Remove(pooledObject);
				}
				currentPoolSize = pooledObjects.Count;
			}

			foreach (var pooledObject in objectsToDispose)
			{
				pooledObject.Dispose();
			}
		}

		private void EnsureIsActiveInternal()
		{
			if (!poolActive)
			{
				throw new ObjectDisposedException("Pool object has been already disposed");
			}
		}

		public void MarkAllItemsAsObsolete()
		{
			lock (lockObject)
			{
				EnsureIsActiveInternal();
				foreach (var pooledObject in pooledObjects)
				{
					pooledObject.Invalidate();
				}
			}
		}

		private readonly DateTime infiniteWait = DateTime.MaxValue - SingleThreadObjectFactory<TObject>.MaxObjectCreationTimeout;

		/// <summary>
		/// Makes an attempt to get an item from the pool. If the pool does not contain available items, 
		/// it will try to wait for the new item to become available either because it is freed by other client of the pool
		/// or because of a new item creation. 
		/// </summary>
		/// <param name="timeout">How long it is allowed to wait for an item</param>
		/// <returns></returns>
		public IObjectPoolItem<TObject> TryGetPoolItem(TimeSpan timeout)
		{
			//Console.Out.WriteLine("Pool: object has been requested");
			PooledObject result = null;
			var waitUntilBase = (timeout == TimeSpan.MaxValue) ? infiniteWait : DateTime.UtcNow + timeout;
			var waitUntil = waitUntilBase;
			do
			{
				List<PooledObject> objectsToDispose = null;
				var issueCreateNewObjectAction = false;
				lock (lockObject)
				{
					EnsureIsActiveInternal();

					while (availableObjects.Count > 0)
					{
						var obj = availableObjects.Dequeue();
						if (obj.IsValid())
						{
							result = obj;
							break;
						}
						objectsToDispose = objectsToDispose ?? new List<PooledObject>();
						objectsToDispose.Add(obj);
						pooledObjects.Remove(obj);
						currentPoolSize = pooledObjects.Count;
					}

					if (result == null)
					{
						// No available objects have been found
						// So prepare to make a request to the factory to create a new one
						issueCreateNewObjectAction = currentPoolSize + pendingNewObjectsRequests < maxPoolSize;
						if (issueCreateNewObjectAction)
						{
							pendingNewObjectsRequests++;
						}
					}
				}

				if (issueCreateNewObjectAction)
				{
					// No available objects have been found
					// So make a request to the factory to create a new one, then wait until a new object becomes available
					objectFactory.CreateNewObject(RegisterNewObject);
					waitUntil = waitUntilBase + objectFactory.ObjectCreationTimeout;
				}

				if (objectsToDispose != null)
				{
					foreach (var pooledObject in objectsToDispose)
					{
						pooledObject.Dispose();
						//Console.Out.WriteLine("Pool: object has been disposed (id: {0})", pooledObject.GetHashCode());
					}
				}

				if (result != null)
				{
					if (!pooledObjectValidator(result.Item))
					{
						lock (lockObject)
						{
							pooledObjects.Remove(result);
							currentPoolSize = pooledObjects.Count;
						}
						result.Dispose();
						result = null;
					}
				}
				else
				{
					var waitTimeSpan = waitUntil - DateTime.UtcNow;
					if (waitTimeSpan > TimeSpan.Zero)
					{
						// Then wait until there is an available object
						lock (lockObject)
						{
							if (availableObjects.Count == 0)
							{
								System.Threading.Monitor.Wait(lockObject, waitTimeSpan);
							}
						}
					}
				}
			} while ((result == null) && (DateTime.UtcNow <= waitUntil));

			//Console.Out.WriteLine("Pool: object has been obtained: (id: {0})", result != null ? result.GetHashCode() : 1010101);

			return result != null ? new ObjectPoolItem(result) : null;
		}

		public bool TryDoWithPooledObject(Action<TObject> action, TimeSpan? waitFOrObjectTimeout = null)
		{
			var timeout = waitFOrObjectTimeout ?? TimeSpan.MaxValue;
			using (var obj = TryGetPoolItem(timeout))
			{
				if (obj != null)
				{
					action(obj.Item);
					return true;
				}
			}
			return false;
		}

		public bool TryDoWithPooledObject<TResult>(Func<TObject, TResult> action, out TResult result, TimeSpan? waitFOrObjectTimeout = null)
		{
			var timeout = waitFOrObjectTimeout ?? TimeSpan.MaxValue;
			using (var obj = TryGetPoolItem(timeout))
			{
				if (obj != null)
				{
					result = action(obj.Item);
					return true;
				}
			}
			result = default(TResult);
			return false;
		}

		private bool RegisterNewObject([CanBeNull] TObject newObject, [CanBeNull] Exception objectCreationException)
		{
			try
			{
				lock (lockObject)
				{
					if (!poolActive)
					{
						return false;
					}

					pendingNewObjectsRequests--;
					if (objectCreationException == null)
					{
						var pooledObject = new PooledObject(this, newObject);
						pooledObjects.Add(pooledObject);
						currentPoolSize = pooledObjects.Count;
						availableObjects.Enqueue(pooledObject);
						System.Threading.Monitor.Pulse(lockObject);
						//Console.Out.WriteLine("Pool: new object has been registered: (id: {0})", pooledObject.GetHashCode());
					}
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private void ReleaseObject(PooledObject objectToRelease)
		{
			bool disposeObject = false;
			lock (lockObject)
			{
				if (pooledObjects.Contains(objectToRelease))
				{
					if (objectToRelease.IsValid())
					{
						availableObjects.Enqueue(objectToRelease);
					}
					else
					{
						pooledObjects.Remove(objectToRelease);
						currentPoolSize = pooledObjects.Count;
						disposeObject = true;
					}

					System.Threading.Monitor.Pulse(lockObject);
					//Console.Out.WriteLine("Pool: object has been released: (id: {0})", objectToRelease.GetHashCode());
				}
			}
			if (disposeObject)
			{
				objectToRelease.Dispose();
			}
		}

		private sealed class ObjectPoolItem : IObjectPoolItem<TObject>
		{
			private volatile PooledObject pooledObject;

			public ObjectPoolItem(PooledObject pooledObject)
			{
				this.pooledObject = pooledObject;
			}

			public void Dispose()
			{
				var toRelease = pooledObject;
				toRelease = Interlocked.CompareExchange(ref pooledObject, null, toRelease);
				if (toRelease != null)
				{
					toRelease.Release();
				}
			}

			public TObject Item
			{
				get
				{
					var pooledObjectLocal = pooledObject;
					if (pooledObjectLocal != null)
					{
						return pooledObjectLocal.Item;
					}
					throw new InvalidOperationException("An attempt to access a disposed object");
				}
			}

			public void MarkAsInvalid()
			{
				pooledObject.Invalidate();
			}
		}


		private sealed class PooledObject : IDisposable
		{
			private volatile int isDisposed = 0;
			private readonly DateTime validUntil;
			private volatile bool isValid = true;
			private readonly TObject item;
			private readonly ObjectPool<TObject> pool;

			public TObject Item { get { return item; } }

			public PooledObject(ObjectPool<TObject> pool, TObject item)
			{
				this.pool = pool;
				this.item = item;
				validUntil = DateTime.UtcNow + pool.objectTtl;
			}

			public void Dispose()
			{
				if (Interlocked.CompareExchange(ref isDisposed, 1, 0) == 0)
				{
					pool.objectFactory.DisposeObject(Item);
					isValid = false;
				}
			}

			public void Release()
			{
				pool.ReleaseObject(this);
			}

			public void Invalidate()
			{
				isValid = false;
			}

			public bool IsValid()
			{
				return isValid && DateTime.UtcNow <= validUntil;
			}
		}
	}
}
