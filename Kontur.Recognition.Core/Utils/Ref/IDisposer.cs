namespace Kontur.Recognition.Utils.Ref
{
	/// <summary>
	/// With this interface it is possible to define external rule to dispose certain object
	/// </summary>
	public interface IDisposer <in TType>
	{
		/// <summary>
		/// Implement this method to provide logic to dispose objects of certain types
		/// </summary>
		/// <param name="obj">The object to dispose</param>
		void Dispose(TType obj);
	}
}
