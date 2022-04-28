using Microsoft.AspNetCore.Components;

namespace Triggered.Extensions
{    
    /// <summary>
    /// Custom blazor component base. Helps queue actions to be run after render.
    /// </summary>
    public class RenderComponentBase : ComponentBase
    {
        private readonly List<Func<Task>> actionsToRunAfterRender = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            foreach (var actionToRun in actionsToRunAfterRender)
            {
                await actionToRun();
            }

            actionsToRunAfterRender.Clear();

            await base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// Run an action once after the component is rendered.
        /// </summary>
        /// <param name="action">Action to invoke after render.</param>
        protected void RunAfterRender(Func<Task> action) => actionsToRunAfterRender.Add(action);
    }
}
