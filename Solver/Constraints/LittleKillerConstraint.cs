using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public class LittleKillerConstraint : Constraint
    {
        public enum Direction
        {
            UpRight,
            UpLeft,
            DownRight,
            DownLeft,
        }

        public readonly (int, int) cellStart;
        public readonly Direction direction;
        public readonly int sum;
        public readonly HashSet<(int, int)> cells;
        
        public LittleKillerConstraint((int, int) cellStart, Direction direction, int sum)
        {
            this.cellStart = cellStart;
            this.direction = direction;
            this.sum = sum;
            cells = new HashSet<(int, int)>();
            (int, int) cell = cellStart;
            while (cell.Item1 >= 0 && cell.Item1 < HEIGHT && cell.Item2 >= 0 && cell.Item2 < WIDTH)
            {
                cells.Add(cell);
                switch (direction)
                {
                    case Direction.UpRight:
                        cell = (cell.Item1 - 1, cell.Item2 + 1);
                        break;
                    case Direction.UpLeft:
                        cell = (cell.Item1 - 1, cell.Item2 - 1);
                        break;
                    case Direction.DownRight:
                        cell = (cell.Item1 + 1, cell.Item2 + 1);
                        break;
                    case Direction.DownLeft:
                        cell = (cell.Item1 + 1, cell.Item2 - 1);
                        break;
                }
            }
        }

        public LittleKillerConstraint(JObject jobject)
        {
            int version = (int)jobject["v"];
            if (version == 1)
            {
                cellStart = CellValue((string)jobject["cellStart"]);
                direction = (Direction)(int)jobject["direction"];
                sum = (int)jobject["sum"];
            }
        }

        public override string Serialized => new JObject()
        {
            ["type"] = "LittleKiller",
            ["v"] = 1,
            ["cellStart"] = CellName(cellStart),
            ["direction"] = (int)direction,
            ["sum"] = sum,
        }.ToString();

        public override string Name => "Little Killer";

        public override string SpecificName => $"Little Killer at {CellName(cellStart)}";

        public override string Icon => "";

        public override string Rules => "Numbers outside the grid indicate the sum of all digits along the indicated diagonal.";

        public override bool MarkConflicts(int[] values, bool[] conflicts)
        {
            if (cells.Any(cell => values[FlatIndex(cell)] == 0))
            {
                return false;
            }

            int cellSum = cells.Sum(cell => values[FlatIndex(cell)]);
            if (cellSum != sum)
            {
                foreach (var cell in cells)
                {
                    conflicts[FlatIndex(cell)] = true;
                }
                return true;
            }
            return false;
        }

        public override LogicResult InitCandidates(SudokuSolver sudokuSolver)
        {
            var board = sudokuSolver.Board;

            bool changed = false;
            int maxValue = sum - cells.Count + 1;
            if (maxValue < MAX_VALUE)
            {
                uint maxValueMask = (1u << maxValue) - 1;
                foreach (var cell in cells)
                {
                    uint cellMask = board[cell.Item1, cell.Item2];
                    uint newCellMask = cellMask & maxValueMask;
                    if (newCellMask == 0)
                    {
                        return LogicResult.Invalid;
                    }

                    if (newCellMask != cellMask)
                    {
                        board[cell.Item1, cell.Item2] = newCellMask;
                        changed = true;
                    }
                }
            }
            return changed ? LogicResult.Changed : LogicResult.None;
        }

        public override bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val)
        {
            if (!cells.Contains((i, j)))
            {
                return false;
            }

            if (cells.All(cell => sudokuSolver.IsValueSet(cell.Item1, cell.Item2)))
            {
                var board = sudokuSolver.Board;
                int actualSum = cells.Select(cell => GetValue(board[cell.Item1, cell.Item2])).Sum();
                return sum == actualSum;
            }
            return true;
        }

        public override LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing)
        {
            if (isBruteForcing)
            {
                return LogicResult.None;
            }

            var board = sudokuSolver.Board;
            var cellMasks = cells.Select(cell => board[cell.Item1, cell.Item2]);

            int setValueSum = cellMasks.Where(mask => IsValueSet(mask)).Select(mask => GetValue(mask)).Sum();
            if (setValueSum > sum)
            {
                logicalStepDescription.Append($"Sum of filled values is too large.");
                return LogicResult.Invalid;
            }

            // Ensure the sum is still possible
            var unsetMasks = cellMasks.Where(mask => !IsValueSet(mask)).ToArray();
            if (unsetMasks.Length == 0)
            {
                if (setValueSum != sum)
                {
                    logicalStepDescription.Append($"Sum of values is incorrect.");
                    return LogicResult.Invalid;
                }
            }
            else if (unsetMasks.Length == 1)
            {
                int exactCellValue = sum - setValueSum;
                if (exactCellValue <= 0 || exactCellValue > MAX_VALUE || !HasValue(unsetMasks[0], exactCellValue))
                {
                    logicalStepDescription.Append($"The final cell cannot fulfill the sum.");
                    return LogicResult.Invalid;
                }
            }
            else
            {
                int minSum = setValueSum + unsetMasks.Select(mask => MinValue(mask)).Sum();
                int maxSum = setValueSum + unsetMasks.Select(mask => MaxValue(mask)).Sum();
                if (minSum > sum || maxSum < sum)
                {
                    logicalStepDescription.Append($"The sum is no longer possible.");
                    return LogicResult.Invalid;
                }
            }

            return LogicResult.None;
        }
    }
}
