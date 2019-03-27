using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.CompoundFiles
{
	/// <summary>
	/// This class implements (partially) logic to read data from Windows Compound File Binary File 
	/// See http://msdn.microsoft.com/en-us/library/dd942138.aspx ("[MS-CFB]: Compound File Binary File Format" for more details).
	/// This class implements retrieving header record, DIFAT and FAT tables, Directory entries, content of specific sector of the file, retrieval of sectors chain.
	/// This class does not implement retrieval of MiniFAT and content of specific object/stream from the file (this support can be easily added if needed)
	/// </summary>
	public class CompoundFile : IDisposable
	{
		private Stream inputStream;

		/// <summary>
		/// Opens given file by specified path
		/// </summary>
		/// <param name="path">The file to open</param>
		/// <exception cref="ArgumentException">ArgumentException is thrown in case when file is corrupt</exception>
		public CompoundFile(string path, bool openCorrupted = false)
		{
			Init(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite), openCorrupted);
		}

		/// <summary>
		/// Opens given binary content as a windows compound binary file
		/// </summary>
		/// <param name="content">The content to read</param>
		/// <exception cref="ArgumentException">ArgumentException is thrown in case when file is corrupt</exception>
		public CompoundFile(byte[] content, bool openCorrupted = false)
		{
			Init(new MemoryStream(content), openCorrupted);
		}

		/// <summary>
		/// Opens given binary content as a windows compound binary file
		/// </summary>
		/// <param name="content">The content to read</param>
		/// <param name="contentLength">The length of content buffer with data (may be less than the whole buffer size)</param>
		/// <exception cref="ArgumentException">ArgumentException is thrown in case when file is corrupt</exception>
		public CompoundFile(byte[] content, int contentLength, bool openCorrupted = false)
		{
			Init(new MemoryStream(content, 0, contentLength), openCorrupted);
		}

		private void Init(Stream stream, bool openCorrupted)
		{
			inputStream = stream;
			try
			{
				LoadHeader(openCorrupted);
			}
			catch (Exception)
			{
				Close();
				throw;
			}
		}

		private readonly byte[] signatureBytes = { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1};
		public const int endOfSectorChain = -2; // 0xFFFFFFFE;
		private const int notUsedSector = -1;	 // 0xFFFFFFFF;

		private int minorVersion;
		private int majorVersion;
		private int sectorSize;
		private int miniSectorSize;
		private int firstDirectorySector;
		private bool isBroken = false;

		/// <summary>
		/// Contains indices of sectors with FAT content
		/// </summary>
		private readonly List<int> fatSectorIndices = new List<int>();
		/// <summary>
		/// Cached sectors of FAT table
		/// </summary>
		private byte[][] fatSectors;
		/// <summary>
		/// Calculated number of FAT entries for one sector
		/// </summary>
		private int fatEntriesPerSector;

		/// <summary>
		/// Contains indices of sectors with miniFAT content
		/// </summary>
		private readonly List<int> miniFatSectorIndices = new List<int>();
		/// <summary>
		/// Cached sectors of miniFAT table
		/// </summary>
		private byte[][] miniFatSectors;

		/// <summary>
		/// Contains indices of sectors with miniStream content
		/// </summary>
		private readonly List<int> miniStreamSectorIndices = new List<int>();

		/// <summary>
		///  If the stream size is less than this value then the stream is stored in mini stream. Otherwise the stream is stored in usual sectors.
		/// </summary>
		private int miniStreamCutoffSize;

		private void LoadHeader(bool openCorrupted)
		{
			inputStream.Seek(0, SeekOrigin.Begin);
			var header = new byte[512];
			var bytesCount = inputStream.Read(header, 0, header.Length);
			if (bytesCount < header.Length)
			{
				throw new ArgumentException("Given file is not a MS Compound binary file: too short");
			}
			// 8 bytes - signature
			if (!header.StartsWith(signatureBytes))
			{
				throw new ArgumentException("Given file is not a MS Compound binary file: header signature is missing");
			}
			// next 16 bytes - CLSID must be filled with zeroes
			minorVersion = header.ReadInt32LittleEndian(0x18, 2);
			majorVersion = header.ReadInt32LittleEndian(0x1A, 2);
			var byteOrder = header.ReadInt32LittleEndian(0x1C, 2);
			if (byteOrder != 0xFFFE)
			{
				throw new ArgumentException("Given file is not a MS Compound binary file: unexpected byte order");
			}
			var sectorShift = header.ReadInt32LittleEndian(0x1E, 2);
			if (sectorShift == 0x0009 || sectorShift == 0x000C)
			{
				sectorSize = 1 << sectorShift;
			}
			else
			{
				throw new ArgumentException("Given file is not a MS Compound binary file: unexpected sector size");
			}
			var miniSectorShift = header.ReadInt32LittleEndian(0x20, 2);
			if (miniSectorShift != 0x0006)
			{
				throw new ArgumentException("Given file is not a MS Compound binary file: unexpected mini sector size");
			}
			miniSectorSize = 64;
			// Next 6 bytes are reserved
			var numOfDirectorySectors = header.ReadInt32LittleEndian(0x28, 4);		// set to 0 for majorVersion 3, otherwise holds the number of sectors in Directory chain
			var numOfFatSectors = header.ReadInt32LittleEndian(0x2C, 4);			// Number of sectors in FAT
			firstDirectorySector = header.ReadInt32LittleEndian(0x30, 4);		// Number of sector for directory stream
			var transactionSignatureNumber = header.ReadInt32LittleEndian(0x34, 4);	// May contain transaction sequence number if supported
			miniStreamCutoffSize = header.ReadInt32LittleEndian(0x38, 4);		// Must be 0x00001000
			var firstMiniFatSector = header.ReadInt32LittleEndian(0x3C, 4);			// First sector of mini FAT
			var numOfMiniFatSectors = header.ReadInt32LittleEndian(0x40, 4);		// Number of sectors in mini FAT
			var firstDiFatSector = header.ReadInt32LittleEndian(0x44, 4);			// First sector of DIFAT
			var numOfDiFatSectors = header.ReadInt32LittleEndian(0x48, 4);			// Number of sectors in DIFAT
			// next block of 436 bytes represents first 109 entries of DIFAT (each entry is a 32-bit integer)
			fatEntriesPerSector = sectorSize/4;
			ReadFATTable(header);
			ReadMiniFATTable(header, !openCorrupted);
		}

		/// <summary>
		/// Reads information from DIFAT to populate information on location of FAT sectors
		/// </summary>
		/// <param name="header"></param>
		private void ReadFATTable(byte [] header)
		{
			var numOfFatSectors = header.ReadInt32LittleEndian(0x2C, 4);			// Number of sectors in FAT
			fatSectors = new byte[numOfFatSectors][];

			var firstDiFatSector = header.ReadInt32LittleEndian(0x44, 4);			// First sector of DIFAT
			var numOfDiFatSectors = header.ReadInt32LittleEndian(0x48, 4);			// Number of sectors in DIFAT

			// Read the portion of DIFAT located in header sector
			var currentDiFatEntry = 0x4C;
			for (var idx = 0; idx < 109; idx++)
			{
				var fatSectorIdx = header.ReadInt32LittleEndian(currentDiFatEntry, 4);
				currentDiFatEntry += 4;
				if (fatSectorIdx != notUsedSector)
				{
					fatSectorIndices.Add(fatSectorIdx);
				}
			}

			// Read the remaining DIFAT sectors
			var currentDiFatSector = firstDiFatSector;
			while (numOfDiFatSectors > 0)
			{
				var currentSector = TryReadSector(currentDiFatSector);
				if (currentSector != null)
				{
					currentDiFatEntry = 0x00;
					while (currentDiFatEntry < sectorSize - 4)
					{
						var fatSectorIdx = currentSector.ReadInt32LittleEndian(currentDiFatEntry, 4);
						currentDiFatEntry += 4;
						if (fatSectorIdx != notUsedSector)
						{
							fatSectorIndices.Add(fatSectorIdx);
						}
					}

					currentDiFatSector = currentSector.ReadInt32LittleEndian(currentSector.Length - 4, 4);
					numOfDiFatSectors--;
				}
				else
				{
					isBroken = true;
					numOfDiFatSectors = 0;
				}
			}
		}

		/// <summary>
		/// Reads miniFAT sector chain to populate information on location of miniFAT sectors
		/// </summary>
		/// <param name="header"></param>
		private void ReadMiniFATTable(byte[] header, bool exceptionOnError)
		{
			var firstMiniFatSector = header.ReadInt32LittleEndian(0x3C, 4);      // First sector of mini FAT
			var numOfMiniFatSectors = header.ReadInt32LittleEndian(0x40, 4);    // Number of sectors in mini FAT
			miniFatSectors = new byte[numOfMiniFatSectors][];

			// Read the remaining DIFAT sectors
			var currentMiniFatSector = firstMiniFatSector;
			while (currentMiniFatSector != endOfSectorChain)
			{
				miniFatSectorIndices.Add(currentMiniFatSector);
				currentMiniFatSector = NextSectorInFatChain(currentMiniFatSector, exceptionOnError);
			}

			// Locate miniStream sectors chain
			var dirFirstSector = TryReadSector(firstDirectorySector);
			if (dirFirstSector != null)
			{
				var rootEntry = ReadDirEntry(dirFirstSector, 0, 0);
				var miniStreamFirstSector = rootEntry.StartSector;
				var currentMiniStreamSector = miniStreamFirstSector;
				if (numOfMiniFatSectors > 0 && currentMiniStreamSector != -1)
				{
					while (currentMiniStreamSector != endOfSectorChain)
					{
						miniStreamSectorIndices.Add(currentMiniStreamSector);
						currentMiniStreamSector = NextSectorInFatChain(currentMiniStreamSector, exceptionOnError);
					}
				}
			}
		}

		/// <summary>
		/// Helper method to resolve next sector in sectors chain
		/// </summary>
		/// <param name="currentSector">Current sector index</param>
		/// <param name="exceptionOnError">Whether an error should be reported in case of broken file 
		/// (if set to false, then errors will be reported as if the current sector is the last in the chain)</param>
		/// <returns>Index of the sector following the current sector in the chain</returns>
		public int NextSectorInFatChain(int currentSector, bool exceptionOnError = true)
		{
			return NextSectorInFatChainImpl(currentSector, false, exceptionOnError);
		}

		/// <summary>
		/// Helper method to resolve next sector in niniFAT sectors chain
		/// </summary>
		/// <param name="currentSector">Current sector index</param>
		/// <param name="exceptionOnError">Whether an error should be reported in case of broken file 
		/// (if set to false, then errors will be reported as if the current sector is the last in the chain)</param>
		/// <returns>Index of the sector following the current sector in the chain</returns>
		public int NextSectorInMiniFatChain(int currentSector, bool exceptionOnError = true)
		{
			return NextSectorInFatChainImpl(currentSector, true, exceptionOnError);
		}

		/// <summary>
		/// Helper method to resolve next sector in miniFAT sectors chain
		/// </summary>
		/// <param name="currentSector">Current sector index</param>
		/// <param name="useMiniFat">Whether miniFat should be used for operation</param>
		/// <param name="exceptionOnError">Whether an error should be reported in case of broken file 
		/// (if set to false, then errors will be reported as if the current sector is the last in the chain)</param>
		/// <returns>Index of the sector following the current sector in the chain</returns>
		private int NextSectorInFatChainImpl(int currentSector, bool useMiniFat, bool exceptionOnError = true)
		{
			var sectorsCache = useMiniFat ? miniFatSectors : fatSectors;
			var sectorIndices = useMiniFat ? miniFatSectorIndices : fatSectorIndices;

			var fatSectorIdx = currentSector / fatEntriesPerSector; // which FAT sector contains info about current sector
			var sector = sectorsCache[fatSectorIdx];
			if (sector == null)
			{
				if (fatSectorIdx < sectorIndices.Count)
				{
					var fatSectorIndex = sectorIndices[fatSectorIdx];
					sector = TryReadSector(fatSectorIndex);
					if (sector != null)
					{
						sectorsCache[fatSectorIdx] = sector;
					}
					else
					{
						isBroken = true;
						if (exceptionOnError)
						{
							throw new InvalidOperationException(string.Format("{1} sector {0} can not be read.", fatSectorIdx, useMiniFat ? "miniFAT" : "FAT"));
						}
						return endOfSectorChain;
					}
				}
				else
				{
					isBroken = true;
					if (exceptionOnError)
					{
						throw new InvalidOperationException(string.Format("{1} table is corrupted, sector index for entry {0} is missing.", fatSectorIdx, useMiniFat ? "miniFAT" : "FAT"));
					}
					return endOfSectorChain;
				}
			}
			var entryOffset = (currentSector % fatEntriesPerSector) * 4;
			return sector.ReadInt32LittleEndian(entryOffset, 4);
		}


		/// <summary>
		/// Retrieves the content of specific entry of compound file
		/// </summary>
		/// <param name="entry">The entry whose content to retrieve</param>
		/// <param name="bytesLimit">Number of bytes to read. If set to null the whole content will be retrieved.</param>
		/// <param name="exceptionOnError">Whether an exception should be thrown in case of broken compound file</param>
		/// <returns></returns>
		public byte[] ReadEntryContent(CompoundFileDirEntry entry, int? bytesLimit = null, bool exceptionOnError = true)
		{
			long size = entry.StreamSize;
			if (bytesLimit.HasValue && bytesLimit < size)
			{
				size = bytesLimit.Value;
			}
			var result = new byte[size];
			var offset = 0;
			var remaining = result.Length;
			var currentSector = entry.StartSector;
			if (entry.StreamSize < miniStreamCutoffSize)
			{
				while (remaining > 0 && currentSector != endOfSectorChain)
				{
					var content = exceptionOnError ? ReadMiniSector(currentSector) : TryReadMiniSector(currentSector);
					content = content ?? new byte[sectorSize];
					var dataLength = content.Length < remaining ? content.Length : remaining;
					Array.Copy(content, 0, result, offset, dataLength);
					offset += dataLength;
					remaining -= dataLength;
					currentSector = NextSectorInMiniFatChain(currentSector, exceptionOnError);
				}
			}
			else
			{
				while (remaining > 0 && currentSector != endOfSectorChain)
				{
					var content = exceptionOnError ? ReadSector(currentSector) : TryReadSector(currentSector);
					content = content ?? new byte[sectorSize];
					var dataLength = content.Length < remaining ? content.Length : remaining;
					Array.Copy(content, 0, result, offset, dataLength);
					offset += dataLength;
					remaining -= dataLength;
					currentSector = NextSectorInFatChain(currentSector, exceptionOnError);
				}
			}
			return result;
		}

		/// <summary>
		/// Reads content of a sector by given sector index
		/// </summary>
		/// <param name="sectorNumber">Index of the sector to read (0-based)</param>
		/// <returns>The content of the sector</returns>
		/// <exception cref="ArgumentException">ArgumentException is thrown when sector can not be read (e.g. file is corrupted)</exception>
		[NotNull]
		public byte[] ReadSector(int sectorNumber)
		{
			var result = TryReadSector(sectorNumber);
			if (result == null)
			{
				throw new ArgumentException(string.Format("Sector #{0} can not be read", sectorNumber));
			}
			return result;
		}

		/// <summary>
		/// Tries to read content of a sector by given sector index
		/// </summary>
		/// <param name="sectorNumber">Index of the sector to read (0-based)</param>
		/// <returns>The content of the sector or null if the sector can not be read</returns>
		[CanBeNull]
		public byte[] TryReadSector(int sectorNumber)
		{
			int offset = (sectorNumber + 1)*sectorSize;
			if (inputStream.Length < offset + sectorSize)
			{
				return null;
			}
			var result = new byte[sectorSize];
			inputStream.Seek(offset, SeekOrigin.Begin);
			inputStream.Read(result, 0, result.Length);
			return result;
		}

		/// <summary>
		/// Reads content of a sector by given sector index
		/// </summary>
		/// <param name="sectorNumber">Index of the sector to read (0-based)</param>
		/// <returns>The content of the sector</returns>
		/// <exception cref="ArgumentException">ArgumentException is thrown when sector can not be read (e.g. file is corrupted)</exception>
		[NotNull]
		public byte[] ReadMiniSector(int sectorNumber)
		{
			var result = TryReadMiniSector(sectorNumber);
			if (result == null)
			{
				throw new ArgumentException(string.Format("Sector #{0} can not be read", sectorNumber));
			}
			return result;
		}

		/// <summary>
		/// Tries to read content of a mini sector by given sector index
		/// </summary>
		/// <param name="sectorNumber">Index of the sector to read (0-based)</param>
		/// <returns>The content of the sector or null if the sector can not be read</returns>
		[CanBeNull]
		public byte[] TryReadMiniSector(int sectorNumber)
		{
			var miniStreamOffset = sectorNumber * miniSectorSize;
			var miniStreamSectorIdx = miniStreamOffset / sectorSize;
			var offsetInSector = miniStreamOffset % sectorSize;

			if (miniStreamSectorIdx >= miniStreamSectorIndices.Count)
			{
				return null; // file is broken: no information about specific sector with mini stream content
			}

			var miniStreamSector = miniStreamSectorIndices[miniStreamSectorIdx];

			int offset = (miniStreamSector + 1) * sectorSize + offsetInSector;
			if (inputStream.Length < offset + miniSectorSize)
			{
				return null;	// file is broken: can not locate sector in file
			}
			var result = new byte[miniSectorSize];
			inputStream.Seek(offset, SeekOrigin.Begin);
			inputStream.Read(result, 0, result.Length);
			return result;
		}

		/// <summary>
		/// Reads directory of compound file
		/// </summary>
		/// <returns>The directory container</returns>
		/// <exception cref="InvalidOperationException">InvalidOperationException is thrown in case of broken file (i.e. there is a problem retrieving directory sectors)</exception>
		public CompoundFileDirectory ReadDirectory()
		{
			return ReadDirectoryImpl(true);
		}

		/// <summary>
		/// Tries to read directory of compound file (in case of broken file the directory can be loaded partially)
		/// </summary>
		/// <returns>The directory container</returns>
		public CompoundFileDirectory TryReadDirectory()
		{
			return ReadDirectoryImpl(false);
		}

		private CompoundFileDirectory ReadDirectoryImpl(bool exceptionOnError = true)
		{
			var result = new CompoundFileDirectory();
			result.IsComplete = true;
			var currentSector = firstDirectorySector;
			while (currentSector != endOfSectorChain)
			{
				var sectorData = TryReadSector(currentSector);
				if (sectorData != null)
				{
					ReadEntries(sectorData, result);
				}
				else
				{
					isBroken = true;
					result.IsComplete = false;
					if (exceptionOnError)
					{
						throw new InvalidOperationException(string.Format("Directory sector {0} can not be read.", currentSector));
					}
				}
				try
				{
					currentSector = NextSectorInFatChain(currentSector);
				}
				catch (Exception)
				{
					isBroken = true;
					result.IsComplete = false;
					if (exceptionOnError)
					{
						throw;
					}
					currentSector = endOfSectorChain;
				}
			}
			return result;
		}

		/// <summary>
		/// Populates directory object with data from given sector of directory
		/// </summary>
		/// <param name="sectorData">The server to process</param>
		/// <param name="directory">The directory object to populate</param>
		/// <returns></returns>
		private void ReadEntries(byte[] sectorData, CompoundFileDirectory directory)
		{
			// each directory entry has size of 128 byte
			var entriesPerSector = sectorSize/128;
			for (var entryIdx = 0; entryIdx < entriesPerSector; entryIdx++)
			{
				var entryOffset = entryIdx*128;
				directory.AddEntry(ReadDirEntry(sectorData, entryOffset, directory.EntriesCount));
			}
		}

		/// <summary>
		/// Reads single directory entry starting with given offset 
		/// </summary>
		/// <param name="sectorData">The server to process</param>
		/// <param name="entryOffset">Starting offset of the data to read</param>
		/// <param name="entryIndex">Index to assign to the newly read entry</param>
		/// <returns></returns>
		private CompoundFileDirEntry ReadDirEntry(byte[] sectorData, int entryOffset, int entryIndex)
		{
			var nameBytesLength = sectorData.ReadInt32LittleEndian(entryOffset + 0x40, 2);
			// Name must be ended with Unicode zero-terminator (two zero bytes)
			var entryName = (nameBytesLength >= 2) ? Encoding.Unicode.GetString(sectorData, entryOffset, nameBytesLength - 2) : "";
			var objectType = sectorData[entryOffset + 0x42];
			var colorFlag = sectorData[entryOffset + 0x43];
			var leftSiblingId = sectorData.ReadInt32LittleEndian(entryOffset + 0x44, 4);
			var rightSiblingId = sectorData.ReadInt32LittleEndian(entryOffset + 0x48, 4);
			var childId = sectorData.ReadInt32LittleEndian(entryOffset + 0x4C, 4);
			// next 16 bytes - CLSID
			var stateBits = sectorData.ReadInt32LittleEndian(entryOffset + 0x60, 4);
			// next 8 bytes - creation time
			// next 8 bytes - modification time
			var startSector = sectorData.ReadInt32LittleEndian(entryOffset + 0x74, 4);
			var streamSize = sectorData.ReadLongLittleEndian(entryOffset + 0x78, 8);
			if (majorVersion == 3)
			{
				streamSize &= 0x00000000FFFFFFFFL;
			}
			return new CompoundFileDirEntry(entryName, 
					ResolveObjectType(objectType), startSector, streamSize, 
					entryIndex, leftSiblingId, rightSiblingId, childId);
		}

		private static DirEntryObjectType ResolveObjectType(byte objectType)
		{
			switch (objectType)
			{
				case 0: return DirEntryObjectType.UnknownOrUnallocated;
				case 1: return DirEntryObjectType.StorageObject;
				case 2: return DirEntryObjectType.StreamObject;
				case 5: return DirEntryObjectType.RootStorageObject;
				default: throw new ArgumentException("Bad object type detected");
			}
		}

		/// <summary>
		/// Closes this compound file
		/// </summary>
		public void Close()
		{
			Stream toClose;
			lock (this)
			{
				toClose = inputStream;
				inputStream = null;
			}
			if (toClose != null)
			{
				toClose.Close();
				toClose.Dispose();
			}
		}

		public void Dispose()
		{
			Close();
		}
	}
}