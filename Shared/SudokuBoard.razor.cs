using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    public partial class SudokuBoard
    {
        // Parameters
        [Parameter]
        public string Givens { get; set; }

        public bool EditingEnabled { get; set; }

        public bool SolveInProgress
        {
            get => _solveInProgress;
            set
            {
                if (_solveInProgress != value)
                {
                    _solveInProgress = value;
                    keypad.SolveInProgress = value;
                    SetDirty();
                }
            }
        }
        private bool _solveInProgress = false;

        // Element References
        private ElementReference sudokusvg;

        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;

        // Input
        private bool mouseDown = false;
        private long touchIdDown = -1;
        private double inputLastX = 0.0;
        private double inputLastY = 0.0;

        // Components
        private SudokuColoring coloring;
        private SudokuSelection selection;
        private SudokuValues values;
        private SudokuKeypad keypad;

        // Component accessors
        public SudokuColoring Coloring => coloring;
        public SudokuSelection Selection => selection;
        public SudokuValues Values => values;
        public SudokuKeypad Keypad => keypad;

        // Services
        private readonly UndoHistory undoHistory = new UndoHistory();

        protected override void OnInitialized()
        {
            Givens ??= "";
        }

        protected override void OnAfterRender(bool firstRender)
        {
            if (firstRender)
            {
                Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
                Snackbar.Configuration.SnackbarVariant = Variant.Filled;

                if (Givens.Length == 81)
                {
                    for (int cellIndex = 0; cellIndex < 81; cellIndex++)
                    {
                        char givenChar = Givens[cellIndex];
                        if (char.IsDigit(givenChar) && givenChar > '0')
                        {
                            values.SetGiven(cellIndex, givenChar - '0');
                        }
                    }
                }

                StoreSnapshot();
            }
        }

        protected async Task SelectCellAtLocation(double clientX, double clientY, bool controlDown, bool shiftDown, bool altDown)
        {
            var infoFromJs = await JS.InvokeAsync<string>("getSVG_XY", sudokusvg, clientX, clientY);
            var xysplit = infoFromJs.Split(" ");
            double x = double.Parse(xysplit[0]);
            double y = double.Parse(xysplit[1]);

            int i = (int)Math.Floor(y / cellRectWidth);
            int j = (int)Math.Floor(x / cellRectWidth);
            selection.SelectCell(i, j, controlDown, shiftDown, altDown);
        }

        protected async Task InputStart(double clientX, double clientY, bool ctrlKey, bool shiftKey, bool altKey)
        {
            if (!ctrlKey && !shiftKey && !altKey)
            {
                selection.SelectNone();
            }
            selection.ResetLastSelectedCell();
            await SelectCellAtLocation(clientX, clientY, ctrlKey, shiftKey, altKey);
            inputLastX = clientX;
            inputLastY = clientY;

            if (selection.HasSelectedCells())
            {
                await SetFocus(sudokusvg);
            }
        }

        protected async Task MouseDown(MouseEventArgs e)
        {
            if (touchIdDown != -1 || mouseDown)
            {
                return;
            }

            mouseDown = true;
            await InputStart(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
        }

        protected async Task TouchStart(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length == 0 || mouseDown)
            {
                return;
            }

            TouchPoint touch = null;
            if (touchIdDown != -1)
            {
                foreach (var curTouch in e.ChangedTouches)
                {
                    if (curTouch.Identifier == touchIdDown)
                    {
                        touch = curTouch;
                        break;
                    }
                }
                if (touch == null)
                {
                    return;
                }
            }
            else
            {
                touch = e.ChangedTouches[0];
                touchIdDown = touch.Identifier;
            }

            await InputStart(touch.ClientX, touch.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
        }

        protected async Task InputMove(double clientX, double clientY, bool ctrlKey, bool shiftKey, bool altKey)
        {
            var boundingRect = await GetBoundingClientRect(sudokusvg);

            double mouseDiffX = clientX - inputLastX;
            double mouseDiffY = clientY - inputLastY;
            double mouseDiffLen = Math.Sqrt(mouseDiffX * mouseDiffX + mouseDiffY * mouseDiffY);
            double stepSizeX = boundingRect.Width / 18.0;
            double stepSizeY = boundingRect.Height / 18.0;
            if (mouseDiffLen > stepSizeX && mouseDiffLen > stepSizeY)
            {
                double mouseDiffInvLen = 1.0 / Math.Sqrt(mouseDiffX * mouseDiffX + mouseDiffY * mouseDiffY);
                mouseDiffX *= stepSizeX * mouseDiffInvLen;
                mouseDiffY *= stepSizeY * mouseDiffInvLen;
                inputLastX += mouseDiffX;
                inputLastY += mouseDiffY;
                while ((mouseDiffX > 0.1 && inputLastX < clientX || mouseDiffX < 0.1 && inputLastX > clientX)
                    || (mouseDiffY > 0.1 && inputLastY < clientY || mouseDiffY < 0.1 && inputLastY > clientY))
                {
                    await SelectCellAtLocation(inputLastX, inputLastY, ctrlKey, shiftKey, altKey);
                    inputLastX += mouseDiffX;
                    inputLastY += mouseDiffY;
                }
            }

            await SelectCellAtLocation(clientX, clientY, ctrlKey, shiftKey, altKey);
            inputLastX = clientX;
            inputLastY = clientY;
        }

        protected async Task MouseMove(MouseEventArgs e)
        {
            if (mouseDown)
            {
                await InputMove(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
            }
        }

        protected async Task TouchMove(TouchEventArgs e)
        {
            if (touchIdDown != -1)
            {
                foreach (var touch in e.ChangedTouches)
                {
                    if (touch.Identifier == touchIdDown)
                    {
                        await InputMove(touch.ClientX, touch.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
                    }
                }
            }
        }

        protected async Task InputEnd(double clientX, double clientY, bool ctrlKey, bool shiftKey, bool altKey)
        {
            await SelectCellAtLocation(clientX, clientY, ctrlKey, shiftKey, altKey);
        }

        protected async Task MouseUp(MouseEventArgs e)
        {
            if (mouseDown)
            {
                await InputEnd(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
                mouseDown = false;
            }
        }

        protected async Task TouchEnd(TouchEventArgs e)
        {
            if (touchIdDown != -1)
            {
                foreach (var touch in e.ChangedTouches)
                {
                    if (touch.Identifier == touchIdDown)
                    {
                        await InputEnd(touch.ClientX, touch.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
                        touchIdDown = -1;
                        return;
                    }
                }
            }
        }

        private enum KeyCodeType
        {
            DeleteCell,
            Value,
            A,
            Y,
            Z,
            MoveUp,
            MoveDown,
            MoveLeft,
            MoveRight,
            RotateMarkMode,
            Ignore
        }
        // Very useful website to get keycodes: https://keycode.info/ (Look at event.code on bottom right)
        private static (KeyCodeType, int) GetKeyCodeType(string keyCode)
        {
            switch (keyCode)
            {
                case "Delete":
                case "Backspace":
                case "Digit0":
                case "Numpad0":
                    return (KeyCodeType.DeleteCell, 0);
                case "KeyA":
                    return (KeyCodeType.A, -1);
                case "KeyY":
                    return (KeyCodeType.Y, -1);
                case "KeyZ":
                    return (KeyCodeType.Z, -1);
                case "ArrowUp":
                    return (KeyCodeType.MoveUp, -1);
                case "ArrowDown":
                    return (KeyCodeType.MoveDown, -1);
                case "ArrowLeft":
                    return (KeyCodeType.MoveLeft, -1);
                case "ArrowRight":
                    return (KeyCodeType.MoveRight, -1);
                case "Space":
                    return (KeyCodeType.RotateMarkMode, -1);
            }
            if (keyCode.StartsWith("Digit"))
            {
                return (KeyCodeType.Value, keyCode[5] - '0');
            }
            if (keyCode.StartsWith("Numpad"))
            {
                return (KeyCodeType.Value, keyCode[6] - '0');
            }
            return (KeyCodeType.Ignore, -1);
        }

        protected void KeyDown(KeyboardEventArgs e)
        {
            var (keyCodeType, value) = GetKeyCodeType(e.Code);
            switch (keyCodeType)
            {
                case KeyCodeType.DeleteCell:
                    CellValueEntered(0);
                    return;
                case KeyCodeType.A:
                    if (e.CtrlKey)
                    {
                        selection.SelectAll();
                    }
                    return;
                case KeyCodeType.Y:
                    if (e.CtrlKey)
                    {
                        Redo();
                    }
                    return;
                case KeyCodeType.Z:
                    if (e.CtrlKey)
                    {
                        Undo();
                    }
                    return;
                case KeyCodeType.Value:
                    CellValueEntered(value);
                    return;
                case KeyCodeType.MoveUp:
                    selection.Move(SudokuSelection.MoveDir.Up, e.CtrlKey, e.ShiftKey, e.AltKey);
                    return;
                case KeyCodeType.MoveDown:
                    selection.Move(SudokuSelection.MoveDir.Down, e.CtrlKey, e.ShiftKey, e.AltKey);
                    return;
                case KeyCodeType.MoveLeft:
                    selection.Move(SudokuSelection.MoveDir.Left, e.CtrlKey, e.ShiftKey, e.AltKey);
                    return;
                case KeyCodeType.MoveRight:
                    selection.Move(SudokuSelection.MoveDir.Right, e.CtrlKey, e.ShiftKey, e.AltKey);
                    return;
                case KeyCodeType.RotateMarkMode:
                    keypad.CurrentMarkMode = (SudokuKeypad.MarkMode)((int)(keypad.CurrentMarkMode + 1) % (int)SudokuKeypad.MarkMode.Max);
                    return;
            }
        }

        protected void FocusLost()
        {
            selection.SelectNone();
        }

        protected void NumpadKeyPressed(int value)
        {
            CellValueEntered(value);
        }

        protected void CustomColorPressed(string value)
        {
            bool hasChange = false;
            foreach (int cellIndex in selection.SelectedCellIndices())
            {
                hasChange |= coloring.ColorCell(cellIndex, value);
            }
            if (hasChange)
            {
                StoreSnapshot();
            }
        }

        private void CellValueEntered(int value)
        {
            if (SolveInProgress)
            {
                return;
            }

            bool hasChange = false;
            if (value == 0 && keypad.CurrentMarkMode != SudokuKeypad.MarkMode.Color)
            {
                foreach (int cellIndex in selection.SelectedCellIndices())
                {
                    hasChange |= values.ClearCell(
                        cellIndex: cellIndex,
                        fullClear: false,
                        clearGivens: EditingEnabled);
                }
            }
            else
            {
                switch (keypad.CurrentMarkMode)
                {
                    case SudokuKeypad.MarkMode.Fill:
                        foreach (int cellIndex in selection.SelectedCellIndices())
                        {
                            hasChange |= values.SetCellValue(cellIndex, value, EditingEnabled);
                        }
                        break;
                    case SudokuKeypad.MarkMode.Corner:
                        hasChange |= values.ToggleCornerMarks(selection.SelectedCellIndices(), value);
                        break;
                    case SudokuKeypad.MarkMode.Center:
                        hasChange |= values.ToggleCenterMarks(selection.SelectedCellIndices(), value);
                        break;
                    case SudokuKeypad.MarkMode.Color:
                        foreach (int cellIndex in selection.SelectedCellIndices())
                        {
                            hasChange |= coloring.ColorCell(cellIndex, keypad.GetColorHexValue(value));
                        }
                        break;
                }
            }
            if (hasChange)
            {
                StoreSnapshot();
            }
        }

        public void StoreSnapshot()
        {
            undoHistory.BeginPendingSnapshot();
            undoHistory.StorePendingSnapshotData("values", values.TakeSnapshot());
            undoHistory.StorePendingSnapshotData("coloring", coloring.TakeSnapshot());
            undoHistory.CommitPendingSnapshot();
        }

        private void Undo()
        {
            if (SolveInProgress)
            {
                return;
            }

            var snapshotData = undoHistory.Undo();
            if (snapshotData != null)
            {
                values.RestoreSnapshot(snapshotData["values"]);
                coloring.RestoreSnapshot(snapshotData["coloring"]);
            }
        }

        private void Redo()
        {
            if (SolveInProgress)
            {
                return;
            }

            var snapshotData = undoHistory.Redo();
            if (snapshotData != null)
            {
                values.RestoreSnapshot(snapshotData["values"]);
                coloring.RestoreSnapshot(snapshotData["coloring"]);
            }
        }

        private async Task SaveScreenshot()
        {
            await JS.InvokeVoidAsync("doSaveSvgAsPng", sudokusvg, "SudokuScreenshot.png");
        }

        private async Task<BoundingClientRect> GetBoundingClientRect(ElementReference element)
        {
            return await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", element);
        }

        private async Task SetFocus(ElementReference element)
        {
            await JS.InvokeVoidAsync("setFocusToElement", element);
        }
    }
}
