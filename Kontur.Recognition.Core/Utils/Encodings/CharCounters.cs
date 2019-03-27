using System.Collections.Generic;

namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// The structure to accumulate information on number of occurences of characters in a text
	/// </summary>
	public class CharCounters
	{
		private readonly Dictionary<char, int> counters = new Dictionary<char, int>();
		private int totalCount = 0;

		/// <summary>
		/// Increments a counter for specific character
		/// </summary>
		/// <param name="c"></param>
		/// <param name="count"></param>
		public void AddChar(char c, int count)
		{
			int counter;
			if (counters.TryGetValue(c, out counter))
			{
				counters[c] = counter + count;
			}
			else
			{
				counters.Add(c, count);
			}

			totalCount += count;
		}

		/// <summary>
		/// Increments a counter for specific character by one
		/// </summary>
		/// <param name="c"></param>
		public void AddChar(char c)
		{
			AddChar(c, 1);
		}

		/// <summary>
		/// Transforms current set of counters into vector of character frequencies
		/// </summary>
		/// <returns></returns>
		public SparseVector ToFrequenciesVector()
		{
			var result = new SparseVector();
			double divisor = totalCount;
			foreach (var counter in counters)
			{
				result.Set(counter.Key, counter.Value / divisor);
			}
			result.Normalize();
			return result;
		}

		/// <summary>
		/// Updates counters for characters by number of their occurrences in the given text
		/// </summary>
		/// <param name="text"></param>
		public void AddAllChars(string text)
		{
			foreach (var c in text)
			{
				AddChar(c);
			}
		}

		/// <summary>
		/// Returns enumeration of characters for which counters are non-zero
		/// </summary>
		/// <returns></returns>
		public IEnumerable<char> Chars()
		{
			return counters.Keys;
		}

		/// <summary>
		/// Returns the counter for the given character
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public int CharCount(char c)
		{
			int result;
			if (counters.TryGetValue(c, out result))
				return result;
			return 0;
		}

		/// <summary>
		/// Returns the total number of characters accounted in this structure
		/// </summary>
		/// <returns></returns>
		public int TotalCount()
		{
			return totalCount;
		}
	}
}