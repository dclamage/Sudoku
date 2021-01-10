using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SudokuBlazor.Shared
{
    partial class SudokuKeypad
    {
        // Public interface
        public Action<int> NumpadPressedAction { get; set; }

        // State
        private bool hasRendered = false;
        protected override bool ShouldRender()
        {
            if (!hasRendered)
            {
                hasRendered = true;
                return true;
            }
            return false;
        }

        protected void NumpadButtonPressed(int value)
        {
            NumpadPressedAction?.Invoke(value);
        }
    }
}
