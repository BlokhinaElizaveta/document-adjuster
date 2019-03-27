using System;
using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;
using Kontur.Recognition.Utils.Ref;

namespace Kontur.Recognition.Utils.Files
{
	/// <summary>
	/// Represents managed reference to a file. The target file can be scheduled to be deleted when all references to it get disposed.
	/// </summary>
	public class LocalFile : IDisposable, ICloneable
	{
		private readonly SharedHandle<FileController> fileControllerHandle;
		
		/// <summary>
		/// Returns location of the file as a full path
		/// </summary>
		[NotNull]
		public string FilePath { get { return GetFileController().FilePath; } }

		/// <summary>
		/// Provides information on whether this file will be automatically deleted when this object is disposed or finalized
		/// </summary>
		public bool WillBeDeletedOnDispose { get { return GetFileController().WillBeDeletedOnDispose; } }

		/// <summary>
		/// Creates new reference to specified file
		/// </summary>
		/// <param name="path">The path to the file</param>
		/// <param name="deleteOnDispose">Whether file should be deleted when the reference gets disposed</param>
		public LocalFile([NotNull] string path, bool deleteOnDispose = false)
		{
			fileControllerHandle = SharedHandle.NewHandle(new FileController(path, deleteOnDispose));
		}

		/// <summary>
		/// Creates another reference to the file referenced by existing reference
		/// </summary>
		/// <param name="another"></param>
		public LocalFile(LocalFile another)
		{
			fileControllerHandle = another.fileControllerHandle.Duplicate();
		}

		public void Dispose()
		{
			fileControllerHandle.Dispose();
			GC.KeepAlive(this);
		}

		[NotNull]
		private FileController GetFileController()
		{
			return fileControllerHandle.GetTarget() ?? ThrowInvalidStateError();
		}

		private static FileController ThrowInvalidStateError()
		{
			throw new InvalidOperationException("Object is in disposed state and can not be used");
		}

		public void DeleteOnDispose()
		{
			GetFileController().DeleteOnDispose();
		}

		public FileStream Open(FileMode fileMode, FileAccess fileAccess, FileShare fileShare)
		{
			return GetFileController().Open(fileMode, fileAccess, fileShare);
		}
		
		public void WriteContent(byte[] content)
		{
			GetFileController().WriteContent(content);
		}

		public void WriteContent(Stream inStream)
		{
			GetFileController().WriteContent(inStream);
		}

		public byte[] ReadContent()
		{
			return GetFileController().ReadContent();
		}

		[NotNull]
		public virtual object Clone()
		{
			return new LocalFile(this);
		}

		[NotNull]
		public LocalFile DuplicateReference()
		{
			var result = Clone() as LocalFile;
			if (result == null)
				throw new NullReferenceException();
			return result;
		}
	}
}