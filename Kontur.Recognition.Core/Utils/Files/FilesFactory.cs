using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Files
{
	public class FilesFactory : IFilesFactory
	{
		[UsedImplicitly]
		public string PathToFilesDir { get; private set; }

		[UsedImplicitly]
		public string PathToTempFilesDir { get; private set; }

		[UsedImplicitly]
		public FilesFactory(string pathToDir = null, string pathToTempDir = null)
		{
			PathToFilesDir = GetValidPath(pathToDir);
			PathToTempFilesDir = (pathToTempDir != null) ? GetValidPath(pathToTempDir) : PathToFilesDir;
		}

		private static string GetValidPath(string pathToDir)
		{
			if (string.IsNullOrEmpty(pathToDir))
			{
				pathToDir = Path.GetTempPath();
			}
			pathToDir = Path.GetFullPath(pathToDir);
			if (!Directory.Exists(pathToDir))
			{
				Directory.CreateDirectory(pathToDir);
			}
			return pathToDir;
		}

		/// <summary>
		/// Makes a reservation of name for new temporary file. The file is considered to be a temporary one,
		/// so it will be automatically deleted when returned object (TemporaryFile) gets disposed or finalized. 
		/// </summary>
		/// <param name="prefix">The prefix of the name of the file being created</param>
		/// <param name="extension">The suffix (extension) of the name of the file being created</param>
		/// <returns></returns>
		[NotNull]
		public virtual TemporaryFile NewTempFile(string prefix, string extension)
		{
			return new TemporaryFile(PathToTempFilesDir.ReserveFileName(prefix, extension));
		}

		/// <summary>
		/// Makes a reservation of name for new file. Unlike NewTempFile this method does not mark the file for 
		/// deletion when returned object (LocalFile) gets disposed or finalized. 
		/// </summary>
		/// <param name="prefix">The prefix of the name of the file being created</param>
		/// <param name="extension">The suffix (extension) of the name of the file being created</param>
		/// <returns></returns>
		[NotNull]
		public virtual LocalFile NewFile(string prefix, string extension)
		{
			return new LocalFile(PathToFilesDir.ReserveFileName(prefix, extension));
		}

		public virtual LocalFile AsFile(string filePath)
		{
			return new LocalFile(filePath);
		}

		public virtual TemporaryFile AsTempFile(string filePath)
		{
			return new TemporaryFile(filePath);
		}
	}
}
