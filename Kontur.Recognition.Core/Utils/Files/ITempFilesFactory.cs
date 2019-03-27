using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.Files
{
	public interface ITempFilesFactory
	{
		/// <summary>
		/// Makes a reservation of name for new temporary file. The file is considered to be a temporary one,
		/// so it will be automatically deleted when returned object (TemporaryFile) gets disposed or finalized. 
		/// </summary>
		/// <param name="prefix">The prefix of the name of the file being created</param>
		/// <param name="extension">The suffix (extension) of the name of the file being created</param>
		/// <returns></returns>
		[NotNull]
		TemporaryFile NewTempFile([NotNull] string prefix, string extension);
	}
}