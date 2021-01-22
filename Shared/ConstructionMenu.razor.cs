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
        PositiveDiagonal,
        NegativeDiagonal,
        DisjointGroups, // Keep DisjointGroups as the last global constraint in the list
        Max
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
            set => SetGlobalConstraint(GlobalConstraints.King, ref _kingEnabled, value);
        }
        private bool _kingEnabled = false;

        public bool KnightEnabled
        {
            get => _knightEnabled;
            set => SetGlobalConstraint(GlobalConstraints.Knight, ref _knightEnabled, value);
        }
        private bool _knightEnabled = false;

        public bool NonconsecutiveEnabled
        {
            get => _nonconsecutiveEnabled;
            set => SetGlobalConstraint(GlobalConstraints.Nonconsecutive, ref _nonconsecutiveEnabled, value);
        }
        private bool _nonconsecutiveEnabled = false;

        public bool DiagNonconsecutiveEnabled
        {
            get => _diagNonconsecutiveEnabled;
            set => SetGlobalConstraint(GlobalConstraints.DiagNonconsecutive, ref _diagNonconsecutiveEnabled, value);
        }
        private bool _diagNonconsecutiveEnabled = false;

        public bool DisjointGroups
        {
            get => _disjointGroups;
            set => SetGlobalConstraint(GlobalConstraints.DisjointGroups, ref _disjointGroups, value);
        }
        private bool _disjointGroups = false;

        public bool PositiveDiagonal
        {
            get => _positiveDiagonal;
            set => SetGlobalConstraint(GlobalConstraints.PositiveDiagonal, ref _positiveDiagonal, value);
        }
        private bool _positiveDiagonal = false;
        public bool NegativeDiagonal
        {
            get => _negativeDiagonal;
            set => SetGlobalConstraint(GlobalConstraints.NegativeDiagonal, ref _negativeDiagonal, value);
        }
        private bool _negativeDiagonal = false;

        private void SetGlobalConstraint(GlobalConstraints constraint, ref bool var, bool value)
        {
            if (var != value)
            {
                var = value;
                GlobalConstraintToggled?.Invoke(constraint, value);
            }
        }
    }
}
