using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuSelection
    {
        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;

        // State
        private bool isDirty = true;
        private int lastCellSelected = -1;

        // UI
        private readonly Rect[] selectionRects = new Rect[81];

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
        }

        protected void SetDirty()
        {
            isDirty = true;
            StateHasChanged();
        }

        public void ResetLastSelectedCell()
        {
            lastCellSelected = -1;
        }

        public void SelectCell(int i, int j, bool controlDown, bool shiftDown, bool altDown)
        {
            if (i >= 0 && i < 9 && j >= 0 && j < 9)
            {
                bool noModifiers = AdjustModifiers(ref controlDown, ref shiftDown, ref altDown);

                int cellIndex = i * 9 + j;
                if (lastCellSelected != cellIndex)
                {
                    lastCellSelected = cellIndex;

                    bool cellExists = selectionRects[cellIndex] != null;
                    if ((noModifiers || controlDown || shiftDown) && !cellExists)
                    {
                        selectionRects[cellIndex] = CreateSelectionRect(i, j);
                        SetDirty();
                    }
                    else if ((controlDown || altDown) && cellExists)
                    {
                        selectionRects[cellIndex] = null;
                        SetDirty();
                    }
                }
            }
        }

        public void SelectNone()
        {
            for (int i = 0; i < selectionRects.Length; i++)
            {
                if (selectionRects[i] != null)
                {
                    selectionRects[i] = null;
                    SetDirty();
                }
            }
        }

        public void SelectAll()
        {
            for (int i = 0; i < 9; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    int cellIndex = i * 9 + j;
                    if (selectionRects[cellIndex] == null)
                    {
                        selectionRects[cellIndex] = CreateSelectionRect(i, j);
                        SetDirty();
                    }
                }
            }
        }

        public IEnumerable<int> SelectedCellIndices()
        {
            for (int i = 0; i < selectionRects.Length; i++)
            {
                if (selectionRects[i] != null)
                {
                    yield return i;
                }
            }
        }

        public bool HasSelectedCells()
        {
            return selectionRects.Where(rect => rect != null).Any();
        }

        public enum MoveDir
        {
            Up,
            Down,
            Left,
            Right
        }
        public void Move(MoveDir dir, bool controlDown, bool shiftDown, bool altDown)
        {
            int i = 0;
            int j = 0;
            if (lastCellSelected >= 0)
            {
                i = lastCellSelected / 9;
                j = lastCellSelected % 9;
                switch (dir)
                {
                    case MoveDir.Up:
                        i = (i + 8) % 9;
                        break;
                    case MoveDir.Down:
                        i = (i + 1) % 9;
                        break;
                    case MoveDir.Left:
                        j = (j + 8) % 9;
                        break;
                    case MoveDir.Right:
                        j = (j + 1) % 9;
                        break;
                }
            }

            bool noModifiers = AdjustModifiers(ref controlDown, ref shiftDown, ref altDown);
            if (noModifiers)
            {
                SelectNone();
            }
            SelectCell(i, j, controlDown, shiftDown, altDown);
        }

        protected static Rect CreateSelectionRect(int i, int j) => new Rect(
            x: j * cellRectWidth,
            y: i * cellRectWidth,
            width: cellRectWidth,
            height: cellRectWidth,
            strokeWidth: 0.0,
            opacity: 0.3
        );

        private bool AdjustModifiers(ref bool controlDown, ref bool shiftDown, ref bool altDown)
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
            return !controlDown && !shiftDown && !altDown;
        }
    }
}
