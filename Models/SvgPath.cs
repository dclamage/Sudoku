namespace SudokuBlazor.Models
{
    public class SvgPath
    {
        public readonly string path;
        public readonly double strokeWidth;
        public readonly double opacity;
        public readonly string color;
        public readonly string strokeDashArray;

        public SvgPath(string path, double strokeWidth, double opacity = 1.0, string color = "#000", string strokeDashArray = "1,0")
        {
            this.path = path;
            this.strokeWidth = strokeWidth;
            this.opacity = opacity;
            this.color = color;
            this.strokeDashArray = strokeDashArray;
        }
    }
}
