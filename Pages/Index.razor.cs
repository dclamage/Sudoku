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
        private readonly List<Rect> selectionRects = new List<Rect>();
        private readonly HashSet<(int, int)> selectedCells = new HashSet<(int, int)>();
        private readonly Dictionary<(int, int), Text> cellText = new Dictionary<(int, int), Text>(81);
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

            //JS.InvokeVoidAsync("setFocusToElement", sudokusvg);
        }

        protected async Task SelectCellAtLocation(double clientX, double clientY)
        {
            var infoFromJs = await JS.InvokeAsync<string>("getSVG_XY", sudokusvg, clientX, clientY);
            var values = infoFromJs.Split(" ");
            double x = Double.Parse(values[0]);
            double y = Double.Parse(values[1]);

            double rectWidth = 1000.0 / 9;
            double i = Math.Floor(x / rectWidth);
            double j = Math.Floor(y / rectWidth);
            var selectCell = ((int)i, (int)j);
            if (i >= 0 && i <= 8 && j >= 0 && j <= 8 && !selectedCells.Contains(selectCell))
            {
                selectedCells.Add(selectCell);
                selectionRects.Add(new Rect(
                    x: i * rectWidth,
                    y: j * rectWidth,
                    width: rectWidth,
                    height: rectWidth,
                    strokeWidth: 0.0,
                    opacity: 0.3
                ));
            }
        }

        protected async Task MouseDown(MouseEventArgs e)
        {
            selectionRects.Clear();
            selectedCells.Clear();
            mouseDown = true;
            await SelectCellAtLocation(e.ClientX, e.ClientY);
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
                while ((mouseDiffX > 0.0 && mouseLastX < e.ClientX || mouseDiffX < 0.0 && mouseLastX > e.ClientX)
                    && (mouseDiffY > 0.0 && mouseLastY < e.ClientY || mouseDiffY < 0.0 && mouseLastY > e.ClientY))
                {
                    await SelectCellAtLocation(mouseLastX, mouseLastY);
                    mouseLastX += mouseDiffX;
                    mouseLastY += mouseDiffY;
                }

                await SelectCellAtLocation(e.ClientX, e.ClientY);
                mouseLastX = e.ClientX;
                mouseLastY = e.ClientY;
            }
        }

        protected async Task MouseUp(MouseEventArgs e)
        {
            if (mouseDown)
            {
                await SelectCellAtLocation(e.ClientX, e.ClientY);
                mouseDown = false;
            }
        }

        protected void KeyDown(KeyboardEventArgs e)
        {
            string keyPressed = e.Key.ToLowerInvariant();
            int value;
            if (keyPressed == "delete" || keyPressed == "backspace")
            {
                value = 0;
            }
            else if (!int.TryParse(keyPressed, out value))
            {
                return;
            }

            if (value > 0)
            {
                const double rectWidth = 1000.0 / 9;
                const double fontSize = rectWidth * 3.0 / 4.0;
                foreach (var (i, j) in selectedCells)
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
                foreach (var cell in selectedCells)
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
