using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Structures
{
	public abstract class Heap
	{
		public const HeapOrder OrderTopMax = HeapOrder.OrderTopMax;
		public const HeapOrder OrderTopMin = HeapOrder.OrderTopMin;

		public enum HeapOrder
		{
			OrderTopMax = 1,
			OrderTopMin = -1
		}

		/// <summary>
		/// Returns the number of elements in the heap
		/// </summary>
		public abstract int Count { get; }

		/// <summary>
		/// Checks whether the heap is empty or not
		/// </summary>
		/// <returns></returns>
		public abstract bool IsEmpty();

		/// <summary>
		/// Recalculates position of top element in the heap (useful after changing key of top element)
		/// </summary>
		public abstract void TouchTopElement();

		/// <summary>
		/// Removes all elements from the heap
		/// </summary>
		public abstract void Clear();
	}

	public class Heap<TValue> : Heap
	{
		/// <summary>
		/// Basic vector for keeping data, object number 0 is not used we start counting from 1
		/// </summary>
		[NotNull]
		private readonly List<HeapElementData> heapElements;

		/// <summary>
		/// Comparer to use for element ordering. Maximal element is placed on top.
		/// </summary>
		[NotNull]
		private readonly Comparison<TValue> comparer;

		/// <summary>
		/// Mapping of content (ContentWrapper) of elements to elements
		/// </summary>
		private readonly Dictionary<TValue, HeapElementData> contentToElementMap = new Dictionary<TValue, HeapElementData>();

		/**
		 * Structure to hold element data together with current element index.
		 */
		private class HeapElementData
		{
			internal readonly TValue content;
			internal int index;

			internal HeapElementData(int iIdx, TValue content)
			{
				index = iIdx;
				this.content = content;
			}
		}

		/// <summary>
		/// Enumerates current state of heap
		/// </summary>
		/// <returns></returns>
		public IEnumerator<TValue> GetSnapshot()
		{
			HeapElementData[] heapState;
			lock (this)
			{
				heapState = heapElements.ToArray();
			}
			return heapState.Select(heapElement => heapElement.content).GetEnumerator();
		}

		private static Comparison<TValue> GetComparizon(HeapOrder iOrder)
		{
			if (typeof(IComparable<TValue>).IsAssignableFrom(typeof(TValue)))
			{
				switch (iOrder)
				{
					case HeapOrder.OrderTopMax:
						{
							return CompareByGenericOrderTopMax;
						}
					case HeapOrder.OrderTopMin:
						{
							return CompareByGenericOrderTopMin;
						}
					default:
						{
							throw new ArgumentException("Bad ordering mode");
						}
				}
			}
			if (typeof(IComparable).IsAssignableFrom(typeof(TValue)))
			{
				switch (iOrder)
				{
					case HeapOrder.OrderTopMax:
						{
							return CompareByComparableOrderTopMax;
						}
					case HeapOrder.OrderTopMin:
						{
							return CompareByComparableOrderTopMin;
						}
					default:
						{
							throw new ArgumentException("Bad ordering mode");
						}
				}
			}
			throw new ArgumentException("Type {0} does not implement either IComparable or IComparable<{0}>",
				typeof(TValue).FullName);
		}

		private static int CompareByGenericOrderTopMax(TValue val1, TValue val2)
		{
			return ((IComparable<TValue>)val1).CompareTo(val2);
		}

		private static int CompareByGenericOrderTopMin(TValue val1, TValue val2)
		{
			return ((IComparable<TValue>)val2).CompareTo(val1);
		}

		private static int CompareByComparableOrderTopMax(TValue val1, TValue val2)
		{
			return ((IComparable)val1).CompareTo(val2);
		}

		private static int CompareByComparableOrderTopMin(TValue val1, TValue val2)
		{
			return ((IComparable)val2).CompareTo(val1);
		}

		/// <summary>
		/// Creates new heap of given capacity with ordering defined by given comparer
		/// (maximal element will go to the top of the heap)
		/// </summary>
		/// <param name="comparer">Comparer defining ordering of the heap</param>
		/// <param name="iInitialCapacity">Initial capacity of the heap</param>
		public Heap(IComparer<TValue> comparer, int iInitialCapacity = 32)
		{
			if (iInitialCapacity <= 0)
			{
				iInitialCapacity = 0;
			}
			this.comparer = comparer.Compare;
			heapElements = new List<HeapElementData>(iInitialCapacity + 1) { new HeapElementData(0, default(TValue)) };
		}

		/// <summary>
		/// Creates new heap of given capacity with ordering defined by given comparer
		/// (maximal element will go to the top of the heap)
		/// </summary>
		/// <param name="comparer">Comparer defining ordering of the heap</param>
		/// <param name="iInitialCapacity">Initial capacity of the heap</param>
		public Heap(Comparison<TValue> comparer, int iInitialCapacity = 32)
		{
			if (iInitialCapacity <= 0)
			{
				iInitialCapacity = 0;
			}
			this.comparer = comparer;
			heapElements = new List<HeapElementData>(iInitialCapacity + 1) { new HeapElementData(0, default(TValue)) };
		}

		/// <summary>
		/// Creates new heap of given capacity with ordering defined by elements 
		/// (elements must implement either version of interface IComparable)
		/// </summary>
		/// <param name="heapOrder">Ascending or descending order</param>
		/// <param name="iInitialCapacity">Initial capacity of the heap</param>
		public Heap(HeapOrder heapOrder, int iInitialCapacity = 32)
			: this(GetComparizon(heapOrder), iInitialCapacity)
		{
		}

		/// <summary>
		/// Private accessor to element at given position
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		private TValue GetAtInternal(int index)
		{
			return heapElements[index].content;
		}

		/// <summary>
		/// Private accessor to element at given position
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		private void SetAtInternal(int index, TValue value)
		{
			var data = new HeapElementData(index, value);
			heapElements[index] = data;
			contentToElementMap[value] = data;
		}

		/// <summary>
		/// Private method to append an element
		/// </summary>
		/// <param name="value"></param>
		private void AppendInternal(TValue value)
		{
			var data = new HeapElementData(heapElements.Count, value);
			heapElements.Add(data);
			contentToElementMap[value] = data;
		}

		/// <summary>
		/// Private method to delete element
		/// </summary>
		/// <param name="iIdx"></param>
		private void RemoveAtInternal(int iIdx)
		{
			var data = heapElements[iIdx];
			heapElements.RemoveAt(iIdx);
			contentToElementMap.Remove(data.content);
		}

		/// <summary>
		/// Swaps two elements of heap
		/// </summary>
		/// <param name="i1"></param>
		/// <param name="i2"></param>
		private void SwapIndicesInternal(int i1, int i2)
		{
			lock (this)
			{
				var oTemp1 = heapElements[i1];
				var oTemp2 = heapElements[i2];
				oTemp1.index = i2;
				oTemp2.index = i1;
				heapElements[i2] = oTemp1;
				heapElements[i1] = oTemp2;
			}
		}

		/// <summary>
		/// Checks whether the heap is empty or not
		/// </summary>
		/// <returns></returns>
		public override bool IsEmpty()
		{
			return (heapElements.Count <= 1);
		}

		/// <summary>
		/// Restores heap order by moving element at given position 
		/// to the top of the heap  
		/// </summary>
		/// <param name="index"></param>
		private void FlowUpInternal(int index)
		{
			while ((index > 1)
				&& comparer(GetAtInternal(index), GetAtInternal(index / 2)) > 0)
			{
				SwapIndicesInternal(index, index / 2);
				index /= 2;
			}
		}

		/// <summary>
		/// Restores heap order by moving element at given position 
		/// to the base of the heap 
		/// </summary>
		/// <param name="index"></param>
		private void FlowDownInternal(int index)
		{
			lock (this)
			{
				var bSwapped = true;
				var iHeapSize = heapElements.Count;
				while (bSwapped)
				{
					bSwapped = false;
					var iMaxIdx = index;
					var iChildIdx = 2 * index; // First child index (if any)
					if ((iChildIdx < iHeapSize)
						&& comparer(GetAtInternal(iMaxIdx), GetAtInternal(iChildIdx)) < 0)
					{
						iMaxIdx = iChildIdx;
					}
					iChildIdx++; // Second child index (if any) 
					if ((iChildIdx < iHeapSize)
						&& comparer(GetAtInternal(iMaxIdx), GetAtInternal(iChildIdx)) < 0)
					{
						iMaxIdx = iChildIdx;
					}
					if (index != iMaxIdx)
					{
						SwapIndicesInternal(index, iMaxIdx);
						bSwapped = true;
						index = iMaxIdx; // next is this son!
					}
				}
			}
		} // Flowdown

		/// <summary>
		/// Recalculates position of top element in the heap (useful after changing key of top element)
		/// </summary>
		public override void TouchTopElement()
		{
			lock (this)
			{
				FlowDownInternal(1);
			}
		}

		/// <summary>
		/// Recalculates position of given element in the heap (useful after changing key of the element)
		/// </summary>
		public void TouchElement(TValue element)
		{
			lock (this)
			{
				var data = FindElementInternal(element);
				if (data != null)
				{
					FlowUpInternal(data.index);
					FlowDownInternal(data.index);
				}
			}
		}

		/// <summary>
		/// Adds element to the heap and normalizes it
		/// </summary>
		/// <param name="element"></param>
		public void AddElement(TValue element)
		{
			lock (this)
			{
				AppendInternal(element);
				FlowUpInternal(heapElements.Count - 1);
			}
		}

		/// <summary>
		/// Adds element to the heap and normalizes it
		/// </summary>
		/// <param name="elements">Elements to add</param>
		public void AddAll(IEnumerable<TValue> elements)
		{
			lock (this)
			{
				foreach (var element in elements)
				{
					AppendInternal(element);
					FlowUpInternal(heapElements.Count - 1);
				}
			}
		}

		/// <summary>
		/// Removes all elements from the heap
		/// </summary>
		public override void Clear()
		{
			lock (this)
			{
				heapElements.Clear();
				contentToElementMap.Clear();
				heapElements.Add(new HeapElementData(0, default(TValue))); // spare element 
			}
		}

		/// <summary>
		/// Removes element at given position of the heap
		/// </summary>
		/// <param name="iPos"></param>
		/// <returns></returns>
		private TValue RemoveElementAtInternal(int iPos)
		{
			if ((heapElements.Count <= iPos) || (iPos == 0))
			{
				throw new IndexOutOfRangeException();
			}

			var result = GetAtInternal(iPos);
			SwapIndicesInternal(iPos, heapElements.Count - 1);
			RemoveAtInternal(heapElements.Count - 1);

			FlowDownInternal(iPos);

			return result;
		}

		/// <summary>
		/// Changes content of the element specified by given content
		/// </summary>
		/// <param name="currentElement">The element to locate</param>
		/// <param name="newElement">New content for element</param>
		public void UpdateElement(TValue currentElement, TValue newElement)
		{
			lock (this)
			{
				var data = FindElementInternal(currentElement);
				SetAtInternal(data.index, newElement);
				FlowDownInternal(data.index);
				FlowUpInternal(data.index);
			}
		}

		private HeapElementData FindElementInternal(TValue content)
		{
			HeapElementData result;
			if (contentToElementMap.TryGetValue(content, out result))
			{
				return result;
			}
			return null;
		}

		/// <summary>
		/// removes element by searching it in the heap
		/// </summary>
		/// <param name="element">The element to remove</param>
		/// <returns>True if element was found and removed, false otherwise</returns>
		public bool RemoveElement(TValue element)
		{
			lock (this)
			{
				var data = FindElementInternal(element);
				if (data != null)
				{
					RemoveElementAtInternal(data.index);
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// If the heap is not empty returns top element of the heap and element gets removed from the heap. If the heap is empty then default value for TValue type is returned.
		/// (Top element is the maximal element in order induced by comparer)
		/// </summary>
		/// <returns></returns>
		public TValue PollTopElement()
		{
			TValue result;
			PollTopElement(out result);
			return result;
		}

		/// <summary>
		/// If the heap is not empty returns top element of the heap and element gets removed from the heap. If the heap is empty then default value for TValue type is returned.
		/// (Top element is the maximal element in an order induced by comparer)
		/// </summary>
		/// <returns></returns>
		public bool PollTopElement(out TValue result)
		{
			return PollTopElementIf(null, out result);
		}

		/// <summary>
		/// If the heap is not empty checks whether given condition holds fot the top element of the heap.
		/// If the top element satisfies the condition, it gets removed from the heap and is returned.
		/// If the heap is empty or condition is not satisfied then default value for TValue type is returned.
		/// (Top element is the maximal element in an order induced by comparer)
		/// </summary>
		/// <param name="condition">The condition to check. The condition is executed in synchronized context so it must be lock free.</param>
		/// <param name="result">The top element of the heap of default value</param>
		/// <returns></returns>
		public bool PollTopElementIf([CanBeNull] Func<TValue, bool> condition, out TValue result)
		{
			lock (this)
			{
				if (heapElements.Count > 1 && (condition == null || condition(GetAtInternal(1))))
				{
					result = RemoveElementAtInternal(1);
					return true;
				}
				result = default(TValue);
				return false;
			}
		}

		/// <summary>
		/// If the heap is not empty returns top element of the heap and element stays in the heap. 
		/// If the heap is empty then default value for TValue type is returned.
		/// (Top element is the maximal element in order induced by comparer)
		/// </summary>
		/// <returns></returns>
		public bool PeekTopElement(out TValue result)
		{
			lock (this)
			{
				if (heapElements.Count > 1)
				{
					result = GetAtInternal(1);
					return true;
				}
				result = default(TValue);
				return false;
			}
		}

		/// <summary>
		/// Returns the number of elements in the heap
		/// </summary>
		public override int Count
		{
			get { return heapElements.Count - 1; }
		}

		/// <summary>
		/// Checks whether heap contains given element.
		/// </summary>
		/// <param name="element"></param>
		/// <returns></returns>
		public bool Contains(TValue element)
		{
			return contentToElementMap.ContainsKey(element);
		}
	}
}
