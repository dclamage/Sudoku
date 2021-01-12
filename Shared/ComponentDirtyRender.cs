using Microsoft.AspNetCore.Components;

namespace SudokuBlazor.Shared
{
    public abstract class ComponentDirtyRender : ComponentBase
    {
        protected bool isDirty = true;

        protected override bool ShouldRender()
        {
            if (isDirty)
            {
                isDirty = false;
                return true;
            }
            return false;
        }

        protected void SetDirty()
        {
            isDirty = true;
            StateHasChanged();
        }
    }
}
