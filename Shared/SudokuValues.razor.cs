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
        private const string givenColor = "#000";
        private const string filledColor = "#575757";
        private const string conflictedColor = "#ee0000";

        // State
        private bool isDirty = true;
        private readonly Dictionary<(int, int, int), Text> cellText = new Dictionary<(int, int, int), Text>();
        private readonly bool[] cellIsGiven = new bool[81];
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

        private record Snapshot(int[] CellValues, uint[] CellCornerMarks, uint[] CellCenterMarks);

        public object TakeSnapshot()
        {
            return new Snapshot(
                (int[])cellValues.Clone(),
                (uint[])cellCornerMarks.Clone(),
                (uint[])cellCenterMarks.Clone()
            );
        }

        public void RestoreSnapshot(object snapshotObj)
        {
            if (snapshotObj != null && snapshotObj is Snapshot snapshot)
            {
                for (int cellIndex = 0; cellIndex < 81; cellIndex++)
                {
                    if (cellValues[cellIndex] != snapshot.CellValues[cellIndex])
                    {
                        SetCellValue(cellIndex, snapshot.CellValues[cellIndex]);
                        SetDirty();
                    }

                    uint prevCornerMarks = cellCornerMarks[cellIndex];
                    uint newCornerMarks = snapshot.CellCornerMarks[cellIndex];
                    if (prevCornerMarks != newCornerMarks)
                    {
                        cellCornerMarks[cellIndex] = newCornerMarks;
                        if (cellValues[cellIndex] == 0)
                        {
                            for (int value = 1; value <= 9; value++)
                            {
                                cellText.Remove((cellIndex, 1, value));
                            }

                            ReRenderCornerMarks(cellIndex);
                            SetDirty();
                        }
                    }

                    uint prevCenterMarks = cellCenterMarks[cellIndex];
                    uint newCenterMarks = snapshot.CellCenterMarks[cellIndex];
                    if (prevCenterMarks != newCenterMarks)
                    {
                        cellCenterMarks[cellIndex] = newCenterMarks;
                        if (cellValues[cellIndex] == 0)
                        {
                            for (int value = 1; value <= 9; value++)
                            {
                                cellText.Remove((cellIndex, 2, value));
                            }

                            ReRenderCenterMarks(cellIndex);
                            SetDirty();
                        }
                    }
                }
            }
        }

        public void SetGiven(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                return;
            }
            ClearCell(cellIndex, fullClear: true);

            cellIsGiven[cellIndex] = true;
            cellValues[cellIndex] = value;
            cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, givenColor);
            SetDirty();
            CheckConflicts();
        }

        public bool SetCellValue(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                return ClearCell(cellIndex);
            }

            if (cellIsGiven[cellIndex] || cellValues[cellIndex] == value)
            {
                return false;
            }

            if (cellValues[cellIndex] == 0)
            {
                ClearPencilmarkVisuals(cellIndex);
            }

            cellValues[cellIndex] = value;
            cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, filledColor);
            SetDirty();
            CheckConflicts();
            return true;
        }

        private static Text CreateFilledText(int cellIndex, int value, string color) => new Text(
            x: (cellIndex % 9 + 0.5) * cellRectWidth,
            y: (cellIndex / 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
            fontSize: valueFontSize,
            fontFamily: fontFamily,
            color: color,
            text: value.ToString()
        );

        public bool ToggleCornerMark(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                return ClearCell(cellIndex);
            }

            if (cellIsGiven[cellIndex] || cellValues[cellIndex] != 0)
            {
                return false;
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
            return true;
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
                    color: filledColor,
                    text: value.ToString()
                );
                cornerIndex++;
            }
        }

        public bool ToggleCenterMark(int cellIndex, int value)
        {
            if (value < 1 || value > 9)
            {
                return ClearCell(cellIndex);
            }

            if (cellIsGiven[cellIndex] || cellValues[cellIndex] != 0)
            {
                return false;
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
            return true;
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

        public bool ClearCell(int cellIndex, bool fullClear = false)
        {
            if (cellIsGiven[cellIndex])
            {
                return false;
            }

            if (cellValues[cellIndex] != 0)
            {
                cellText.Remove((cellIndex, 0, 0));
                cellValues[cellIndex] = 0;
                SetDirty();
                CheckConflicts();
                ReRenderCenterMarks(cellIndex);
                ReRenderCornerMarks(cellIndex);
                if (!fullClear)
                {
                    return true;
                }
            }

            bool hadChange = false;
            if (cellCornerMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    cellText.Remove((cellIndex, 1, v));
                }
                cellCornerMarks[cellIndex] = 0;
                SetDirty();
                hadChange = true;
            }

            if (cellCenterMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    cellText.Remove((cellIndex, 2, v));
                }
                cellCenterMarks[cellIndex] = 0;
                SetDirty();
                hadChange = true;
            }

            return hadChange;
        }

        public void ClearPencilmarkVisuals(int cellIndex)
        {
            if (cellCornerMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    if (cellText.Remove((cellIndex, 1, v)))
                    {
                        SetDirty();
                    }
                }
            }

            if (cellCenterMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    if (cellText.Remove((cellIndex, 2, v)))
                    {
                        SetDirty();
                    }
                }
            }
        }

        public int GetCellValue(int cellIndex)
        {
            return cellValues[cellIndex];
        }

        public void CheckConflicts()
        {
            int[] digitCount = new int[9];
            bool[] cellIsConflicted = new bool[81];

            // Rows
            for (int i = 0; i < 9; i++)
            {
                Array.Clear(digitCount, 0, 9);
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = i * 9 + j;
                    int curValue = cellValues[cellIndex];
                    if (curValue > 0)
                    {
                        digitCount[curValue - 1]++;
                    }
                }
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = i * 9 + j;
                    int curValue = cellValues[cellIndex];
                    if (curValue > 0)
                    {
                        if (digitCount[curValue - 1] > 1)
                        {
                            cellIsConflicted[cellIndex] = true;
                        }
                    }
                }
            }

            // Cols
            for (int j = 0; j < 9; j++)
            {
                Array.Clear(digitCount, 0, 9);
                for (int i = 0; i < 9; i++)
                {
                    int cellIndex = i * 9 + j;
                    int curValue = cellValues[cellIndex];
                    if (curValue > 0)
                    {
                        digitCount[curValue - 1]++;
                    }
                }
                for (int i = 0; i < 9; i++)
                {
                    int cellIndex = i * 9 + j;
                    int curValue = cellValues[cellIndex];
                    if (curValue > 0)
                    {
                        if (digitCount[curValue - 1] > 1)
                        {
                            cellIsConflicted[cellIndex] = true;
                        }
                    }
                }
            }

            // Boxes
            const int boxRowSpan = 27;
            for (int bi = 0; bi < 3; bi++)
            {
                for (int bj = 0; bj < 3; bj++)
                {
                    int baseBoxIndex = (bi * boxRowSpan) + (bj * 3);
                    Array.Clear(digitCount, 0, 9);
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int cellIndex = baseBoxIndex + i * 9 + j;
                            int curValue = cellValues[cellIndex];
                            if (curValue > 0)
                            {
                                digitCount[curValue - 1]++;
                            }
                        }
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            int cellIndex = baseBoxIndex + i * 9 + j;
                            int curValue = cellValues[cellIndex];
                            if (curValue > 0)
                            {
                                if (digitCount[curValue - 1] > 1)
                                {
                                    cellIsConflicted[cellIndex] = true;
                                }
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < 81; i++)
            {
                if (cellValues[i] != 0)
                {
                    SetConflict(i, cellIsConflicted[i]);
                }
            }
        }

        public void SetConflict(int cellIndex, bool conflict)
        {
            Text existingText = cellText[(cellIndex, 0, 0)];
            string desiredColor = conflict ? conflictedColor : (cellIsGiven[cellIndex] ? givenColor : filledColor);
            if (existingText.color != desiredColor)
            {
                cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, cellValues[cellIndex], desiredColor);
                SetDirty();
            }
        }
    }
}
