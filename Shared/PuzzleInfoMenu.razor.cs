using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace SudokuBlazor.Shared
{
    public partial class PuzzleInfoMenu
    {
        [Parameter]
        public Action<string> PuzzleNameChanged { get; set; }
        [Parameter]
        public Action<string> AuthorChanged { get; set; }
        [Parameter]
        public Action<string> DescriptionChanged { get; set; }

        [Parameter]
        public Action<string> RulesChanged { get; set; }

        public string PuzzleName
        {
            get => _puzzleName;
            set
            {
                if (_puzzleName != value)
                {
                    PuzzleNameChanged?.Invoke(value);
                    _puzzleName = value;
                }
            }
        }
        private string _puzzleName = "";

        public string Author
        {
            get => _author;
            set
            {
                if (_author != value)
                {
                    AuthorChanged?.Invoke(value);
                    _author = value;
                }
            }
        }
        private string _author = "";

        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    DescriptionChanged?.Invoke(value);
                    _description = value;
                }
            }
        }
        private string _description = "";

        public string Rules
        {
            get => _rules;
            set
            {
                if (_rules != value)
                {
                    RulesChanged?.Invoke(value);
                    _rules = value;
                }
            }
        }
        private string _rules = "";
    }
}
