using System.Collections.Generic;

namespace Kontur.Recognition.Utils
{
	/// <summary>
	/// This class contains helper methods to work with dictionaries
	/// </summary>
	public static class DictionaryExtensions
	{
		public static TValue Get<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
		{
			TValue result;
			return dictionary.TryGetValue(key, out result) ? result : default(TValue);
		}
	}
}