using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuValues
    {
        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;
        private const double valueFontSize = cellRectWidth * 3.0 / 4.0;
        private const string fontFamily = "sans-serif";

        // State
        private bool isDirty = true;
        private readonly Text[] cellText = new Text[81];
        private readonly int[] cellValues = new int[81];

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

        public void SetCellValue(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                ClearCell(cellIndex);
                return;
            }

            if (cellValues[cellIndex] == value)
            {
                return;
            }

            cellValues[cellIndex] = value;
            cellText[cellIndex] = new Text(
                x: (cellIndex % 9 + 0.5) * cellRectWidth,
                y: (cellIndex / 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
                fontSize: valueFontSize,
                fontFamily: fontFamily,
                text: value.ToString()
            );
            SetDirty();
        }

        public void ClearCell(int cellIndex)
        {
            cellValues[cellIndex] = 0;
            if (cellText[cellIndex] != null)
            {
                cellText[cellIndex] = null;
                SetDirty();
            }
        }

        public int GetCellValue(int cellIndex)
        {
            return cellValues[cellIndex];
        }
    }
}
