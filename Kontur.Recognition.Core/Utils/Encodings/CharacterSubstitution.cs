using System.Collections.Generic;
using System.Text;

namespace Kontur.Recognition.Utils.Encodings
{
	/// <summary>
	/// This class defines a substitution on chars 
	/// (for example to translade data between different character encodings
	/// or to restore a broken encoding)
	/// </summary>
	public class CharacterSubstitution
	{
		private readonly Dictionary<char, char> charMap = new Dictionary<char, char>();

		/// <summary>
		/// Adds a mapping for a specific character
		/// </summary>
		/// <param name="from">source character</param>
		/// <param name="to">character to map source character to</param>
		public void MapChar(char from, char to)
		{
			if (charMap.ContainsKey(from))
			{
				charMap[from] = to;
			}
			else
			{
				charMap.Add(from, to);
			}
		}

		/// <summary>
		/// Defines a mapping for specific range of subsequent characters
		/// </summary>
		/// <param name="from">The first character of the source range</param>
		/// <param name="to">The first character of the target range</param>
		/// <param name="count">Number of characters to map</param>
		public void MapRange(char from, char to, int count)
		{
			while (count-- > 0)
			{
				MapChar(from++, to++);
			}
		}

		/// <summary>
		/// Applies the defined mapping to the given string
		/// </summary>
		/// <param name="source"></param>
		/// <returns></returns>
		public string TransformString(string source)
		{
			if (source == null)
				return null;

			var result = new StringBuilder(source.Length);
			foreach (var c in source)
			{
				char newChar;
				if (!charMap.TryGetValue(c, out newChar))
					newChar = c;
				result.Append(newChar);
			}

			return result.ToString();
		}

		/// <summary>
		/// Applies the defined mapping to the given string 
		/// with an ability to disable translation for specific chars
		/// </summary>
		/// <param name="source">The line to translate</param>
		/// <param name="charsToProtect">The set of characters to protect from being transformed</param>
		/// <returns></returns>
		public string TransformString(string source, ISet<char> charsToProtect)
		{
			if (source == null)
				return null;

			var result = new StringBuilder(source.Length);
			foreach (var c in source)
			{
				char newChar;
				if (charsToProtect.Contains(c) || !charMap.TryGetValue(c, out newChar))
					newChar = c;
				result.Append(newChar);
			}

			return result.ToString();
		}

		private static readonly HashSet<char> protectedLineChars = new HashSet<char>{ ' ' };

		/// <summary>
		/// Applies the defined mapping to the given text (the text is split into 
		/// separate tokens which are then translated independently; 
		/// line breaks, page breaks and spaces are preserved)
		/// </summary>
		/// <param name="text">The text to translate</param>
		/// <returns></returns>
		public string Transform(string text)
		{
			var result = new StringBuilder(text.Length);
			foreach (var line in text.Split('\n'))
			{
				var lineToProcess = line;
				var leadingFormFeed = lineToProcess.StartsWith("\u000C");
				if (leadingFormFeed)
				{
					lineToProcess = lineToProcess.Substring(1);
				}
				var newLine = TransformString(lineToProcess, protectedLineChars);
				if (leadingFormFeed)
				{
					result.Append('\u000C');
				}
				if (line.EndsWith("\r"))
				{
					result.Append(newLine.Substring(0, newLine.Length - 1));
					result.Append("\r\n");
				}
				else
				{
					result.Append(newLine);
					result.Append("\n");
				}
			}

			return result.ToString();
		}
	}
}