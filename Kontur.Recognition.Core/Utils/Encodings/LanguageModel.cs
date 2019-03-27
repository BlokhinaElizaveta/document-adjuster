using System;
using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.Utils.Encodings
{
	public class LanguageModel : ILanguageModel
	{
		private readonly SparseVector frequencies;
		private readonly SparseVector lowerCaseFrequencies;
		private readonly HashSet<char> modelChars;

		protected LanguageModel(CharCounters lowerCaseCharCounts, double upperCaseToLowerCaseFactor)
		{
			lowerCaseFrequencies = lowerCaseCharCounts.ToFrequenciesVector();
			var counters = new CharCounters();
			foreach (var lowerCaseChar in lowerCaseCharCounts.Chars())
			{
				var upperCaseChar = Char.ToUpperInvariant(lowerCaseChar);
				var lowerCaseCounter = lowerCaseCharCounts.CharCount(lowerCaseChar) * 1000;
				var upperCaseCounter = (int)Math.Round(upperCaseToLowerCaseFactor * lowerCaseCounter);
				counters.AddChar(lowerCaseChar, lowerCaseCounter);
				counters.AddChar(upperCaseChar, upperCaseCounter);
			}

			frequencies = counters.ToFrequenciesVector();
			modelChars = new HashSet<char>(frequencies.NonZeroComponents().Select(c => (char)c));
		}

		protected LanguageModel(SparseVector frequencies)
		{
			this.frequencies = frequencies.Duplicate();
			var normalized = frequencies.Duplicate();
			normalized.Normalize();
			modelChars = new HashSet<char>(frequencies.NonZeroComponents().Select(c => (char)c));
		}

		public double GetFrequency(char c)
		{
			return frequencies.Get(c);
		}

		public double GetCompliance(string text, bool ignoreCase, bool applyModelFilter)
		{
			var charCounters = new CharCounters();
			if (ignoreCase)
			{
				text = text.ToLowerInvariant();
			}

			if (applyModelFilter)
			{
				foreach (var c in text)
				{
					if (modelChars.Contains(c))
					{
						charCounters.AddChar(c);
					}
				}
			}
			else
			{
				charCounters.AddAllChars(text);
			}

			var textFrequencies = charCounters.ToFrequenciesVector();

			var frequenciesSet = ignoreCase ? lowerCaseFrequencies : frequencies;

			double maxDiff = 0;
			double diff = 0;
			foreach (var entry in frequenciesSet.NonZeroEntries())
			{
				diff += Math.Abs(entry.Value - textFrequencies.Get(entry.Key));
				maxDiff += entry.Value;
			}

			var result = (maxDiff - diff) / maxDiff;
			return result >= 0 ? result: 0;
		}
	}
}