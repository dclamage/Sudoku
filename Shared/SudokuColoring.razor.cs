using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuColoring
    {
        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;

        // State
        private bool isDirty = true;

        // UI
        private readonly Rect[] colorRects = new Rect[81];

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
        }

        protected void SetDirty()
        {
            isDirty = true;
            StateHasChanged();
        }

        public void ColorCell(int cellIndex, string color)
        {
            if (cellIndex >= 0 && cellIndex < colorRects.Length)
            {
                if (string.IsNullOrWhiteSpace(color))
                {
                    if (colorRects[cellIndex] != null)
                    {
                        colorRects[cellIndex] = null;
                        SetDirty();
                    }
                }
                else if(colorRects[cellIndex] == null || colorRects[cellIndex].color != color)
                {
                    int i = cellIndex / 9;
                    int j = cellIndex % 9;
                    colorRects[cellIndex] = CreateColorRect(i, j, color);
                    SetDirty();
                }
            }
        }

        protected static Rect CreateColorRect(int i, int j, string color) => new Rect(
            x: j * cellRectWidth,
            y: i * cellRectWidth,
            width: cellRectWidth,
            height: cellRectWidth,
            strokeWidth: 0.0,
            opacity: 1.0,
            color: color
        );
    }
}
