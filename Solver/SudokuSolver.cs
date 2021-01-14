#define LEAST_CANDIDATE_CELL
//#define VERBOSE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using SudokuBlazor.Shared;

namespace SudokuBlazor.Solver
{
    public class SudokuSolver
    {
        public const int WIDTH = 9;
        public const int HEIGHT = 9;
        public const int MAX_EXTENT = WIDTH > HEIGHT ? WIDTH : HEIGHT;
        public const int NUM_CELLS = WIDTH * HEIGHT;

        public const int MAX_VALUE = 9;
        public const uint ALL_VALUES_MASK = (1u << MAX_VALUE) - 1;

        public const int BOX_WIDTH = 3;
        public const int BOX_HEIGHT = 3;
        public const int BOX_CELL_COUNT = BOX_WIDTH * BOX_HEIGHT;
        public const int NUM_BOXES_WIDTH = WIDTH / BOX_WIDTH;
        public const int NUM_BOXES_HEIGHT = HEIGHT / BOX_HEIGHT;
        public const int NUM_BOXES = NUM_BOXES_WIDTH * NUM_BOXES_HEIGHT;

        // These are compile-time asserts
        private const byte ASSERT_VALUES_MIN = (MAX_VALUE >= 1) ? 0 : -1;
        private const byte ASSERT_VALUES_MAX = (MAX_VALUE <= 9) ? 0 : -1; // No support for more than 9 values yet
        private const byte ASSERT_BOX_SIZE = BOX_CELL_COUNT == MAX_VALUE ? 0 : -1;
        private const byte ASSERT_BOX_WIDTH_MAX = BOX_WIDTH <= WIDTH ? 0 : -1;
        private const byte ASSERT_BOX_HEIGHT_MAX = BOX_HEIGHT <= HEIGHT ? 0 : -1;
        private const byte ASSERT_BOX_WIDTH_DIVISIBILITY = (WIDTH % BOX_WIDTH) == 0 ? 0 : -1;
        private const byte ASSERT_BOX_HEIGHT_DIVISIBILITY = (HEIGHT % BOX_HEIGHT) == 0 ? 0 : -1;

        private uint[,] board = new uint[HEIGHT, WIDTH];
        public uint[,] Board => board;
        public bool BoxConstraint { get; set; } = true;
        public bool KingConstraint { get; set; } = false;
        public bool KnightConstraint { get; set; } = false;
        public bool NonconsecutiveConstraint { get; set; } = false;
        public bool DiagonalNonconsecutiveConstraint { get; set; } = false;
        public int NumSimpleContradictionsUsed { get; private set; } = 0;
        public int MostComplexLogicUsed { get; private set; } = 0;
        public Func<SudokuSolver, int, int, bool> CustomConstraint { get; set; } = null;

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
        private bool consolidatingArrows = false;
#if VERBOSE
        private StringBuilder findingContradictionSteps = new StringBuilder();
        private string lastContradictionReason = "";
#endif

        private List<Group> killerCages = new();
        private List<Group> littleKillerSums = new();
        private List<ArrowSum> arrowSums = new();
        private List<ArrowSum>[] arrowSumLookup = new List<ArrowSum>[NUM_CELLS];

