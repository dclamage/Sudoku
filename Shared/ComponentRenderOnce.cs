using Microsoft.AspNetCore.Components;

namespace SudokuBlazor.Shared
{
    public abstract class ComponentRenderOnce : ComponentBase
    {
        protected bool rendered = false;

        protected override bool ShouldRender()
        {
            if (!rendered)
            {
                rendered = true;
                return true;
            }
            return false;
        }
    }
}
