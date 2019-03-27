using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	/// <summary>
	/// Represents fragment of the page which is a block of text.
	/// Text block consists of paragraphs (if can be recognized) and/or of set of separate words
	/// </summary>
	public class GMTextBlock : GMElement
	{
		/// <summary>
		/// List of recognized paragraphs
		/// </summary>
		private readonly List<GMParagraph> paragraphs = new List<GMParagraph>();

		/// <summary>
		/// List of words which are not related to any paragraph
		/// </summary>
		private readonly List<GMWord> words = new List<GMWord>();

		public GMTextBlock([NotNull] BoundingBox boundingBox) 
			: base(boundingBox)
		{
		}

		public IEnumerable<GMParagraph> Paragraphs()
		{
			return paragraphs;
		}

		public void AddParagraph(GMParagraph para)
		{
			paragraphs.Add(para);
		}

		public GMParagraph AddParagraph(BoundingBox boundingBox)
		{
			var paragraph = new GMParagraph(boundingBox);
			AddParagraph(paragraph);
			return paragraph;
		}

		/// <summary>
		/// Returns all words of this text block (both related to some paragraph and standalone ones)
		/// </summary>
		/// <returns></returns>
		public IEnumerable<GMWord> AllWords()
		{
			return paragraphs.SelectMany(para => para.Lines().SelectMany(line => line.Words())).Concat(words);
		}

		public IEnumerable<GMWord> StandaloneWords()
		{
			return words;
		}

		public void AddStandaloneWord(GMWord word)
		{
			words.Add(word);
		}
	}
}