using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuValues
    {
        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;
        private const double valueFontSize = cellRectWidth * 3.0 / 4.0;
        private const double markupFontSize = cellRectWidth * 1.0 / 4.0;
        private const double fontWidthHeightRatio = 0.6;
        private const string fontFamily = "sans-serif";

        // State
        private bool isDirty = true;
        private readonly Dictionary<(int, int, int), Text> cellText = new Dictionary<(int, int, int), Text>();
        private readonly int[] cellValues = new int[81];
        private readonly uint[] cellCenterMarks = new uint[81];
        private readonly uint[] cellCornerMarks = new uint[81];

        // Tables
        private readonly (double, double)[] cornerMarkOffsets = new (double, double)[9];

        protected override void OnInitialized()
        {
            const double offsetBase = cellRectWidth / 12.0;
            const double offsetX0 = 3.0 * offsetBase;
            const double offsetX1 = 6.0 * offsetBase;
            const double offsetX2 = 9.0 * offsetBase;
            const double offsetY0 = 3.0 * offsetBase;
            const double offsetY1 = 6.0 * offsetBase;
            const double offsetY2 = 9.0 * offsetBase;
            cornerMarkOffsets[0] = (offsetX0, offsetY0);
            cornerMarkOffsets[1] = (offsetX2, offsetY0);
            cornerMarkOffsets[2] = (offsetX0, offsetY2);
            cornerMarkOffsets[3] = (offsetX2, offsetY2);
            cornerMarkOffsets[4] = (offsetX1, offsetY0);
            cornerMarkOffsets[5] = (offsetX1, offsetY2);
            cornerMarkOffsets[6] = (offsetX0, offsetY1);
            cornerMarkOffsets[7] = (offsetX1, offsetY1);
            cornerMarkOffsets[8] = (offsetX2, offsetY1);
        }

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

            ClearCell(cellIndex);
            cellValues[cellIndex] = value;
            cellText[(cellIndex, 0, 0)] = new Text(
                x: (cellIndex % 9 + 0.5) * cellRectWidth,
                y: (cellIndex / 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
                fontSize: valueFontSize,
                fontFamily: fontFamily,
                color: "#000",
                text: value.ToString()
            );
            SetDirty();
        }

        public void ToggleCornerMark(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                ClearCell(cellIndex);
                return;
            }

            if (cellValues[cellIndex] != 0)
            {
                return;
            }

            uint valueMask = 1u << (value - 1);
            if ((cellCornerMarks[cellIndex] & valueMask) != 0)
            {
                cellCornerMarks[cellIndex] &= ~valueMask;
                cellText.Remove((cellIndex, 1, value));
            }
            else
            {
                cellCornerMarks[cellIndex] |= valueMask;
            }

            ReRenderCornerMarks(cellIndex);
            SetDirty();
        }

        protected void ReRenderCornerMarks(int cellIndex)
        {
            if (cellCornerMarks[cellIndex] == 0)
            {
                return;
            }

            uint curCornerMarks = cellCornerMarks[cellIndex];
            int cornerIndex = 0;
            for (int value = 1; value <= 9; value++)
            {
                uint valueMask = 1u << (value - 1);
                if ((curCornerMarks & valueMask) == 0)
                {
                    continue;
                }

                double cellStartX = (cellIndex % 9) * cellRectWidth;
                double cellStartY = (cellIndex / 9) * cellRectWidth;
                var (offsetX, offsetY) = cornerMarkOffsets[cornerIndex];

                cellText[(cellIndex, 1, value)] = new Text(
                    x: cellStartX + offsetX,
                    y: cellStartY + offsetY,
                    fontSize: markupFontSize,
                    fontFamily: fontFamily,
                    color: "#555",
                    text: value.ToString()
                );
                cornerIndex++;
            }
        }

        public void ToggleCenterMark(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                ClearCell(cellIndex);
                return;
            }

            if (cellValues[cellIndex] != 0)
            {
                return;
            }

            uint valueMask = 1u << (value - 1);
            if ((cellCenterMarks[cellIndex] & valueMask) != 0)
            {
                cellCenterMarks[cellIndex] &= ~valueMask;
                cellText.Remove((cellIndex, 2, value));
            }
            else
            {
                cellCenterMarks[cellIndex] |= valueMask;
            }

            ReRenderCenterMarks(cellIndex);
            SetDirty();
        }

        protected void ReRenderCenterMarks(int cellIndex)
        {
            if (cellCenterMarks[cellIndex] == 0)
            {
                return;
            }

            uint curCenterMarks = cellCenterMarks[cellIndex];
            int numCenterMarks = BitOperations.PopCount(curCenterMarks);
            double fontSize = Math.Min(markupFontSize, (cellRectWidth * 0.8) / numCenterMarks / fontWidthHeightRatio);

            int valueIndex = 0;
            for (int value = 1; value <= 9; value++)
            {
                uint valueMask = 1u << (value - 1);
                if ((curCenterMarks & valueMask) == 0)
                {
                    continue;
                }

                double cellStartX = (cellIndex % 9) * cellRectWidth + cellRectWidth / 2.0;
                double cellStartY = (cellIndex / 9) * cellRectWidth + cellRectWidth / 2.0;
                double offsetX = (-0.5 * numCenterMarks + valueIndex + 0.5) * fontSize * fontWidthHeightRatio;

                cellText[(cellIndex, 2, value)] = new Text(
                    x: cellStartX + offsetX,
                    y: cellStartY,
                    fontSize: fontSize,
                    fontFamily: fontFamily,
                    color: "#555",
                    text: value.ToString()
                );
                valueIndex++;
            }
        }

        public void ClearCell(int cellIndex)
        {
            if (cellValues[cellIndex] != 0)
            {
                cellText.Remove((cellIndex, 0, 0));
                cellValues[cellIndex] = 0;
                SetDirty();
            }

            if (cellCornerMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    cellText.Remove((cellIndex, 1, v));
                }
                cellCornerMarks[cellIndex] = 0;
                SetDirty();
            }

            if (cellCenterMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    cellText.Remove((cellIndex, 2, v));
                }
                cellCenterMarks[cellIndex] = 0;
                SetDirty();
            }
        }

        public int GetCellValue(int cellIndex)
        {
            return cellValues[cellIndex];
        }
    }
}
