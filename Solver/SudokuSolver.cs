using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SudokuBlazor.Solver.Constraints;
using static SudokuBlazor.Solver.SolverUtility;

namespace SudokuBlazor.Solver
{
    public class SudokuSolver
    {
        private uint[,] board;
        public uint[,] Board => board;
        public uint[] FlatBoard
        {
            get
            {
                uint[] flatBoard = new uint[NUM_CELLS];
                for (int i = 0; i < HEIGHT; i++)
                {
                    for (int j = 0; j < WIDTH; j++)
                    {
                        flatBoard[i * WIDTH + j] = board[i, j];
                    }
                }
                return flatBoard;
            }
        }

        private readonly List<Constraint> constraints;
        
        /// <summary>
        /// Groups which cannot contain more than one of the same digit.
        /// This will at least contain all rows, columns, and boxes.
        /// Will also contain any groups from constraints (such as killer cages).
        /// </summary>
        public List<SudokuGroup> Groups { get; }

        /// <summary>
        /// Maps a cell to the list of groups which contain that cell.
        /// </summary>
        public Dictionary<(int, int), List<SudokuGroup>> CellToGroupMap { get; }

        /// <summary>
        /// Determines if the board has all values set.
        /// </summary>
        public bool IsComplete
        {
            get
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    for (int j = 0; j < HEIGHT; j++)
                    {
                        if (!IsValueSet(i, j))
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
        }

        private bool isBruteForcing = false;

        public SudokuSolver()
        {
            board = new uint[HEIGHT, WIDTH];
            constraints = new();

            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    board[i, j] = ALL_VALUES_MASK;
                }
            }
            Groups = new();
            CellToGroupMap = new();
            InitStandardGroups();
        }

        public SudokuSolver(SudokuSolver other)
        {
            board = (uint[,])other.board.Clone();
            constraints = other.constraints;
            Groups = other.Groups;
            CellToGroupMap = other.CellToGroupMap;
        }

        private void InitStandardGroups()
        {
            for (int i = 0; i < HEIGHT; i++)
            {
                List<(int, int)> cells = new(9);
                for (int j = 0; j < WIDTH; j++)
                {
                    cells.Add((i, j));
                }
                SudokuGroup group = new($"Row {i + 1}", cells);
                Groups.Add(group);
                InitMapForGroup(group);
            }

            // Add col groups
            for (int j = 0; j < WIDTH; j++)
            {
                List<(int, int)> cells = new(9);
                for (int i = 0; i < HEIGHT; i++)
                {
                    cells.Add((i, j));
                }
                SudokuGroup group = new($"Column {j + 1}", cells);
                Groups.Add(group);
                InitMapForGroup(group);
            }

            // Add box groups
            for (int boxi = 0; boxi < NUM_BOXES_HEIGHT; boxi++)
            {
                int basei = boxi * BOX_HEIGHT;
                for (int boxj = 0; boxj < NUM_BOXES_WIDTH; boxj++)
                {
                    int basej = boxj * BOX_WIDTH;

                    List<(int, int)> cells = new(9);
                    for (int offi = 0; offi < BOX_HEIGHT; offi++)
                    {
                        int i = basei + offi;
                        for (int offj = 0; offj < BOX_WIDTH; offj++)
                        {
                            int j = basej + offj;
                            cells.Add((i, j));
                        }
                    }
                    SudokuGroup group = new($"Box {boxi * 3 + boxj + 1}", cells);
                    Groups.Add(group);
                    InitMapForGroup(group);
                }
            }
        }

        private void InitMapForGroup(SudokuGroup group)
        {
            foreach (var pair in group.Cells)
            {
                if (CellToGroupMap.TryGetValue(pair, out var value))
                {
                    value.Add(group);
                }
                else
                {
                    // Reserve 3 entries: row, col, and box
                    CellToGroupMap[pair] = new(3) { group };
                }
            }
        }

        /// <summary>
        /// Adds a new constraint to the board.
        /// Only call this before any values have been set onto the board.
        /// </summary>
        /// <param name="constraint"></param>
        public void AddConstraint(Constraint constraint)
        {
            constraints.Add(constraint);

            var cells = constraint.Group;
            if (cells != null)
            {
                SudokuGroup group = new(constraint.SpecificName, cells.ToList());
                Groups.Add(group);
                InitMapForGroup(group);
            }
        }

