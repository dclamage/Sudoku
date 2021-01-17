using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using BlazorWorker.Core;
using BlazorWorker.BackgroundServiceFactory;
using BlazorWorker.WorkerBackgroundService;
using SudokuBlazor.Solver;

namespace SudokuBlazor.Shared
{
    public partial class SolverMenu
    {
        [Parameter]
        public Pages.Index IndexPage { get; set; }

        public bool SolveInProgress
        {
            get => _solveInProgress;
            set
            {
                if (!_solveInProgress == value)
                {
                    _solveInProgress = value;
                    Board.SolveInProgress = value;
                }
            }
        }
        private bool _solveInProgress = false;

        private string ConsoleText { get; set; } = "";

        private SudokuBoard Board => _board ??= IndexPage.SudokuBoard;
        private SudokuBoard _board = null;
        
        // Web Workers
        private Task initSolverWorkersTask;
        private IWorker solverWorker;
        private IWorkerBackgroundService<SudokuSolveService> solverService;
        private bool solveCancelled = false;

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                initSolverWorkersTask = InitSolverService();
            }
        }

        public void SetConsoleText(string text)
        {
            ConsoleText = text ?? "";
            StateHasChanged();
        }

        private async Task InitSolverService()
        {
            solverWorker = await workerFactory.CreateAsync();
            solverService = await solverWorker.CreateBackgroundServiceAsync<SudokuSolveService>();
            await solverService.RegisterEventListenerAsync<int[]>(nameof(SudokuSolveService.SolveResultEvent), ReceiveSolveResult);
            await solverService.RegisterEventListenerAsync<ulong>(nameof(SudokuSolveService.SolutionCountProgressEvent), ReceiveSolutionCountProgress);
            await solverService.RegisterEventListenerAsync<ulong>(nameof(SudokuSolveService.SolutionCountCompleteEvent), ReceiveSolutionCountResult);
            await solverService.RegisterEventListenerAsync<(int, uint[])>(nameof(SudokuSolveService.CandidatesProgressEvent), ReceiveCandidateProgress);
            await solverService.RegisterEventListenerAsync<uint[]>(nameof(SudokuSolveService.CandidatesSolutionEvent), ReceiveCandidateSolution);
        }

        private void ReceiveSolveResult(object _, int[] solveResult)
        {
            if (solveCancelled)
            {
                Snackbar.Add($"Solve Cancelled.", Severity.Warning);
            }
            else if (solveResult == null)
            {
                Snackbar.Add($"Puzzle has no solutions!", Severity.Error);
            }
            else
            {
                Board.StoreSnapshot();
                Board.Values.SetAllCellValues(solveResult);
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }

        private async Task CancelSolve()
        {
            if (SolveInProgress && !solveCancelled)
            {
                solveCancelled = true;
                StateHasChanged();

                await solverService.RunAsync(s => s.Cancel());
            }
        }

        private async Task LogicalSolve()
        {
        }

        private async Task SolvePuzzle()
        {
            await SolvePuzzle(false);
        }

        private async Task RandomSolvePuzzle()
        {
            await SolvePuzzle(true);
        }

        private async Task SolvePuzzle(bool random)
        {
            SolveInProgress = true;
            solveCancelled = false;
            ConsoleText = "";

            int[] cellValues = Board.Values.CellValues;

            await initSolverWorkersTask;
            if (solveCancelled)
            {
                ReceiveSolveResult(null, null);
                return;
            }

            await solverService.RunAsync(s => s.PrepSolve());
            if (solveCancelled)
            {
                ReceiveSolveResult(null, null);
                return;
            }

            await solverService.RunAsync(s => s.Solve(cellValues, random));
        }

        private async Task CountSolutions()
        {
            SolveInProgress = true;
            solveCancelled = false;
            ReceiveSolutionCountProgress(null, 0);

            int[] cellValues = Board.Values.CellValues;

            await initSolverWorkersTask;
            if (solveCancelled)
            {
                ReceiveSolutionCountResult(null, 0);
                return;
            }

            await solverService.RunAsync(s => s.PrepSolve());
            if (solveCancelled)
            {
                ReceiveSolutionCountResult(null, 0);
                return;
            }

            await solverService.RunAsync(s => s.CountSolutions(cellValues, 0));
        }

        private void ReceiveSolutionCountProgress(object _, ulong count)
        {
            ConsoleText = $"Found {count} solutions so far...";
            StateHasChanged();
        }

        private void ReceiveSolutionCountResult(object _, ulong count)
        {
            if (solveCancelled)
            {
                ConsoleText = $"Found {count} solutions before canceling.";
            }
            else
            {
                ConsoleText = $"Found {count} solutions.";
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }

        private async Task FillTrueCandidates()
        {
            SolveInProgress = true;
            solveCancelled = false;
            Board.StoreSnapshot();
            ReceiveCandidateProgress(null, (0, null));

            int[] cellValues = Board.Values.CellValues;

            await initSolverWorkersTask;
            if (solveCancelled)
            {
                ReceiveCandidateSolution(null, null);
                return;
            }

            await solverService.RunAsync(s => s.PrepSolve());
            if (solveCancelled)
            {
                ReceiveCandidateSolution(null, null);
                return;
            }

            await solverService.RunAsync(s => s.FindRealCandidates(cellValues));
        }

        private void FillCandidates(uint[] candidates)
        {
            Board.Values.SetAllCenterPencilMarks(candidates);
        }

        private void ReceiveCandidateProgress(object _, (int, uint[]) parameters)
        {
            ConsoleText = $"{parameters.Item1} / 81 cells computed.";
            if (parameters.Item2 != null)
            {
                FillCandidates(parameters.Item2);
            }
            StateHasChanged();
        }

        private void ReceiveCandidateSolution(object _, uint[] candidates)
        {
            if (solveCancelled)
            {
                ConsoleText = $"Finding candidates cancelled.";
            }
            else if (candidates == null || candidates.All(mask => mask == 0))
            {
                ConsoleText = $"There are no solutions.";
            }
            else if (candidates != null)
            {
                ConsoleText = $"All candidates filled.";
            }

            if (candidates != null)
            {
                FillCandidates(candidates);
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }
    }
}
