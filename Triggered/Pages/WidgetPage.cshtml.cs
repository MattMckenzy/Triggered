using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Pages
{
    public class WidgetModel : PageModel
    {
        private WidgetService WidgetService { get; }
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MessagingService MessagingService { get; }

        private Widget Widget { get; set; } = null!;

        [ViewData]
        public string WidgetMarkup { get; private set; } = string.Empty;

        public WidgetModel(WidgetService widgetService, IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService)
        {
            WidgetService = widgetService;
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;
        }

        public async Task OnGet(string key)
        {
            Widget? widget = (await DbContextFactory.CreateDbContextAsync()).Widgets.FirstOrDefault(widget => widget.Key.Equals(key));
            if (widget == null)
            {
                string errorMessage = $"The widget \"{key}\" could not be found.";
                await MessagingService.AddMessage(errorMessage, MessageCategory.Widget, LogLevel.Warning);
                WidgetMarkup = $"<strong style=\"color:red;\">{errorMessage}</strong>";
            }
            else
            {
                Widget = widget;
                WidgetMarkup = new(await WidgetService.ReplaceTokens(Widget));
            }
        }
    }
}