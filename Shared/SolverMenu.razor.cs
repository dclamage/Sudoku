using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
        private bool lastActionWasStep = false;

        public bool RespectFilledMarks
        {
            get => _respectFilledMarks;
            set
            {
                if (_respectFilledMarks != value)
                {
                    _respectFilledMarks = value;
                    if (!value)
                    {
                        RespectCenterMarks = false;
                    }
                }
            }
        }
        private bool _respectFilledMarks = false;

        public bool RespectCenterMarks
        {
            get => _respectCenterMarks;
            set
            {
                if (_respectCenterMarks != value)
                {
                    _respectCenterMarks = value;
                    if (value)
                    {
                        RespectFilledMarks = true;
                    }
                }
            }
        }
        private bool _respectCenterMarks = false;

        private const int spinnerDelay = 2000;
        private int spinnerToken = 0;
        private bool displaySpinner = false;
        private bool needSnapshot = false;

        private List<string> ConsoleLines { get; } = new List<string>();

        private SudokuBoard Board => _board ??= IndexPage.SudokuBoard;
        private SudokuBoard _board = null;

        // Element References
        private ElementReference consoleScroll;

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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await ScrollConsoleToBottom();
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
            await solverService.RegisterEventListenerAsync<(string, uint[])>(nameof(SudokuSolveService.LogicalSolveProgress), ReceiveLogicalSolveProgress);
            await solverService.RegisterEventListenerAsync<(string, uint[])>(nameof(SudokuSolveService.LogicalSolveCompleted), ReceiveLogicalSolveCompleted);
            await solverService.RegisterEventListenerAsync<(string, uint[])>(nameof(SudokuSolveService.LogicalStepCompleted), ReceiveLogicalStepCompleted);
        }

        private async Task ScrollConsoleToBottom()
        {
            await JS.InvokeVoidAsync("scrollToBottom", consoleScroll);
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

        private async Task ShowDelayedSpinner()
        {
            int curSpinnerToken = ++spinnerToken;
            await Task.Delay(spinnerDelay);
            if (SolveInProgress && curSpinnerToken == spinnerToken)
            {
                displaySpinner = true;
                StateHasChanged();
            }
        }

        private async Task LogicalStep()
        {
            SolveInProgress = true;
            displaySpinner = false;
            solveCancelled = false;
            if (!lastActionWasStep)
            {
                ConsoleLines.Clear();
                lastActionWasStep = true;
            }

            if (!RespectFilledMarks)
            {
                needSnapshot |= Board.Values.ResetToGivens();
            }
            uint[] cellValues = Board.Values.GetCellCandidates(RespectFilledMarks, RespectCenterMarks);
            string[] constraints = Board.Values.ConstraintStrings;
            RespectFilledMarks = true;
            RespectCenterMarks = true;

            await initSolverWorkersTask;
            await solverService.RunAsync(s => s.PrepSolve());
            await solverService.RunAsync(s => s.LogicalStep(cellValues, constraints));
            await ShowDelayedSpinner();
        }

        private void ReceiveLogicalStepCompleted(object _, (string, uint[]) parameters)
        {
            ConsoleLines.Add(parameters.Item1);
            if (parameters.Item2 != null)
            {
                needSnapshot |= Board.Values.SetAllCenterPencilMarks(parameters.Item2, SudokuValues.SingleValueBehavior.RespectValueSetBit);
            }
            if (needSnapshot)
            {
                Board.StoreSnapshot();
            }

            needSnapshot = false;
            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }

        private async Task LogicalSolve()
        {
            lastActionWasStep = false;
            SolveInProgress = true;
            solveCancelled = false;
            displaySpinner = false;
            needSnapshot = false;
            ConsoleLines.Clear();

            if (!RespectFilledMarks)
            {
                needSnapshot |= Board.Values.ResetToGivens();
            }
            uint[] cellValues = Board.Values.GetCellCandidates(RespectFilledMarks, RespectCenterMarks);
            string[] constraints = Board.Values.ConstraintStrings;

            await initSolverWorkersTask;
            await solverService.RunAsync(s => s.PrepSolve());
            await solverService.RunAsync(s => s.LogicalSolve(cellValues, constraints));
            await ShowDelayedSpinner();
        }

        private void ReceiveLogicalSolveProgress(object _, (string, uint[]) parameters)
        {
            ConsoleLines.Add(parameters.Item1);
            needSnapshot |= Board.Values.SetAllCenterPencilMarks(parameters.Item2, SudokuValues.SingleValueBehavior.RespectValueSetBit);
            StateHasChanged();
        }

        private void ReceiveLogicalSolveCompleted(object _, (string, uint[]) parameters)
        {
            ConsoleLines.Add(parameters.Item1);
            if (parameters.Item2 != null)
            {
                needSnapshot |= Board.Values.SetAllCenterPencilMarks(parameters.Item2, SudokuValues.SingleValueBehavior.RespectValueSetBit);
            }
            if (needSnapshot)
            {
                Board.StoreSnapshot();
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
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
            lastActionWasStep = false;
            SolveInProgress = true;
            solveCancelled = false;
            displaySpinner = false;
            ConsoleLines.Clear();

            if (!RespectFilledMarks)
            {
                needSnapshot |= Board.Values.ResetToGivens();
            }
            uint[] cellValues = Board.Values.GetCellCandidates(RespectFilledMarks, RespectCenterMarks);
            string[] constraints = Board.Values.ConstraintStrings;

            await initSolverWorkersTask;
            await solverService.RunAsync(s => s.PrepSolve());
            await solverService.RunAsync(s => s.Solve(cellValues, constraints, random));
            await ShowDelayedSpinner();
        }

        private void ReceiveSolveResult(object _, int[] solveResult)
        {
            if (solveCancelled)
            {
                ConsoleLines.Add("Solve Cancelled.");
            }
            else if (solveResult == null)
            {
                ConsoleLines.Add("Puzzle has no solutions.");
            }
            else
            {
                needSnapshot |= Board.Values.SetAllCellValues(solveResult);
            }
            if (needSnapshot)
            {
                Board.StoreSnapshot();
            }

            needSnapshot = false;
            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }

        private async Task CountSolutions()
        {
            lastActionWasStep = false;
            SolveInProgress = true;
            solveCancelled = false;
            displaySpinner = false;
            ReceiveSolutionCountProgress(null, 0);

            uint[] cellValues = Board.Values.GetCellCandidates(RespectFilledMarks, RespectCenterMarks);
            string[] constraints = Board.Values.ConstraintStrings;

            await initSolverWorkersTask;
            await solverService.RunAsync(s => s.PrepSolve());
            await solverService.RunAsync(s => s.CountSolutions(cellValues, constraints, 0));
            await ShowDelayedSpinner();
        }

        private void ReceiveSolutionCountProgress(object _, ulong count)
        {
            ConsoleLines.Clear();
            ConsoleLines.Add($"Found {count} solutions so far...");
            StateHasChanged();
        }

        private void ReceiveSolutionCountResult(object _, ulong count)
        {
            ConsoleLines.Clear();
            if (solveCancelled)
            {
                ConsoleLines.Add($"Found {count} solutions before canceling.");
            }
            else
            {
                ConsoleLines.Add($"Found {count} solutions.");
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }

        private void FillRuleCandidates()
        {
            bool resetBoard = false;
            if (!RespectFilledMarks)
            {
                resetBoard = Board.Values.ResetToGivens();
            }
            int[] board = Board.Values.GetCellValues(RespectFilledMarks);
            string[] constraints = Board.Values.ConstraintStrings;
            SudokuSolver solver = SudokuSolveService.CreateSolver(board, constraints);
            if (Board.Values.SetAllCenterPencilMarks(solver.FlatBoard, SudokuValues.SingleValueBehavior.RespectValueSetBit) || resetBoard)
            {
                Board.StoreSnapshot();
            }
        }

        private async Task FillTrueCandidates()
        {
            lastActionWasStep = false;
            SolveInProgress = true;
            solveCancelled = false;
            displaySpinner = false;
            needSnapshot = false;
            ReceiveCandidateProgress(null, (0, null));

            if (!RespectFilledMarks)
            {
                needSnapshot |= Board.Values.ResetToGivens();
            }
            int[] cellValues = Board.Values.GetCellValues(RespectFilledMarks);
            string[] constraints = Board.Values.ConstraintStrings;

            await initSolverWorkersTask;
            await solverService.RunAsync(s => s.PrepSolve());
            await solverService.RunAsync(s => s.FindRealCandidates(cellValues, constraints));
            await ShowDelayedSpinner();
        }

        private void ReceiveCandidateProgress(object _, (int, uint[]) parameters)
        {
            ConsoleLines.Clear();
            ConsoleLines.Add($"{parameters.Item1} / 81 cells computed.");
            if (parameters.Item2 != null)
            {
                needSnapshot |= Board.Values.SetAllCenterPencilMarks(parameters.Item2, SudokuValues.SingleValueBehavior.AlwaysKeepAsPencilmark);
            }
            StateHasChanged();
        }

        private void ReceiveCandidateSolution(object _, uint[] candidates)
        {
            ConsoleLines.Clear();
            if (solveCancelled)
            {
                ConsoleLines.Add($"Finding candidates cancelled.");
            }
            else if (candidates == null || candidates.All(mask => mask == 0))
            {
                ConsoleLines.Add($"There are no solutions.");
            }
            else if (candidates != null)
            {
                ConsoleLines.Add($"All candidates filled.");
            }

            if (candidates != null)
            {
                if (Board.Values.SetAllCenterPencilMarks(candidates, SudokuValues.SingleValueBehavior.SingleValueAlwaysSetAsValue) || needSnapshot)
                {
                    Board.StoreSnapshot();
                }
            }

            solveCancelled = false;
            SolveInProgress = false;
            StateHasChanged();
        }
    }
}
