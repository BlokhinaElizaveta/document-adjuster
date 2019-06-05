namespace DocumentAdjuster.Models
{
    // уравнение прямой в полярных координатах 
    // radius = x*cos(angle)+y*sin(angle)
    internal class EquationOfLine
    {
        public int Angle { get; set; }
        public int Radius { get; set; }
    }
}
