using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using SudokuBlazor.Shared;

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
        private SolverMenu solverMenu;

        // Element accessors
        public SudokuBoard SudokuBoard => sudokuBoard;
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
    }
}
