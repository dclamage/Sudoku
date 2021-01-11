using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SudokuBlazor.Models
{
    public record ColorInfo(string Name, string HexValue)
    {
        public string Style => $"background-color: {HexValue};";
    }
}
