using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver.Constraints
{
    public abstract class Constraint
    {
        /// <summary>
        /// Returns a serialized version of this constraint so it can be reconstructed later.
        /// </summary>
        public abstract string Serialized { get; }

        /// <summary>
        /// The generic name of this constraint to present to the end-user.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Override if there is a more specific name for this constraint instance, such as "Killer Cage at r1c1".
        /// </summary>
        public virtual string SpecificName => Name;

        /// <summary>
        /// An svg path for the iconic representation of this constraint.
        /// </summary>
        public abstract string Icon { get; }

        /// <summary>
        /// Human-readable rules (in English) describing this constraint, which is presented to the end-user.
        /// </summary>
        public abstract string Rules { get; }

        /// <summary>
        /// Go through the values that aren't currently marked as conflicts, and check if the value conflicts
        /// based on this constraint.
        /// </summary>
        /// <param name="values">The current filled values on the board. 0 is unfilled.</param>
        /// <param name="conflicts">in/out conflicted cells.</param>
        /// <returns>True if any conflicts added. False otherwise.</returns>
        public abstract bool MarkConflicts(int[] values, bool[] conflicts);

        /// <summary>
        /// Return an enumerable of cells which cannot be the same digit as this cell.
        /// Only need to return cells which wouldn't be seen by normal sudoku rules.
        /// Also no need to return any cells if the Group property is used.
        /// </summary>
        /// <param name="cell">The cell which is seeing other cells.</param>
        /// <returns>All cells which are seen by this cell.</returns>
        public virtual IEnumerable<(int, int)> SeenCells((int, int) cell) => Enumerable.Empty<(int, int)>();

        /// <summary>
        /// Called once all constraints are finalized on the board.
        /// This is the initial opportunity to remove candidates from the empty board before any values are set to it.
        /// For example, a two cell killer cage with sum of 10 might remove the 9 candidate from its two cells.
        /// Each constraint gets a round of inits until all of them return LogicResult.None.
        /// </summary>
        /// <returns>
        /// LogicResult.None: Board is unchanged.
        /// LogicResult.Changed: Board is changed.
        /// LogicResult.Invalid: This constraint has made the solve impossible.
        /// LogicResult.PuzzleComplete: Avoid returning this. It is used internally by the solver.
        /// </returns>
        public virtual LogicResult InitCandidates(SudokuSolver sudokuSolver) { return LogicResult.None; }

        /// <summary>
        /// Called when a value has just been set on the board.
        /// The job of this function is twofold:
        ///   1) Remove candidates from any other cells that are no longer possible because this value was set.
        ///   2) Determine if setting this value is a simple rules violation.
        ///   
        /// Avoid complex logic in this function. Just enforcement of the direct, actual rule is advised.
        /// 
        /// There is no need to specifically enforce distinct digits in groups as long as the Groups property is provided.
        /// By the time this function is called, group distinctness will already have been enforced.
        /// 
        /// For example, a nonconsecutive constraint would remove the consecutive candidates from the
        /// cells adjacent to [i,j] and return false if any of those cells end up with no candidates left.
        /// </summary>
        /// <param name="sudokuSolver">The main Sudoku solver.</param>
        /// <param name="i">The row index (0-8)</param>
        /// <param name="j">The col index (0-8)</param>
        /// <param name="val">The value which has been set in the cell (1-9)</param>
        /// <returns>True if the board is still valid; false otherwise.</returns>
        public abstract bool EnforceConstraint(SudokuSolver sudokuSolver, int i, int j, int val);

        /// <summary>
        /// Called during logical solving.
        /// Go through the board and perform a single step of logic related to this constraint.
        /// For example, a nonconsecutive constraint might look for a cell with only two consecutive
        /// candidates left and eliminate those candidates from all adjacent cells.
        /// 
        /// Use your judgement and testing to determine if any of the logic should occur during brute force
        /// solving. The brute force solving boolean is set to true when this logic is not going to be
        /// visible to the end-user and so anything done during brute forcing is only advised if it's faster
        /// than guessing.
        /// 
        /// Do not attempt to do any logic which isn't relevant to this constraint.
        /// </summary>
        /// <param name="sudokuSolver">The Sudoku board.</param>
        /// <param name="logicalStepDescription">If a logical step is found, store a human-readable description of what was performed here.</param>
        /// <param name="isBruteForcing">Whether the solver is currently brute forcing a solution.</param>
        /// <returns>
        /// LogicResult.None: No logic found.
        /// LogicResult.Changed: Logic found which changed the board.
        /// LogicResult.Invalid: Your logic has determined that there are no solutions (such as when removing the last candidate from a cell).
        /// LogicResult.PuzzleComplete: Avoid returning this. It is used internally by the solver.
        /// </returns>
        public abstract LogicResult StepLogic(SudokuSolver sudokuSolver, StringBuilder logicalStepDescription, bool isBruteForcing);

        /// <summary>
        /// Provide a lists of cells in which all digits must be distinct.
        /// For example, a killer cage would provide all its cells.
        /// A little killer clue would provide nothing, as it does not enforce distinctness.
        /// The list contents are expected to remain the same over the lifetime of the object.
        /// </summary>
        public virtual List<(int, int)> Group => null;

        protected static JArray SerializeCells(IEnumerable<(int, int)> cells)
        {
            JArray jarray = new();
            foreach (var cell in cells)
            {
                jarray.Add(SolverUtility.CellName(cell));
            }
            return jarray;
        }

        protected IEnumerable<(int, int)> DeserializeCells(JToken cells)
        {
            foreach (var cell in cells)
            {
                yield return SolverUtility.CellValue((string)cell);
            }
        }

        /// <summary>
        /// Useful for constraints that just need to mark conflicts based on the seen cells.
        /// </summary>
        /// <param name="values"></param>
        /// <param name="conflicts"></param>
        /// <returns></returns>
        protected bool MarkConflictsBasedOnSeenCells(int[] values, bool[] conflicts)
        {
            bool conflict = false;
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = FlatIndex((i, j));
                    int val = values[cellIndex];
                    if (val == 0)
                    {
                        continue;
                    }

                    foreach (var seenCell in SeenCells((i, j)))
                    {
                        int curCellIndex = FlatIndex(seenCell);
                        if (values[curCellIndex] == val)
                        {
                            if (!conflicts[cellIndex] || !conflicts[curCellIndex])
                            {
                                conflicts[cellIndex] = true;
                                conflicts[curCellIndex] = true;
                                conflict = true;
                            }
                        }
                    }
                }
            }
            return conflict;
        }

        /// <summary>
        /// Useful for constraints that just need to enforce seen cells.
        /// </summary>
        /// <param name="sudokuSolver"></param>
        /// <param name="i"></param>
        /// <param name="j"></param>
        /// <param name="val"></param>
        /// <returns></returns>
        protected bool EnforceConstraintBasedOnSeenCells(SudokuSolver sudokuSolver, int i, int j, int val)
        {
            foreach (var cell in SeenCells((i, j)))
            {
                if (!sudokuSolver.ClearValue(cell.Item1, cell.Item2, val))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
