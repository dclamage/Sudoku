using System;
using System.Collections.Generic;
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
        private double mouseLastX = 0.0;
        private double mouseLastY = 0.0;

        // Selection
        private readonly Rect[] selectionRects = new Rect[81];
        private int lastCellSelected = -1;

        // Values
        private readonly Text[] cellText = new Text[81];
        const double valueFontSize = cellRectWidth * 3.0 / 4.0;

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
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
                        isDirty = true;
                    }
                    else if ((controlDown || altDown) && cellExists)
                    {
                        selectionRects[cellIndex] = null;
                        isDirty = true;
                    }
                }
            }
        }

        protected void SelectNone()
        {
            for (int i = 0; i < selectionRects.Length; i++)
            {
                if (selectionRects[i] != null)
                {
                    selectionRects[i] = null;
                    isDirty = true;
                }
            }
        }

        protected void SelectAll()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = i * 9 + j;
                    if (selectionRects[cellIndex] == null)
                    {
                        selectionRects[cellIndex] = CreateSelectionRect(i, j);
                        isDirty = true;
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

        protected async Task MouseDown(MouseEventArgs e)
        {
            if (!e.CtrlKey && !e.ShiftKey && !e.AltKey)
            {
                SelectNone();
            }
            lastCellSelected = -1;
            mouseDown = true;
            await SelectCellAtLocation(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
            mouseLastX = e.ClientX;
            mouseLastY = e.ClientY;
        }

        protected async Task MouseMove(MouseEventArgs e)
        {
            if (mouseDown)
            {
                var boundingRect = await GetBoundingClientRect(sudokusvg);

                double mouseDiffX = e.ClientX - mouseLastX;
                double mouseDiffY = e.ClientY - mouseLastY;
                double mouseDiffInvLen = 1.0 / Math.Sqrt(mouseDiffX * mouseDiffX + mouseDiffY * mouseDiffY);
                mouseDiffX *= (boundingRect.Width / 18.0) * mouseDiffInvLen;
                mouseDiffY *= (boundingRect.Height / 18.0) * mouseDiffInvLen;
                mouseLastX += mouseDiffX;
                mouseLastY += mouseDiffY;
                while ((mouseDiffX > 0.1 && mouseLastX < e.ClientX || mouseDiffX < 0.1 && mouseLastX > e.ClientX)
                    || (mouseDiffY > 0.1 && mouseLastY < e.ClientY || mouseDiffY < 0.1 && mouseLastY > e.ClientY))
                {
                    await SelectCellAtLocation(mouseLastX, mouseLastY, e.CtrlKey, e.ShiftKey, e.AltKey);
                    mouseLastX += mouseDiffX;
                    mouseLastY += mouseDiffY;
                }

                await SelectCellAtLocation(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
                mouseLastX = e.ClientX;
                mouseLastY = e.ClientY;
            }
        }

        protected async Task MouseUp(MouseEventArgs e)
        {
            if (mouseDown)
            {
                await SelectCellAtLocation(e.ClientX, e.ClientY, e.CtrlKey, e.ShiftKey, e.AltKey);
                mouseDown = false;
            }
        }

        protected void KeyDown(KeyboardEventArgs e)
        {
            string keyPressed = e.Key.ToLowerInvariant();
            int value = 0;
            switch (keyPressed)
            {
                case "delete":
                case "backspace":
                    break;
                case "a":
                    if (e.CtrlKey)
                    {
                        SelectAll();
                    }
                    return;
                default:
                    if (!int.TryParse(keyPressed, out value))
                    {
                        return;
                    }
                    break;
            }

            if (value > 0)
            {
                foreach (int cellIndex in SelectedCellIndices())
                {
                    cellText[cellIndex] = new Text(
                        x: (cellIndex / 9 + 0.5) * cellRectWidth,
                        y: (cellIndex % 9 + 0.5) * cellRectWidth + (cellRectWidth - valueFontSize) / 4.0,
                        fontSize: valueFontSize,
                        fontFamily: "sans-serif",
                        text: value.ToString()
                    );
                    isDirty = true;
                }
            }
            else
            {
                foreach (int cellIndex in SelectedCellIndices())
                {
                    if (cellText[cellIndex] != null)
                    {
                        cellText[cellIndex] = null;
                        isDirty = true;
                    }
                }
            }
        }

        private async Task<BoundingClientRect> GetBoundingClientRect(ElementReference element)
        {
            return await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", element);
        }
    }
}
