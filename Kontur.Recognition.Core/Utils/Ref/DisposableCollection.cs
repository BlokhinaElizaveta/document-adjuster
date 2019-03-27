using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Ref
{
	/// <summary>
	/// Collection to store multiple disposable objects and then dispose them collectively
	/// </summary>
	public sealed class DisposableCollection : IDisposable, IEnumerable<IDisposable>
	{
		/// <summary>
		/// List to store objects
		/// </summary>
		private readonly List<IDisposable> objectsToDispose = new List<IDisposable>();

		/// <summary>
		/// Creates new empty collection of objects which are to be disposed
		/// </summary>
		public DisposableCollection()
		{
		}

		/// <summary>
		/// Create new collection of objects which are to be disposed and populates it with objects returned by given iterator.
		/// </summary>
		/// <param name="objs"></param>
		public DisposableCollection(IEnumerable<IDisposable> objs)
		{
			objectsToDispose.AddRange(objs);
		}

		[CanBeNull]
		private static Exception TryToDispose(IDisposable obj)
		{
			try
			{
				obj.Dispose();
				return null;
			}
			catch (Exception ex)
			{
				return ex;
			}
		}

		public void Dispose()
		{
			var disposalResults = objectsToDispose.Select(obj => new Tuple<IDisposable, Exception>(obj, TryToDispose(obj))).ToList();

			foreach (var obj in disposalResults.Where(tuple => tuple.Item2 == null).Select(tuple => tuple.Item1))
			{
				objectsToDispose.Remove(obj);
			}

			var exception = disposalResults.Where(tuple => tuple.Item2 != null).Select(tuple => tuple.Item2).FirstOrDefault();
			if (exception != null)
				throw exception;
		}

		/// <summary>
		/// Chechs whether given object is scheduled for disposal with this collection
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(IDisposable obj)
		{
			return objectsToDispose.Any(o => (ReferenceEquals(o, obj)));
		}

		/// <summary>
		/// Addes an object to collection of objects which are to be disposed.
		/// </summary>
		/// <param name="obj"></param>
		public void Add(IDisposable obj)
		{
			if (!Contains(obj))
			{
				objectsToDispose.Add(obj);
			}
		}

		/// <summary>
		/// Atomic operation which creates given disposable object and registers it for disposal in this collection. The newly created object is returned as method result.
		/// </summary>
		/// <typeparam name="T">The type of an object being created</typeparam>
		/// <param name="constructor">The constructor to create an object</param>
		/// <returns></returns>
		public T CreateAndScheduleDisposal<T>(Func<T> constructor) where T : class, IDisposable
		{
			var obj = constructor();
			if (obj == null)
				return null;
			try
			{
				Add(obj);
				return obj;
			}
			catch (Exception)
			{
				obj.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Enumerates all the object scheduled for disposal with this collection
		/// </summary>
		/// <returns></returns>
		public IEnumerator<IDisposable> GetEnumerator()
		{
			return objectsToDispose.GetEnumerator();
		}

		/// <summary>
		/// Returns enumerator of objects scheduled for disposal
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
