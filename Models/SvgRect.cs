namespace SudokuBlazor.Models
{
    public class SvgRect
    {
        public readonly double x;
        public readonly double y;
        public readonly double width;
        public readonly double height;
        public readonly double strokeWidth;
        public readonly double opacity;
        public readonly string color;

        public SvgRect(double x, double y, double width, double height, double strokeWidth, double opacity = 1.0, string color = null)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
            this.strokeWidth = strokeWidth;
            this.opacity = opacity;
            this.color = color;
        }
    }
}
