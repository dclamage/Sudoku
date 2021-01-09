using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using SudokuBlazor.Models;

namespace SudokuBlazor.Pages
{
    partial class Index
    {
        // Element References
        private ElementReference sudokusvg;

        // Constants
        private const double cellRectWidth = 1000.0 / 9.0;

        // State
        private bool isDirty = true;

        // Input
        private bool mouseDown = false;
        private long touchIdDown = -1;
        private double inputLastX = 0.0;
        private double inputLastY = 0.0;

        // Selection
        private readonly Rect[] selectionRects = new Rect[81];
        private int lastCellSelected = -1;

        // Values
        private readonly Text[] cellText = new Text[81];
        const double valueFontSize = cellRectWidth * 3.0 / 4.0;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await SetFocus(sudokusvg);
        }

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
        }

        protected async Task SetDirty()
        {
            if (!isDirty)
            {
                await SetFocus(sudokusvg);
                isDirty = true;
            }
        }

        protected async Task SelectCellAtLocation(double clientX, double clientY, bool controlDown, bool shiftDown, bool altDown)
        {
            // Choose a priority for each modifier, so only one is used
            // Control -> Shift -> Alt
            if (controlDown)
            {
                shiftDown = false;
                altDown = false;
            }
            else if (shiftDown)
            {
                altDown = false;
            }
            bool noModifiers = !controlDown && !shiftDown && !altDown;

            var infoFromJs = await JS.InvokeAsync<string>("getSVG_XY", sudokusvg, clientX, clientY);
            var values = infoFromJs.Split(" ");
            double x = Double.Parse(values[0]);
            double y = Double.Parse(values[1]);

            int i = (int)Math.Floor(x / cellRectWidth);
            int j = (int)Math.Floor(y / cellRectWidth);
            if (i >= 0 && i < 9 && j >= 0 && j < 9)
            {
                int cellIndex = i * 9 + j;
                if (lastCellSelected != cellIndex)
                {
                    lastCellSelected = cellIndex;

                    bool cellExists = selectionRects[cellIndex] != null;
                    if ((noModifiers || controlDown || shiftDown) && !cellExists)
                    {
                        selectionRects[cellIndex] = CreateSelectionRect(i, j);
                        await SetDirty();
                    }
                    else if ((controlDown || altDown) && cellExists)
                    {
                        selectionRects[cellIndex] = null;
                        await SetDirty();
                    }
                }
            }
        }

        protected async Task SelectNone()
        {
            for (int i = 0; i < selectionRects.Length; i++)
            {
                if (selectionRects[i] != null)
                {
                    selectionRects[i] = null;
                    await SetDirty();
                }
            }
        }

        protected async Task SelectAll()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = i * 9 + j;
                    if (selectionRects[cellIndex] == null)
                    {
                        selectionRects[cellIndex] = CreateSelectionRect(i, j);
                        await SetDirty();
                    }
                }
            }
        }

        protected IEnumerable<int> SelectedCellIndices()
        {
            for (int i = 0; i < selectionRects.Length; i++)
            {
                if (selectionRects[i] != null)
                {
                    yield return i;
                }
            }
        }

        protected static Rect CreateSelectionRect(int i, int j) => new Rect(
            x: i * cellRectWidth,
            y: j * cellRectWidth,
            width: cellRectWidth,
            height: cellRectWidth,
            strokeWidth: 0.0,
            opacity: 0.3
        );

        protected async Task InputStart(double clientX, double clientY, bool ctrlKey, bool shiftKey, bool altKey)
        {
            if (!ctrlKey && !shiftKey && !altKey)
            {
                await SelectNone();
            }
            lastCellSelected = -1;
            await SelectCellAtLocation(clientX, clientY, ctrlKey, shiftKey, altKey);
            inputLastX = clientX;
            inputLastY = clientY;
        }

        protected async Task MouseDown(MouseEventArgs e)
        {
            mouseDown = true;
            await InputStart(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
        }

        protected async Task TouchStart(TouchEventArgs e)
        {
            if (e.ChangedTouches.Length == 0 || touchIdDown != -1)
            {
                return;
            }


            touchIdDown = e.ChangedTouches[0].Identifier;
            Console.WriteLine($"{e.Type}: {touchIdDown}");
            await InputStart(e.ChangedTouches[0].ClientX, e.ChangedTouches[0].ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
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
                        Console.WriteLine($"{e.Type}: {touchIdDown}");
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
            Ignore
        }
        private static (KeyCodeType, int) GetKeyCodeType(string keyCode)
        {
            switch (keyCode)
            {
                case "Delete":
                case "Backspace":
                case "Digit0":
                case "Numpad0":
                    return (KeyCodeType.DeleteCell, 0);
                case "KeyA": // a
                    return (KeyCodeType.A, -1);
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

        protected async Task KeyDown(KeyboardEventArgs e)
        {
            var (keyCodeType, value) = GetKeyCodeType(e.Code);
            switch (keyCodeType)
            {
                case KeyCodeType.DeleteCell:
                    foreach (int cellIndex in SelectedCellIndices())
                    {
                        if (cellText[cellIndex] != null)
                        {
                            cellText[cellIndex] = null;
                            await SetDirty();
                        }
                    }
                    return;
                case KeyCodeType.A:
                    if (e.CtrlKey)
                    {
                        await SelectAll();
                    }
                    return;
                case KeyCodeType.Value:
                    foreach (int cellIndex in SelectedCellIndices())
                    {
                        cellText[cellIndex] = new Text(
                            x: (cellIndex / 9 + 0.5) * cellRectWidth,
                            y: (cellIndex % 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
                            fontSize: valueFontSize,
                            fontFamily: "sans-serif",
                            text: value.ToString()
                        );
                        await SetDirty();
                    }
                    return;
            }
        }

        protected async Task KeyUp(KeyboardEventArgs e)
        {
            var (keyCodeType, _) = GetKeyCodeType(e.Code);
            if (keyCodeType != KeyCodeType.Ignore)
            {
                await SetFocus(sudokusvg);
            }
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
