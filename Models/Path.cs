namespace SudokuBlazor.Models
{
    public class Path
    {
        public readonly string path;
        public readonly double strokeWidth;
        public readonly double opacity;
        public readonly string color;

        public Path(string path, double strokeWidth, double opacity = 1.0, string color = "#000")
        {
            this.path = path;
            this.strokeWidth = strokeWidth;
            this.opacity = opacity;
            this.color = color;
        }
    }
}
