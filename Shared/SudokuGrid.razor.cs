using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuGrid
    {
        // Constants
        private const double boxRectWidth = SudokuConstants.boxRectWidth;
        private const double cellRectWidth = SudokuConstants.cellRectWidth;

        // State
        private readonly List<Rect> rects = new List<Rect>();
        private bool rendered = false;

        protected override void OnInitialized()
        {
            InitRects();
            base.OnInitialized();
        }

        protected override bool ShouldRender()
        {
            if (!rendered)
            {
                rendered = true;
                return true;
            }
            return false;
        }

        protected void InitRects()
        {
            if (rects.Count == 0)
            {
                for (int i = 0; i < 9; i++)
                {
                    for (int j = 0; j < 9; j++)
                    {
                        rects.Add(new Rect(
                            x: i * cellRectWidth,
                            y: j * cellRectWidth,
                            width: cellRectWidth,
                            height: cellRectWidth,
                            strokeWidth: 2.0,
                            opacity: 0.0
                        ));
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        rects.Add(new Rect(
                            x: i * boxRectWidth,
                            y: j * boxRectWidth,
                            width: boxRectWidth,
                            height: boxRectWidth,
                            strokeWidth: 6.0,
                            opacity: 0.0
                        ));
                    }
                }
            }
        }
    }
}
