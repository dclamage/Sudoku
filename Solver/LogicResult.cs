using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SudokuBlazor.Solver
{
    public enum LogicResult
    {
        None,
        Changed,
        Invalid,
        PuzzleComplete
    }
}
