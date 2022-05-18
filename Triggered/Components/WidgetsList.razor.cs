using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Components
{
    public partial class WidgetsList
    {
        #region Services Injection

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private WidgetService WidgetService { get; set; } = null!;

        [Inject]
        private MessagingService MessagingService { get; set; } = null!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = null!;

        #endregion

        #region Private Variables

        private DotNetObjectReference<WidgetsList>? WidgetsGridHelper;

        private string MarkupTemplate { get; set; } = string.Empty;

        private IEnumerable<Widget> Widgets { get; set; } = Array.Empty<Widget>();
        private Dictionary<string, string>? WidgetNames { get; set; }

        private Widget CurrentWidget { get; set; } = new();
        private bool CurrentWidgetIsValid = false;
        private bool CurrentWidgetIsDirty = false;

        private string WidgetMarkup = string.Empty;

        private object? CodeEditorRef = null;
        private readonly Dictionary<string, Widget> ExternalWidgets = new(); 
        private bool IsExternalWidgetsLoading = false;
        private bool IsEditingCode = false;

        private ModalPrompt ModalPromptReference = null!;

        #endregion

        #region Lifecycle Methods

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                MarkupTemplate = DbContextFactory.CreateDbContext().Settings.GetSetting("WidgetTemplate");
                await SetCurrentWidget(new Widget() { Markup = MarkupTemplate });

                _ = Task.Run(async () => {
                    await PopulateExternalWidgets();
                });

                WidgetsGridHelper = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("initializeCodeEditor", CodeEditorRef, WidgetsGridHelper, true);
                await UpdatePageState();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion

        #region UI Events

        private async Task UpdateProperty(string propertyName, object value)
        { 
            typeof(Widget).GetProperty(propertyName)!.SetValue(CurrentWidget, value);

            await UpdatePageState();
        }

        private async Task PopulateExternalWidgets()
        {
            IsExternalWidgetsLoading = true;
            await InvokeAsync(StateHasChanged);

            ExternalWidgets.Clear();
            if (DbContextFactory.CreateDbContext().Settings.GetSetting("ExternalWidgetsPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                foreach (FileInfo Widget in directoryInfo!.EnumerateFiles("*.html", SearchOption.AllDirectories).OrderBy(file => file.Name))
                {
                    string widgetMarkup = File.ReadAllText(Widget.FullName);
                    string widgetName = Path.GetRelativePath(directoryInfo!.FullName, Widget.FullName); 
                    
                    ExternalWidgets.Add(widgetName, new Widget
                    {
                        Key = widgetName,
                        Markup = widgetMarkup
                    });
                }

            IsExternalWidgetsLoading = false;
            await InvokeAsync(StateHasChanged);
        }

        private async Task SetExternalWidget(string externalWidgetKey)
        {
            async void setCode()
            {
                CurrentWidget.Markup = ExternalWidgets[externalWidgetKey].Markup;
                await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentWidget.Markup);
                await UpdatePageState();
            };

            if (CurrentWidget.Markup != MarkupTemplate)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing markup changes!",
                    Message = $"Are you sure you want to replace the current markup with the external widget \"{externalWidgetKey}\" and lose your changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = setCode
                });
            else
                setCode();
        }

        private async Task CreateWidget()
        {
            async void createAction()
            {
                await SetCurrentWidget(new Widget()
                {
                    Markup = MarkupTemplate
                });
            }

            if (CurrentWidgetIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to create a new widget and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = createAction
                });
            else
                createAction();
        }

        private async Task LoadWidget(string widgetKey)
        {
            if (widgetKey.Equals(CurrentWidget.Key))
                return;

            Widget? widget = Widgets.FirstOrDefault(widget => widget.Key.Equals(widgetKey));

            if (widget == null)
                return;            

            async void loadAction()
            {
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentWidget((Widget)triggeredDbContext.Entry(widget).CurrentValues.Clone().ToObject());
            }

            if (CurrentWidgetIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to edit the widget \"{widget.Key}\" and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = loadAction
                });
            else
                loadAction();
        }

        private async Task SaveWidget()
        {
            using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            if (triggeredDbContext.Widgets.Any(widget => widget.Key.Equals(CurrentWidget.Key)))
            {
                triggeredDbContext.Widgets.Update(CurrentWidget);
                await triggeredDbContext.SaveChangesAsync();
                await MessagingService.AddMessage($"Succesfully updated widget \"{CurrentWidget.Key}\"!", MessageCategory.Widget);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Widget updated!",
                    Message = $"Successfully updated the widget \"{CurrentWidget.Key}\".",
                    CancelChoice = "Dismiss"
                });
            }
            else
            {
                triggeredDbContext.Widgets.Add(CurrentWidget);
                await triggeredDbContext.SaveChangesAsync();
                await MessagingService.AddMessage($"Succesfully added new widget \"{CurrentWidget.Key}\"!", MessageCategory.Widget);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Widget added!",
                    Message = $"Successfully added the widget \"{CurrentWidget.Key}\".",
                    CancelChoice = "Dismiss"
                });
            }

            await UpdatePageState();
        }

        private async Task DeleteWidget(string widgetKey)
        {
            Widget? widget = Widgets.FirstOrDefault(Widget => Widget.Key.Equals(widgetKey));

            if (widget == null)
                return;

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Widget deletion!",
                Message = $"Are you sure you want to delete the widget \"{widget.Key}\"?",
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    if (CurrentWidget.Key == widget.Key)
                        await CreateWidget();

                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                    triggeredDbContext.Remove(widget);
                    await triggeredDbContext.SaveChangesAsync();
                    await MessagingService.AddMessage($"Succesfully removed the widget \"{widget.Key}\"!", MessageCategory.Widget);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Widget removed!",
                        Message = $"Successfully removed the Widget \"{widget.Key}\".",
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });

        }

        private async Task CopyToClipboard()
        {
            string link = NavigationManager.ToAbsoluteUri($"widget/{CurrentWidget.Key}").ToString();
            await JSRuntime.InvokeVoidAsync("navigator.clipboard.writeText", link);

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "Link copied to clipboard.",
                Message = $"Succesfully copied \"{link}\" to clipboard!",
                CancelChoice = "Close"
            });
        }

        #endregion

        #region JS Invokable

        [JSInvokable]
        public async Task SetCode(string markup)
        {
            CurrentWidget.Markup = markup;
            WidgetMarkup = await WidgetService.ReplaceTokens(CurrentWidget);
            await InvokeAsync(StateHasChanged);
        }

        #endregion

        #region Helper Methods

        private async Task UpdatePageState()
        {
            CurrentWidgetIsValid =
                !string.IsNullOrWhiteSpace(CurrentWidget.Key) &&
                !string.IsNullOrWhiteSpace(CurrentWidget.Markup);

            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
            Widget? unsavedWidget = triggeredDbContext.Widgets.FirstOrDefault(dataObject => dataObject.Key == CurrentWidget.Key);

            CurrentWidgetIsDirty = (unsavedWidget == null && !string.IsNullOrWhiteSpace(CurrentWidget.Key)) ||
                 (unsavedWidget != null && !CurrentWidget.Markup.Equals(unsavedWidget.Markup));

            Widgets = triggeredDbContext.Widgets.ToList();
            WidgetNames = Widgets.OrderBy(Widget => Widget.Key).ToDictionary(Widget => Widget.Key, Widget => Widget.Key);
            
            await InvokeAsync(StateHasChanged);
        }

        private async Task SetCurrentWidget(Widget Widget)
        {
            CurrentWidget = Widget;
            await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentWidget.Markup);
            WidgetMarkup = await WidgetService.ReplaceTokens(CurrentWidget);
            await UpdatePageState();
        }

        private async Task ResetCode()
        {
            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Losing markup changes!",
                Message = $"Are you sure you want to replace the current marup with templated markup and lose any changes?",
                CancelChoice = "Cancel",
                Choice = "Yes",
                ChoiceColour = "danger",
                ChoiceAction =  async () =>
                {
                    CurrentWidget.Markup = MarkupTemplate;
                    await Task.CompletedTask;
                }
            });
        }

        #endregion
    }
}
