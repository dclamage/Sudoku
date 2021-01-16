using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SudokuBlazor.Solver
{
    public record SudokuGroup(string Name, List<(int, int)> Cells)
    {
        public override string ToString() => Name;
    }
}
