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
	public class DisposableEnumerable<TElement> : IDisposable, IEnumerable<TElement> where TElement : IDisposable
	{
		/// <summary>
		/// Handle to the list of objects (use of handles allows to control correct disposal of the collection with lazy enumerations)
		/// </summary>
		private readonly SharedHandle<List<TElement>> objectsToDisposeHandle;

		/// <summary>
		/// Returns number of elements in this enumeration
		/// </summary>
		public int Count
		{
			get { return objectsToDisposeHandle.GetTarget().Count; }
		}

		/// <summary>
		/// Creates new empty collection of objects which are to be disposed
		/// </summary>
		public DisposableEnumerable()
		{
			var objectsToDispose = new List<TElement>();
			objectsToDisposeHandle = SharedHandle.NewHandle(objectsToDispose, DisposeElements);
		}

		/// <summary>
		/// Create new collection of objects which are to be disposed and populates it with objects returned by given iterator.
		/// </summary>
		/// <param name="objs"></param>
		public DisposableEnumerable(IEnumerable<TElement> objs)
		{
			var objectsToDispose = new List<TElement>(objs);
			objectsToDisposeHandle = SharedHandle.NewHandle(objectsToDispose, DisposeElements);
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

		private static void DisposeElements(List<TElement> listToDispose)
		{
			var disposalResults = listToDispose.Select(obj => new Tuple<TElement, Exception>(obj, TryToDispose(obj))).ToList();

			foreach (var obj in disposalResults.Where(tuple => tuple.Item2 == null).Select(tuple => tuple.Item1))
			{
				listToDispose.Remove(obj);
			}

			var exception = disposalResults.Where(tuple => tuple.Item2 != null).Select(tuple => tuple.Item2).FirstOrDefault();
			if (exception != null)
				throw exception;
		}

		public void Dispose()
		{
			objectsToDisposeHandle.Dispose();
		}

		/// <summary>
		/// Chechs whether given object is scheduled for disposal with this collection
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public bool Contains(IDisposable obj)
		{
			return objectsToDisposeHandle.GetTarget().Any(o => (ReferenceEquals(o, obj)));
		}

		/// <summary>
		/// Addes an object to collection of objects which are to be disposed.
		/// </summary>
		/// <param name="obj"></param>
		public void Add(TElement obj)
		{
			if (!Contains(obj))
			{
				objectsToDisposeHandle.GetTarget().Add(obj);
			}
		}

		/// <summary>
		/// Atomic operation which creates given disposable object and registers it for disposal in this collection. The newly created object is returned as method result.
		/// </summary>
		/// <typeparam name="T">The type of an object being created</typeparam>
		/// <param name="constructor">The constructor to create an object</param>
		/// <returns></returns>
		public T CreateAndScheduleDisposal<T>(Func<T> constructor) where T : class, TElement
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
		public IEnumerator<TElement> GetEnumerator()
		{
			return GetEnumeratorImpl(objectsToDisposeHandle.Duplicate());
		}

		private static IEnumerator<TElement> GetEnumeratorImpl(SharedHandle<List<TElement>> handle)
		{
			using (handle)
			{
				foreach (var item in handle.GetTarget())
				{
					yield return item;
				}
			}
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
