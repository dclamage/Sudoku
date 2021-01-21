using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public class DisjointGroupsConstraint : Constraint
    {
        public static IEnumerable<DisjointGroupsConstraint> All(int numGroups)
        {
            for (int i = 0; i < numGroups; i++)
            {
                yield return new DisjointGroupsConstraint(i);
            }
        }

        public int GroupIndex { get; set; }

        public DisjointGroupsConstraint(int groupIndex)
        {
            GroupIndex = groupIndex;
        }

        public DisjointGroupsConstraint(JObject jobject)
        {
            int version = (int)jobject["v"];
            if (version != 1)
            {
                return;
            }
            GroupIndex = (int)jobject["groupIndex"];
        }

        public override string Serialized => new JObject()
        {
            ["type"] = "DisjointGroups",
            ["v"] = 1,
            ["groupIndex"] = GroupIndex,
        }.ToString();

        public override string Name => "Disjoint Groups";

        public override string SpecificName => $"Disjoint Group {GroupIndex + 1}";

        public override string Icon => "";

        public override string Rules => "Cells that appear in the same position relative to their region must not contain the same number.";

        public override bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val) => true;

        public override bool MarkConflicts(int[] values, bool[] conflicts) => true;

        public override LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing) => LogicResult.None;

        public override List<(int, int)> Group
        {
            get
            {
                int groupi = GroupIndex / 3;
                int groupj = GroupIndex % 3;

                List<(int, int)> group = new(9);
                for (int boxi = 0; boxi < 3; boxi++)
                {
                    int celli = boxi * 3 + groupi;
                    for (int boxj = 0; boxj < 3; boxj++)
                    {
                        int cellj = boxj * 3 + groupj;
                        group.Add((celli, cellj));
                    }
                }
                return group;
            }
        }
    }
}
