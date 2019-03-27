using System;
using System.Collections.Concurrent;
using System.Threading;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Concurrent.ObjectPool
{
	public class SingleThreadObjectFactory<TObject> : IDisposable where TObject : class
	{
		private readonly Func<TObject> producer;
		private readonly Action<TObject> disposer;

		// ReSharper disable once StaticMemberInGenericType
		private static readonly TimeSpan defaultObjectCreationTimeoutAfterSuccess = TimeSpan.FromMilliseconds(100);
		// ReSharper disable once StaticMemberInGenericType
		private static readonly TimeSpan defaultObjectCreationTimeoutAfterFailure = TimeSpan.FromMilliseconds(10);
		private volatile TimeSpanReference objectCreationTimeout = new TimeSpanReference(defaultObjectCreationTimeoutAfterSuccess);
		// ReSharper disable once StaticMemberInGenericType
		public static readonly TimeSpan MaxObjectCreationTimeout = defaultObjectCreationTimeoutAfterSuccess;

		// The thread dedicated to perform object creation/disposal operations
		private readonly Thread serviceThread;

		// This lock object controls checks of whether service thread can enter waiting state
		private readonly object serviceThreadWaitMonitor = new object();

		private readonly ConcurrentQueue<Action> tasksQueue = new ConcurrentQueue<Action>();

		private volatile bool isActive = true;

		public TimeSpan ObjectCreationTimeout
		{
			get
			{
				var local = objectCreationTimeout;
				return local.Value;
			}
		}

		public SingleThreadObjectFactory(Func<TObject> producer, Action<TObject> disposer)
		{
			this.producer = producer;
			this.disposer = disposer;
			serviceThread = new Thread(ServiceThreadBody) { IsBackground = true };
			serviceThread.Start();
		}

		~SingleThreadObjectFactory()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			StopWorkerThread();
		}

		public void Stop(TimeSpan terminationTimeout)
		{
			StopWorkerThread();
			serviceThread.Join(terminationTimeout);
		}

		private void StopWorkerThread()
		{
			lock (serviceThreadWaitMonitor)
			{
				isActive = false;
				System.Threading.Monitor.Pulse(serviceThreadWaitMonitor);
			}
		}

		private void ServiceThreadBody()
		{
			while (true)
			{
				Action action;
				while (tasksQueue.TryDequeue(out action))
				{
					action();
				}
				lock (serviceThreadWaitMonitor)
				{
					if (tasksQueue.IsEmpty)
					{
						if (!isActive)
						{
							break;
						}
						System.Threading.Monitor.Wait(serviceThreadWaitMonitor, 30000);
					}
				}
			}
		}

		/// <summary>
		/// Requests to create new object. As soon as the object is created, consumedObjectAction will be invoked.
		/// If consumeObjectAction returns false, it means that the object has not been consumed, so the object will be scheduled for disposal.
		/// If an exception occurred while the new object was being created the exception will be passed co consumeObjectAction.
		/// </summary>
		/// <param name="consumeObjectAction">Callback function to consume the newly created object. Must return true if the object has been taken into use. 
		/// Otherwise the object will be scheduled for disposal</param>
		public void CreateNewObject(Func<TObject, Exception, bool> consumeObjectAction)
		{
			lock (serviceThreadWaitMonitor)
			{
				tasksQueue.Enqueue(() => DoCreateNewObjectInternal(consumeObjectAction));
				System.Threading.Monitor.Pulse(serviceThreadWaitMonitor);
			}
		}

		public void DisposeObject([NotNull] TObject pooledObject)
		{
			lock (serviceThreadWaitMonitor)
			{
				tasksQueue.Enqueue(() => DoDisposeObjectInternal(pooledObject));
				System.Threading.Monitor.Pulse(serviceThreadWaitMonitor);
			}
		}

		private void DoCreateNewObjectInternal(Func<TObject, Exception, bool> consumeObjectAction)
		{
			TObject newObject;
			Exception createObjectException = null; 
			try
			{
				newObject = producer();
				objectCreationTimeout = new TimeSpanReference(defaultObjectCreationTimeoutAfterSuccess);
			}
			catch (Exception ex)
			{
				newObject = null;
				createObjectException = ex;
				objectCreationTimeout = new TimeSpanReference(defaultObjectCreationTimeoutAfterFailure);
			}

			bool objectConsumed;
			try
			{
				objectConsumed = consumeObjectAction(newObject, createObjectException);
			}
			catch
			{
				objectConsumed = false;
			}
			if (!objectConsumed && newObject != null)
			{
				// It is safe to access tasks queue without a lock because this method is executed
				// from the service thread itself
				tasksQueue.Enqueue(() => DoDisposeObjectInternal(newObject));
			}
		}

		private void DoDisposeObjectInternal(TObject obj)
		{
			try
			{
				disposer(obj);
			}
			catch (Exception ex)
			{
				// This is a safety measure to prevent exceptions in internal pool code
				// It is expected that disposer must not throw anything
				Log.ErrorFormat(GetType(), "ObjectPool: an uncaught exception in object destruction routine has been detected: {0}", ex);
			}
		}

		private class TimeSpanReference
		{
			private readonly TimeSpan value;

			public TimeSpanReference(TimeSpan timeSpan)
			{
				value = timeSpan;
			}

			public TimeSpan Value
			{
				get { return value; }
			}
		}
	}
}