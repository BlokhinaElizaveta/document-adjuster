using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Files
{
	public interface IFilesFactory : ITempFilesFactory
	{
		/// <summary>
		/// Makes a reservation of name for new file. Unlike NewTempFile this method does not mark the file for 
		/// deletion when returned object (LocalFile) gets disposed or finalized. 
		/// </summary>
		/// <param name="prefix">The prefix of the name of the file being created</param>
		/// <param name="extension">The suffix (extension) of the name of the file being created</param>
		/// <returns></returns>
		[NotNull]
		LocalFile NewFile(string prefix, string extension);

		LocalFile AsFile(string filePath);
		
		TemporaryFile AsTempFile(string filePath);
	}
}