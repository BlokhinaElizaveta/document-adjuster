using System;
using System.Collections.Generic;
using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Logging;

namespace Kontur.Recognition.Utils.Files
{
	/// <summary>
	/// This class provides functionality to work with file as with a disposable resource
	/// </summary>
	internal class FileController : IDisposable
	{
		private readonly string filePath;
		private bool deleteOnDispose;
		private readonly Lazy<List<FileStream>> openedStreams = new Lazy<List<FileStream>>(() => new List<FileStream>());


		[NotNull]
		public string FilePath { get { return filePath; } }

		/// <summary>
		/// Provides information on whether this file will be automatically deleted when this object is disposed or finalized
		/// </summary>
		public bool WillBeDeletedOnDispose { get { return deleteOnDispose; } }

		public FileController([NotNull] string path, bool deleteOnDispose = false)
		{
			filePath = Path.GetFullPath(path);
			this.deleteOnDispose = deleteOnDispose;
		}

		~FileController()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public void Dispose(bool disposing)
		{
			if (deleteOnDispose)
			{
				// We can not rely on existence and valid state of referenced objects 
				// when this method is invoked from the finalizer
				if (disposing)
				{
					// Preventive code to close all opened streams before file gets deteled
					if (openedStreams.IsValueCreated)
					{
						foreach (var stream in openedStreams.Value)
						{
							stream.Dispose();
						}
						openedStreams.Value.Clear();
					}
				}
				try
				{
					if (File.Exists(FilePath))
					{
						File.Delete(FilePath);
					}
				}
				catch(Exception e)
				{
					Log.ErrorFormat(typeof(LocalFile), "Cannot delete file: {0}; error: {1}", FilePath, e);
				}
			}
		}

		public void DeleteOnDispose()
		{
			deleteOnDispose = true;
		}

		public FileStream Open(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
		{
			var stream = File.Open(FilePath, fileMode, fileAccess, fileShare);
			openedStreams.Value.Add(stream);
			return stream;
		}

		public void WriteContent(byte[] content)
		{
			using (var stream = Open(FileMode.Create, FileAccess.Write, FileShare.None))
				stream.Write(content, 0, content.Length);
		}

		public void WriteContent(Stream inStream)
		{
			using (var outStream = Open(FileMode.Create, FileAccess.Write, FileShare.None))
			{
				var buffer = new byte[8192];
				int byteCount;
				while ((byteCount = inStream.Read(buffer, 0, buffer.Length)) > 0)
				{
					outStream.Write(buffer, 0, byteCount);
				}
			}
		}

		public byte[] ReadContent()
		{
			return File.ReadAllBytes(FilePath);
		}
	}
}