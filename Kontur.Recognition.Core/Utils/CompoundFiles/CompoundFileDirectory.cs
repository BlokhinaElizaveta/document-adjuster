using System;
using System.Collections.Generic;

namespace Kontur.Recognition.Utils.CompoundFiles
{
	public class CompoundFileDirectory
	{
		private readonly List<CompoundFileDirEntry> entries = new List<CompoundFileDirEntry>();
		public bool IsComplete { get; internal set; }

		public IEnumerable<CompoundFileDirEntry> Entries
		{
			get { return entries; }
		}

		public CompoundFileDirEntry RootEntry
		{
			get { return entries[0]; }
		}

		public int EntriesCount
		{
			get { return entries.Count; } 
		}

		public void AddEntry(CompoundFileDirEntry entry)
		{
			entries.Add(entry);
		}

		public IEnumerable<CompoundFileDirEntry> GetFolder(CompoundFileDirEntry storageEntry)
		{
			var result = new List<CompoundFileDirEntry>();
			if (storageEntry.ObjectType == DirEntryObjectType.StorageObject ||
			    storageEntry.ObjectType == DirEntryObjectType.RootStorageObject)
			{
				var treeRootEntry = entries[storageEntry.ChildId];
				TraverseTree(treeRootEntry, result);
			}
			return result;
		}

		private void TraverseTree(CompoundFileDirEntry entry, List<CompoundFileDirEntry> result)
		{
			var leftSiblingId = entry.LeftSiblingId;
			var rightSiblingId = entry.RightSiblingId;
			// In case of incomplete directory (partially loaded directory)
			// some referenced entries can be unavailable, so the range check here is important
			if (leftSiblingId >= 0 && leftSiblingId < entries.Count)
			{
				var leftSiblilng = entries[leftSiblingId];
				TraverseTree(leftSiblilng, result);
			}
			result.Add(entry);
			if (rightSiblingId >= 0 && rightSiblingId < entries.Count)
			{
				var rightSiblilng = entries[rightSiblingId];
				TraverseTree(rightSiblilng, result);
			}
		}

		public void Print()
		{
			foreach (var compoundFileDirEntry in entries)
			{
				Console.Out.WriteLine(compoundFileDirEntry.ToString());
			}
		}
	}
}