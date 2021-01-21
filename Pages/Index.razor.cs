using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SudokuBlazor.Shared;
using SudokuBlazor.Solver.Constraints;

namespace SudokuBlazor.Pages
{
    partial class Index
    {
        // Parameters
        [Parameter]
        public string Givens { get; set; }

        // Element References
        private ElementReference appbarDiv;
        private ElementReference drawerDiv;
        private SudokuBoard sudokuBoard;
        private PuzzleInfoMenu puzzleInfoMenu;
        private ConstructionMenu constructionMenu;
        private SolverMenu solverMenu;

        // Element accessors
        public SudokuBoard SudokuBoard => sudokuBoard;
        public PuzzleInfoMenu PuzzleInfoMenu => puzzleInfoMenu;
        public ConstructionMenu ConstructionMenu => constructionMenu;
        public SolverMenu SolverMenu => solverMenu;

        // State
        public bool DrawerOpen
        {
            get => _drawerOpen;
            set
            {
                if (_drawerOpen == value)
                {
                    return;
                }
                _drawerOpen = value;
                StateHasChanged();
            }
        }
        private bool _drawerOpen = false;

        // Title
        private string puzzleName = "Sudoku";
        private string author = "";

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("setAppbarHeight", appbarDiv);
            if (DrawerOpen)
            {
                await JS.InvokeVoidAsync("setDrawerSize", drawerDiv);
            }
            else
            {
                await JS.InvokeVoidAsync("setDrawerSize");
            }
        }

        void ToggleDrawer()
        {
            DrawerOpen = !DrawerOpen;
        }

        void PuzzleNameChanged(string puzzleName)
        {
            this.puzzleName = string.IsNullOrWhiteSpace(puzzleName) ? "Sudoku" : puzzleName;
            StateHasChanged();
        }

        void AuthorChanged(string author)
        {
            this.author = author;
            StateHasChanged();
        }

        void EditingToggled(bool enabled)
        {
            sudokuBoard.EditingEnabled = enabled;
        }

        void GlobalConstraintToggled(GlobalConstraints constraint, bool enabled)
        {
            if (enabled)
            {
                Constraint newConstraint = null;
                switch (constraint)
                {
                    case GlobalConstraints.King:
                        newConstraint = new KingConstraint();
                        break;
                    case GlobalConstraints.Knight:
                        newConstraint = new KnightConstraint();
                        break;
                    case GlobalConstraints.Nonconsecutive:
                        newConstraint = new NonconsecutiveConstraint();
                        break;
                    case GlobalConstraints.DiagNonconsecutive:
                        newConstraint = new DiagonalNonconsecutiveConstraint();
                        break;
                    case GlobalConstraints.DisjointGroups:
                        foreach (var curConstraint in DisjointGroupsConstraint.All(9))
                        {
                            sudokuBoard.Values.AddConstraint(-(int)constraint - 100 * curConstraint.GroupIndex, curConstraint);
                        }
                        break;
                }
                if (newConstraint != null)
                {
                    sudokuBoard.Values.AddConstraint(-(int)constraint, newConstraint);
                }
            }
            else if (constraint == GlobalConstraints.DisjointGroups)
            {
                for (int i = 0; i < 9; i++)
                {
                    sudokuBoard.Values.RemoveConstraint(-(int)constraint - 100 * i);
                }
            }
            else
            {
                sudokuBoard.Values.RemoveConstraint(-(int)constraint);
            }
        }
    }
}
