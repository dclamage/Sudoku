﻿using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public class KingConstraint : Constraint
    {
        public KingConstraint() { }

        public KingConstraint(JObject _) { }

        public override string Serialized => new JObject()
        {
            ["type"] = "King",
            ["v"] = 1,
        }.ToString();

        public override string Name => "Anti-King";

        public override string Icon => "<path d=\"m 11.99975,1.71 c -1.409569,0 -2.571376,1.1618064 -2.571376,2.571375 0,0.9642656 0.542401,1.8113459 1.339258,2.2499531 L 8.249828,11.566937 4.3124101,8.754496 C 4.8280247,8.2824078 5.1427499,7.5993863 5.1427499,6.8527499 5.1427499,5.4431812 3.9809436,4.281375 2.571375,4.281375 1.1618064,4.281375 0,5.4431812 0,6.8527499 0,8.0078596 0.7935101,8.968778 1.8481758,9.290199 l 1.5803241,8.705175 V 22.281 H 20.571 V 17.995374 L 22.151324,9.290199 C 23.205989,8.968778 23.9995,8.0078596 23.9995,6.8527499 c 0,-1.4095687 -1.161807,-2.5713749 -2.571376,-2.5713749 -1.409568,0 -2.571374,1.1618062 -2.571374,2.5713749 0,0.7466364 0.314725,1.4296579 0.830339,1.9017461 L 15.749672,11.566937 13.231867,6.5313281 C 14.028725,6.0927209 14.571125,5.2456406 14.571125,4.281375 14.571125,2.8718064 13.409318,1.71 11.99975,1.71 Z m 0,1.71425 c 0.482133,0 0.857125,0.3749921 0.857125,0.857125 0,0.4821327 -0.374992,0.8571249 -0.857125,0.8571249 -0.482133,0 -0.857125,-0.3749922 -0.857125,-0.8571249 0,-0.4821329 0.374992,-0.857125 0.857125,-0.857125 z M 2.571375,5.9956249 c 0.4821327,0 0.8571249,0.3749922 0.8571249,0.857125 0,0.4821328 -0.3749922,0.857125 -0.8571249,0.857125 -0.4821329,0 -0.857125,-0.3749922 -0.857125,-0.857125 0,-0.4821328 0.3749921,-0.857125 0.857125,-0.857125 z m 18.856749,0 c 0.482133,0 0.857125,0.3749922 0.857125,0.857125 0,0.4821328 -0.374992,0.857125 -0.857125,0.857125 -0.482132,0 -0.857124,-0.3749922 -0.857124,-0.857125 0,-0.4821328 0.374992,-0.857125 0.857124,-0.857125 z m -9.428374,1.9285312 2.65173,5.3034609 1.285688,0.321422 4.285624,-3.053508 -1.205332,6.642718 H 4.982039 L 3.7767069,10.495531 8.062332,13.549039 9.348019,13.227617 Z M 5.1427499,18.8525 H 18.85675 v 1.71425 H 5.1427499 Z\" />";

        public override string Rules => "Cells which are a chess king's move apart cannot contain the same digit.";

        public override bool MarkConflicts(int[] values, bool[] conflicts) => MarkConflictsBasedOnSeenCells(values, conflicts);

        public override bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val) => EnforceConstraintBasedOnSeenCells(sudokuSolver, i, j, val);

        public override IEnumerable<(int, int)> SeenCells((int, int) cell)
        {
            var (i, j) = cell;
            if (i - 1 >= 0 && j - 1 >= 0)
            {
                yield return (i - 1, j - 1);
            }
            if (i - 1 >= 0 && j + 1 < WIDTH)
            {
                yield return (i - 1, j + 1);
            }
            if (i + 1 < HEIGHT && j - 1 >= 0)
            {
                yield return (i + 1, j - 1);
            }
            if (i + 1 < HEIGHT && j + 1 < WIDTH)
            {
                yield return (i + 1, j + 1);
            }
        }

        public override LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing)
        {
            return LogicResult.None;
        }
    }
}
