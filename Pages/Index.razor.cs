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
        private readonly List<Rect> rects = new List<Rect>();
        private readonly Dictionary<(int, int), Rect> selectionRects = new Dictionary<(int, int), Rect>();
        private readonly List<Rect> sortedSelectionRects = new List<Rect>();
        private bool selectionRectsDirty = false;
        private readonly Dictionary<(int, int), Text> cellText = new Dictionary<(int, int), Text>(81);
        private (int, int) lastCellSelected = (-1, -1);
        private bool mouseDown = false;
        private double mouseLastX = 0.0;
        private double mouseLastY = 0.0;
        private ElementReference sudokusvg;

        protected void InitRects()
        {
            if (rects.Count == 0)
            {
                double rectWidth = 1000.0 / 9;
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        rects.Add(new Rect(
                            x: i * rectWidth,
                            y: j * rectWidth,
                            width: rectWidth,
                            height: rectWidth,
                            strokeWidth: 2.0,
                            opacity: 0.0
                        ));
                    }
                }

                rectWidth = 1000.0 / 3;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        rects.Add(new Rect(
                                x: i * rectWidth,
                                y: j * rectWidth,
                                width: rectWidth,
                                height: rectWidth,
                                strokeWidth: 6.0,
                                opacity: 0.0
                            ));
                    }
                }
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

            const double rectWidth = 1000.0 / 9;
            double i = Math.Floor(x / rectWidth);
            double j = Math.Floor(y / rectWidth);
            var selectCell = ((int)i, (int)j);
            if (i >= 0 && i <= 8 && j >= 0 && j <= 8 && selectCell != lastCellSelected)
            {
                lastCellSelected = selectCell;

                bool cellExists = selectionRects.ContainsKey(selectCell);
                if ((noModifiers || controlDown || shiftDown) && !cellExists)
                {
                    selectionRects[selectCell] = new Rect(
                        x: i * rectWidth,
                        y: j * rectWidth,
                        width: rectWidth,
                        height: rectWidth,
                        strokeWidth: 0.0,
                        opacity: 0.3
                    );
                    selectionRectsDirty = true;
                }
                else if ((controlDown || altDown) && cellExists)
                {
                    selectionRects.Remove(selectCell);
                    selectionRectsDirty = true;
                }
            }
        }

        private void UpdateSortedSelectionRects()
        {
            if (selectionRectsDirty)
            {
                sortedSelectionRects.Clear();
                foreach (var rect in selectionRects.Values)
                {
                    sortedSelectionRects.Add(rect);
                }
                sortedSelectionRects.Sort((a, b) => (a.y * 10000 + a.x).CompareTo(b.y * 10000 + b.x));
                selectionRectsDirty = false;
            }
        }

        protected async Task MouseDown(MouseEventArgs e)
        {
            if (!e.CtrlKey && !e.ShiftKey && !e.AltKey)
            {
                selectionRects.Clear();
                sortedSelectionRects.Clear();
                selectionRectsDirty = false;
            }
            lastCellSelected = (-1, -1);
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
                default:
                    if (!int.TryParse(keyPressed, out value))
                    {
                        return;
                    }
                    break;
            }

            if (value > 0)
            {
                const double rectWidth = 1000.0 / 9;
                const double fontSize = rectWidth * 3.0 / 4.0;
                foreach (var (i, j) in selectionRects.Keys)
                {
                    cellText[(i, j)] = new Text(
                        x: (i + 0.5) * rectWidth,
                        y: (j + 0.5) * rectWidth + (rectWidth - fontSize) / 4.0,
                        fontSize: fontSize,
                        fontFamily: "sans-serif",
                        text: value.ToString()
                    );
                }
            }
            else
            {
                foreach (var cell in selectionRects.Keys)
                {
                    cellText.Remove(cell);
                }
            }
        }

        private async Task<BoundingClientRect> GetBoundingClientRect(ElementReference element)
        {
            return await JS.InvokeAsync<BoundingClientRect>("getBoundingClientRect", element);
        }
    }
}
