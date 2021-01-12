using System.Linq;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuColoring : ComponentDirtyRender
    {
        // Constants
        private const double cellRectWidth = SudokuConstants.cellRectWidth;

        // UI
        private readonly Rect[] colorRects = new Rect[81];

        private record Snapshot(string[] Colors);

        public object TakeSnapshot()
        {
            return new Snapshot(colorRects.Select(rect => rect?.color ?? null).ToArray());
        }

        public void RestoreSnapshot(object snapshotObj)
        {
            if (snapshotObj != null && snapshotObj is Snapshot snapshot)
            {
                for (int i = 0; i < 81; i++)
                {
                    ColorCell(i, snapshot.Colors[i]);
                }
            }
        }

        public bool ColorCell(int cellIndex, string color)
        {
            bool hadChange = false;
            if (cellIndex >= 0 && cellIndex < colorRects.Length)
            {
                if (string.IsNullOrWhiteSpace(color))
                {
                    if (colorRects[cellIndex] != null)
                    {
                        colorRects[cellIndex] = null;
                        SetDirty();
                        hadChange = true;
                    }
                }
                else if(colorRects[cellIndex] == null || colorRects[cellIndex].color != color)
                {
                    int i = cellIndex / 9;
                    int j = cellIndex % 9;
                    colorRects[cellIndex] = CreateColorRect(i, j, color);
                    SetDirty();
                    hadChange = true;
                }
           }
            return hadChange;
        }

        protected static Rect CreateColorRect(int i, int j, string color) => new Rect(
            x: j * cellRectWidth,
            y: i * cellRectWidth,
            width: cellRectWidth,
            height: cellRectWidth,
            strokeWidth: 0.0,
            opacity: 1.0,
            color: color
        );
    }
}
