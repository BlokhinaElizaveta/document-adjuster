using System.IO;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.ResourceLoader
{
	/// <summary>
	///  Generic interface for resource loader service
	/// </summary>
	public interface IResourceLoader
	{
		/// <summary>
		/// Retrieves given resource as a byte array. In case resource is missing the null is returned
		/// </summary>
		/// <param name="name">Name of the resource</param>
		/// <param name="bypassCache">If true, then the requested content should not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		byte[] LoadResource(string name, bool bypassCache = false);

		/// <summary>
		/// Retrieves given resource as a stream. In case resource is missing the null is returned
		/// </summary>
		/// <param name="name">Name of the resource</param>
		/// <param name="bypassCache">If true, then the requested content should not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		Stream LoadResourceAsStream(string name, bool bypassCache = false);
	}
}