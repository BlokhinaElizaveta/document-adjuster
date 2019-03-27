namespace Kontur.Recognition.Utils.CompoundFiles
{
	public class CompoundFileDirEntry
	{
		private readonly string entryName;
		private readonly DirEntryObjectType objectType;
		private readonly int startSector;
		private readonly long streamSize;
		private readonly int entryId;
		private readonly int leftSiblingId;
		private readonly int rightSiblingId;
		private readonly int childId;

		public string EntryName { get { return entryName; }}
		public DirEntryObjectType ObjectType { get { return objectType; }}
		public int StartSector { get { return startSector; }}
		public long StreamSize { get { return streamSize; }}
		public int EntryId { get { return entryId; }}
		public int LeftSiblingId { get { return leftSiblingId; }}
		public int RightSiblingId { get { return rightSiblingId; }}
		public int ChildId { get { return childId; }}

		public CompoundFileDirEntry(string entryName, DirEntryObjectType objectType, int startSector, long streamSize, int entryId, int leftSiblingId, int rightSiblingId, int childId)
		{
			this.entryName = entryName;
			this.objectType = objectType;
			this.startSector = startSector;
			this.streamSize = streamSize;
			this.entryId = entryId;
			this.leftSiblingId = leftSiblingId;
			this.rightSiblingId = rightSiblingId;
			this.childId = childId;
		}

		public override string ToString()
		{
			return string.Format("EntryName: {0}, ObjectType: {1}, StartSector: {2}, StreamSize: {3}, EntryId: {4}, LeftSiblingId: {5}, RightSiblingId: {6}, ChildId: {7}", EntryName, ObjectType, StartSector, StreamSize, EntryId, LeftSiblingId, RightSiblingId, ChildId);
		}
	}
}