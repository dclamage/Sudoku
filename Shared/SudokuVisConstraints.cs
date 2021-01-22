using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    public partial class SudokuVisConstraints
    {
        private SvgPath[] svgPaths = null;
        private SvgText[] svgText = null;

        public void SetSvgContent(SvgPath[] svgPaths, SvgText[] svgText)
        {
            this.svgPaths = svgPaths;
            this.svgText = svgText;
            SetDirty();
        }
    }
}
