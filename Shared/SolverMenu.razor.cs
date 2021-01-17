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
        private bool ShowSpinner { get; set; } = false;
        private bool IsWorking { get; set; } = false;

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

                var values = Board.Values;
                for (int i = 0; i < 81; i++)
                {
                    values.SetCellValue(i, solveResult[i]);
                }
            }

            solveCancelled = false;
            SolveInProgress = false;
            IsWorking = false;
            ShowSpinner = false;
            StateHasChanged();
        }

        private async Task CancelSolve()
        {
            if (SolveInProgress)
            {
                await solverService.RunAsync(s => s.Cancel());
                solveCancelled = true;
            }
        }

        private async void LogicalSolve()
        {
        }

        private async Task SolvePuzzle()
        {
            IsWorking = true;
            ShowSpinner = true;
            SolveInProgress = true;
            solveCancelled = false;

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

            await solverService.RunAsync(s => s.Solve(cellValues));
        }

        private async void RandomSolvePuzzle()
        {
        }

        private async void CountSolutions()
        {
        }

        private async void FillTrueCandidates()
        {
        }
    }
}
