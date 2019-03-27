using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.GeometryModel
{
	public abstract class GMElement
	{
		[NotNull]
		private readonly BoundingBox boundingBox;

		public GMElement([NotNull] BoundingBox boundingBox)
		{
			this.boundingBox = boundingBox;
		}

		/// <summary>
		/// Returns position of this element in model as a minimal bounding rectangle with sides parallel to coordinate axes
		/// </summary>
		[NotNull]
		public BoundingBox BoundingBox { get { return boundingBox; } }

		/// <summary>
		/// Returns whether this element occupies any space (i.e. both height and width are non-zero)
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty()
		{
			return boundingBox.IsEmpty();
		}

		/// <summary>
		/// Returns geometry model in which this element is defined (bounding box is specified in that model)
		/// </summary>
		//public abstract ITextGeometryModel GeometryModel { get; }
	}
}
