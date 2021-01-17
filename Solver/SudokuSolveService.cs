﻿using System;
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
        public event EventHandler<ulong> SolutionCountProgressEvent;
        public event EventHandler<ulong> SolutionCountCompleteEvent;
        public event EventHandler<(int, uint[])> CandidatesProgressEvent;
        public event EventHandler<uint[]> CandidatesSolutionEvent;

        public void PrepSolve()
        {
            if (cancellationToken == null || cancellationToken.IsCancellationRequested)
            {
                cancellationToken?.Dispose();
                cancellationToken = new CancellationTokenSource();
            }
        }

        public void Solve(int[] board, bool random)
        {
            if (board.Length != SolverUtility.NUM_CELLS)
            {
                SolveResultEvent?.Invoke(this, null);
                return;
            }

            SudokuSolver solver = CreateSolver(board);
            if (solver == null)
            {
                SolveResultEvent?.Invoke(this, null);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    if (!random)
                    {
                        if (!await solver.FindSolution(cancellationToken.Token))
                        {
                            SolveResultEvent?.Invoke(this, null);
                            return;
                        }
                    }
                    else
                    {
                        if (!await solver.FindRandomSolution(cancellationToken.Token))
                        {
                            SolveResultEvent?.Invoke(this, null);
                            return;
                        }
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
            }, cancellationToken.Token);
        }

        public void CountSolutions(int[] board, ulong maxSolutions)
        {
            if (board.Length != SolverUtility.NUM_CELLS)
            {
                SolutionCountCompleteEvent?.Invoke(this, 0);
                return;
            }

            SudokuSolver solver = CreateSolver(board);
            if (solver == null)
            {
                SolutionCountCompleteEvent?.Invoke(this, 0);
                return;
            }

            Task.Run(async () =>
            {
                ulong count = await solver.CountSolutions(maxSolutions, SolutionCountProgressEvent, cancellationToken.Token);
                SolutionCountCompleteEvent?.Invoke(null, count);
            }, cancellationToken.Token);
        }

        public void FindRealCandidates(int[] board)
        {
            if (board.Length != SolverUtility.NUM_CELLS)
            {
                CandidatesSolutionEvent?.Invoke(this, null);
                return;
            }

            SudokuSolver solver = CreateSolver(board);
            if (solver == null)
            {
                CandidatesSolutionEvent?.Invoke(this, null);
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await solver.FillRealCandidates(CandidatesProgressEvent, CandidatesSolutionEvent, cancellationToken.Token);
                }
                catch (OperationCanceledException)
                {
                    cancellationToken.Dispose();
                    cancellationToken = null;
                    CandidatesSolutionEvent?.Invoke(this, null);
                    return;
                }
            }, cancellationToken.Token);
        }

        private static SudokuSolver CreateSolver(int[] board)
        {
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
                            return null;
                        }
                    }
                }
            }
            return solver;
        }

        public void Cancel()
        {
            cancellationToken?.Cancel();
        }
    }
}
