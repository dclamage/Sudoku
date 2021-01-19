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
        // Parameters
        [Parameter]
        public Action<int> NumpadPressedAction { get; set; }
        [Parameter]
        public Action UndoPressedAction { get; set; }
        [Parameter]
        public Action RedoPressedAction { get; set; }
        [Parameter]
        public Func<Task> SaveScreenshotAsyncAction { get; set; }
        [Parameter]
        public Action<string> CustomColorPressedAction { get; set; }

        // Public interface
        public enum MarkMode
        {
            Fill,
            Corner,
            Center,
            Color,
            Max
        }
        public MarkMode CurrentMarkMode
        {
            get => currentMarkMode;
            set
            {
                ColorModePressed(value);
            }
        }
        public bool SolveInProgress
        {
            get => _solveInProgress;
            set
            {
                if (_solveInProgress != value)
                {
                    _solveInProgress = value;
                    SetDirty();
                }
            }
        }
        private bool _solveInProgress = false;

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
        public bool IsKeypadDisabled => SolveInProgress;
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

        public string PickedColor
        {
            get => _pickedColor;
            set
            {
                if (_pickedColor != value)
                {
                    _pickedColor = value;
                    AddCustomColor();
                    SetDirty();
                }
            }
        }
        private string _pickedColor = "#ffffff";
        private string PickedColorStyle => $"background-color: {PickedColor};";
        private bool scrollToBottom = false;
        private string NumpadScrollClass
        {
            get
            {
                string scrollClass = "numpad-outer";
                if (currentMarkMode != MarkMode.Color || colors.Count <= 9)
                {
                    scrollClass += " noscroll";
                }
                return scrollClass;
            }
        }

        // Components
        private ElementReference outerDiv;
        private ElementReference numpadScroll;

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
            await JS.InvokeVoidAsync("setKeypadSize", outerDiv);
            if (scrollToBottom)
            {
                await JS.InvokeVoidAsync("scrollToBottom", numpadScroll);
                scrollToBottom = false;
            }
        }

        protected void NumpadButtonPressed(int value)
        {
            if (value <= 9)
            {
                NumpadPressedAction?.Invoke(value);
            }
            else
            {
                CustomColorPressedAction?.Invoke(colors[value - 1].HexValue);
            }
        }

        protected void AddCustomColor()
        {
            if (!colors.Any(c => c.HexValue == PickedColor))
            {
                colors.Add(new("Custom Color", PickedColor));
                SetDirty();
                scrollToBottom = true;
            }
        }

        protected void DeleteCustomColor(int index)
        {
            if (index < colors.Count)
            {
                colors.RemoveAt(index);
                SetDirty();
            }
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
        private readonly string fillMarkIcon = "M 13.989462,18.33898 H 12.439168 V 8.4601618 C 12.065949,8.8161552 11.575022,9.1721486 10.966388,9.5281421 10.363496,9.8841354 9.8208934,10.15113 9.33858,10.329127 V 8.8305098 c 0.867016,-0.4076699 1.624938,-0.9014673 2.273764,-1.4813921 0.648827,-0.5799248 1.108173,-1.1426241 1.378039,-1.6880979 h 0.999079 z M 0.57229047,0 C 0.25622155,1.8027832e-5 5.1750684e-6,0.25619812 0,0.57220944 V 23.42779 c 5.4181024e-6,0.316011 0.25622172,0.572191 0.57229047,0.572209 H 23.427747 c 0.316054,-3.9e-5 0.572247,-0.256213 0.572252,-0.572209 V 0.57220944 C 23.999994,0.25621314 23.743801,3.9267385e-5 23.427747,0 Z M 1.1445054,1.1443433 H 22.855456 V 22.855656 H 1.1445054 Z";
        private readonly string cornerMarkIcon = "m 3.5987718,19.105793 0.5581054,-0.07441 q 0.096118,0.474389 0.3255615,0.685229 0.232544,0.207739 0.5643067,0.207739 0.3937744,0 0.6635254,-0.272851 0.2728515,-0.272852 0.2728515,-0.675928 0,-0.384473 -0.2511474,-0.632519 -0.2511475,-0.251148 -0.6387207,-0.251148 -0.1581299,0 -0.3937744,0.06201 l 0.062012,-0.489893 q 0.05581,0.0062 0.089917,0.0062 0.3565673,0 0.6418212,-0.186035 0.2852539,-0.186035 0.2852539,-0.573608 0,-0.306958 -0.2077392,-0.508496 -0.2077393,-0.201538 -0.5364014,-0.201538 -0.3255615,0 -0.5426025,0.204638 -0.217041,0.204639 -0.2790527,0.613916 L 3.6545823,16.91988 q 0.1023193,-0.561206 0.4650879,-0.868164 0.3627685,-0.310059 0.9022705,-0.310059 0.3720703,0 0.6852295,0.16123 0.3131591,0.15813 0.4774902,0.434082 0.1674316,0.275953 0.1674316,0.586011 0,0.294556 -0.1581299,0.536402 -0.1581298,0.241845 -0.4681884,0.384472 0.4030761,0.09302 0.6263183,0.387573 0.2232422,0.291455 0.2232422,0.731739 0,0.595312 -0.434082,1.010791 -0.434082,0.412378 -1.0976074,0.412378 -0.5984131,0 -0.9952881,-0.356568 Q 3.6545823,19.6732 3.5987718,19.105793 Z M 20.048013,6.867731 v 0.5364014 h -3.004468 q -0.0062,-0.2015381 0.06511,-0.3875733 0.114722,-0.306958 0.36587,-0.6046142 0.254248,-0.2976563 0.731738,-0.6883301 0.74104,-0.6077148 1.001489,-0.9611816 0.260449,-0.3565674 0.260449,-0.6728272 0,-0.3317626 -0.238745,-0.5581054 -0.235644,-0.2294434 -0.617016,-0.2294434 -0.403077,0 -0.644922,0.2418457 -0.241846,0.2418457 -0.244947,0.6697266 L 17.148963,4.1547185 Q 17.207873,3.5128972 17.592347,3.1780339 17.97682,2.84007 18.624842,2.84007 q 0.654224,0 1.035596,0.3627686 0.381372,0.3627685 0.381372,0.8991699 0,0.2728516 -0.111621,0.5364014 Q 19.81857,4.9019596 19.55812,5.1934146 19.300772,5.4848697 18.699258,5.9933658 18.196963,6.4150455 18.054336,6.5669742 17.911709,6.7158023 17.818692,6.867731 Z M 5.483156,7.4532661 H 4.9250506 V 3.8968941 Q 4.7235125,4.0891305 4.3948504,4.2813668 4.0692889,4.4736031 3.8088396,4.5697213 V 4.0302193 Q 4.2770281,3.8100777 4.6273943,3.4969185 4.9777605,3.1837594 5.1234881,2.8892037 H 5.483156 Z M 0.57229047,0 C 0.25622155,1.8027832e-5 5.1750684e-6,0.25619812 0,0.57220944 V 23.42779 c 5.4181024e-6,0.316011 0.25622172,0.572191 0.57229047,0.572209 H 23.427747 c 0.316054,-3.9e-5 0.572247,-0.256213 0.572252,-0.572209 V 0.57220944 C 23.999994,0.25621314 23.743801,3.9267385e-5 23.427747,0 Z M 1.1445054,1.1443433 H 22.855456 V 22.855656 H 1.1445054 Z";
        private readonly string centerMarkIcon = "M 0.57229047,0 C 0.25622155,1.8027832e-5 5.1750684e-6,0.25619812 0,0.57220944 V 23.42779 c 5.4181024e-6,0.316011 0.25622172,0.572191 0.57229047,0.572209 H 23.427747 c 0.316054,-3.9e-5 0.572247,-0.256213 0.572252,-0.572209 V 0.57220944 C 23.999994,0.25621314 23.743801,3.9267385e-5 23.427747,0 Z M 1.1445054,1.1443433 H 22.855456 V 22.855656 H 1.1445054 Z m 13.4589736,11.8974527 0.558106,-0.07441 q 0.09612,0.47439 0.325561,0.68523 0.232544,0.207739 0.564307,0.207739 0.393774,0 0.663525,-0.272852 0.272852,-0.272851 0.272852,-0.675927 0,-0.384473 -0.251148,-0.63252 -0.251147,-0.251147 -0.638721,-0.251147 -0.158129,0 -0.393774,0.06201 l 0.06201,-0.489892 q 0.05581,0.0062 0.08992,0.0062 0.356567,0 0.641821,-0.186035 0.285254,-0.186035 0.285254,-0.573608 0,-0.306958 -0.207739,-0.508497 -0.20774,-0.201538 -0.536402,-0.201538 -0.325561,0 -0.542602,0.204639 -0.217041,0.204639 -0.279053,0.613916 l -0.558105,-0.09922 q 0.102319,-0.561206 0.465088,-0.8681638 0.362768,-0.3100586 0.90227,-0.3100586 0.37207,0 0.68523,0.1612305 0.313159,0.1581299 0.47749,0.4340819 0.167431,0.275952 0.167431,0.586011 0,0.294556 -0.15813,0.536401 -0.158129,0.241846 -0.468188,0.384473 0.403076,0.09302 0.626318,0.387573 0.223243,0.291455 0.223243,0.731738 0,0.595313 -0.434082,1.010791 -0.434082,0.412378 -1.097608,0.412378 -0.598413,0 -0.995288,-0.356567 -0.393774,-0.356567 -0.449585,-0.923975 z m -1.374097,0.663526 v 0.536401 h -3.004468 q -0.0062,-0.201538 0.06511,-0.387573 0.114721,-0.306958 0.365869,-0.604614 0.254248,-0.297657 0.731738,-0.68833 0.74104,-0.607715 1.001489,-0.961182 0.26045,-0.356567 0.26045,-0.672827 0,-0.331763 -0.238745,-0.558106 -0.235645,-0.229443 -0.617017,-0.229443 -0.403076,0 -0.644922,0.241846 -0.241846,0.241845 -0.244946,0.669726 l -0.573609,-0.05891 q 0.05891,-0.641821 0.443384,-0.976685 0.384473,-0.3379634 1.032495,-0.3379634 0.654224,0 1.035596,0.3627684 0.381372,0.362769 0.381372,0.89917 0,0.272852 -0.111621,0.536401 -0.111621,0.26355 -0.37207,0.555005 -0.257349,0.291455 -0.858863,0.799951 -0.502295,0.42168 -0.644922,0.573609 -0.142626,0.148828 -0.235644,0.300757 z M 8.0942746,14.241723 H 7.5361691 V 10.685351 Q 7.334631,10.877587 7.0059689,11.069824 6.6804074,11.26206 6.4199582,11.358178 V 10.818676 Q 6.8881467,10.598535 7.2385129,10.285375 7.5888791,9.9722163 7.7346066,9.6776606 h 0.359668 z";
    }
}
