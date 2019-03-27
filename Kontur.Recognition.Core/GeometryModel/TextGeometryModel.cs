using System.Collections.Generic;
using System.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public class TextGeometryModel
	{
		/// <summary>
		/// Holds dimensions of this model
		/// </summary>
		private readonly BoundingBox pageBox;

		/// <summary>
		/// Returns dimentions of this model
		/// </summary>
		public BoundingBox PageBox { get { return pageBox; } }

		/// <summary>
		/// Holds resolution of coordinates in this model (number of grid cells per inch)
		/// </summary>
		[NotNull] 
		private readonly GridUnit gridUnit;

		/// <summary>
		/// Returns resolution of grid used by the model (number of grid cells per inch)
		/// </summary>
		[NotNull] 
		public GridUnit GridUnit { get { return gridUnit; } }

		private readonly List<GMTextBlock> textBlocks = new List<GMTextBlock>();
		private readonly List<GMSeparator> separators = new List<GMSeparator>();
		private readonly List<GMTable> tables = new List<GMTable>();

		public TextGeometryModel(BoundingBox pageBox, GridUnit gridUnit)
		{
			this.pageBox = pageBox;
			this.gridUnit = gridUnit;
		}


		public void AddTextBlock(GMTextBlock textBlock)
		{
			textBlocks.Add(textBlock);
		}

		public GMTextBlock AddTextBlock([NotNull] BoundingBox boundingBox)
		{
			var result = new GMTextBlock(boundingBox);
			textBlocks.Add(result);
			return result;
		}

		public void AddSeparator(GMSeparator separator)
		{
			separators.Add(separator);
		}

		public void AddTable(GMTable table)
		{
			tables.Add(table);
		}

		/// <summary>
		/// Enumerates all the words of the model including those that are not related to any paragraphs or lines
		/// </summary>
		/// <returns></returns>
		public IEnumerable<GMWord> Words()
		{
			return AllTextBlocks().SelectMany(block => block.AllWords());
		}

		public IEnumerable<GMParagraph> Paragraphs()
		{
			return AllTextBlocks().SelectMany(block => block.Paragraphs());
		}

		public IEnumerable<GMTextBlock> AllTextBlocks()
		{
			return textBlocks.Concat(tables.SelectMany(t => t.Cells().Select(c => c.TextBlock)));
		}

		public IEnumerable<GMTextBlock> TextBlocks()
		{
			return textBlocks;
		} 

		public IEnumerable<GMSeparator> Separators()
		{
			return separators;
		}

		public IEnumerable<GMTable> Tables()
		{
			return tables;
		}
	}
}