using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// The structure to represent sparse vector (only non-zero components are stored)
	/// </summary>
	public class SparseVector
	{
		// TODO: надо заменить словарь на более эффективную структуру, позволяющую
		// обновлять данные при итерировании.
		private readonly Dictionary<int, double> values = new Dictionary<int, double>();

		/// <summary>
		/// Returns a component value by specific component index
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public double Get(int index)
		{
			double result;
			if (values.TryGetValue(index, out result))
				return result;
			return 0;
		}

		/// <summary>
		/// Sets a component value by component index
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void Set(int index, double value)
		{
			if (values.ContainsKey(index))
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (value != 0)
				{
					values[index] = value;
				}
				else
				{
					values.Remove(index);
				}
			}
			else
			{
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (value != 0)
				{
					values.Add(index, value);
				}
			}
		}

		/// <summary>
		/// Multiplies the vector by given factor
		/// </summary>
		/// <param name="value"></param>
		public void MultiplyBy(double value)
		{
			foreach (var entry in values.ToList())
			{
				values[entry.Key] = entry.Value * value;
			}
		}

		/// <summary>
		/// The length of the vector
		/// </summary>
		/// <returns></returns>
		public double Length()
		{
			double length = 0;
			foreach (var entry in values)
			{
				length += entry.Value * entry.Value;
			}

			return Math.Sqrt(length);
		}

		/// <summary>
		/// Normalizes the vector (makes it be of  length 1)
		/// </summary>
		public void Normalize()
		{
			MultiplyBy(1 / Length());
		}

		/// <summary>
		/// Duplicates the current state of the vector
		/// </summary>
		/// <returns></returns>
		public SparseVector Duplicate()
		{
			var result = new SparseVector();
			foreach (var value in values)
			{
				result.Set(value.Key, value.Value);
			}

			return result;
		}

		/// <summary>
		/// Scalar product
		/// </summary>
		/// <param name="textFrequencies"></param>
		/// <returns></returns>
		public double ScalarTo(SparseVector textFrequencies)
		{
			double result = 0;
			// It is correct to scan stored entries only as 
			// for non-stored entries the value is zero
			foreach (var entry in values)
			{
				var tf = textFrequencies.Get(entry.Key);
				result += tf * entry.Value;
			}

			return result;
		}

		/// <summary>
		/// Returns the sum of the components
		/// </summary>
		/// <returns></returns>
		public double ComponentSum()
		{
			double result = 0;
			// It is correct to scan stored entries only as 
			// for non-stored entries the value is zero
			foreach (var entry in values)
			{
				result += entry.Value;
			}

			return result;
		}

		/// <summary>
		/// Returns enumeration of non-zero components (index and value)
		/// </summary>
		/// <returns></returns>
		public IEnumerable<KeyValuePair<int, double>> NonZeroEntries()
		{
			return values;
		}

		/// <summary>
		/// Returns enumeration of non-zero component indices
		/// </summary>
		/// <returns></returns>
		public IEnumerable<int> NonZeroComponents()
		{
			return values.Keys;
		}
	}
}