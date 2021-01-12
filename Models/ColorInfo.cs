namespace SudokuBlazor.Models
{
    public record ColorInfo(string Name, string HexValue)
    {
        public string Style => $"background-color: {HexValue};";
    }
}
