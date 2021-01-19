using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SudokuBlazor.Shared
{
    public partial class ConstructionMenu
    {
        [Parameter]
        public Action<bool> EditingToggled { get; set; }

        public bool EditingEnabled
        {
            get => _editingEnabled;
            set
            {
                if (_editingEnabled != value)
                {
                    _editingEnabled = value;
                    EditingToggled?.Invoke(value);
                }
            }
        }
        private bool _editingEnabled = false;
    }
}
