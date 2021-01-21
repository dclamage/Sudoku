using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using SudokuBlazor.Models;
using SudokuBlazor.Solver;
using SudokuBlazor.Solver.Constraints;

namespace SudokuBlazor.Shared
{
    partial class SudokuValues : ComponentDirtyRender
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
        enum ValueType
        {
            Filled,
            Corner,
            Center
        }
        private readonly Dictionary<(int, ValueType, int), Text> cellText = new Dictionary<(int, ValueType, int), Text>();
        private readonly bool[] cellIsGiven = new bool[81];
        private readonly int[] cellValues = new int[81];
        private readonly uint[] cellCenterMarks = new uint[81];
        private readonly uint[] cellCornerMarks = new uint[81];
        private readonly Dictionary<int, Constraint> constraints = new();
        public string[] ConstraintStrings => constraints.Values.Select(c => c.Serialized).ToArray();

        // Tables
        private readonly (double, double)[] cornerMarkOffsets = new (double, double)[9];

        public int[] GetCellValues(bool respectFilledMarks)
        {
            if (respectFilledMarks)
            {
                return (int[])cellValues.Clone();
            }

            int[] result = new int[81];
            for (int i = 0; i < 81; i++)
            {
                result[i] = cellIsGiven[i] ? cellValues[i] : 0;
            }
            return result;
        }
        public uint[] GetCellCandidates(bool respectFilledMarks, bool respectCenterMarks)
        {
            uint[] cellCandidates = new uint[81];
            for (int i = 0; i < 81; i++)
            {
                if (cellValues[i] != 0)
                {
                    cellCandidates[i] = respectFilledMarks || cellIsGiven[i] ? SolverUtility.ValueMask(cellValues[i]) | SolverUtility.valueSetMask : SolverUtility.ALL_VALUES_MASK;
                }
                else
                {
                    cellCandidates[i] = (!respectCenterMarks || cellCenterMarks[i] == 0) ? SolverUtility.ALL_VALUES_MASK : cellCenterMarks[i];
                }
            }
            return cellCandidates;
        }

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

        private record Snapshot(int[] CellValues, bool[] CellIsGiven, uint[] CellCornerMarks, uint[] CellCenterMarks);

        public object TakeSnapshot()
        {
            return new Snapshot(
                (int[])cellValues.Clone(),
                (bool[])cellIsGiven.Clone(),
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
                    if (cellValues[cellIndex] != snapshot.CellValues[cellIndex] || cellIsGiven[cellIndex] != snapshot.CellIsGiven[cellIndex])
                    {
                        SetCellValue(cellIndex, snapshot.CellValues[cellIndex], snapshot.CellIsGiven[cellIndex], true);
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
                                cellText.Remove((cellIndex, ValueType.Corner, value));
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
                                cellText.Remove((cellIndex, ValueType.Center, value));
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
            ClearCell(
                cellIndex: cellIndex,
                fullClear: true,
                clearGivens: true);

            cellIsGiven[cellIndex] = true;
            cellValues[cellIndex] = value;
            cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, givenColor);
            SetDirty();
            CheckConflicts();
        }

        public bool SetCellValue(int cellIndex, int value, bool given, bool force = false)
        {
            if (value < 1 || value > 9)
            {
                return ClearCell(
                    cellIndex: cellIndex,
                    fullClear: false,
                    clearGivens: given || force);
            }

            if (cellValues[cellIndex] == value)
            {
                if (given && !cellIsGiven[cellIndex])
                {
                    cellIsGiven[cellIndex] = true;
                    cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, givenColor);
                    SetDirty();
                    return true;
                }
                else if (force && !given && cellIsGiven[cellIndex])
                {
                    cellIsGiven[cellIndex] = false;
                    cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, filledColor);
                    SetDirty();
                    return true;
                }
                return false;
            }

            if (!force && !given && cellIsGiven[cellIndex])
            {
                return false;
            }

            if (cellValues[cellIndex] == 0)
            {
                ClearPencilmarkVisuals(cellIndex);
            }

