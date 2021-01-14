using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using SudokuBlazor.Models;

namespace SudokuBlazor.Shared
{
    partial class SudokuKeypad : ComponentDirtyRender
    {
        // Public interface
        public enum MarkMode
        {
            Fill,
            Corner,
            Center,
            Color,
            Max
        }
        public Action<int> NumpadPressedAction { get; set; }
        public Action UndoPressedAction { get; set; }
        public Action RedoPressedAction { get; set; }
        public Func<Task> SaveScreenshotAsyncAction { get; set; }
        public MarkMode CurrentMarkMode
        {
            get => currentMarkMode;
            set
            {
                ColorModePressed(value);
            }
        }
        public string GetColorHexValue(int colorIndex)
        {
            if (colorIndex <= 0 || colorIndex - 1 >= colors.Count)
            {
                return null;
            }
            return colors[colorIndex - 1].HexValue;
        }

        // State
        private readonly Color[] modeButtonColors = new Color[] { Color.Success, Color.Primary, Color.Primary, Color.Primary };
        private MarkMode currentMarkMode = MarkMode.Fill;
        private readonly List<ColorInfo> colors = new List<ColorInfo>
        {
            new ColorInfo("Silver", "#cbcbcb"),
            new ColorInfo("Tangerine", "#ff8080"),
            new ColorInfo("Orange", "#ffa865"), 
            new ColorInfo("Lime", "#b1ff60"), 
            new ColorInfo("Green", "#3fff55"), 
            new ColorInfo("Aquamarine", "#89fff2"),
            new ColorInfo("Malibu", "#82c3ff"),
            new ColorInfo("Mauve", "#d391ff"), 
            new ColorInfo("Blush", "#ff7bd9"), 
        };

        // Components
        private ElementReference outerDiv;

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JS.InvokeVoidAsync("setSidebarSize", outerDiv);
        }

        protected void NumpadButtonPressed(int value)
        {
            NumpadPressedAction?.Invoke(value);
        }

        protected void ColorModePressed(MarkMode markMode)
        {
            if (currentMarkMode == markMode || markMode >= MarkMode.Max)
            {
                return;
            }
            currentMarkMode = markMode;

            for (MarkMode mode = MarkMode.Fill; mode < MarkMode.Max; mode++)
            { 
                modeButtonColors[(int)mode] = mode == markMode ? Color.Success : Color.Primary;
            }
            SetDirty();
        }

        protected void Undo()
        {
            UndoPressedAction?.Invoke();
        }

        protected void Redo()
        {
            RedoPressedAction?.Invoke();
        }

        protected async void SaveScreenshot()
        {
            await SaveScreenshotAsyncAction?.Invoke();
        }

