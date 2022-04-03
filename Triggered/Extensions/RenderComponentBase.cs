using Microsoft.AspNetCore.Components;

namespace Triggered.Extensions
{    
    public class RenderComponentBase : ComponentBase
    {
        // store all the actions you want to run **once** after rendering
        private readonly List<Func<Task>> actionsToRunAfterRender = new();
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // run all the actions (.NET code) **once** after rendering
            foreach (var actionToRun in actionsToRunAfterRender)
            {
                await actionToRun();
            }
            // clear the actions to make sure the actions only run **once**
            actionsToRunAfterRender.Clear();

            await base.OnAfterRenderAsync(firstRender);
        }

        /// <summary>
        /// Run an action once after the component is rendered
        /// </summary>
        /// <param name="action">Action to invoke after render</param>
        protected void RunAfterRender(Func<Task> action) => actionsToRunAfterRender.Add(action);
    }
}