            cellValues[cellIndex] = value;
            cellIsGiven[cellIndex] = given;
            cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, value, given ? givenColor : filledColor);
            SetDirty();
            CheckConflicts();
            return true;
        }

        public bool SetAllCellValues(int[] newCellValues)
        {
            bool changed = false;
            for (int cellIndex = 0; cellIndex < cellValues.Length; cellIndex++)
            {
                int newValue = newCellValues[cellIndex];
                if (cellIsGiven[cellIndex] || cellValues[cellIndex] == newValue)
                {
                    continue;
                }

                if (newValue == 0)
                {
                    cellText.Remove((cellIndex, 0, 0));
                    cellValues[cellIndex] = 0;
                    ReRenderCenterMarks(cellIndex);
                    ReRenderCornerMarks(cellIndex);
                }
                else
                {
                    ClearPencilmarkVisuals(cellIndex);
                    cellValues[cellIndex] = newValue;
                    cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, newValue, filledColor);
                }
                changed = true;
            }

            if (changed)
            {
                SetDirty();
                CheckConflicts();
            }
            return changed;
        }

        private static Text CreateFilledText(int cellIndex, int value, string color) => new Text(
            x: (cellIndex % 9 + 0.5) * cellRectWidth,
            y: (cellIndex / 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
            fontSize: valueFontSize,
            fontFamily: fontFamily,
            color: color,
            text: value.ToString()
        );

        private bool ToggleMarks(IEnumerable<int> cellIndexes, int value, ValueType type)
        {
            bool dirty = false;

            // An invalid value mean to clear the cell instead.
            if (value < 1 || value > 9)
            {
                foreach (int cellIndex in cellIndexes)
                {
                    dirty = ClearCell(
                        cellIndex: cellIndex,
                        fullClear: false,
                        clearGivens: false);
                }
                return dirty;
            }

            // Only care about cells which aren't given and have no value filled.
            List<int> cellIndexList = cellIndexes.Where(i => !cellIsGiven[i] && cellValues[i] == 0).ToList();
            if (cellIndexList.Count == 0)
            {
                return false;
            }

            uint valueMask = 1u << (value - 1);
            uint[] cellMarks = type == ValueType.Corner ? cellCornerMarks : cellCenterMarks;

            // Determine if the selection is a mix between filled and unfilled cells
            bool haveFilledCell = false;
            bool haveUnfilledCell = false;
            foreach (int cellIndex in cellIndexList)
            {
                if ((cellMarks[cellIndex] & valueMask) == 0)
                {
                    haveUnfilledCell = true;
                }
                else
                {
                    haveFilledCell = true;
                }
            }

            if (haveUnfilledCell)
            {
                // If any unfilled cells exist, then the prefence is to filled just those cells
                foreach (int cellIndex in cellIndexList)
                {
                    if ((cellMarks[cellIndex] & valueMask) == 0)
                    {
                        cellMarks[cellIndex] |= valueMask;
                        if (type == ValueType.Corner)
                        {
                            ReRenderCornerMarks(cellIndex);
                        }
                        else
                        {
                            ReRenderCenterMarks(cellIndex);
                        }
                        dirty = true;
                    }
                }
            }
            else if (haveFilledCell)
            {
                // Otherwise, clear all cells
                foreach (int cellIndex in cellIndexList)
                {
                    cellMarks[cellIndex] &= ~valueMask;
                    cellText.Remove((cellIndex, type, value));
                    if (type == ValueType.Corner)
                    {
                        ReRenderCornerMarks(cellIndex);
                    }
                    else
                    {
                        ReRenderCenterMarks(cellIndex);
                    }
                }
                dirty = true;
            }

            if (dirty)
            {
                SetDirty();
            }
            return dirty;
        }

        public enum SingleValueBehavior
        {
            AlwaysKeepAsPencilmark,
            RespectValueSetBit,
            SingleValueAlwaysSetAsValue
        }
        public bool SetAllCenterPencilMarks(uint[] candidates, SingleValueBehavior singleValueBehavior = SingleValueBehavior.AlwaysKeepAsPencilmark)
        {
            bool changed = false;
            for (int cellIndex = 0; cellIndex < cellValues.Length; cellIndex++)
            {
                uint curCandidates = cellCenterMarks[cellIndex];
                uint newCandidates = candidates[cellIndex];
                if (cellIsGiven[cellIndex] || cellValues[cellIndex] != 0)
                {
                    continue;
                }

                if (singleValueBehavior == SingleValueBehavior.RespectValueSetBit && SolverUtility.IsValueSet(candidates[cellIndex]) ||
                    singleValueBehavior == SingleValueBehavior.SingleValueAlwaysSetAsValue && SolverUtility.ValueCount(newCandidates) == 1)
                {
                    int newValue = SolverUtility.GetValue(newCandidates);
                    ClearPencilmarkVisuals(cellIndex);
                    cellValues[cellIndex] = newValue;
                    cellText[(cellIndex, 0, 0)] = CreateFilledText(cellIndex, newValue, filledColor);
                    changed = true;
                    continue;
                }

                bool centerMarksChanged = false;
                for (int value = 1; value <= 9; value++)
                {
                    uint valueMask = SolverUtility.ValueMask(value);
                    bool curHaveValue = (curCandidates & valueMask) != 0;
                    bool newHaveValue = (newCandidates & valueMask) != 0;
                    if (curHaveValue != newHaveValue)
                    {
                        if (curHaveValue)
                        {
                            cellText.Remove((cellIndex, ValueType.Center, value));
                        }
                        centerMarksChanged = true;
                    }
                }

                if (centerMarksChanged)
                {
                    cellCenterMarks[cellIndex] = newCandidates & ~SolverUtility.valueSetMask;
                    ReRenderCenterMarks(cellIndex);
                    changed = true;
                }
            }

            if (changed)
            {
                SetDirty();
            }
            return changed;
        }

        public bool ToggleCornerMarks(IEnumerable<int> cellIndexes, int value)
        {
            return ToggleMarks(cellIndexes, value, ValueType.Corner);
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

                cellText[(cellIndex, ValueType.Corner, value)] = new Text(
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

        public bool ToggleCenterMarks(IEnumerable<int> cellIndexes, int value)
        {
            return ToggleMarks(cellIndexes, value, ValueType.Center);
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

                cellText[(cellIndex, ValueType.Center, value)] = new Text(
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

        public bool ClearCell(int cellIndex, bool fullClear, bool clearGivens)
        {
            if (!clearGivens && cellIsGiven[cellIndex])
            {
                return false;
            }

            if (cellValues[cellIndex] != 0)
            {
                cellText.Remove((cellIndex, 0, 0));
                cellValues[cellIndex] = 0;
                cellIsGiven[cellIndex] = false;
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
                    cellText.Remove((cellIndex, ValueType.Corner, v));
                }
                cellCornerMarks[cellIndex] = 0;
                SetDirty();
                hadChange = true;
            }

            if (cellCenterMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    cellText.Remove((cellIndex, ValueType.Center, v));
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
                    if (cellText.Remove((cellIndex, ValueType.Corner, v)))
                    {
                        SetDirty();
                    }
                }
            }

            if (cellCenterMarks[cellIndex] != 0)
            {
                for (int v = 1; v <= 9; v++)
                {
                    if (cellText.Remove((cellIndex, ValueType.Center, v)))
                    {
                        SetDirty();
                    }
                }
            }
        }

        public bool ResetToGivens()
        {
            bool changed = false;
            for (int i = 0; i < 81; i++)
            {
                changed |= ClearCell(i, true, false);
            }
            return changed;
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

            foreach (var constraint in constraints.Values)
            {
                var group = constraint.Group;
                if (group != null)
                {
                    Array.Clear(digitCount, 0, 9);
                    foreach (var cell in constraint.Group)
                    {
                        int cellIndex = SolverUtility.FlatIndex(cell);
                        int curValue = cellValues[cellIndex];
                        if (curValue > 0)
                        {
                            digitCount[curValue - 1]++;
                        }
                    }
                    foreach (var cell in constraint.Group)
                    {
                        int cellIndex = SolverUtility.FlatIndex(cell);
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
                constraint.MarkConflicts(cellValues, cellIsConflicted);
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

        public void AddConstraint(int id, Constraint constraint)
        {
            constraints[id] = constraint;
            CheckConflicts();
        }

        public void RemoveConstraint(int id)
        {
            constraints.Remove(id);
            CheckConflicts();
        }
    }
}
