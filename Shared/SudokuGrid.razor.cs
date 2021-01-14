using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuGrid : ComponentRenderOnce
    {
        // Constants
        private const double boxStrokeWidth = 8.0;
        private const double cellStrokeWidth = 2.0;
        private const double borderWidth = SudokuConstants.viewboxSize - boxStrokeWidth;
        private const double innerBoxWidth = (SudokuConstants.viewboxSize - boxStrokeWidth) / 3.0;
        private const double innerCellWidth = (SudokuConstants.viewboxSize - cellStrokeWidth) / 9.0;

        // State
        private List<Path> paths = new List<Path>();

        protected override void OnInitialized()
        {
            InitRects();
            base.OnInitialized();
        }

        protected void InitRects()
        {
            if (paths.Count == 0)
            {
                StringBuilder cellPath = new StringBuilder();
                StringBuilder boxPath = new StringBuilder();
                boxPath.Append($"M0,0H{borderWidth}V{borderWidth}H0Z");
                for (int i = 1; i < 9; i++)
                {
                    if ((i % 3) != 0)
                    {
                        cellPath.Append($"M{0},{i * innerCellWidth}H{SudokuConstants.viewboxSize}M{i * innerCellWidth},{0}V{SudokuConstants.viewboxSize}");
                    }
                    else
                    {
                        int bi = i / 3;
                        boxPath.Append($"M{0},{bi * innerBoxWidth}H{SudokuConstants.viewboxSize}M{bi * innerBoxWidth},{0}V{SudokuConstants.viewboxSize}");
                    }
                }
                paths.Add(new Path(cellPath.ToString(), cellStrokeWidth));
                paths.Add(new Path(boxPath.ToString(), boxStrokeWidth));
            }
        }
    }
}