        // Icons (See the RawAssets folder for the original svgs)
        private readonly string fillMarkIcon = "M 0.57229047,0 A 0.57232312,0.57221881 0 0 0 0,0.57220944 V 23.42779 a 0.57232312,0.57221881 0 0 0 0.57229047,0.572209 H 23.427747 A 0.57232312,0.57221881 0 0 0 23.999999,23.42779 V 0.57220944 A 0.57232312,0.57221881 0 0 0 23.427747,0 Z M 1.1445054,1.1443433 H 22.855456 V 22.855656 H 1.1445054 Z M 10.893471,4.686155 7.5312594,5.3641234 V 7.0967346 L 10.912327,6.4187284 V 17.143915 H 7.8043869 v 1.600743 h 8.0995071 v -1.600743 h -3.10794 V 4.686155 Z";
        private readonly string cornerMarkIcon = "M 0.57228409,0 A 0.57232318,0.57221948 0 0 0 0,0.57218039 V 23.427819 A 0.57232318,0.57221948 0 0 0 0.57228409,24 H 23.427762 A 0.57232318,0.57221948 0 0 0 24,23.427819 V 0.57218039 A 0.57232318,0.57221948 0 0 0 23.427762,0 Z M 1.1445224,1.144315 H 22.855478 v 21.71137 H 1.1445224 Z M 19.425939,2.6599547 c -0.174023,0 -0.365824,0.024707 -0.575442,0.074137 -0.209618,0.049429 -0.435065,0.1226 -0.676323,0.2194816 V 3.558566 c 0.237303,-0.1324705 0.459802,-0.2313463 0.667443,-0.2965928 0.209618,-0.065247 0.408358,-0.097842 0.596223,-0.097842 0.264989,0 0.479552,0.074123 0.643688,0.2224103 0.166112,0.1482879 0.249135,0.3401004 0.249135,0.5753838 0,0.1443335 -0.03854,0.2916269 -0.115665,0.4418919 -0.07515,0.1482878 -0.208637,0.3301717 -0.400457,0.5456833 -0.100854,0.1146759 -0.34802,0.3707282 -0.741548,0.7681394 -0.391551,0.3954344 -0.701064,0.7107846 -0.92848,0.9460679 v 0.504176 h 2.812033 v -0.504176 h -2.091219 c 0.482517,-0.4923156 0.856281,-0.8748977 1.12127,-1.1477474 0.264989,-0.2748267 0.422194,-0.4408886 0.471632,-0.4982266 0.179955,-0.2214432 0.303561,-0.4132556 0.370797,-0.5753838 0.06921,-0.1641052 0.10381,-0.3371034 0.10381,-0.5190033 0,-0.3835712 -0.136437,-0.6900358 -0.409336,-0.9193876 C 20.2506,2.7746067 19.884726,2.6599547 19.425939,2.6599547 Z M 4.3049697,2.6777566 3.2459947,2.8912888 V 3.4369723 L 4.3108742,3.2234401 V 6.601469 H 3.3319998 V 7.1056449 H 5.883042 V 6.601469 H 4.9041674 V 2.6777566 Z m 0.060831,13.6791064 c -0.1760002,0 -0.3638572,0.01578 -0.5635875,0.04741 -0.1977529,0.03163 -0.4103218,0.0791 -0.6377377,0.14237 v 0.533831 c 0.2254385,-0.07513 0.4320672,-0.130486 0.6199326,-0.166076 0.1878652,-0.03559 0.3638878,-0.05336 0.5280226,-0.05336 0.3005846,0 0.5309278,0.06129 0.6911076,0.183877 0.1621573,0.120609 0.2432769,0.294604 0.2432769,0.521979 0,0.221442 -0.078127,0.391448 -0.2343514,0.510078 -0.1562249,0.116654 -0.3816713,0.175 -0.6763233,0.175 H 3.7962624 v 0.492323 h 0.5161679 c 0.3262925,0 0.5803959,0.07412 0.7623284,0.222411 0.1839102,0.146311 0.2758664,0.349955 0.2758664,0.610942 0,0.282735 -0.098894,0.498258 -0.2966468,0.646546 -0.1957752,0.148288 -0.4815282,0.22241 -0.857259,0.22241 -0.2155505,0 -0.4221794,-0.02471 -0.6199323,-0.07414 C 3.3790341,20.323034 3.1970715,20.24991 3.0309588,20.153029 v 0.578312 c 0.2096184,0.07315 0.4123482,0.127512 0.6081234,0.163102 0.1977532,0.03757 0.3895547,0.05633 0.5754424,0.05633 0.5537084,0 0.9808411,-0.119594 1.2814258,-0.358832 0.3005843,-0.239237 0.4508973,-0.577345 0.4508973,-1.0143 0,-0.284712 -0.081074,-0.523945 -0.2432309,-0.717709 C 5.5434367,18.66617 5.3199396,18.538642 5.0331979,18.47735 5.2922543,18.4121 5.4919912,18.295457 5.6323958,18.127397 5.7728003,17.95736 5.8430375,17.749771 5.8430373,17.504602 5.8430375,17.152665 5.7105447,16.873855 5.4455557,16.66823 5.1805667,16.460627 4.8206321,16.356855 4.3658007,16.356855 Z";
        private readonly string centerMarkIcon = "M 0.57228409,0 A 0.5723232,0.5722195 0 0 0 0,0.57218039 V 23.427819 A 0.5723232,0.5722195 0 0 0 0.57228409,24 H 23.427762 A 0.5723232,0.5722195 0 0 0 24,23.427819 V 0.57218039 A 0.5723232,0.5722195 0 0 0 23.427762,0 Z M 1.1445224,1.144315 H 22.855478 v 21.71137 H 1.1445224 Z m 7.9527028,8.6263643 -1.058975,0.213532 v 0.5456837 l 1.0648794,-0.213532 v 3.378028 H 8.1242552 v 0.504177 H 10.675297 V 13.694391 H 9.696423 V 9.7706793 Z m 5.4953558,0 c -0.174023,0 -0.365825,0.024708 -0.575443,0.074137 -0.209618,0.049429 -0.435064,0.1226002 -0.676323,0.2194817 v 0.604992 c 0.237304,-0.13247 0.459757,-0.2313 0.667398,-0.296547 0.209618,-0.06525 0.408403,-0.09789 0.596268,-0.09789 0.264989,0 0.479507,0.07417 0.643642,0.222456 0.166113,0.148288 0.249182,0.340055 0.249182,0.575338 0,0.144334 -0.03854,0.291627 -0.115665,0.441892 -0.07515,0.148288 -0.208637,0.330172 -0.400457,0.545684 -0.100854,0.114676 -0.348066,0.370728 -0.741594,0.768139 -0.391552,0.395434 -0.701019,0.710785 -0.928434,0.946068 v 0.504176 h 2.812033 V 13.77443 h -2.09122 c 0.482517,-0.492316 0.856282,-0.874898 1.121271,-1.147748 0.264988,-0.274826 0.422193,-0.440888 0.471632,-0.498226 0.179954,-0.221443 0.30356,-0.413211 0.370796,-0.575338 0.06921,-0.164106 0.103811,-0.337149 0.103811,-0.519049 0,-0.383572 -0.136438,-0.68999 -0.409337,-0.919342 -0.272899,-0.2293524 -0.638774,-0.3440496 -1.09756,-0.3440496 z";
    }
}
