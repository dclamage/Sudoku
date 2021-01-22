using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SudokuBlazor.Models;
using SudokuBlazor.Shared;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public class DiagonalGroupConstraint : Constraint
    {
        public enum Direction
        {
            Positive,
            Negative,
            Max
        }

        public Direction Dir { get; set; }

        public DiagonalGroupConstraint(Direction dir)
        {
            Dir = dir;
        }

        public DiagonalGroupConstraint(JObject jobject)
        {
            int version = (int)jobject["v"];
            if (version != 1)
            {
                return;
            }
            Dir = (Direction)(int)jobject["dir"];
        }

        public override string Serialized => new JObject()
        {
            ["type"] = "DiagonalGroup",
            ["v"] = 1,
            ["dir"] = (int)Dir,
        }.ToString();

        public override string Name => "Diagonal Group";

        public override string SpecificName => $"{Dir} Diagonal";

        public override string Icon => "";

        public override string Rules => $"Cells along the {Dir.ToString().ToLowerInvariant()} diagonal line cannot contain repeated digits.";

        public override SvgPath[] SvgPaths
        {
            get
            {
                if (_svgContent != null)
                {
                    return _svgContent;
                }

                string path = null;
                switch (Dir)
                {
                    case Direction.Positive:
                        path = $"M{SudokuConstants.viewboxSize},0L0,{SudokuConstants.viewboxSize}";
                        break;
                    case Direction.Negative:
                        path = $"M0,0L{SudokuConstants.viewboxSize},{SudokuConstants.viewboxSize}";
                        break;
                }
                if (path != null)
                {
                    _svgContent = new SvgPath[] { new SvgPath(path, 2.0) };
                }
                return _svgContent;
            }
        }
        private SvgPath[] _svgContent = null;

        public override bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val) => true;

        public override bool MarkConflicts(int[] values, bool[] conflicts) => true;

        public override LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing) => LogicResult.None;

        public override List<(int, int)> Group
        {
            get
            {
                if (_group != null)
                {
                    return _group;
                }

                _group = new(9);
                switch (Dir)
                {
                    case Direction.Positive:
                        for (int i = HEIGHT - 1, j = 0; j < WIDTH; i--, j++)
                        {
                            _group.Add((i, j));
                        }
                        break;
                    case Direction.Negative:
                        for (int i = 0, j = 0; i < HEIGHT; i++, j++)
                        {
                            _group.Add((i, j));
                        }
                        break;
                }
                return _group;
            }
        }
        private List<(int, int)> _group = null;
    }
}