        public SudokuSolver()
        {
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    board[i, j] = ALL_VALUES_MASK;
                }
            }
        }

        public SudokuSolver(string str)
        {
            if (str.Length != 81)
            {
                throw new ArgumentException("Board strings must be length 81");
            }
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    board[i, j] = ALL_VALUES_MASK;
                }
            }

            for (int i = 0; i < 81; i++)
            {
                if (str[i] >= '1' && str[i] <= '9')
                {
                    SetValue(i / 9, i % 9, str[i] - '0');
                }
            }
        }

        public SudokuSolver Clone()
        {
            return new SudokuSolver()
            {
                board = (uint[,])board.Clone(),
                BoxConstraint = BoxConstraint,
                KingConstraint = KingConstraint,
                KnightConstraint = KnightConstraint,
                NonconsecutiveConstraint = NonconsecutiveConstraint,
                DiagonalNonconsecutiveConstraint = DiagonalNonconsecutiveConstraint,
                CustomConstraint = CustomConstraint,
                killerCages = killerCages,
                littleKillerSums = littleKillerSums,
                arrowSums = arrowSums,
                arrowSumLookup = arrowSumLookup,
            };
        }

        public const uint valueSetMask = 1u << 31;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ValueCount(uint mask)
        {
            return BitOperations.PopCount(mask & ~valueSetMask);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetValue(uint mask)
        {
            return BitOperations.Log2(mask & ~valueSetMask) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetValue((int, int) cell)
        {
            return BitOperations.Log2(board[cell.Item1, cell.Item2] & ~valueSetMask) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValueSet(uint mask)
        {
            return (mask & valueSetMask) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValueSet(int i, int j)
        {
            return (board[i, j] & valueSetMask) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ValueMask(int val)
        {
            return 1u << (val - 1);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ValuesMask(params int[] vals)
        {
            uint mask = 0;
            foreach (int val in vals)
            {
                mask |= ValueMask(val);
            }
            return mask;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasValue(uint mask, int val)
        {
            uint valueMask = ValueMask(val);
            return (mask & valueMask) != 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MinValue(uint mask)
        {
            return BitOperations.TrailingZeroCount(mask) + 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MaxValue(uint mask)
        {
            return 32 - BitOperations.LeadingZeroCount(mask);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (int, int) BoxCoord(int i, int j)
        {
            return (i / BOX_HEIGHT, j / BOX_WIDTH);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int BoxIndex(int i, int j)
        {
            var (bi, bj) = BoxCoord(i, j);
            return bi * NUM_BOXES_WIDTH + bj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSameBox(int i0, int j0, int i1, int j1)
        {
            return BoxCoord(i0, j0) == BoxCoord(i1, j1);
        }

        public static string MaskToString(uint mask)
        {
            StringBuilder sb = new StringBuilder(MAX_VALUE);
            for (int v = 1; v <= MAX_VALUE; v++)
            {
                if (HasValue(mask, v))
                {
                    sb.Append((char)('0' + v));
                }
            }

            return sb.ToString();
        }

        private static int Gcd(int a, int b)
        {
            // Everything divides 0 
            if (a == 0 || b == 0)
                return 0;

            // base case 
            if (a == b)
                return a;

            // a is greater 
            if (a > b)
                return Gcd(a - b, b);

            return Gcd(a, b - a);
        }

        enum GroupType
        {
            Empty,
            Row,
            Col,
            Box,
            Killer,
            LittleKiller
        }
        class Group
        {
            public readonly GroupType groupType;
            public readonly int groupTypeIndex;
            public readonly (int, int)[] indexes;
            public readonly int sum; // For killer cages
            public readonly List<List<int>> sumCombinations; // For killer cages
            public readonly HashSet<int> possibleValues; // For killer cages

            public static Group Empty => new Group(GroupType.Empty, 0, Array.Empty<(int, int)>());

            public string TypeString
            {
                get
                {
                    switch (groupType)
                    {
                        case GroupType.Row:
                            return "Row";
                        case GroupType.Col:
                            return "Col";
                        case GroupType.Box:
                            return "Box";
                        case GroupType.Killer:
                            return "Killer";
                        case GroupType.LittleKiller:
                            return "Little Killer";
                        default:
                            return "Unk";
                    }
                }
            }

            public Group(GroupType type, int groupTypeIndex, (int, int)[] indexes, int sum = 0)
            {
                this.groupType = type;
                this.groupTypeIndex = groupTypeIndex;
                this.indexes = indexes;
                this.sum = sum;

                const int allValueSum = (MAX_VALUE * (MAX_VALUE + 1)) / 2;
                if (sum > 0 && sum < allValueSum)
                {
                    sumCombinations = new();
                    possibleValues = new();
                    int numCells = indexes.Length;
                    foreach (var combination in Enumerable.Range(1, 9).Combinations(numCells))
                    {
                        if (combination.Sum() == sum)
                        {
                            sumCombinations.Add(combination);
                            foreach (int value in combination)
                            {
                                possibleValues.Add(value);
                            }
                        }
                    }
                }
            }

            public override string ToString()
            {
                return $"{TypeString} {groupTypeIndex + 1}";
            }
        }
        record ArrowSum((int, int)[] SumCells, (int, int)[] ShaftCells)
        {
            public bool HasSumValue(SudokuSolver board) => HasValue(board, SumCells);
            public bool HasShaftValue(SudokuSolver board) => HasValue(board, ShaftCells);

            private static bool HasValue(SudokuSolver board, (int, int)[] cells)
            {
                foreach (var cell in cells)
                {
                    if (ValueCount(board.Board[cell.Item1, cell.Item2]) != 1)
                    {
                        return false;
                    }
                }
                return true;
            }

            public int SumValue(SudokuSolver board)
            {
                int sum = 0;
                foreach (int v in SumCells.Select(cell => board.GetValue(cell)))
                {
                    sum *= 10;
                    sum += v;
                }
                return sum;
            }
            public int ShaftValue(SudokuSolver board) => ShaftCells.Select(cell => board.GetValue(cell)).Sum();
        }

        private static readonly Group[] indexGroups = new Group[WIDTH + HEIGHT + NUM_BOXES];
        private static readonly Dictionary<(int, int), List<Group>> groupsForIndexMap = new Dictionary<(int, int), List<Group>>();
        private static void InitIndexGroups()
        {
            int groupIndex = 0;

            // Add row groups
            for (int i = 0; i < HEIGHT; i++)
            {
                var group = new (int, int)[WIDTH];
                for (int j = 0; j < WIDTH; j++)
                {
                    group[j] = (i, j);
                }
                indexGroups[groupIndex++] = new Group(GroupType.Row, i, group);
            }

            // Add col groups
            for (int j = 0; j < WIDTH; j++)
            {
                var group = new (int, int)[HEIGHT];
                for (int i = 0; i < HEIGHT; i++)
                {
                    group[i] = (i, j);
                }
                indexGroups[groupIndex++] = new Group(GroupType.Col, j, group);
            }

            // Add box groups
            for (int boxi = 0; boxi < NUM_BOXES_HEIGHT; boxi++)
            {
                int basei = boxi * BOX_HEIGHT;
                for (int boxj = 0; boxj < NUM_BOXES_WIDTH; boxj++)
                {
                    int basej = boxj * BOX_WIDTH;

                    var group = new (int, int)[BOX_CELL_COUNT];
                    int groupi = 0;
                    for (int offi = 0; offi < BOX_HEIGHT; offi++)
                    {
                        int i = basei + offi;
                        for (int offj = 0; offj < BOX_WIDTH; offj++)
                        {
                            int j = basej + offj;
                            group[groupi++] = (i, j);
                        }
                    }
                    indexGroups[groupIndex++] = new Group(GroupType.Box, boxi * NUM_BOXES_WIDTH + boxj, group);
                }
            }

            // Fill map
            foreach (var group in indexGroups)
            {
                foreach (var pair in group.indexes)
                {
                    if (groupsForIndexMap.TryGetValue(pair, out var value))
                    {
                        value.Add(group);
                    }
                    else
                    {
                        // Reserve 3 entries: row, col, and box
                        groupsForIndexMap[pair] = new List<Group>(3) { group };
                    }
                }
            }
        }

        private HashSet<(int, int)> SeenCells(params (int, int)[] cells)
        {
            HashSet<(int, int)> result = null;
            foreach (var cell in cells)
            {
                if (!groupsForIndexMap.TryGetValue(cell, out var groupList) || groupList.Count == 0)
                {
                    return new HashSet<(int, int)>();
                }
                Group curSeenGroup = Group.Empty;
                var curSeen = new HashSet<(int, int)>(curSeenGroup.indexes);
                for (int i = 0; i < groupList.Count; i++)
                {
                    if (BoxConstraint || groupList[i].groupType != GroupType.Box)
                    {
                        curSeen.UnionWith(groupList[i].indexes);
                    }
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
            return result ?? new HashSet<(int, int)>();
        }

        private static readonly int[][][] combinations = new int[MAX_VALUE][][];
        private static void InitCombinations()
        {
            for (int n = 1; n <= combinations.Length; n++)
            {
                combinations[n - 1] = new int[n][];
                for (int k = 1; k <= n; k++)
                {
                    int numCombinations = BinomialCoeff(n, k);
                    combinations[n - 1][k - 1] = new int[numCombinations * k];
                    FillCombinations(combinations[n - 1][k - 1], n, k);
                }
            }
        }

        private static int BinomialCoeff(int n, int k)
        {
            return
                (k > n) ? 0 :          // out of range
                (k == 0 || k == n) ? 1 :          // edge
                (k == 1 || k == n - 1) ? n :          // first
                (k + k < n) ?              // recursive:
                (BinomialCoeff(n - 1, k - 1) * n) / k :       //  path to k=1   is faster
                (BinomialCoeff(n - 1, k) * n) / (n - k);      //  path to k=n-1 is faster
        }

        private static void FillCombinations(int[] combinations, int n, int k, ref int numCombinations, int offset, int[] curCombination, int curCombinationSize)
        {
            if (k == 0)
            {
                for (int i = 0; i < curCombinationSize; i++)
                {
                    combinations[numCombinations * curCombinationSize + i] = curCombination[i];
                }
                numCombinations++;
                return;
            }
            for (int i = offset; i <= n - k; ++i)
            {
                curCombination[curCombinationSize] = i;
                FillCombinations(combinations, n, k - 1, ref numCombinations, i + 1, curCombination, curCombinationSize + 1);
            }
        }

        private static void FillCombinations(int[] combinations, int n, int k)
        {
            int numCombinations = 0;
            int[] curCombination = new int[k];
            FillCombinations(combinations, n, k, ref numCombinations, 0, curCombination, 0);
        }

        static SudokuSolver()
        {
            InitIndexGroups();
            InitCombinations();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ClearValue(int i, int j, int val)
        {
            uint curMask = board[i, j];
            uint valMask = 1u << (val - 1);

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
                return false;
            }

            board[i, j] = newMask;
            return true;
        }

        public static IEnumerable<(int, int)> AdjacentCells(int i, int j)
        {
            if (i > 0)
            {
                yield return (i - 1, j);
            }
            if (i < HEIGHT - 1)
            {
                yield return (i + 1, j);
            }
            if (j > 0)
            {
                yield return (i, j - 1);
            }
            if (j < WIDTH - 1)
            {
                yield return (i, j + 1);
            }
        }

        public static IEnumerable<(int, int)> DiagonalCells(int i, int j, bool sameBox = false)
        {
            if (i > 0 && j > 0)
            {
                if (!sameBox || IsSameBox(i, j, i - 1, j - 1))
                {
                    yield return (i - 1, j - 1);
                }
            }
            if (i < HEIGHT - 1 && j > 0)
            {
                if (!sameBox || IsSameBox(i, j, i + 1, j - 1))
                {
                    yield return (i + 1, j - 1);
                }
            }
            if (i > 0 && j < WIDTH - 1)
            {
                if (!sameBox || IsSameBox(i, j, i - 1, j + 1))
                {
                    yield return (i - 1, j + 1);
                }
            }
            if (i < HEIGHT - 1 && j < WIDTH - 1)
            {
                if (!sameBox || IsSameBox(i, j, i + 1, j + 1))
                {
                    yield return (i + 1, j + 1);
                }
            }
        }

        public void AddKillerCage(IEnumerable<(int, int)> cells, int sum)
        {
            Group killerCage = new Group(
                type: GroupType.Killer,
                groupTypeIndex: killerCages.Count,
                indexes: cells.ToArray(),
                sum: sum);
            killerCages.Add(killerCage);

            if (killerCage.possibleValues != null)
            {
                for (int v = 1; v <= 9; v++)
                {
                    if (!killerCage.possibleValues.Contains(v))
                    {
                        foreach (var cell in cells)
                        {
                            if (!ClearValue(cell.Item1, cell.Item2, v))
                            {
                                throw new ArgumentException("Killer cage is invalid!");
                            }
                        }
                    }
                }
            }
        }

        public enum LittleKillerDirection
        {
            UpRight,
            UpLeft,
            DownRight,
            DownLeft
        }
        public void AddLittleKillerSum((int, int) start, LittleKillerDirection dir, int sum)
        {
            List<(int, int)> cells = new List<(int, int)>(MAX_EXTENT);
            {
                (int, int) cell = start;
                while (cell.Item1 >= 0 && cell.Item1 < HEIGHT && cell.Item2 >= 0 && cell.Item2 < WIDTH)
                {
                    cells.Add(cell);
                    switch (dir)
                    {
                        case LittleKillerDirection.UpRight:
                            cell = (cell.Item1 - 1, cell.Item2 + 1);
                            break;
                        case LittleKillerDirection.UpLeft:
                            cell = (cell.Item1 - 1, cell.Item2 - 1);
                            break;
                        case LittleKillerDirection.DownRight:
                            cell = (cell.Item1 + 1, cell.Item2 + 1);
                            break;
                        case LittleKillerDirection.DownLeft:
                            cell = (cell.Item1 + 1, cell.Item2 - 1);
                            break;
                    }
                }
            }

            littleKillerSums.Add(new Group(
                type: GroupType.LittleKiller,
                groupTypeIndex: littleKillerSums.Count,
                indexes: cells.ToArray(),
                sum: sum));

            int maxValue = sum - cells.Count + 1;
            if (maxValue < MAX_VALUE)
            {
                uint maxValueMask = (1u << maxValue) - 1;
                foreach (var cell in cells)
                {
                    if (!IsValueSet(board[cell.Item1, cell.Item2]))
                    {
                        board[cell.Item1, cell.Item2] &= maxValueMask;
                    }
                }
            }
        }

        public void AddArrowSum(IEnumerable<(int, int)> sumCells, IEnumerable<(int, int)> shaftCells)
        {
            ArrowSum arrowSum = new ArrowSum(sumCells.ToArray(), shaftCells.ToArray());
            arrowSums.Add(arrowSum);
            foreach (var (i, j) in sumCells.Concat(shaftCells))
            {
                int cell = i * WIDTH + j;
                if (arrowSumLookup[cell] != null)
                {
                    arrowSumLookup[cell].Add(arrowSum);
                }
                else
                {
                    arrowSumLookup[cell] = new() { arrowSum };
                }
            }

            if (arrowSum.SumCells.Length == 1)
            {
                int maxValue = MAX_VALUE - arrowSum.ShaftCells.Length + 1;
                if (maxValue < MAX_VALUE)
                {
                    uint maxValueMask = (1u << maxValue) - 1;
                    foreach (var cell in arrowSum.ShaftCells)
                    {
                        if (!IsValueSet(board[cell.Item1, cell.Item2]))
                        {
                            board[cell.Item1, cell.Item2] &= maxValueMask;
                        }
                    }
                }

                int minSum = arrowSum.ShaftCells.Length - 1;
                if (minSum > 0)
                {
                    var sumCell = arrowSum.SumCells[0];
                    uint minValueMask = ~((1u << minSum) - 1);
                    if (!IsValueSet(board[sumCell.Item1, sumCell.Item2]))
                    {
                        board[sumCell.Item1, sumCell.Item2] &= minValueMask;
                    }
                }
            }
            else if (arrowSum.SumCells.Length == 2)
            {
                int maxSum = arrowSum.ShaftCells.Length * MAX_VALUE;
                if (maxSum <= MAX_VALUE)
                {
                    throw new ArgumentException("Arrow sum not possible!");
                }
                if (maxSum <= 99)
                {
                    int maxSumPrefix = maxSum / 10;

                    var sumCell = arrowSum.SumCells[0];
                    uint maxValueMask = (1u << maxSumPrefix) - 1;
                    if (!IsValueSet(board[sumCell.Item1, sumCell.Item2]))
                    {
                        board[sumCell.Item1, sumCell.Item2] &= maxValueMask;
                    }
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public bool SetValue(int i, int j, int val)
        {
            int cellIndex = i * WIDTH + j;
            uint valMask = ValueMask(val);
            if ((board[i, j] & valMask) == 0)
            {
                return false;
            }
            board[i, j] = valueSetMask | valMask;

            // Restrict the row
            for (int curj = 0; curj < WIDTH; curj++)
            {
                if (curj != j)
                {
                    if (!ClearValue(i, curj, val))
                    {
                        return false;
                    }
                }
            }

            // Restrict the column
            for (int curi = 0; curi < HEIGHT; curi++)
            {
                if (curi != i)
                {
                    if (!ClearValue(curi, j, val))
                    {
                        return false;
                    }
                }
            }

            // Restrict the box
            if (BoxConstraint)
            {
                int basei = (i / BOX_HEIGHT) * BOX_HEIGHT;
                int basej = (j / BOX_WIDTH) * BOX_WIDTH;
                for (int boxi = 0; boxi < BOX_HEIGHT; boxi++)
                {
                    for (int boxj = 0; boxj < BOX_WIDTH; boxj++)
                    {
                        int curi = basei + boxi;
                        int curj = basej + boxj;
                        if (curi != i || curj != j)
                        {
                            if (!ClearValue(curi, curj, val))
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            // Restrict killer cages
            {
                foreach (var group in killerCages)
                {
                    // Check if cell is in the cage
                    int inCageIndex = -1;
                    for (int cageIndex = 0; cageIndex < group.indexes.Length; cageIndex++)
                    {
                        var curCell = group.indexes[cageIndex];
                        if (curCell.Item1 == i && curCell.Item2 == j)
                        {
                            inCageIndex = cageIndex;
                            break;
                        }
                    }

                    if (inCageIndex != -1)
                    {
                        // Enforce uniqueness in the cage
                        for (int cageIndex = 0; cageIndex < group.indexes.Length; cageIndex++)
                        {
                            if (cageIndex != inCageIndex)
                            {
                                var curCell = group.indexes[cageIndex];
                                if (!ClearValue(curCell.Item1, curCell.Item2, val))
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }

            // Restrict little killer sums
            {
                foreach (var group in littleKillerSums)
                {
                    // Check if cell is in the little killer
                    if (group.indexes.Contains((i, j)))
                    {
                        var cellMasks = group.indexes.Select(cell => board[cell.Item1, cell.Item2]);

                        int setValueSum = cellMasks.Where(mask => IsValueSet(mask)).Select(mask => GetValue(mask)).Sum();
                        if (setValueSum > group.sum)
                        {
                            return false;
                        }

                        // Ensure the sum is still possible
                        var unsetMasks = cellMasks.Where(mask => !IsValueSet(mask)).ToArray();
                        if (unsetMasks.Length == 0)
                        {
                            if (setValueSum != group.sum)
                            {
                                return false;
                            }
                        }
                        else if (unsetMasks.Length == 1)
                        {
                            int exactCellValue = group.sum - setValueSum;
                            if (exactCellValue <= 0 || exactCellValue > MAX_VALUE || !HasValue(unsetMasks[0], exactCellValue))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            int minSum = setValueSum + unsetMasks.Select(mask => MinValue(mask)).Sum();
                            int maxSum = setValueSum + unsetMasks.Select(mask => MaxValue(mask)).Sum();
                            if (minSum > group.sum || maxSum < group.sum)
                            {
                                return false;
                            }
                        }
                    }
                }
            }

            // Apply king's constraint
            if (KingConstraint)
            {
                for (int offi = -1; offi < 2; offi++)
                {
                    for (int offj = -1; offj < 2; offj++)
                    {
                        if (offi == 0 && offj == 0)
                        {
                            continue;
                        }
                        int curi = i + offi;
                        int curj = j + offj;
                        if (curi < 0 || curi > 8 || curj < 0 || curj > 8)
                        {
                            continue;
                        }
                        if (!ClearValue(curi, curj, val))
                        {
                            return false;
                        }
                    }
                }
            }

            // Apply knight's constraint
            if (KnightConstraint)
            {
                if (i - 2 >= 0 && j - 1 >= 0)
                {
                    if (!ClearValue(i - 2, j - 1, val))
                    {
                        return false;
                    }
                }
                if (i - 2 >= 0 && j + 1 < WIDTH)
                {
                    if (!ClearValue(i - 2, j + 1, val))
                    {
                        return false;
                    }
                }
                if (i - 1 >= 0 && j - 2 >= 0)
                {
                    if (!ClearValue(i - 1, j - 2, val))
                    {
                        return false;
                    }
                }
                if (i - 1 >= 0 && j + 2 < WIDTH)
                {
                    if (!ClearValue(i - 1, j + 2, val))
                    {
                        return false;
                    }
                }
                if (i + 2 < HEIGHT && j - 1 >= 0)
                {
                    if (!ClearValue(i + 2, j - 1, val))
                    {
                        return false;
                    }
                }
                if (i + 2 < HEIGHT && j + 1 < WIDTH)
                {
                    if (!ClearValue(i + 2, j + 1, val))
                    {
                        return false;
                    }
                }
                if (i + 1 < HEIGHT && j - 2 >= 0)
                {
                    if (!ClearValue(i + 1, j - 2, val))
                    {
                        return false;
                    }
                }
                if (i + 1 < HEIGHT && j + 2 < WIDTH)
                {
                    if (!ClearValue(i + 1, j + 2, val))
                    {
                        return false;
                    }
                }
            }

            // Apply orthogonal nonconsecutive constraint
            if (NonconsecutiveConstraint)
            {
                if (val > 1)
                {
                    int adjVal = val - 1;
                    foreach (var pair in AdjacentCells(i, j))
                    {
                        if (!ClearValue(pair.Item1, pair.Item2, adjVal))
                        {
                            return false;
                        }
                    }
                }
                if (val < MAX_VALUE)
                {
                    int adjVal = val + 1;
                    foreach (var pair in AdjacentCells(i, j))
                    {
                        if (!ClearValue(pair.Item1, pair.Item2, adjVal))
                        {
                            return false;
                        }
                    }
                }
            }

            // Apply diagonal nonconsecutive constraint
            if (DiagonalNonconsecutiveConstraint)
            {
                if (val > 1)
                {
                    int adjVal = val - 1;
                    foreach (var pair in DiagonalCells(i, j))
                    {
                        if (!ClearValue(pair.Item1, pair.Item2, adjVal))
                        {
                            return false;
                        }
                    }
                }
                if (val < MAX_VALUE)
                {
                    int adjVal = val + 1;
                    foreach (var pair in DiagonalCells(i, j))
                    {
                        if (!ClearValue(pair.Item1, pair.Item2, adjVal))
                        {
                            return false;
                        }
                    }
                }
            }

            // Very simple arrow sum check only when all cells are filled for the arrow
            // More complex arrow logic is performed in ConsolidateBoard()
            if (arrowSumLookup[cellIndex] != null)
            {
                foreach (var arrow in arrowSumLookup[cellIndex])
                {
                    bool sumCellsFilled = arrow.HasSumValue(this);
                    bool shaftCellsFilled = arrow.HasShaftValue(this);
                    if (sumCellsFilled && shaftCellsFilled)
                    {
                        // Both the sum and shaft cell values are known, so check to ensure the sum in correct
                        int sumValue = arrow.SumValue(this);
                        int shaftValue = arrow.ShaftValue(this);
                        if (sumValue != shaftValue)
                        {
                            return false;
                        }
                    }
                    else if (shaftCellsFilled)
                    {
                        // The shaft sum is known, so the sum cells are forced.
                        int shaftSum = arrow.ShaftValue(this);
                        if (arrow.SumCells.Length == 1)
                        {
                            if (shaftSum <= 0 || shaftSum > 9)
                            {
                                return false;
                            }
                            uint shaftSumMask = ValueMask(shaftSum);
                            var sumCell = arrow.SumCells[0];
                            uint sumCellMask = board[sumCell.Item1, sumCell.Item2];
                            if ((sumCellMask & shaftSumMask) == 0)
                            {
                                return false;
                            }

                            // Let SetValue run again on this as a "naked single" instead of doing it recursively
                            board[sumCell.Item1, sumCell.Item2] = shaftSumMask;
                        }
                        else if (arrow.SumCells.Length == 2)
                        {
                            if (shaftSum <= 9 || shaftSum >= 100)
                            {
                                return false;
                            }

                            int shaftSumTens = shaftSum / 10;
                            int shaftSumOnes = shaftSum % 10;
                            if (shaftSumOnes == 0)
                            {
                                return false;
                            }

                            uint shaftSumTensMask = ValueMask(shaftSumTens);
                            uint shaftSumOnesMask = ValueMask(shaftSumOnes);

                            var sumCellTens = arrow.SumCells[0];
                            var sumCellOnes = arrow.SumCells[1];
                            uint sumCellTensMask = board[sumCellTens.Item1, sumCellTens.Item2];
                            uint sumCellOnesMask = board[sumCellOnes.Item1, sumCellOnes.Item2];
                            if ((sumCellTensMask & shaftSumTensMask) == 0 ||
                                (sumCellOnesMask & shaftSumOnesMask) == 0)
                            {
                                return false;
                            }

                            // Let SetValue run again on these as a "naked single" instead of calling SetValue recursively
                            if (!IsValueSet(sumCellTensMask))
                            {
                                board[sumCellTens.Item1, sumCellTens.Item2] = shaftSumTensMask;
                            }
                            if (!IsValueSet(sumCellOnesMask))
                            {
                                board[sumCellOnes.Item1, sumCellOnes.Item2] = shaftSumOnesMask;
                            }
                        }
                    }
                }
            }

            if (CustomConstraint != null && !CustomConstraint(this, i, j))
            {
                return false;
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
                VerboseOut($"{CellName(i, j)} is now a naked single: {GetValue(mask)}");
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
        private FindResult ClearMask(int i, int j, uint mask)
        {
            if (mask == 0)
            {
                return FindResult.None;
            }

            uint curMask = board[i, j];
            if ((curMask & mask) == 0)
            {
                return FindResult.None;
            }

            return SetMask(i, j, curMask & ~mask) ? FindResult.Changed : FindResult.Invalid;
        }

        public void WriteBoard(TextWriter solutionWriter)
        {
            StringBuilder line = new StringBuilder();
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    line.Append(GetValue(board[i, j]));
                    if (i != HEIGHT - 1 || j != WIDTH - 1)
                    {
                        line.Append(',');
                    }
                }
            }
            solutionWriter.WriteLine(line.ToString());
            solutionWriter.Flush();
        }

        private (int, int) GetLeastCandidateCellInKillerCage()
        {
            int i = -1, j = -1;
            int numCandidates = 10;
            foreach (var cage in killerCages)
            {
                foreach (var cell in cage.indexes)
                {
                    int curNumCandidates = ValueCount(board[cell.Item1, cell.Item2]);
                    if (curNumCandidates == 2)
                    {
                        return (cell.Item1, cell.Item2);
                    }
                    if (curNumCandidates < numCandidates)
                    {
                        numCandidates = curNumCandidates;
                        i = cell.Item1;
                        j = cell.Item2;
                    }
                }
            }
            return (i, j);
        }

        private (int, int) GetLeastCandidateCell()
        {
            // Try killer cages first
            var (i, j) = GetLeastCandidateCellInKillerCage();
            if (i != -1 && j != -1)
            {
                return (i, j);
            }

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

        public readonly struct SolutionResult
        {
            public readonly bool foundSolution;
            public readonly ulong numGuesses;
            public readonly ulong numContradictions;
            public readonly uint[,] solutionBoard;

            public SolutionResult(bool foundSolution, ulong numGuesses, ulong numContradictions, uint[,] solutionBoard)
            {
                this.foundSolution = foundSolution;
                this.numGuesses = numGuesses;
                this.numContradictions = numContradictions;
                this.solutionBoard = solutionBoard;
            }
        }

        public bool FindSolution(CancellationToken? cancellationToken = null)
        {
            ulong numGuesses = 0;
            ulong numContradictions = 0;

            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            var boardStack = new Stack<SudokuSolver>();
            while (true)
            {
                cancellationToken?.ThrowIfCancellationRequested();

                if (ConsolidateBoard())
                {
                    (int i, int j) = GetLeastCandidateCell();
                    if (i < 0)
                    {
                        isBruteForcing = wasBruteForcing;
                        return true;
                    }

                    // Try a possible value for this cell
                    int val = 0;
                    uint valMask = 0;
                    for (int curVal = 1; curVal <= MAX_VALUE; curVal++)
                    {
                        val = curVal;
                        valMask = 1u << (curVal - 1);

                        // Don't bother trying the value if it's not a possibility
                        if ((board[i, j] & valMask) != 0)
                        {
                            break;
                        }
                    }

                    // Create a backup board in case it needs to be restored
                    SudokuSolver backupBoard = Clone();
                    backupBoard.isBruteForcing = true;
                    backupBoard.board[i, j] &= ~valMask;
                    if (backupBoard.board[i, j] != 0 && backupBoard.ConsolidateBoard())
                    {
                        boardStack.Push(backupBoard);
                    }

                    // Change the board to only allow this value in the slot
                    numGuesses++;
                    if (SetValue(i, j, val))
                    {
                        continue;
                    }
                }

                numContradictions++;
                if (boardStack.Count == 0)
                {
                    isBruteForcing = wasBruteForcing;
                    return false;
                }
                board = boardStack.Pop().board;
            }
        }

        public bool FillRandomSolution()
        {
            Random rand = new Random();

            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            var boardStack = new Stack<SudokuSolver>();
            while (true)
            {
                if (ConsolidateBoard())
                {
                    int i, j;
                    for (int i1 = 0; i1 < 9; i1++)
                    {
                        for (int j1 = 0; j1 < 9; j1++)
                        {
                            if (!IsValueSet(i1, j1))
                            {
                                i = i1;
                                j = j1;
                                goto have_cell;
                            }
                        }
                    }
                    isBruteForcing = wasBruteForcing;
                    return true;
                have_cell:

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
                    if (backupBoard.board[i, j] != 0 && backupBoard.ConsolidateBoard())
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

        public ulong CountSolutions(TextWriter solutionWriter, ulong maxSolutions = 0, string consolePrefix = "")
        {
            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            CountSolutionsState state = new CountSolutionsState(maxSolutions, consolePrefix);
            CountSolutions(0, solutionWriter, state);
            isBruteForcing = wasBruteForcing;
            return state.numSolutions;
        }

        private class CountSolutionsState
        {
            public ulong numSolutions = 0;
            public ulong maxSolutions = 0;
            public ulong lastNumSolutionsPrint = 0;
            public ulong nextNumSolutionsPrint = 1;
            public string consolePrefix;

            public CountSolutionsState(ulong maxSolutions, string consolePrefix)
            {
                this.maxSolutions = maxSolutions;
                this.consolePrefix = consolePrefix;
            }
        }

        private void CountSolutions(int cell, TextWriter solutionWriter, CountSolutionsState state)
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
                if (solutionWriter != null)
                {
                    WriteBoard(solutionWriter);
                }
                state.numSolutions++;
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

                // Create a duplicate board with one guess and count the solutions for that one
                SudokuSolver boardCopy = Clone();
                boardCopy.isBruteForcing = true;

                // Change the board to only allow this value in the slot
                if (boardCopy.SetValue(i, j, val) && boardCopy.ConsolidateBoard())
                {
                    // Accumulate how many solutions there are with this cell value
                    boardCopy.CountSolutions(cell + 1, solutionWriter, state);
                    if (state.maxSolutions > 0 && state.numSolutions >= state.maxSolutions)
                    {
                        return;
                    }
                    if (state.lastNumSolutionsPrint != state.numSolutions && state.nextNumSolutionsPrint == state.numSolutions)
                    {
                        Console.WriteLine($"[{state.consolePrefix}] Found {state.numSolutions} solutions so far...");
                        state.lastNumSolutionsPrint = state.numSolutions;
                        state.nextNumSolutionsPrint *= 2;
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

        public bool FixCandidates()
        {
            bool wasBruteForcing = isBruteForcing;
            isBruteForcing = true;

            if (!ConsolidateBoard())
            {
                isBruteForcing = wasBruteForcing;
                return false;
            }

            uint[,] fixedBoard = new uint[HEIGHT, WIDTH];
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    if (IsValueSet(i, j))
                    {
                        fixedBoard[i, j] = board[i, j];
                        continue;
                    }
                    Console.WriteLine($"Candidates for {i},{j}");

                    for (int val = 1; val <= MAX_VALUE; val++)
                    {
                        uint valMask = ValueMask(val);

                        // Don't bother trying the value if it's not a possibility
                        if ((board[i, j] & valMask) == 0)
                        {
                            continue;
                        }

                        // Don't bother trying this value if it's already confirmed in the fixed board
                        if ((fixedBoard[i, j] & valMask) != 0)
                        {
                            continue;
                        }

                        // Do the solve on a copy of the board
                        SudokuSolver boardCopy = Clone();
                        boardCopy.isBruteForcing = true;

                        // Set the board to use this candidate's value
                        if (boardCopy.SetValue(i, j, val) && boardCopy.FindSolution())
                        {
                            for (int si = 0; si < HEIGHT; si++)
                            {
                                for (int sj = 0; sj < WIDTH; sj++)
                                {
                                    uint solutionValMask = boardCopy.board[si, sj] & ~valueSetMask;
                                    fixedBoard[si, sj] |= solutionValMask;
                                }
                            }
                        }
                    }
                }
            }

            board = fixedBoard;

            bool boardValid = ConsolidateBoard();
            isBruteForcing = wasBruteForcing;
            return boardValid;
        }

        // Remove "obvious" possibilities from the board
        public bool ConsolidateBoard()
        {
            int numChanges = 0;
            MostComplexLogicUsed = 0;

            bool hadChanges = true;
            while (hadChanges)
            {
                FindResult lastResult = FindResult.None;
                for (int i = 0; lastResult == FindResult.None; i++)
                {
                    switch (i)
                    {
                        case 0:
                            lastResult = FindNakedSingles();
                            if (lastResult == FindResult.None)
                            {
                                lastResult = ConsolidateKillerCages();
                            }
                            break;
                        case 1:
                            lastResult = FindHiddenSingle();
                            if (lastResult != FindResult.None)
                            {
                                MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 1);
                            }
                            break;
                        case 2:
                            // Check for naked tuples (pairs/triples/etc)
                            lastResult = FindNakedTuples();
                            if (lastResult != FindResult.None)
                            {
                                MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 2);
                            }
                            break;
                        case 3:
                            // Find pointing pairs/triples in boxes
                            lastResult = FindPointingTuples();
                            if (lastResult != FindResult.None)
                            {
                                MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 3);
                            }
                            break;
                        case 4:
                            // Find nonconsecutive pairs (if enabled)
                            lastResult = FindNonconsecutivePairs();
                            if (lastResult != FindResult.None)
                            {
                                MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 4);
                            }
                            break;
                        case 5:
                            lastResult = FindDiagonalNonconsecutiveLogic();
                            if (lastResult != FindResult.None)
                            {
                                MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 5);
                            }
                            break;
                        case 6:
                            if (!consolidatingArrows)
                            {
                                lastResult = ConsolidateArrows();
                                if (lastResult != FindResult.None)
                                {
                                    MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 6);
                                }
                            }
                            break;
                        case 7:
                            // Find x-wings/swordfish/jellyfish/etc
                            if (!consolidatingArrows && !isBruteForcing)
                            {
                                lastResult = FindFishes();
                                if (lastResult != FindResult.None)
                                {
                                    MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 7);
                                }
                            }
                            break;
                        case 8:
                            // Find simple contradictions
                            if (!consolidatingArrows && !isBruteForcing)
                            {
                                VerboseOut("Looking for simple contradictions");
                                lastResult = FindSimpleContradictions();
                                if (lastResult == FindResult.Changed)
                                {
                                    MostComplexLogicUsed = Math.Max(MostComplexLogicUsed, 8);
                                    NumSimpleContradictionsUsed++;
                                }
                            }
                            break;
                        default:
                            return true;
                    }
                }

                switch (lastResult)
                {
                    case FindResult.Changed:
                        hadChanges = true;
                        numChanges++;
                        continue;
                    case FindResult.Invalid:
                        return false;
                    case FindResult.PuzzleComplete:
                        return true;
                }
            }
            return true;
        }

        public FindResult FindNakedSingles()
        {
            bool haveChange = false;
            while (true)
            {
                FindResult findResult = FindNakedSinglesHelper();
                switch (findResult)
                {
                    case FindResult.None:
                        return haveChange ? FindResult.Changed : FindResult.None;
                    case FindResult.Changed:
                        haveChange = true;
                        break;
                    default:
                        return findResult;
                }
            }
        }

        public FindResult FindNakedSinglesHelper()
        {
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
#if VERBOSE
                        lastContradictionReason = $"[FindNakedSingles] Contradiction: r{i + 1}c{j + 1} has no possible values.";
#endif
                        return FindResult.Invalid;
                    }

                    if (!IsValueSet(mask))
                    {
                        hasUnsetCells = true;

                        if (ValueCount(mask) == 1)
                        {
                            int value = GetValue(mask);
                            if (!SetValue(i, j, value))
                            {
#if VERBOSE
                                lastContradictionReason = $"[FindNakedSingles] Contradiction: r{i + 1}c{j + 1} cannot be {value}.";
#endif
                                return FindResult.Invalid;
                            }
                            VerboseOut($"[FindNakedSingles] r{i + 1}c{j + 1} set to be {value}.");
                            hadChanges = true;
                        }
                    }
                }
            }
            if (!hasUnsetCells)
            {
                return FindResult.PuzzleComplete;
            }
            return hadChanges ? FindResult.Changed : FindResult.None;
        }

        public enum FindResult
        {
            None,
            Changed,
            Invalid,
            PuzzleComplete
        }
        private FindResult FindHiddenSingle()
        {
            FindResult finalFindResult = FindResult.None;
            foreach (var group in indexGroups.Concat(killerCages.Where(group => group.indexes.Length == MAX_VALUE)))
            {
                if (group.indexes.Length != MAX_VALUE)
                {
                    continue;
                }
                if (!BoxConstraint && group.groupType == GroupType.Box)
                {
                    continue;
                }

                for (int val = 1; val <= MAX_VALUE; val++)
                {
                    uint valMask = 1u << (val - 1);
                    int numWithVal = 0;
                    int vali = 0;
                    int valj = 0;
                    foreach (var pair in group.indexes)
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
#if VERBOSE
                            lastContradictionReason = $"[FindHiddenSingle] Contradiction: r{vali + 1}c{valj + 1} cannot be set to {val}";
#endif
                            return FindResult.Invalid;
                        }
                        VerboseOut($"[FindHiddenSingle] Hidden single in {group} r{vali + 1}c{valj + 1} set to {val}");
                        return FindResult.Changed;
                    }
                    else if (numWithVal == 0)
                    {
#if VERBOSE
                        lastContradictionReason = $"[FindHiddenSingle] Contradiction: {group} has nowhere to place {val}";
#endif
                        return FindResult.Invalid;
                    }
                }
            }
            return finalFindResult;
        }

        private FindResult FindNakedTuples()
        {
            List<(int, int)> unsetCells = new List<(int, int)>(MAX_VALUE);
            for (int tupleSize = 2; tupleSize < 8; tupleSize++)
            {
                foreach (var group in indexGroups.Concat(killerCages))
                {
                    if (!BoxConstraint && group.groupType == GroupType.Box)
                    {
                        continue;
                    }

                    // Make a list of pairs for the group which aren't already filled
                    unsetCells.Clear();
                    foreach (var pair in group.indexes)
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
                            foreach (var curCell in group.indexes)
                            {
                                uint curMask = board[curCell.Item1, curCell.Item2];
                                uint remainingMask = curMask & invCombinationMask;
                                if (remainingMask != 0)
                                {
                                    if (remainingMask != curMask)
                                    {
                                        board[curCell.Item1, curCell.Item2] = remainingMask;
                                        VerboseOut($"[FindNakedTuples] {group} has tuple {MaskToString(combinationMask)}, removing those values from {CellName(curCell)}");
                                        changed = true;
                                    }
                                }
                                else
                                {
                                    numMatching++;
                                }
                            }

                            if (numMatching > tupleSize)
                            {
#if VERBOSE
                                lastContradictionReason = $"[FindNakedTuples] Contradiction: {group} has too many cells ({tupleSize}) which can only have {MaskToString(combinationMask)}";
#endif
                                return FindResult.Invalid;
                            }
                            if (changed)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                }
            }
            return FindResult.None;
        }

        private FindResult FindPointingTuples()
        {
            if (!BoxConstraint)
            {
                return FindResult.None;
            }

            foreach (var group in indexGroups)
            {
                if (group.indexes.Length != MAX_VALUE)
                {
                    continue;
                }
                if (!BoxConstraint && group.groupType == GroupType.Box)
                {
                    continue;
                }

                for (int v = 1; v <= MAX_VALUE; v++)
                {
                    int presentRow = -1;
                    int presentCol = -1;
                    int presentBox = -1;
                    uint valueMask = 1u << (v - 1);
                    foreach (var pair in group.indexes)
                    {
                        int i = pair.Item1;
                        int j = pair.Item2;
                        uint cellMask = board[i, j];
                        if (IsValueSet(cellMask) || (cellMask & valueMask) == 0)
                        {
                            continue;
                        }
                        int box = BoxIndex(i, j);

                        if (presentRow == -1)
                        {
                            presentRow = i;
                        }
                        else if (presentRow != i)
                        {
                            presentRow = -2;
                        }

                        if (presentCol == -1)
                        {
                            presentCol = j;
                        }
                        else if (presentCol != j)
                        {
                            presentCol = -2;
                        }

                        if (presentBox == -1)
                        {
                            presentBox = box;
                        }
                        else if (presentBox != box)
                        {
                            presentBox = -2;
                        }
                    }

                    if (group.groupType == GroupType.Box)
                    {
                        // If all instances of the value in the box are in the same row or column,
                        // then the rest of that row or column can be cleared of this value
                        if (presentRow >= 0)
                        {
                            int i = presentRow;

                            bool changed = false;
                            for (int j = 0; j < WIDTH; j++)
                            {
                                if (BoxIndex(i, j) != group.groupTypeIndex)
                                {
                                    if ((board[i, j] & valueMask) != 0)
                                    {
                                        if (!ClearValue(i, j, v))
                                        {
#if VERBOSE
                                            lastContradictionReason = $"[FindPointingTuples] Contradiction: r{i + 1}c{j + 1} cannot clear {v}";
#endif
                                            return FindResult.Invalid;
                                        }
                                        VerboseOut($"[FindPointingTuples] Pointing tuple on {v}s in {group} removes that value from {CellName((i, j))}");
                                        changed = true;
                                    }
                                }
                            }
                            if (changed)
                            {
                                return FindResult.Changed;
                            }
                        }
                        else if (presentCol >= 0)
                        {
                            int j = presentCol;

                            bool changed = false;
                            for (int i = 0; i < HEIGHT; i++)
                            {
                                if (BoxIndex(i, j) != group.groupTypeIndex)
                                {
                                    if ((board[i, j] & valueMask) != 0)
                                    {
                                        if (!ClearValue(i, j, v))
                                        {
#if VERBOSE
                                            lastContradictionReason = $"[FindPointingTuples] Contradiction: r{i + 1}c{j + 1} cannot clear {v}";
#endif
                                            return FindResult.Invalid;
                                        }
                                        VerboseOut($"[FindPointingTuples] Pointing tuple on {v}s in {group} removes that value from {CellName((i, j))}");
                                        changed = true;
                                    }
                                }
                            }
                            if (changed)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                    else if ((group.groupType == GroupType.Row || group.groupType == GroupType.Col) && presentBox >= 0)
                    {
                        // All instances of the value in the row/col are in the same box,
                        // so the rest of that box can be cleared of this value
                        bool changed = false;
                        int boxi = presentBox / NUM_BOXES_WIDTH;
                        int boxj = presentBox % NUM_BOXES_WIDTH;
                        for (int i = boxi * BOX_HEIGHT; i < boxi * BOX_HEIGHT + BOX_HEIGHT; i++)
                        {
                            if (group.groupType == GroupType.Row && group.groupTypeIndex == i)
                            {
                                continue;
                            }

                            for (int j = boxj * BOX_WIDTH; j < boxj * BOX_WIDTH + BOX_WIDTH; j++)
                            {
                                if (group.groupType == GroupType.Col && group.groupTypeIndex == j)
                                {
                                    continue;
                                }

                                if ((board[i, j] & valueMask) != 0)
                                {
                                    if (!ClearValue(i, j, v))
                                    {
#if VERBOSE
                                        lastContradictionReason = $"[FindPointingTuples] Contradiction: r{i + 1}c{j + 1} cannot clear {v}";
#endif
                                        return FindResult.Invalid;
                                    }
                                    VerboseOut($"[FindPointingTuples] Pointing tuple on {v}s in {group} removes that value from {CellName((i, j))}");
                                    changed = true;
                                }
                            }
                        }
                        if (changed)
                        {
                            return FindResult.Changed;
                        }
                    }
                }
            }
            return FindResult.None;
        }

        private FindResult FindNonconsecutivePairs()
        {
            if (!NonconsecutiveConstraint)
            {
                return FindResult.None;
            }

            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    uint mask = board[i, j];
                    if (IsValueSet(mask))
                    {
                        continue;
                    }

                    int maskValueCount = ValueCount(mask);
                    if (maskValueCount == 2)
                    {
                        int val1 = 0;
                        int val2 = 0;
                        for (int curVal = 1; curVal <= MAX_VALUE; curVal++)
                        {
                            uint valMask = 1u << (curVal - 1);
                            if ((mask & valMask) != 0)
                            {
                                if (val1 == 0)
                                {
                                    val1 = curVal;
                                }
                                else
                                {
                                    val2 = curVal;
                                    break;
                                }
                            }
                        }
                        if (val2 == 0)
                        {
                            continue;
                        }

                        if (val2 - val1 == 1)
                        {
                            // Two consecutive values in a cell means that all of its adjacent cells cannot be either of those values
                            bool haveChanges = false;
                            foreach (var otherPair in AdjacentCells(i, j))
                            {
                                uint otherMask = board[otherPair.Item1, otherPair.Item2];
                                FindResult findResult = ClearMask(otherPair.Item1, otherPair.Item2, mask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[Nonconsecutive] Contradiction: {CellName(i, j)} with values {MaskToString(mask)} removes the only candidates {MaskToString(otherMask)} from {CellName(otherPair)}";
#endif
                                    return FindResult.Invalid;
                                }

                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[Nonconsecutive] {CellName((i, j))} having candidates {MaskToString(mask)} removes those values from {CellName(otherPair)}");
                                    haveChanges = true;
                                }
                            }
                            if (haveChanges)
                            {
                                return FindResult.Changed;
                            }
                        }
                        else if (val2 - val1 == 2)
                        {
                            // Values are 2 apart, which means adjacent cells can't be the value between those two
                            bool haveChanges = false;
                            int bannedVal = val1 + 1;
                            uint clearMask = 1u << (bannedVal - 1);
                            foreach (var otherPair in AdjacentCells(i, j))
                            {
                                uint otherMask = board[otherPair.Item1, otherPair.Item2];
                                FindResult findResult = ClearMask(otherPair.Item1, otherPair.Item2, clearMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[Nonconsecutive] Contradiction: {CellName(i, j)} with values {MaskToString(mask)} removes the only candidate {bannedVal} from {CellName(otherPair)}";
#endif
                                    return FindResult.Invalid;
                                }

                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[Nonconsecutive] {CellName((i, j))} having candidates {MaskToString(mask)} removes {bannedVal} from {CellName(otherPair)}");
                                    haveChanges = true;
                                }
                            }
                            if (haveChanges)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                    else if (maskValueCount == 3)
                    {
                        int val1 = 0;
                        int val2 = 0;
                        int val3 = 0;
                        for (int curVal = 1; curVal <= MAX_VALUE; curVal++)
                        {
                            uint valMask = 1u << (curVal - 1);
                            if ((mask & valMask) != 0)
                            {
                                if (val1 == 0)
                                {
                                    val1 = curVal;
                                }
                                else if (val2 == 0)
                                {
                                    val2 = curVal;
                                }
                                else
                                {
                                    val3 = curVal;
                                    break;
                                }
                            }
                        }
                        if (val3 == 0)
                        {
                            continue;
                        }
                        if (val1 + 1 == val2 && val1 + 2 == val3)
                        {
                            // Three consecutive values means adjacent cells can't be the middle value
                            bool haveChanges = false;
                            int clearValue = val2;
                            uint clearMask = ValueMask(clearValue);
                            foreach (var otherPair in AdjacentCells(i, j))
                            {
                                FindResult findResult = ClearMask(otherPair.Item1, otherPair.Item2, clearMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[Nonconsecutive] Contradiction: {CellName(i, j)} with values {MaskToString(mask)} removes the only candidate {clearValue} from {CellName(otherPair)}";
#endif
                                    return FindResult.Invalid;
                                }

                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[Nonconsecutive] {CellName((i, j))} having candidates {MaskToString(mask)} removes {clearValue} from {CellName(otherPair)}");
                                    haveChanges = true;
                                }
                            }
                            if (haveChanges)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                }
            }

            // Look for groups where a particular digit is locked to 2, 3, or 4 places
            // For the case of 2 places, if they are adjacent then neither can be a consecutive digit
            // For the case of 3 places, if they are all adjacent then the center one cannot be a consecutive digit
            // For all cases, any cell that is adjacent to all of them cannot be a consecutive digit
            // That last one should be a generalized version of the first two if we count a cell as adjacent to itself
            var valInstances = new (int, int)[MAX_VALUE];
            foreach (var group in indexGroups)
            {
                if (group.indexes.Length != MAX_VALUE)
                {
                    continue;
                }
                if (!BoxConstraint && group.groupType == GroupType.Box)
                {
                    continue;
                }

                for (int val = 1; val <= MAX_VALUE; val++)
                {
                    uint valMask = 1u << (val - 1);
                    int numValInstances = 0;
                    foreach (var pair in group.indexes)
                    {
                        uint mask = board[pair.Item1, pair.Item2];
                        if (IsValueSet(mask))
                        {
                            if ((mask & valMask) != 0)
                            {
                                numValInstances = 0;
                                break;
                            }
                            continue;
                        }
                        if ((mask & valMask) != 0)
                        {
                            valInstances[numValInstances++] = pair;
                        }
                    }
                    if (numValInstances >= 2 && numValInstances <= 5)
                    {
                        bool tooFar = false;
                        var firstCell = valInstances[0];
                        var minCoord = firstCell;
                        var maxCoord = firstCell;
                        for (int i = 1; i < numValInstances; i++)
                        {
                            var curCell = valInstances[i];
                            int curDist = TaxicabDistance(firstCell.Item1, firstCell.Item2, curCell.Item1, curCell.Item2);
                            if (curDist > 2)
                            {
                                tooFar = true;
                                break;
                            }
                            minCoord = (Math.Min(minCoord.Item1, curCell.Item1), Math.Min(minCoord.Item2, curCell.Item2));
                            maxCoord = (Math.Max(maxCoord.Item1, curCell.Item1), Math.Max(maxCoord.Item2, curCell.Item2));
                        }

                        if (!tooFar)
                        {
                            int consecVal1 = val - 1;
                            int consecVal2 = val + 1;
                            uint consecMask1 = consecVal1 >= 1 && consecVal1 <= MAX_VALUE ? 1u << (consecVal1 - 1) : 0u;
                            uint consecMask2 = consecVal2 >= 1 && consecVal2 <= MAX_VALUE ? 1u << (consecVal2 - 1) : 0u;
                            uint consecMask = consecMask1 | consecMask2;

                            bool changed = false;
                            for (int i = minCoord.Item1; i <= maxCoord.Item1; i++)
                            {
                                for (int j = minCoord.Item2; j <= maxCoord.Item2; j++)
                                {
                                    uint otherMask = board[i, j];
                                    if (IsValueSet(otherMask) || (otherMask & consecMask) == 0)
                                    {
                                        continue;
                                    }

                                    bool allAdjacent = true;
                                    for (int valIndex = 0; valIndex < numValInstances; valIndex++)
                                    {
                                        var curCell = valInstances[valIndex];
                                        if (!IsAdjacent(i, j, curCell.Item1, curCell.Item2))
                                        {
                                            allAdjacent = false;
                                            break;
                                        }
                                    }
                                    if (allAdjacent)
                                    {
                                        FindResult clearResult = ClearMask(i, j, consecMask);
                                        if (clearResult == FindResult.Invalid)
                                        {
#if VERBOSE
                                            lastContradictionReason = $"[Nonconsecutive] Contradiction: {group} has {val} always adjacent to {CellName(i, j)}, but cannot clear values {MaskToString(consecMask)} from that cell.";
#endif
                                            return FindResult.Invalid;
                                        }
                                        if (clearResult == FindResult.Changed)
                                        {
                                            VerboseOut($"[Nonconsecutive] {group} has {val} always adjacent to {CellName(i, j)}, removing {MaskToString(consecMask)} from that cell.");
                                            changed = true;
                                        }
                                    }
                                }
                            }
                            if (changed)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                }
            }

            // Look for adjacent squares with a shared value plus two consecutive values.
            // The shared value must be in one of those two squares, eliminating it from
            // the rest of their shared groups.
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    (int, int) cellA = (i, j);
                    uint maskA = board[i, j];
                    if (IsValueSet(maskA) || ValueCount(maskA) > 3)
                    {
                        continue;
                    }
                    for (int d = 0; d < 2; d++)
                    {
                        if (d == 0 && i == HEIGHT - 1)
                        {
                            continue;
                        }
                        if (d == 1 && j == WIDTH - 1)
                        {
                            continue;
                        }
                        (int, int) cellB = d == 0 ? (i + 1, j) : (i, j + 1);
                        uint maskB = board[cellB.Item1, cellB.Item2];
                        if (IsValueSet(maskB))
                        {
                            continue;
                        }

                        uint combinedMask = maskA | maskB;
                        if (ValueCount(combinedMask) != 3)
                        {
                            continue;
                        }
                        int valA = 0;
                        int valB = 0;
                        int valC = 0;
                        for (int v = 1; v <= MAX_VALUE; v++)
                        {
                            if ((combinedMask & (1u << (v - 1))) != 0)
                            {
                                if (valA == 0)
                                {
                                    valA = v;
                                }
                                else if (valB == 0)
                                {
                                    valB = v;
                                }
                                else
                                {
                                    valC = v;
                                    break;
                                }
                            }
                        }
                        int mustHaveVal = 0;
                        if (valA + 1 == valB)
                        {
                            mustHaveVal = valC;
                        }
                        else if (valB + 1 == valC)
                        {
                            mustHaveVal = valA;
                        }
                        bool haveChanges = false;
                        if (mustHaveVal != 0)
                        {
                            uint mustHaveMask = 1u << (mustHaveVal - 1);
                            foreach (var otherCell in SeenCells(cellA, cellB))
                            {
                                if (otherCell == cellA || otherCell == cellB)
                                {
                                    continue;
                                }

                                uint otherMask = board[otherCell.Item1, otherCell.Item2];
                                if (IsValueSet(otherMask))
                                {
                                    continue;
                                }
                                FindResult findResult = ClearMask(otherCell.Item1, otherCell.Item2, mustHaveMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[Nonconsecutive] Contradiction: {CellName(i, j)} with candidates {MaskToString(maskA)} and {CellName(cellB)} with candidates {MaskToString(maskB)} are adjacent, meaning they must contain {mustHaveVal}, but cannot clear that value from {CellName(otherCell)}.";
#endif
                                    return FindResult.Invalid;
                                }
                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[Nonconsecutive] {CellName(i, j)} with candidates {MaskToString(maskA)} and {CellName(cellB)} with candidates {MaskToString(maskB)} are adjacent, meaning they must contain {mustHaveVal}, clearing it from {CellName(otherCell)}.");
                                    haveChanges = true;
                                }
                            }
                        }
                        if (haveChanges)
                        {
                            return FindResult.Changed;
                        }
                    }
                }
            }
            return FindResult.None;
        }

        public FindResult FindDiagonalNonconsecutiveLogic()
        {
            if (!DiagonalNonconsecutiveConstraint)
            {
                return FindResult.None;
            }

            // Look for single cells that can eliminate on its diagonals.
            // Some eliminations can only happen within the same box.
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    uint mask = board[i, j];
                    if (IsValueSet(mask))
                    {
                        continue;
                    }

                    int valueCount = ValueCount(mask);
                    if (valueCount <= 3)
                    {
                        int minValue = MinValue(mask);
                        int maxValue = MaxValue(mask);
                        if (maxValue - minValue == 2)
                        {
                            // Values 2 apart will always remove the center value, but if there are 3 candidates this only applies to the same box
                            bool haveChanges = false;
                            int removeValue = minValue + 1;
                            uint removeValueMask = ValueMask(removeValue);
                            foreach (var cell in DiagonalCells(i, j, valueCount != 2))
                            {
                                FindResult findResult = ClearMask(cell.Item1, cell.Item2, removeValueMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[DiagonalNonconsecutive] Contradiction: r{i+1}c{j+1} removes the only candidate {removeValue} from r{cell.Item1+1}c{cell.Item2+1}";
#endif
                                    return FindResult.Invalid;
                                }

                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[DiagonalNonconsecutive] {CellName((i, j))} having candidates {MaskToString(mask)} remove {removeValue} from r{cell.Item1 + 1}c{cell.Item2 + 1}");
                                    haveChanges = true;
                                }
                            }
                            if (haveChanges)
                            {
                                return FindResult.Changed;
                            }
                        }
                        else if (maxValue - minValue == 1)
                        {
                            // Values 1 apart will always remove both values, but only for diagonals in the same box
                            bool haveChanges = false;
                            uint removeValueMask = ValueMask(minValue) | ValueMask(maxValue);
                            foreach (var cell in DiagonalCells(i, j, true))
                            {
                                FindResult findResult = ClearMask(cell.Item1, cell.Item2, removeValueMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[DiagonalNonconsecutive] Contradiction: r{i+1}c{j+1} removes the only candidates {MaskToString(removeValueMask)} from r{cell.Item1+1}c{cell.Item2+1}";
#endif
                                    return FindResult.Invalid;
                                }

                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[DiagonalNonconsecutive] {CellName((i, j))} having candidates {minValue}{maxValue} remove those values from r{cell.Item1 + 1}c{cell.Item2 + 1}");
                                    haveChanges = true;
                                }
                            }
                            if (haveChanges)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                }
            }

            // Look for groups where a particular digit is locked to 2, 3, or 4 places
            // Any cell that is diagonal to all of them cannot be either consecutive digit
            var valInstances = new (int, int)[MAX_VALUE];
            foreach (var group in indexGroups)
            {
                if (group.indexes.Length != MAX_VALUE)
                {
                    continue;
                }
                if (!BoxConstraint && group.groupType == GroupType.Box)
                {
                    continue;
                }

                for (int val = 1; val <= MAX_VALUE; val++)
                {
                    uint valMask = 1u << (val - 1);
                    int numValInstances = 0;
                    foreach (var pair in group.indexes)
                    {
                        uint mask = board[pair.Item1, pair.Item2];
                        if (IsValueSet(mask))
                        {
                            if ((mask & valMask) != 0)
                            {
                                numValInstances = 0;
                                break;
                            }
                            continue;
                        }
                        if ((mask & valMask) != 0)
                        {
                            valInstances[numValInstances++] = pair;
                        }
                    }
                    if (numValInstances >= 2 && numValInstances <= 5)
                    {
                        bool tooFar = false;
                        var firstCell = valInstances[0];
                        var minCoord = firstCell;
                        var maxCoord = firstCell;
                        for (int i = 1; i < numValInstances; i++)
                        {
                            var curCell = valInstances[i];
                            int curDist = TaxicabDistance(firstCell.Item1, firstCell.Item2, curCell.Item1, curCell.Item2);
                            if (curDist > 5)
                            {
                                tooFar = true;
                                break;
                            }
                            minCoord = (Math.Min(minCoord.Item1, curCell.Item1), Math.Min(minCoord.Item2, curCell.Item2));
                            maxCoord = (Math.Max(maxCoord.Item1, curCell.Item1), Math.Max(maxCoord.Item2, curCell.Item2));
                        }

                        if (!tooFar)
                        {
                            int consecVal1 = val - 1;
                            int consecVal2 = val + 1;
                            uint consecMask1 = consecVal1 >= 1 && consecVal1 <= MAX_VALUE ? ValueMask(consecVal1) : 0u;
                            uint consecMask2 = consecVal2 >= 1 && consecVal2 <= MAX_VALUE ? ValueMask(consecVal2) : 0u;
                            uint consecMask = consecMask1 | consecMask2;

                            bool changed = false;
                            for (int i = minCoord.Item1 - 1; i <= maxCoord.Item1 + 1; i++)
                            {
                                if (i < 0 || i > 8)
                                {
                                    continue;
                                }

                                for (int j = minCoord.Item2 - 1; j <= maxCoord.Item2 + 1; j++)
                                {
                                    if (j < 0 || j > 8)
                                    {
                                        continue;
                                    }

                                    uint otherMask = board[i, j];
                                    if (IsValueSet(otherMask) || (otherMask & consecMask) == 0)
                                    {
                                        continue;
                                    }

                                    bool allDiagonal = true;
                                    for (int valIndex = 0; valIndex < numValInstances; valIndex++)
                                    {
                                        var curCell = valInstances[valIndex];
                                        if (curCell != (i, j) && !IsDiagonal(i, j, curCell.Item1, curCell.Item2))
                                        {
                                            allDiagonal = false;
                                            break;
                                        }
                                    }
                                    if (allDiagonal)
                                    {
                                        FindResult clearResult = ClearMask(i, j, consecMask);
                                        if (clearResult == FindResult.Invalid)
                                        {
#if VERBOSE
                                            lastContradictionReason = $"[DiagonalNonconsecutive] Contradiction: r{i + 1}c{j + 1} cannot clear values {MaskToString(consecMask)}";
#endif
                                            return FindResult.Invalid;
                                        }
                                        if (clearResult == FindResult.Changed)
                                        {
                                            VerboseOut($"[DiagonalNonconsecutive] Value {val} is locked to {numValInstances} cells in {group}, removing {MaskToString(consecMask)} from r{i + 1}c{j + 1}");
                                            changed = true;
                                        }
                                    }
                                }
                            }
                            if (changed)
                            {
                                return FindResult.Changed;
                            }
                        }
                    }
                }
            }

            // Look for diagonal squares in the same box with a shared value plus two consecutive values.
            // The shared value must be in one of those two squares, eliminating it from
            // the rest of their shared box.
            for (int i = 0; i < HEIGHT; i++)
            {
                for (int j = 0; j < WIDTH; j++)
                {
                    (int, int) cellA = (i, j);
                    uint maskA = board[i, j];
                    if (IsValueSet(maskA) || ValueCount(maskA) > 3)
                    {
                        continue;
                    }
                    foreach (var cellB in DiagonalCells(i, j, true))
                    {
                        uint maskB = board[cellB.Item1, cellB.Item2];
                        if (IsValueSet(maskB))
                        {
                            continue;
                        }

                        uint combinedMask = maskA | maskB;
                        if (ValueCount(combinedMask) != 3)
                        {
                            continue;
                        }
                        int valA = 0;
                        int valB = 0;
                        int valC = 0;
                        for (int v = 1; v <= MAX_VALUE; v++)
                        {
                            if ((combinedMask & (1u << (v - 1))) != 0)
                            {
                                if (valA == 0)
                                {
                                    valA = v;
                                }
                                else if (valB == 0)
                                {
                                    valB = v;
                                }
                                else
                                {
                                    valC = v;
                                    break;
                                }
                            }
                        }
                        int mustHaveVal = 0;
                        if (valA + 1 == valB)
                        {
                            mustHaveVal = valC;
                        }
                        else if (valB + 1 == valC)
                        {
                            mustHaveVal = valA;
                        }
                        bool haveChanges = false;
                        if (mustHaveVal != 0)
                        {
                            uint mustHaveMask = ValueMask(mustHaveVal);
                            foreach (var otherCell in SeenCells(cellA, cellB))
                            {
                                if (otherCell == cellA || otherCell == cellB)
                                {
                                    continue;
                                }

                                uint otherMask = board[otherCell.Item1, otherCell.Item2];
                                if (IsValueSet(otherMask))
                                {
                                    continue;
                                }
                                FindResult findResult = ClearMask(otherCell.Item1, otherCell.Item2, mustHaveMask);
                                if (findResult == FindResult.Invalid)
                                {
#if VERBOSE
                                    lastContradictionReason = $"[DiagonalNonconsecutive] Contradiction: r{otherCell.Item1 + 1}c{otherCell.Item2 + 1} cannot clear values {MaskToString(mustHaveMask)}";
#endif
                                    return FindResult.Invalid;
                                }
                                if (findResult == FindResult.Changed)
                                {
                                    VerboseOut($"[DiagonalNonconsecutive] r{i + 1}c{j + 1} and r{cellB.Item1 + 1}c{cellB.Item2 + 1} have combined mask {MaskToString(combinedMask)}, removing {mustHaveVal} from r{otherCell.Item1 + 1}c{otherCell.Item2 + 1}");
                                    haveChanges = true;
                                }
                            }
                        }
                        if (haveChanges)
                        {
                            return FindResult.Changed;
                        }
                    }
                }
            }

            return FindResult.None;
        }

        public FindResult ConsolidateKillerCages()
        {
            bool changed = false;
            foreach (var group in killerCages)
            {
                if (group.sumCombinations == null || group.sumCombinations.Count == 0)
                {
                    continue;
                }

                // Reduce the remaining cell options
                int numUnset = 0;
                int setSum = 0;
                uint valueUsedMask = 0;
                uint valuePresentMask = 0;
                List<List<int>> validCombinations = group.sumCombinations.ToList();
                foreach (var curCell in group.indexes)
                {
                    uint cellMask = board[curCell.Item1, curCell.Item2];
                    valuePresentMask |= (cellMask & ~valueSetMask);
                    if (IsValueSet(cellMask))
                    {
                        int curValue = GetValue(cellMask);
                        setSum += curValue;
                        validCombinations.RemoveAll(list => !list.Contains(curValue));
                        valueUsedMask |= (cellMask & ~valueSetMask);
                    }
                    else
                    {
                        numUnset++;
                    }
                }

                // Remove combinations which require a value which isn't present
                validCombinations.RemoveAll(list => list.Any(v => (valuePresentMask & ValueMask(v)) == 0));

                if (validCombinations.Count == 0)
                {
                    // Sum is no longer possible
#if VERBOSE
                    lastContradictionReason = $"[KillerCages] Contradiction: {group} has no more valid combinations which sum to {group.sum}.";
#endif
                    return FindResult.Invalid;
                }

                if (numUnset > 0)
                {
                    uint valueRemainingMask = 0;
                    foreach (var combination in validCombinations)
                    {
                        foreach (int v in combination)
                        {
                            valueRemainingMask |= ValueMask(v);
                        }
                    }
                    valueRemainingMask &= ~valueUsedMask;

                    var unsetCells = group.indexes.Where(cell => !IsValueSet(board[cell.Item1, cell.Item2])).ToList();
                    var unsetCombinations = validCombinations.Select(list => list.Where(v => (valueUsedMask & ValueMask(v)) == 0).ToList()).ToList();
                    var unsetCellCurMasks = unsetCells.Select(cell => board[cell.Item1, cell.Item2]).ToList();
                    uint[] unsetCellNewMasks = new uint[unsetCells.Count];
                    foreach (var curCombination in unsetCombinations)
                    {
                        foreach (var permutation in curCombination.Permuatations())
                        {
                            bool permutationValid = true;
                            for (int i = 0; i < permutation.Count; i++)
                            {
                                uint cellMask = unsetCellCurMasks[i];
                                uint permValueMask = ValueMask(permutation[i]);
                                if ((cellMask & permValueMask) == 0)
                                {
                                    permutationValid = false;
                                    break;
                                }
                            }

                            if (permutationValid)
                            {
                                for (int i = 0; i < permutation.Count; i++)
                                {
                                    unsetCellNewMasks[i] |= ValueMask(permutation[i]);
                                }
                            }
                        }
                    }

                    for (int i = 0; i < unsetCells.Count; i++)
                    {
                        var curCell = unsetCells[i];
                        uint cellMask = board[curCell.Item1, curCell.Item2];
                        uint newCellMask = unsetCellNewMasks[i];
                        if (newCellMask != cellMask)
                        {
                            changed = true;
                            if (!SetMask(curCell.Item1, curCell.Item2, newCellMask))
                            {
                                // Cell has no values remaining
#if VERBOSE
                                lastContradictionReason = $"[KillerCages] Contradiction: {CellName(curCell)} has no more remaining values.";
#endif
                                return FindResult.Invalid;
                            }
                            VerboseOut($"[KillerCages] {CellName(curCell)} reduced to possibilities: {MaskToString(newCellMask)}");
                        }
                    }

#if false
                    foreach (var curCell in unsetCells)
                    {
                        uint cellMask = board[curCell.Item1, curCell.Item2];
                        uint newCellMask = cellMask & valueRemainingMask;
                        if (newCellMask != cellMask)
                        {
                            changed = true;
                            if (!SetMask(curCell.Item1, curCell.Item2, newCellMask))
                            {
                                // Cell has no values remaining
#if VERBOSE
                                lastContradictionReason = $"[KillerCages] Contradiction: {CellName(curCell)} has no more remaining values.";
#endif
                                return FindResult.Invalid;
                            }
                            VerboseOut($"[KillerCages] {CellName(curCell)} reduced to possibilities: {MaskToString(newCellMask)}");
                        }
                    }
#endif
                }
                else
                {
                    // Ensure the sum is correct
                    if (setSum != group.sum)
                    {
#if VERBOSE
                        lastContradictionReason = $"[KillerCages] Contradiction: {group} sums to {setSum} instead of {group.sum}.";
#endif
                        return FindResult.Invalid;
                    }
                }
            }

            return changed ? FindResult.Changed : FindResult.None;
        }

        public FindResult ConsolidateArrows()
        {
            // Disabled until this code can be fixed
            return FindResult.None;

#if false
            if (arrowSums.Count == 0)
            {
                return FindResult.None;
            }

            bool changed = false;
            foreach (var arrow in arrowSums)
            {
                // Don't bother if all the values in the arrow are already set
                if (arrow.HasSumValue(this) && arrow.HasShaftValue(this))
                {
                    continue;
                }

                // Just in case...
                if (arrow.SumCells.Length != 1 && arrow.SumCells.Length != 2)
                {
                    continue;
                }

                var findResult = arrow.SumCells.Length == 1 ? ConsolidateSingleSumArrow(arrow) : ConsolidateDoubleSumArrow(arrow);
                if (findResult == FindResult.Invalid)
                {
#if VERBOSE
                    lastContradictionReason = $"[Arrow] Contradiction: Arrow with sum starting at {CellName(arrow.SumCells[0])} cannot be fulfilled.";
#endif
                    return FindResult.Invalid;
                }
                else if (findResult == FindResult.Changed)
                {
                    changed = true;
                }
            }

            return changed ? FindResult.Changed : FindResult.None;
#endif
        }

        private FindResult ConsolidateSingleSumArrow(ArrowSum arrow)
        {
            var sumCell = arrow.SumCells[0];
            uint sumCellMask = board[sumCell.Item1, sumCell.Item2];
            uint[] possibleShaftValues = new uint[arrow.ShaftCells.Length];
            if (IsValueSet(sumCellMask))
            {
                FindResult findResult = ConsolidateArrowShaft(arrow, possibleShaftValues);
                if (findResult == FindResult.Invalid)
                {
                    return FindResult.Invalid;
                }

                findResult = ApplyArrowShaft(arrow, possibleShaftValues);
                switch (findResult)
                {
                    case FindResult.Invalid:
                        return FindResult.Invalid;
                    case FindResult.Changed:
                        return FindResult.Changed;
                }
                return FindResult.None;
            }

            bool changed = false;
            for (int v = 1; v <= 9; v++)
            {
                if (HasValue(sumCellMask, v))
                {
                    var boardCopy = Clone();
                    boardCopy.consolidatingArrows = true;
                    if (boardCopy.SetValue(sumCell.Item1, sumCell.Item2, v) && boardCopy.ConsolidateBoard())
                    {
                        var findResult = boardCopy.ConsolidateArrowShaft(arrow, possibleShaftValues);
                        if (findResult == FindResult.Invalid)
                        {
                            VerboseOut($"[Arrow] {CellName(sumCell)} cannot be value {v} because the sum can't be fulfilled.");
                            if (!ClearValue(sumCell.Item1, sumCell.Item2, v))
                            {
                                return FindResult.Invalid;
                            }
                            changed = true;
                        }
                        else
                        {
                            for (int x = 0; x < arrow.ShaftCells.Length; x++)
                            {
                                var (i, j) = arrow.ShaftCells[x];
                                if (!IsValueSet(board[i, j]))
                                {
                                    possibleShaftValues[x] |= boardCopy.board[i, j] & ~valueSetMask;
                                }
                            }
                        }
                    }
                    else
                    {
                        VerboseOut($"[Arrow] {CellName(sumCell)} cannot be value {v} because it breaks the board restrictions.");
                        if (!ClearValue(sumCell.Item1, sumCell.Item2, v))
                        {
                            return FindResult.Invalid;
                        }
                        changed = true;
                    }
                }
            }

            {
                var findResult = ApplyArrowShaft(arrow, possibleShaftValues);
                if (findResult == FindResult.Invalid)
                {
                    return FindResult.Invalid;
                }
                if (findResult == FindResult.Changed)
                {
                    changed = true;
                }
            }
            return changed ? FindResult.Changed : FindResult.None;
        }

        private FindResult ConsolidateDoubleSumArrow(ArrowSum arrow)
        {
            var sumCell1 = arrow.SumCells[0];
            var sumCell2 = arrow.SumCells[1];
            uint sumCellMask1 = board[sumCell1.Item1, sumCell1.Item2];
            bool sumCellHasValue1 = ValueCount(sumCellMask1) == 1;
            uint sumCellMask2 = board[sumCell2.Item1, sumCell2.Item2];
            bool sumCellHasValue2 = ValueCount(sumCellMask2) == 1;
            uint[] possibleShaftValues = new uint[arrow.ShaftCells.Length];
            bool changed = false;
            if (sumCellHasValue1 && sumCellHasValue2)
            {
                FindResult findResult = ConsolidateArrowShaft(arrow, possibleShaftValues);
                if (findResult == FindResult.Invalid)
                {
                    return FindResult.Invalid;
                }
            }
            else if (sumCellHasValue1)
            {
                int v1 = GetValue(sumCellMask1);
                int v2min = MinValue(sumCellMask2);
                int v2max = MaxValue(sumCellMask2);
                for (int v2 = v2min; v2 <= v2max; v2++)
                {
                    if (!HasValue(sumCellMask2, v2))
                    {
                        continue;
                    }

                    var boardv2 = Clone();
                    boardv2.consolidatingArrows = true;

                    FindResult DoInvalidBoard()
                    {
                        VerboseOut($"[Arrow] Sum at {CellName(sumCell1)}{CellName(sumCell2)} cannot be value {v1 * 10 + v2} because the sum can't be fulfilled. Removing candidate {v2} from {CellName(sumCell2)}");
                        if (!ClearValue(sumCell2.Item1, sumCell2.Item2, v2))
                        {
                            return FindResult.Invalid;
                        }
                        return FindResult.Changed;
                    }

                    if (boardv2.SetValue(sumCell2.Item1, sumCell2.Item2, v2) && boardv2.ConsolidateBoard())
                    {
                        var findResult = boardv2.ConsolidateArrowShaft(arrow, possibleShaftValues);
                        if (findResult == FindResult.Invalid)
                        {
                            FindResult invalidBoardResult = DoInvalidBoard();
                            switch (invalidBoardResult)
                            {
                                case FindResult.Invalid:
                                    return FindResult.Invalid;
                                case FindResult.Changed:
                                    changed = true;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        FindResult invalidBoardResult = DoInvalidBoard();
                        switch (invalidBoardResult)
                        {
                            case FindResult.Invalid:
                                return FindResult.Invalid;
                            case FindResult.Changed:
                                changed = true;
                                break;
                        }
                    }
                }
            }
            else if (sumCellHasValue2)
            {
                int v2 = GetValue(sumCellMask2);
                int v1min = MinValue(sumCellMask1);
                int v1max = MaxValue(sumCellMask1);
                for (int v1 = v1min; v1 <= v1max; v1++)
                {
                    if (!HasValue(sumCellMask1, v1))
                    {
                        continue;
                    }

                    var boardv1 = Clone();
                    boardv1.consolidatingArrows = true;

                    FindResult DoInvalidBoard()
                    {
                        VerboseOut($"[Arrow] Sum at {CellName(sumCell1)}{CellName(sumCell2)} cannot be value {v1 * 10 + v2} because the sum can't be fulfilled. Removing candidate {v1} from {CellName(sumCell1)}");
                        if (!ClearValue(sumCell1.Item1, sumCell1.Item2, v1))
                        {
                            return FindResult.Invalid;
                        }
                        return FindResult.Changed;
                    }

                    if (boardv1.SetValue(sumCell1.Item1, sumCell1.Item2, v1) && boardv1.ConsolidateBoard())
                    {
                        var findResult = boardv1.ConsolidateArrowShaft(arrow, possibleShaftValues);
                        if (findResult == FindResult.Invalid)
                        {
                            FindResult invalidBoardResult = DoInvalidBoard();
                            switch (invalidBoardResult)
                            {
                                case FindResult.Invalid:
                                    return FindResult.Invalid;
                                case FindResult.Changed:
                                    changed = true;
                                    break;
                            }
                        }
                    }
                    else
                    {
                        FindResult invalidBoardResult = DoInvalidBoard();
                        switch (invalidBoardResult)
                        {
                            case FindResult.Invalid:
                                return FindResult.Invalid;
                            case FindResult.Changed:
                                changed = true;
                                break;
                        }
                    }
                }
            }
            else
            {
                int v1min = MinValue(sumCellMask1);
                int v1max = MaxValue(sumCellMask1);
                uint v1ValidMask = 0;
                uint v2ValidMask = 0;
                for (int v1 = v1min; v1 <= v1max; v1++)
                {
                    if (!HasValue(sumCellMask1, v1))
                    {
                        continue;
                    }

                    var boardv1 = Clone();
                    boardv1.consolidatingArrows = true;
                    if (!sumCellHasValue1 && (!boardv1.SetValue(sumCell1.Item1, sumCell1.Item2, v1) || !boardv1.ConsolidateBoard()))
                    {
                        // Remove this possibility
                        VerboseOut($"[Arrow] Sum at {CellName(sumCell1)}{CellName(sumCell2)} cannot have tens digit {v1} due to board constraints.");
                        if (!ClearValue(sumCell1.Item1, sumCell1.Item2, v1))
                        {
                            return FindResult.Invalid;
                        }
                        changed = true;
                        continue;
                    }

                    uint v1sumCellMask2 = boardv1.board[sumCell2.Item1, sumCell2.Item2];
                    bool v1sumCellHasValue2 = ValueCount(sumCellMask2) == 1;
                    int v2min = v1sumCellHasValue2 ? GetValue(v1sumCellMask2) : MinValue(v1sumCellMask2);
                    int v2max = v1sumCellHasValue2 ? GetValue(v1sumCellMask2) : MaxValue(v1sumCellMask2);
                    for (int v2 = v2min; v2 <= v2max; v2++)
                    {
                        if (!HasValue(v1sumCellMask2, v2))
                        {
                            continue;
                        }

                        var boardv2 = boardv1.Clone();
                        boardv2.consolidatingArrows = true;

                        if ((sumCellHasValue1 || boardv2.SetValue(sumCell1.Item1, sumCell1.Item2, v1)) &&
                            (sumCellHasValue2 || boardv2.SetValue(sumCell2.Item1, sumCell2.Item2, v2)) &&
                            boardv2.ConsolidateBoard())
                        {
                            var findResult = boardv2.ConsolidateArrowShaft(arrow, possibleShaftValues);
                            if (findResult != FindResult.Invalid)
                            {
                                v1ValidMask |= ValueMask(v1);
                                v2ValidMask |= ValueMask(v2);
                            }
                        }
                    }
                }

                if (v1ValidMask == 0 || v2ValidMask == 0)
                {
                    return FindResult.Invalid;
                }
                if (sumCellMask1 != v1ValidMask)
                {
                    VerboseOut($"[Arrow] Sum at {CellName(sumCell1)}{CellName(sumCell2)} has reduced tens digit to candidates {MaskToString(v1ValidMask)}.");
                    if (!SetMask(sumCell1.Item1, sumCell1.Item2, v1ValidMask))
                    {
                        return FindResult.Invalid;
                    }
                    changed = true;
                }
                if (sumCellMask2 != v2ValidMask)
                {
                    VerboseOut($"[Arrow] Sum at {CellName(sumCell1)}{CellName(sumCell2)} has reduced ones digit to candidates {MaskToString(v2ValidMask)}.");
                    if (!SetMask(sumCell2.Item1, sumCell2.Item2, v2ValidMask))
                    {
                        return FindResult.Invalid;
                    }
                    changed = true;
                }
            }

            FindResult applyArrowShaftResult = ApplyArrowShaft(arrow, possibleShaftValues);
            switch (applyArrowShaftResult)
            {
                case FindResult.Invalid:
                    return FindResult.Invalid;
                case FindResult.Changed:
                    changed = true;
                    break;
            }
            return changed ? FindResult.Changed : FindResult.None;
        }

        /// <summary>
        /// Recursively determine whether the sum is possible, and if so, reduce the candidates to the set of possible ways to make the sum.
        /// </summary>
        /// <param name="arrow"></param>
        /// <returns></returns>
        private FindResult ConsolidateArrowShaft(ArrowSum arrow, uint[] possibleShaftValues, int nextShaftIndex = 0)
        {
            // Find the first unset value
            int unsetCellIndex = -1;
            for (int x = nextShaftIndex; x < arrow.ShaftCells.Length; x++)
            {
                var (i, j) = arrow.ShaftCells[x];
                uint mask = board[i, j] & ~valueSetMask;
                if (mask == 0)
                {
                    return FindResult.Invalid;
                }
                if (ValueCount(mask) > 1)
                {
                    unsetCellIndex = x;
                    break;
                }
            }
            if (unsetCellIndex == -1)
            {
                return FindResult.None;
            }

            var unsetCell = arrow.ShaftCells[unsetCellIndex];
            uint unsetCellMask = board[unsetCell.Item1, unsetCell.Item2];

            int minValue = MinValue(unsetCellMask);
            int maxValue = MaxValue(unsetCellMask);
            bool anyValid = false;
            for (int v = minValue; v <= maxValue; v++)
            {
                uint valueMask = ValueMask(v);
                if ((unsetCellMask & valueMask) == 0)
                {
                    continue;
                }

                var boardCopy = Clone();
                boardCopy.consolidatingArrows = true;
                if (boardCopy.SetValue(unsetCell.Item1, unsetCell.Item2, v))
                {
                    var findResult = boardCopy.ConsolidateArrowShaft(arrow, possibleShaftValues, unsetCellIndex + 1);
                    if (findResult != FindResult.Invalid)
                    {
                        possibleShaftValues[unsetCellIndex] |= valueMask;
                        anyValid = true;
                    }
                }
            }

            return anyValid ? FindResult.None : FindResult.Invalid;
        }

        private FindResult ApplyArrowShaft(ArrowSum arrow, uint[] shaftValues)
        {
            bool changed = false;
            for (int x = 0; x < arrow.ShaftCells.Length; x++)
            {
                uint shaftValueMask = shaftValues[x] & ~valueSetMask;
                if (shaftValueMask == 0)
                {
                    return FindResult.Invalid;
                }

                var (i, j) = arrow.ShaftCells[x];
                if (ValueCount(board[i, j]) > 1 && shaftValueMask != board[i, j])
                {
                    if (ValueCount(shaftValueMask) == 1)
                    {
                        int v = GetValue(shaftValueMask);
                        VerboseOut($"[Arrow] {CellName(i, j)} can only be value {v} based on the arrow sum possibilities.");
                        if (!SetValue(i, j, v))
                        {
                            return FindResult.Invalid;
                        }
                        changed = true;
                    }
                    else
                    {
                        VerboseOut($"[Arrow] {CellName(i, j)} has been reduced to candidates {MaskToString(shaftValueMask)} based on the arrow sum possibilities.");
                        board[i, j] = shaftValueMask;
                        changed = true;
                    }
                }
            }
            return changed ? FindResult.Changed : FindResult.None;
        }

        private FindResult FindFishes()
        {
#pragma warning disable CS0162
            if (WIDTH != MAX_VALUE || HEIGHT != MAX_VALUE)
            {
                return FindResult.None;
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
#if VERBOSE
                                string rowName = isCol ? "cols" : "rows";
                                lastContradictionReason = $"[FindFishes] Contradiction: Too many {rowName} ({fishRows.Count}) have only {n} locations for {v}";
#endif
                                return FindResult.Invalid;
                            }
                            if (fishRows.Count == n && notFishRows.Count > 0)
                            {
                                bool changed = false;
                                foreach (int curRow in notFishRows)
                                {
                                    for (int curCol = 0; curCol < width; curCol++)
                                    {
                                        if ((refMask & (1u << curCol)) != 0)
                                        {
                                            int i = isCol ? curCol : curRow;
                                            int j = isCol ? curRow : curCol;
                                            if (!ClearValue(i, j, v))
                                            {
#if VERBOSE
                                                lastContradictionReason = $"[FindFishes] Contradiction: Cannot clear value {v} from r{i + 1}c{j + 1}";
#endif
                                                return FindResult.Invalid;
                                            }
                                            changed = true;
                                        }
                                    }
                                }

                                if (changed)
                                {
#if VERBOSE
                                    string rowName = isCol ? "c" : "r";
                                    string desc = "";
                                    foreach (int curRow in fishRows)
                                    {
                                        desc = $"{desc}{rowName}{curRow + 1}";
                                    }
                                    string techniqueName = n switch
                                    {
                                        2 => "X-Wing",
                                        3 => "Swordfish",
                                        4 => "Jellyfish",
                                        _ => $"{n}-Fish",
                                    };
                                    VerboseOut($"{techniqueName} found on {desc} for value {v}");
#endif
                                    return FindResult.Changed;
                                }
                            }
                        }
                    }
                }
            }

            return FindResult.None;
        }

        private FindResult FindSimpleContradictions()
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
                                uint valueMask = (1u << (v - 1));
                                if ((cellMask & valueMask) != 0)
                                {
                                    SudokuSolver boardCopy = Clone();
                                    boardCopy.isBruteForcing = true;
                                    if (!boardCopy.SetValue(i, j, v) || !boardCopy.ConsolidateBoard())
                                    {
#if VERBOSE
                                        Print();
                                        Console.WriteLine($"Setting r{i + 1}c{j + 1} to {v} causes a contradiction:");
                                        Console.Write(boardCopy.findingContradictionSteps.ToString());
                                        Console.WriteLine($"Contradiction: {boardCopy.lastContradictionReason}");
                                        boardCopy.Print();
#endif
                                        if (!ClearValue(i, j, v))
                                        {
#if VERBOSE
                                            lastContradictionReason = $"[FindSimpleContradictions] Contradiction: Cannot clear value {v} from r{i + 1}c{j + 1}";
#endif
                                            return FindResult.Invalid;
                                        }
                                        return FindResult.Changed;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return FindResult.None;
        }

        public static int TaxicabDistance(int i0, int j0, int i1, int j1) => Math.Abs(i0 - i1) + Math.Abs(j0 - j1);
        public static bool IsAdjacent(int i0, int j0, int i1, int j1) => i0 == i1 && Math.Abs(j0 - j1) <= 1 || j0 == j1 && Math.Abs(i0 - i1) <= 1;
        public static bool IsDiagonal(int i0, int j0, int i1, int j1) => (i0 == i1 - 1 || i0 == i1 + 1) && (j0 == j1 - 1 || j0 == j1 + 1);
        public static string CellName((int, int) cell) => $"r{cell.Item1 + 1}c{cell.Item2 + 1}";
        public static string CellName(int i, int j) => CellName((i, j));

        public void VerboseOut(string s)
        {
            if (consolidatingArrows)
            {
                return;
            }

#if VERBOSE
            if (!findingContradiction)
            {
                Console.WriteLine(s);
            }
            else
            {
                findingContradictionSteps.Append("    ");
                findingContradictionSteps.Append(s);
                findingContradictionSteps.Append('\n');
            }
#endif
        }
    }
}
