using System.Collections.Generic;
using System.Linq;

namespace Kontur.Recognition.GeometryModel.Transform
{
	public class Composite : ITransform
	{
		private readonly IList<ITransform> transformations;

		public IList<ITransform> Transformations { get { return transformations; } }

		public Composite()
		{
			transformations = new List<ITransform>();
		}

		public Composite(Composite transformation) : this()
		{
			Add(transformation);
		}

		public Composite(IList<ITransform> transformations)
		{
			this.transformations = transformations;
		}

		public void Add(Composite transformation)
		{
			transformations.Add(transformation);
		}

		public Point Transform(Point point)
		{
			return transformations.Aggregate(point, (current, transformation) => transformation.Transform(current));
		}

		public ITransform Reverse()
		{
			return new Composite(transformations.Reverse().Select(t => t.Reverse()).ToList());
		}
	}
}