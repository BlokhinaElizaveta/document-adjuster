namespace Kontur.Recognition.GeometryModel.Transform
{
	public interface ITransform
	{
		Point Transform(Point point);
		ITransform Reverse();
	}
}