        /// <summary>
        /// Call this once after all constraints are set, and before setting any values.
        /// </summary>
        /// <returns>True if the board is still valid. False if the constraints cause there to be trivially no solutions.</returns>
        public bool FinalizeConstraints()
        {
            bool haveChange = true;
            while (haveChange)
            {
                haveChange = false;
                foreach (var constraint in constraints)
                {
                    LogicResult result = constraint.InitCandidates(this);
                    if (result == LogicResult.Invalid)
                    {
                        return false;
                    }

                    if (result == LogicResult.Changed)
                    {
                        haveChange = true;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Creates a copy of the board, including all constraints, set values, and candidates.
        /// </summary>
        /// <returns></returns>
        public SudokuSolver Clone() => new SudokuSolver(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue((int, int) cell)
        {
            return SolverUtility.GetValue(board[cell.Item1, cell.Item2]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValue(uint mask)
        {
            return SolverUtility.GetValue(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValueSet(int i, int j)
        {
            return SolverUtility.IsValueSet(board[i, j]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValueSet(uint mask)
        {
            return SolverUtility.IsValueSet(mask);
        }

        /// <summary>
        /// Returns which cells must be distinct from the all the inputted cells.
        /// </summary>
        /// <param name="cells"></param>
        /// <returns></returns>
        public HashSet<(int, int)> SeenCells(params (int, int)[] cells)
        {
            HashSet<(int, int)> result = null;
            foreach (var cell in cells)
            {
                if (!CellToGroupMap.TryGetValue(cell, out var groupList) || groupList.Count == 0)
                {
                    return new HashSet<(int, int)>();
                }

                HashSet<(int, int)> curSeen = new(groupList.First().Cells);
                foreach (var group in groupList.Skip(1))
                {
                    curSeen.UnionWith(group.Cells);
                }

                foreach (var constraint in constraints)
                {
                    curSeen.UnionWith(constraint.SeenCells(cell));
                }

                if (result == null)
                {
                    result = curSeen;
                }
                else
                {
                    result.IntersectWith(curSeen);
                }
            }
            if (result != null)
            {
                foreach (var cell in cells)
                {
                    result.Remove(cell);
                }
            }
            return result ?? new HashSet<(int, int)>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClearValue(int i, int j, int val)
        {
            uint curMask = board[i, j];
            uint valMask = ValueMask(val);

            if ((curMask & valMask) == 0)
            {
                // Clearing the bit would do nothing
                return true;
            }

            // From this point on, a bit will be cleared
            uint newMask = curMask & ~valMask;
            if ((newMask & ~valueSetMask) == 0)
            {
                // Can't clear the only remaining bit
                if (!IsValueSet(curMask))
                {
                    board[i, j] = 0;
                }
                return false;
            }

            board[i, j] = newMask;
            return true;
        }

        public bool SetValue(int i, int j, int val)
        {
            uint valMask = ValueMask(val);
            if ((board[i, j] & valMask) == 0)
            {
                return false;
            }
            board[i, j] = valueSetMask | valMask;

            // Enforce distinctness in groups
            var setCell = (i, j);
            foreach (var group in CellToGroupMap[setCell])
            {
                foreach (var cell in group.Cells)
                {
                    if (cell != setCell && !ClearValue(cell.Item1, cell.Item2, val))
                    {
                        return false;
                    }
                }
            }

            // Enforce all constraints
            foreach (var constraint in constraints)
            {
                if (!constraint.EnforceConstraint(this, i, j, val))
                {
                    return false;
                }
            }

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetMask(int i, int j, uint mask)
        {
            if ((mask & ~valueSetMask) == 0)
            {
                return false;
            }

            if (ValueCount(mask) == 1)
            {
                return SetValue(i, j, GetValue(mask));
            }

            board[i, j] = mask;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SetMask(int i, int j, params int[] values)
        {
            uint mask = 0;
            foreach (int v in values)
            {
                mask |= ValueMask(v);
            }
            return SetMask(i, j, mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal LogicResult ClearMask(int i, int j, uint mask)
        {
            if (mask == 0)
            {
                return LogicResult.None;
            }

            uint curMask = board[i, j];
            if ((curMask & mask) == 0)
            {
                return LogicResult.None;
            }

            return SetMask(i, j, curMask & ~mask) ? LogicResult.Changed : LogicResult.Invalid;
        }

        private (int, int) GetLeastCandidateCell()
        {
            int i = -1, j = -1;
            int numCandidates = MAX_VALUE + 1;
            for (int x = 0; x < HEIGHT; x++)
            {
                for (int y = 0; y < WIDTH; y++)
                {
                    if (!IsValueSet(x, y))
                    {
                        int curNumCandidates = ValueCount(board[x, y]);
                        if (curNumCandidates == 2)
                        {
                            return (x, y);
                        }
                        if (curNumCandidates < numCandidates)
                        {
                            numCandidates = curNumCandidates;
                            i = x;
                            j = y;
                        }
                    }
                }
            }
            return (i, j);
        }


        /// <summary>
        /// Performs a single logical step.
        /// </summary>
        /// <param name="progressEvent">An event to report progress whenever a new step is found.</param>
        /// <param name="completedEvent">An event to report the final status of the puzzle (solved, no more logical steps, invalid)</param>
        /// <param name="cancellationToken">Pass in to support cancelling the solve.</param>
        /// <returns></returns>
        public void LogicalStep(EventHandler<(string, uint[])> completedEvent)
        {
            StringBuilder logicDescription = new StringBuilder();
            LogicResult result = StepLogic(logicDescription, true);
            switch (result)
            {
                case LogicResult.None:
                    completedEvent?.Invoke(null, ("No more logical steps found.", FlatBoard));
                    return;
                default:
                    completedEvent?.Invoke(null, (logicDescription.ToString(), FlatBoard));
                    break;
            }
        }

        /// <summary>
        /// Performs logical solve steps until no more logic is found.
        /// </summary>
        /// <param name="progressEvent">An event to report progress whenever a new step is found.</param>
        /// <param name="completedEvent">An event to report the final status of the puzzle (solved, no more logical steps, invalid)</param>
        /// <param name="cancellationToken">Pass in to support cancelling the solve.</param>
        /// <returns></returns>
        public async Task LogicalSolve(EventHandler<(string, uint[])> progressEvent, EventHandler<(string, uint[])> completedEvent, CancellationToken? cancellationToken)
        {
            Stopwatch timeSinceCheck = Stopwatch.StartNew();
            while (true)
            {
                if (timeSinceCheck.ElapsedMilliseconds > 1000)
                {
                    await Task.Delay(1);
                    cancellationToken?.ThrowIfCancellationRequested();
                    timeSinceCheck.Restart();
                }

                StringBuilder logicDescription = new StringBuilder();
                LogicResult result = StepLogic(logicDescription);
                switch (result)
                {
                    case LogicResult.None:
                        completedEvent?.Invoke(null, ("No more logical steps found.", FlatBoard));
                        return;
                    case LogicResult.PuzzleComplete:
                        completedEvent?.Invoke(null, (logicDescription.ToString(), FlatBoard));
                        return;
                    default:
                        progressEvent?.Invoke(null, (logicDescription.ToString(), FlatBoard));
                        break;
                }
            }
        }

        /// <summary>
        /// Finds a single solution to the board. This may not be the only solution.
        /// For the exact same board inputs, the solution will always be the same.
        /// The board itself is modified to have the solution as its board values.
        /// If no solution is found, the board is left in an invalid state.
        /// </summary>
        /// <param name="cancellationToken">Pass in to support cancelling the solve.</param>
        /// <returns>True if a solution is found, otherwise false.</returns>
        public async Task<bool> FindSolution(CancellationToken? cancellationToken = null)
        {
            Stopwatch timeSinceCheck = Stopwatch.StartNew();

            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            var boardStack = new Stack<SudokuSolver>();
            while (true)
            {
                if (timeSinceCheck.ElapsedMilliseconds > 1000)
                {
                    await Task.Delay(1);
                    cancellationToken?.ThrowIfCancellationRequested();
                    timeSinceCheck.Restart();
                }

                if (ConsolidateBoard())
                {
                    (int i, int j) = GetLeastCandidateCell();
                    if (i < 0)
                    {
                        isBruteForcing = wasBruteForcing;
                        return true;
                    }

                    // Try a possible value for this cell
                    int val = MinValue(board[i, j]);
                    uint valMask = ValueMask(val);

                    // Create a backup board in case it needs to be restored
                    SudokuSolver backupBoard = Clone();
                    backupBoard.isBruteForcing = true;
                    backupBoard.board[i, j] &= ~valMask;
                    if (backupBoard.board[i, j] != 0)
                    {
                        boardStack.Push(backupBoard);
                    }

                    // Change the board to only allow this value in the slot
                    if (SetValue(i, j, val))
                    {
                        continue;
                    }
                }

                if (boardStack.Count == 0)
                {
                    isBruteForcing = wasBruteForcing;
                    return false;
                }
                board = boardStack.Pop().board;
            }
        }

        /// <summary>
        /// Finds a single random solution to the board. This may not be the only solution.
        /// The board itself is modified to have the solution as its board values.
        /// If no solution is found, the board is left in an invalid state.
        /// </summary>
        /// <param name="cancellationToken">Pass in to support cancelling the solve.</param>
        /// <returns>True if a solution is found, otherwise false.</returns>
        public async Task<bool> FindRandomSolution(CancellationToken? cancellationToken = null)
        {
            Stopwatch timeSinceCheck = Stopwatch.StartNew();

            Random rand = new Random();

            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            var boardStack = new Stack<SudokuSolver>();
            while (true)
            {
                if (timeSinceCheck.ElapsedMilliseconds > 1000)
                {
                    await Task.Delay(1);
                    cancellationToken?.ThrowIfCancellationRequested();
                    timeSinceCheck.Restart();
                }

                if (ConsolidateBoard())
                {
                    (int i, int j) = GetLeastCandidateCell();
                    if (i < 0)
                    {
                        isBruteForcing = wasBruteForcing;
                        return true;
                    }

                    // Try a possible value for this cell
                    uint cellMask = board[i, j];
                    int numCellVals = ValueCount(cellMask);
                    int targetValIndex = rand.Next(0, numCellVals);

                    int valIndex = 0;
                    int val = 0;
                    uint valMask = 0;
                    for (int curVal = 1; curVal <= MAX_VALUE; curVal++)
                    {
                        val = curVal;
                        valMask = ValueMask(curVal);

                        // Don't bother trying the value if it's not a possibility
                        if ((board[i, j] & valMask) != 0)
                        {
                            if (valIndex == targetValIndex)
                            {
                                break;
                            }
                            valIndex++;
                        }
                    }

                    // Create a backup board in case it needs to be restored
                    SudokuSolver backupBoard = Clone();
                    backupBoard.isBruteForcing = true;
                    backupBoard.board[i, j] &= ~valMask;
                    if (backupBoard.board[i, j] != 0)
                    {
                        boardStack.Push(backupBoard);
                    }

                    // Change the board to only allow this value in the slot
                    if (SetValue(i, j, val))
                    {
                        continue;
                    }
                }

                if (boardStack.Count == 0)
                {
                    isBruteForcing = wasBruteForcing;
                    return false;
                }
                board = boardStack.Pop().board;
            }
        }

        /// <summary>
        /// Determine how many solutions the board has.
        /// </summary>
        /// <param name="maxSolutions">The maximum number of solutions to find. Pass 0 for no maximum.</param>
        /// <param name="progressEvent">An event to receive the progress count as solutions are found.</param>
        /// <param name="cancellationToken">Pass in to support cancelling the count.</param>
        /// <returns>The solution count found.</returns>
        public async Task<ulong> CountSolutions(ulong maxSolutions = 0, EventHandler<ulong> progressEvent = null,  CancellationToken? cancellationToken = null)
        {
            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            CountSolutionsState state = new CountSolutionsState(maxSolutions, progressEvent, cancellationToken);
            try
            {
                await CountSolutions(0, state);
            }
            catch (OperationCanceledException) { }

            isBruteForcing = wasBruteForcing;
            return state.numSolutions;
        }

        private class CountSolutionsState
        {
            public ulong numSolutions = 0;
            public ulong nextNumSolutionsEvent = 1;
            public ulong numSolutionsEventIncrement = 1;
            public readonly Stopwatch timeSinceCheck = Stopwatch.StartNew();

            public readonly ulong maxSolutions;
            public readonly EventHandler<ulong> progressEvent;
            public readonly CancellationToken? cancellationToken;

            public CountSolutionsState(ulong maxSolutions, EventHandler<ulong> progressEvent, CancellationToken? cancellationToken)
            {
                this.maxSolutions = maxSolutions;
                this.progressEvent = progressEvent;
                this.cancellationToken = cancellationToken;
            }

            public void IncrementSolutions()
            {
                numSolutions++;
                if (nextNumSolutionsEvent == numSolutions)
                {
                    progressEvent?.Invoke(null, numSolutions);
                    if (numSolutionsEventIncrement < 1000 && numSolutions / numSolutionsEventIncrement >= 10)
                    {
                        numSolutionsEventIncrement *= 10;
                    }
                    nextNumSolutionsEvent += numSolutionsEventIncrement;
                }
            }
        }

        private async Task CountSolutions(int cell, CountSolutionsState state)
        {
            int i = cell / WIDTH;
            int j = cell % WIDTH;

            // Skip cells until one that is not already filled
            while (cell < NUM_CELLS && IsValueSet(i, j))
            {
                cell++;
                i = cell / WIDTH;
                j = cell % WIDTH;
            }
            if (cell >= NUM_CELLS)
            {
                // Found a solution
                state.IncrementSolutions();
                return;
            }

            // If there are no possible values then a contradiction was reached
            if (board[i, j] == 0)
            {
                return;
            }

            // Try all possible values for this cell, recording how many solutions exist for that value
            for (int val = 1; val <= MAX_VALUE; val++)
            {
                uint valMask = ValueMask(val);

                // Don't bother trying the value if it's not a possibility
                if ((board[i, j] & valMask) == 0)
                {
                    continue;
                }

                // Check for cancel
                if (state.timeSinceCheck.ElapsedMilliseconds > 1000)
                {
                    await Task.Delay(1);
                    state.cancellationToken?.ThrowIfCancellationRequested();
                    state.timeSinceCheck.Restart();
                }

                // Create a duplicate board with one guess and count the solutions for that one
                SudokuSolver boardCopy = Clone();
                boardCopy.isBruteForcing = true;

                // Change the board to only allow this value in the slot
                if (boardCopy.SetValue(i, j, val) && boardCopy.ConsolidateBoard())
                {
                    // Accumulate how many solutions there are with this cell value
                    await boardCopy.CountSolutions(cell + 1, state);
                    if (state.maxSolutions > 0 && state.numSolutions >= state.maxSolutions)
                    {
                        return;
                    }
                }

                // Mark the value as not possible since all solutions with that value are already recorded
                board[i, j] &= ~valMask;

                // If that was the last possible change, then this board has no more solutions
                if (board[i, j] == 0)
                {
                    return;
                }

                // If consolodating this board reaches a contradiction, then none of the remaining values have solutions.
                if (!ConsolidateBoard())
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Remove any candidates which do not lead to an actual solution to the board.
        /// </summary>
        /// <param name="progressEvent">Recieve progress notifications. Will send 0 through 80 (assume 81 is 100%, though that will never be sent).</param>
        /// <param name="cancellationToken">Pass in to support cancelling.</param>
        /// <returns>True if there are solutions and candidates are filled. False if there are no solutions.</returns>
        public async Task FillRealCandidates(EventHandler<(int, uint[])> progressEvent = null, EventHandler<uint[]> completionEvent = null, CancellationToken? cancellationToken = null)
        {
            Stopwatch timeSinceCheck = Stopwatch.StartNew();
            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            if (!ConsolidateBoard())
            {
                completionEvent?.Invoke(null, null);
                isBruteForcing = wasBruteForcing;
                return;
            }

            uint[] fixedBoard = new uint[NUM_CELLS];
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    int cellIndex = i * WIDTH + j;

                    if (IsValueSet(i, j))
                    {
                        fixedBoard[cellIndex] = board[i, j];
                        continue;
                    }

                    for (int val = 1; val <= MAX_VALUE; val++)
                    {
                        uint valMask = ValueMask(val);

                        // Don't bother trying the value if it's not a possibility
                        if ((board[i, j] & valMask) == 0)
                        {
                            continue;
                        }

                        // Don't bother trying this value if it's already confirmed in the fixed board
                        if ((fixedBoard[cellIndex] & valMask) != 0)
                        {
                            continue;
                        }

                        // Check for cancellation and send progress updates once per second
                        if (timeSinceCheck.ElapsedMilliseconds > 1000)
                        {
                            await Task.Delay(1);
                            cancellationToken?.ThrowIfCancellationRequested();

                            progressEvent?.Invoke(null, (cellIndex, fixedBoard));
                            timeSinceCheck.Restart();
                        }

                        // Do the solve on a copy of the board
                        SudokuSolver boardCopy = Clone();
                        boardCopy.isBruteForcing = true;

                        // Go through all previous cells and set only their real candidates as possibilities
                        for (int fixedCellIndex = 0; fixedCellIndex < cellIndex; fixedCellIndex++)
                        {
                            if (ValueCount(fixedBoard[fixedCellIndex]) > 1)
                            {
                                int fi = fixedCellIndex / WIDTH;
                                int fj = fixedCellIndex % WIDTH;
                                boardCopy.board[fi, fj] = fixedBoard[fixedCellIndex];
                            }
                        }
                        for (int fixedCellIndex = 0; fixedCellIndex < cellIndex; fixedCellIndex++)
                        {
                            if (ValueCount(fixedBoard[fixedCellIndex]) == 1)
                            {
                                int fi = fixedCellIndex / WIDTH;
                                int fj = fixedCellIndex % WIDTH;
                                if (!boardCopy.IsValueSet(fi, fj))
                                {
                                    boardCopy.SetValue(fi, fj, GetValue(fixedBoard[fixedCellIndex]));
                                }
                            }
                        }

                        // Set the board to use this candidate's value
                        if (boardCopy.SetValue(i, j, val) && await boardCopy.FindSolution(cancellationToken))
                        {
                            for (int si = 0; si < HEIGHT; si++)
                            {
                                for (int sj = 0; sj < WIDTH; sj++)
                                {
                                    uint solutionValMask = boardCopy.board[si, sj] & ~valueSetMask;
                                    fixedBoard[si * WIDTH + sj] |= solutionValMask;
                                }
                            }
                        }
                    }

                    // If a cell has no possible candidates then there are no solutions and thus all candidates are empty.
                    // This will really only happen on the first cell attempted.
                    if (fixedBoard[cellIndex] == 0)
                    {
                        completionEvent?.Invoke(null, null);
                        isBruteForcing = wasBruteForcing;
                        return;
                    }
                }
            }

            completionEvent?.Invoke(null, fixedBoard);
            isBruteForcing = wasBruteForcing;
        }

        /// <summary>
        /// Perform a logical solve until either the board is solved or there are no logical steps found.
        /// </summary>
        /// <param name="stepsDescription">Get a full description of all logical steps taken.</param>
        /// <returns></returns>
        public bool ConsolidateBoard(StringBuilder stepsDescription = null)
        {
            LogicResult result;
            do
            {
                StringBuilder stepDescription = new StringBuilder();
                result = StepLogic(stepDescription);
                stepsDescription?.Append(stepDescription).AppendLine();
            } while (result == LogicResult.Changed);

            return result != LogicResult.Invalid;
        }

        /// <summary>
        /// Perform one step of a logical solve and fill a description of the step taken.
        /// The description will contain the reason the board is invalid if that is what is returned.
        /// </summary>
        /// <param name="stepDescription"></param>
        /// <returns></returns>
        public LogicResult StepLogic(StringBuilder stepDescription, bool humanStepping = false)
        {
            LogicResult result = FindNakedSingles(stepDescription, humanStepping);
            if (result != LogicResult.None)
            {
                return result;
            }

            result = FindHiddenSingle(stepDescription);
            if (result != LogicResult.None)
            {
                return result;
            }

            result = FindNakedTuples(stepDescription);
            if (result != LogicResult.None)
            {
                return result;
            }

            result = FindPointingTuples(stepDescription);
            if (result != LogicResult.None)
            {
                return result;
            }

            foreach (var constraint in constraints)
            {
                result = constraint.StepLogic(this, stepDescription, isBruteForcing);
                if (result != LogicResult.None)
                {
                    stepDescription.Insert(0, $"{constraint.SpecificName}: ");
                    return result;
                }
            }

            if (isBruteForcing)
            {
                return LogicResult.None;
            }

            result = FindFishes(stepDescription);
            if (result != LogicResult.None)
            {
                return result;
            }

            result = FindSimpleContradictions(stepDescription);
            if (result != LogicResult.None)
            {
                return result;
            }

            return LogicResult.None;
        }

        private LogicResult FindNakedSingles(StringBuilder stepDescription, bool humanStepping)
        {
            if (humanStepping)
            {
                return FindNakedSinglesHelper(stepDescription, humanStepping);
            }

            bool haveChange = false;
            while (true)
            {
                StringBuilder curStepDescription = new StringBuilder();
                LogicResult findResult = FindNakedSinglesHelper(curStepDescription, humanStepping);
                if (stepDescription != null)
                {
                    if (stepDescription.Length > 0)
                    {
                        stepDescription.AppendLine();
                    }
                    stepDescription.Append(curStepDescription);
                }
                switch (findResult)
                {
                    case LogicResult.None:
                        return haveChange ? LogicResult.Changed : LogicResult.None;
                    case LogicResult.Changed:
                        haveChange = true;
                        break;
                    default:
                        return findResult;
                }
            }
        }

        private LogicResult FindNakedSinglesHelper(StringBuilder stepDescription, bool humanStepping)
        {
            string stepPrefix = humanStepping ? "Naked Single:" : "Naked Single(s):";

            bool hasUnsetCells = false;
            bool hadChanges = false;
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    uint mask = board[i, j];

                    // If there are no possibilies on a square, then bail out
                    if (mask == 0)
                    {
                        stepDescription.AppendLine();
                        stepDescription.Append($"{CellName(i, j)} has no possible values.");
                        return LogicResult.Invalid;
                    }

                    if (!IsValueSet(mask))
                    {
                        hasUnsetCells = true;

                        if (ValueCount(mask) == 1)
                        {
                            int value = GetValue(mask);
                            if (!hadChanges)
                            {
                                stepDescription.Append($"{stepPrefix} {CellName(i, j)} = {value}");
                                hadChanges = true;
                                if (humanStepping)
                                {
                                    return LogicResult.Changed;
                                }
                            }
                            else
                            {
                                stepDescription.Append($", {CellName(i, j)} = {value}");
                            }

                            if (!SetValue(i, j, value))
                            {
                                for (int ci = 0; ci < 9; ci++)
                                {
                                    for (int cj = 0; cj < 9; cj++)
                                    {
                                        if (board[ci, cj] == 0)
                                        {
                                            stepDescription.AppendLine().Append($"{CellName(ci, cj)} has no candidates remaining.");
                                            return LogicResult.Invalid;
                                        }
                                    }
                                }
                                stepDescription.AppendLine().Append($"{CellName(i, j)} cannot be {value}.");
                                return LogicResult.Invalid;
                            }
                        }
                    }
                }
            }
            if (!hasUnsetCells)
            {
                if (stepDescription.Length > 0)
                {
                    stepDescription.AppendLine();
                }
                stepDescription.Append("Solution found!");
                return LogicResult.PuzzleComplete;
            }
            return hadChanges ? LogicResult.Changed : LogicResult.None;
        }

        private LogicResult FindHiddenSingle(StringBuilder stepDescription)
        {
            string stepPrefix = "Hidden single:";

            LogicResult finalFindResult = LogicResult.None;
            foreach (var group in Groups)
            {
                if (group.Cells.Count != MAX_VALUE)
                {
                    continue;
                }

                for (int val = 1; val <= MAX_VALUE; val++)
                {
                    uint valMask = 1u << (val - 1);
                    int numWithVal = 0;
                    int vali = 0;
                    int valj = 0;
                    foreach (var pair in group.Cells)
                    {
                        int i = pair.Item1;
                        int j = pair.Item2;
                        if ((board[i, j] & valMask) != 0)
                        {
                            numWithVal++;
                            vali = i;
                            valj = j;
                        }
                    }
                    if (numWithVal == 1 && !IsValueSet(vali, valj))
                    {
                        if (!SetValue(vali, valj, val))
                        {
                            stepDescription.Clear();
                            stepDescription.Append($"{stepPrefix} {CellName(vali, valj)} cannot be set to {val}.");
                            return LogicResult.Invalid;
                        }
                        stepDescription.Append($"Hidden single {val} in {group.Name} {CellName(vali, valj)}");
                        return LogicResult.Changed;
                    }
                    else if (numWithVal == 0)
                    {
                        stepDescription.Clear();
                        stepDescription.Append($"{stepPrefix} {group.Name} has nowhere to place {val}.");
                        return LogicResult.Invalid;
                    }
                }
            }
            return finalFindResult;
        }

        private LogicResult FindNakedTuples(StringBuilder stepDescription)
        {
            const string stepPrefix = "Naked tuple:";

            List<(int, int)> unsetCells = new List<(int, int)>(MAX_VALUE);
            for (int tupleSize = 2; tupleSize < 8; tupleSize++)
            {
                foreach (var group in Groups)
                {
                    // Make a list of pairs for the group which aren't already filled
                    unsetCells.Clear();
                    foreach (var pair in group.Cells)
                    {
                        uint cellMask = board[pair.Item1, pair.Item2];
                        if (!IsValueSet(cellMask) && ValueCount(cellMask) <= tupleSize)
                        {
                            unsetCells.Add(pair);
                        }
                    }
                    if (unsetCells.Count < tupleSize)
                    {
                        continue;
                    }

                    int[] cellCombinations = combinations[unsetCells.Count - 1][tupleSize - 1];
                    int numCombinations = cellCombinations.Length / tupleSize;
                    for (int combinationIndex = 0; combinationIndex < numCombinations; combinationIndex++)
                    {
                        Span<int> curCombination = new Span<int>(cellCombinations, combinationIndex * tupleSize, tupleSize);

                        uint combinationMask = 0;
                        foreach (int cellIndex in curCombination)
                        {
                            var curCell = unsetCells[cellIndex];
                            combinationMask |= board[curCell.Item1, curCell.Item2];
                        }

                        if (ValueCount(combinationMask) == tupleSize)
                        {
                            uint[,] oldBoard = (uint[,])board.Clone();

                            uint invCombinationMask = ~combinationMask;

                            bool changed = false;
                            int numMatching = 0;
                            foreach (var curCell in group.Cells)
                            {
                                uint curMask = board[curCell.Item1, curCell.Item2];
                                uint remainingMask = curMask & invCombinationMask;
                                if (remainingMask != 0)
                                {
                                    if (remainingMask != curMask)
                                    {
                                        board[curCell.Item1, curCell.Item2] = remainingMask;
                                        if (!changed)
                                        {
                                            stepDescription.Append($"{stepPrefix} {group} has tuple {MaskToString(combinationMask)}, removing those values from {CellName(curCell)}");
                                            changed = true;
                                        }
                                        else
                                        {
                                            stepDescription.Append($", {CellName(curCell)}");
                                        }
                                    }
                                }
                                else
                                {
                                    numMatching++;
                                }
                            }

                            if (numMatching > tupleSize)
                            {
                                stepDescription.Clear();
                                stepDescription.Append($"{stepPrefix} {group} has too many cells ({tupleSize}) which can only have {MaskToString(combinationMask)}");
                                return LogicResult.Invalid;
                            }
                            if (changed)
                            {
                                return LogicResult.Changed;
                            }
                        }
                    }
                }
            }
            return LogicResult.None;
        }

        private LogicResult FindPointingTuples(StringBuilder stepDescription)
        {
            const string stepPrefix = "Pointing tuple:";

            foreach (var group in Groups)
            {
                if (group.Cells.Count != MAX_VALUE)
                {
                    continue;
                }

                for (int v = 1; v <= MAX_VALUE; v++)
                {
                    if (group.Cells.Any(cell => IsValueSet(cell.Item1, cell.Item2) && GetValue(cell) == v))
                    {
                        continue;
                    }

                    (int, int)[] cellsWithValue = group.Cells.Where(cell => HasValue(board[cell.Item1, cell.Item2], v)).ToArray();
                    if (cellsWithValue.Length <= 1 || cellsWithValue.Length > 3)
                    {
                        continue;
                    }

                    var seenCells = SeenCells(cellsWithValue);
                    if (seenCells.Count == 0)
                    {
                        continue;
                    }

                    StringBuilder cellsWithValueStringBuilder = null;
                    uint valueMask = ValueMask(v);
                    bool changed = false;
                    foreach ((int i, int j) in seenCells)
                    {
                        if (cellsWithValue.Contains((i, j)))
                        {
                            continue;
                        }
                        if ((board[i, j] & valueMask) != 0)
                        {
                            if (cellsWithValueStringBuilder == null)
                            {
                                cellsWithValueStringBuilder = new();
                                foreach (var cell in cellsWithValue)
                                {
                                    if (cellsWithValueStringBuilder.Length != 0)
                                    {
                                        cellsWithValueStringBuilder.Append(", ");
                                    }
                                    cellsWithValueStringBuilder.Append(CellName(cell));
                                }
                            }

                            if (!ClearValue(i, j, v))
                            {
                                stepDescription.Clear();
                                stepDescription.Append($"{stepPrefix} {v} is limited to {cellsWithValueStringBuilder} in {group}, but that value cannot be removed from {CellName(i, j)}");
                                return LogicResult.Invalid;
                            }
                            if (!changed)
                            {
                                stepDescription.Append($"{v} is limited to {cellsWithValueStringBuilder} in {group}, which removes that value from {CellName((i, j))}");
                                changed = true;
                            }
                            else
                            {
                                stepDescription.Append($", {CellName((i, j))}");
                            }
                        }
                    }
                    if (changed)
                    {
                        return LogicResult.Changed;
                    }
                }
            }
            return LogicResult.None;
        }

        private LogicResult FindFishes(StringBuilder stepDescription)
        {
#pragma warning disable CS0162
            if (WIDTH != MAX_VALUE || HEIGHT != MAX_VALUE)
            {
                return LogicResult.None;
            }
#pragma warning restore CS0162

            List<int> fishRows = new List<int>(MAX_EXTENT);
            List<int> notFishRows = new List<int>(MAX_EXTENT);
            for (int n = 2; n <= 4; n++)
            {
                for (int rowOrCol = 0; rowOrCol < 2; rowOrCol++)
                {
                    bool isCol = rowOrCol != 0;
                    int height = isCol ? WIDTH : HEIGHT;
                    int width = isCol ? HEIGHT : WIDTH;
                    for (int v = 1; v <= MAX_VALUE; v++)
                    {
                        uint[] rows = new uint[height];
                        for (int curRow = 0; curRow < height; curRow++)
                        {
                            for (int curCol = 0; curCol < width; curCol++)
                            {
                                int i = isCol ? curCol : curRow;
                                int j = isCol ? curRow : curCol;
                                uint curMask = board[i, j];
                                if ((curMask & (1u << (v - 1))) != 0)
                                {
                                    rows[curRow] |= 1u << curCol;
                                }
                            }
                        }

                        for (int refRow = 0; refRow < height; refRow++)
                        {
                            uint refMask = rows[refRow];
                            int valCount = ValueCount(refMask);
                            if (valCount != n)
                            {
                                continue;
                            }

                            fishRows.Clear();
                            notFishRows.Clear();

                            uint invRefMask = ~refMask;
                            for (int checkRow = 0; checkRow < height; checkRow++)
                            {
                                if ((rows[checkRow] & invRefMask) == 0)
                                {
                                    fishRows.Add(checkRow);
                                }
                                else if ((rows[checkRow] & refMask) != 0)
                                {
                                    notFishRows.Add(checkRow);
                                }
                            }
                            if (fishRows.Count > n)
                            {
                                string rowName = isCol ? "cols" : "rows";

                                stepDescription.Clear();
                                stepDescription.Append($"Too many {rowName} ({fishRows.Count}) have only {n} locations for {v}");
                                return LogicResult.Invalid;
                            }
                            if (fishRows.Count == n && notFishRows.Count > 0)
                            {
                                bool changed = false;
                                string fishDesc = null;
                                foreach (int curRow in notFishRows)
                                {
                                    for (int curCol = 0; curCol < width; curCol++)
                                    {
                                        if ((refMask & (1u << curCol)) != 0)
                                        {
                                            int i = isCol ? curCol : curRow;
                                            int j = isCol ? curRow : curCol;

                                            if (fishDesc == null)
                                            {
                                                string rowName = isCol ? "c" : "r";
                                                string desc = "";
                                                foreach (int fishRow in fishRows)
                                                {
                                                    desc = $"{desc}{rowName}{fishRow + 1}";
                                                }

                                                string techniqueName = n switch
                                                {
                                                    2 => "X-Wing",
                                                    3 => "Swordfish",
                                                    4 => "Jellyfish",
                                                    _ => $"{n}-Fish",
                                                };

                                                fishDesc = $"{techniqueName} on {desc} for value {v}";
                                            }

                                            if (!ClearValue(i, j, v))
                                            {
                                                stepDescription.Clear();
                                                stepDescription.Append($"{fishDesc}, but it cannot be removed from {CellName(i, j)}");
                                                return LogicResult.Invalid;
                                            }

                                            if (!changed)
                                            {
                                                stepDescription.Append($"{fishDesc}, removing that value from {CellName(i, j)}");
                                                changed = true;
                                            }
                                            else
                                            {
                                                stepDescription.Append($", {CellName(i, j)}");
                                            }
                                        }
                                    }
                                }

                                if (changed)
                                {
                                    return LogicResult.Changed;
                                }
                            }
                        }
                    }
                }
            }

            return LogicResult.None;
        }

        private LogicResult FindSimpleContradictions(StringBuilder stepDescription)
        {
            for (int allowedValueCount = 2; allowedValueCount <= MAX_VALUE; allowedValueCount++)
            {
                for (int i = 0; i < HEIGHT; i++)
                {
                    for (int j = 0; j < WIDTH; j++)
                    {
                        uint cellMask = board[i, j];
                        if (!IsValueSet(cellMask) && ValueCount(cellMask) == allowedValueCount)
                        {
                            for (int v = 1; v <= MAX_VALUE; v++)
                            {
                                uint valueMask = ValueMask(v);
                                if ((cellMask & valueMask) != 0)
                                {
                                    SudokuSolver boardCopy = Clone();
                                    boardCopy.isBruteForcing = true;

                                    StringBuilder contradictionReason = new();
                                    if (!boardCopy.SetValue(i, j, v) || !boardCopy.ConsolidateBoard(contradictionReason))
                                    {
                                        StringBuilder formattedContraditionReason = new StringBuilder();
                                        foreach (string line in contradictionReason.ToString().Split('\n'))
                                        {
                                            if (!string.IsNullOrWhiteSpace(line))
                                            {
                                                formattedContraditionReason.Append("  ").Append(line).AppendLine();
                                            }
                                        }

                                        stepDescription.Append($"Setting {CellName(i, j)} to {v} causes a contradiction:");
                                        stepDescription.AppendLine();
                                        stepDescription.Append(formattedContraditionReason);
                                        if (!ClearValue(i, j, v))
                                        {
                                            stepDescription.AppendLine();
                                            stepDescription.Append($"This clears the last candidate from {CellName(i, j)}.");
                                            return LogicResult.Invalid;
                                        }
                                        return LogicResult.Changed;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return LogicResult.None;
        }
    }
}
