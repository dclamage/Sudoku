using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SudokuBlazor.Shared
{
    public enum GlobalConstraints
    {
        King,
        Knight,
        Nonconsecutive,
        DiagNonconsecutive,
        DisjointGroups,
    }

    public partial class ConstructionMenu
    {
        [Parameter]
        public Action<bool> EditingToggled { get; set; }
        [Parameter]
        public Action<GlobalConstraints, bool> GlobalConstraintToggled { get; set; }

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

        public bool KingEnabled
        {
            get => _kingEnabled;
            set
            {
                if (_kingEnabled != value)
                {
                    _kingEnabled = value;
                    GlobalConstraintToggled?.Invoke(GlobalConstraints.King, value);
                }
            }
        }
        private bool _kingEnabled = false;

        public bool KnightEnabled
        {
            get => _knightEnabled;
            set
            {
                if (_knightEnabled != value)
                {
                    _knightEnabled = value;
                    GlobalConstraintToggled?.Invoke(GlobalConstraints.Knight, value);
                }
            }
        }
        private bool _knightEnabled = false;

        public bool NonconsecutiveEnabled
        {
            get => _nonconsecutiveEnabled;
            set
            {
                if (_nonconsecutiveEnabled != value)
                {
                    _nonconsecutiveEnabled = value;
                    GlobalConstraintToggled?.Invoke(GlobalConstraints.Nonconsecutive, value);
                }
            }
        }
        private bool _nonconsecutiveEnabled = false;

        public bool DiagNonconsecutiveEnabled
        {
            get => _diagNonconsecutiveEnabled;
            set
            {
                if (_diagNonconsecutiveEnabled != value)
                {
                    _diagNonconsecutiveEnabled = value;
                    GlobalConstraintToggled?.Invoke(GlobalConstraints.DiagNonconsecutive, value);
                }
            }
        }
        private bool _diagNonconsecutiveEnabled = false;

        public bool DisjointGroups
        {
            get => _disjointGroups;
            set
            {
                if (_disjointGroups != value)
                {
                    _disjointGroups = value;
                    GlobalConstraintToggled?.Invoke(GlobalConstraints.DisjointGroups, value);
                }
            }
        }
        private bool _disjointGroups = false;
    }
}
