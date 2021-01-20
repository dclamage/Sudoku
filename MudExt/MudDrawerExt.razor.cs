using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using MudBlazor.Extensions;
using MudBlazor.Services;
using MudBlazor.Utilities;

namespace MudBlazor
{
    public partial class MudDrawerExt
    {
        private bool _rtl;

        protected string Classname =>
        new CssBuilder("mud-drawer-ext")
          .AddClass($"mud-drawer-ext-anchor-{Anchor.ToDescriptionString()}")
          .AddClass($"mud-drawer-ext--open", Open)
          .AddClass($"mud-drawer-ext--closed", !Open)
          .AddClass($"mud-drawer-ext-clipped", Clipped)
          .AddClass($"mud-drawer-color-{Color.ToDescriptionString()}", Color != Color.Default)
          .AddClass($"mud-elevation-{Elevation.ToString()}")
          .AddClass(Class)
        .Build();

        [CascadingParameter] MudLayoutExt Layout { get; set; }

        [CascadingParameter]
        bool RightToLeft
        {
            get
            {
                return _rtl;
            }
            set
            {
                if (_rtl != value)
                {
                    _rtl = value;
                    this.Anchor = this.Anchor == Anchor.Left ? Anchor.Right : Anchor.Left;
                }
            }
        }

        /// <summary>
        /// The higher the number, the heavier the drop-shadow. 0 for no shadow.
        /// </summary>
        [Parameter] public int Elevation { set; get; } = 1;

        /// <summary>
        /// Side from which the drawer will appear.
        /// </summary>
        [Parameter] public Anchor Anchor { get; set; }

        /// <summary>
        /// The color of the component. It supports the theme colors.
        /// </summary>
        [Parameter] public Color Color { get; set; } = Color.Default;

        /// <summary>
        /// Child content of component.
        /// </summary>
        [Parameter] public RenderFragment ChildContent { get; set; }

        /// <summary>
        /// Sets the opened state on the drawer. Can be used with two-way binding to close itself on navigation.
        /// </summary>
        [Parameter]
        public bool Open
        {
            get => _open;
            set
            {
                if (_open == value)
                {
                    return;
                }
                _open = value;
                Layout?.FireDrawersChanged();
                StateHasChanged();
            }
        }

        [Parameter] public EventCallback<bool> OpenChanged { get; set; }

        private bool _open;
        private bool _clipped;

        [Parameter]
        public bool Clipped
        {
            get => _clipped;
            set
            {
                if (_clipped == value)
                {
                    return;
                }
                _clipped = value;
                Layout?.FireDrawersChanged();
                StateHasChanged();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Layout?.Add(this);
        }

        public void Dispose()
        {
            try
            {
                Layout?.Remove(this);
            }
            catch (Exception) { }
        }

        private void DrawerClose()
        {
            if (Open)
            {
                OpenChanged.InvokeAsync(false);
            }
            else
            {
                OpenChanged.InvokeAsync(true);
            }
        }
    }
}
