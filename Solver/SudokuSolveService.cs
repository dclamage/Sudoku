using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SudokuBlazor.Solver
{
    public class SudokuSolveService
    {
        const int H = SolverUtility.HEIGHT;
        const int W = SolverUtility.WIDTH;
        const int N = SolverUtility.NUM_CELLS;

        private CancellationTokenSource cancellationToken;

        public event EventHandler<int[]> SolveResultEvent;

        public void PrepSolve()
        {
            if (cancellationToken == null || cancellationToken.IsCancellationRequested)
            {
                cancellationToken?.Dispose();
                cancellationToken = new CancellationTokenSource();
            }
        }

        public void Solve(int[] board)
        {
            if (board.Length != SolverUtility.NUM_CELLS)
            {
                SolveResultEvent?.Invoke(this, null);
                return;
            }

            SudokuSolver solver = new SudokuSolver();

            for (int i = 0; i < H; i++)
            {
                for (int j = 0; j < W; j++)
                {
                    int cellIndex = i * W + j;
                    int curValue = board[cellIndex];
                    if (curValue >= 1 && curValue <= 9)
                    {
                        if (!solver.SetValue(i, j, curValue))
                        {
                            SolveResultEvent?.Invoke(this, null);
                            return;
                        }
                    }
                }
            }

            Task.Run(() =>
            {
                try
                {
                    if (!solver.FindSolution(cancellationToken.Token))
                    {
                        SolveResultEvent?.Invoke(this, null);
                        return;
                    }
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                    SolveResultEvent?.Invoke(this, null);
                    return;
                }

                int[] result = new int[N];
                for (int i = 0; i < H; i++)
                {
                    for (int j = 0; j < W; j++)
                    {
                        int cellIndex = i * W + j;
                        result[cellIndex] = SudokuSolver.GetValue(solver.Board[i, j]);
                    }
                }
                SolveResultEvent?.Invoke(this, result);
            });
        }

        public void Cancel()
        {
            cancellationToken?.Cancel();
        }
    }
}
