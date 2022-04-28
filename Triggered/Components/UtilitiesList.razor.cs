using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Web;
using Triggered.Extensions;
using Triggered.Models;
using Triggered.Services;

namespace Triggered.Components
{
    public partial class UtilitiesList
    {
        #region Services Injection

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private ModuleService ModuleService { get; set; } = null!;

        [Inject]
        private MessagingService MessagingService { get; set; } = null!;

        #endregion

        #region Private Variables

        private DotNetObjectReference<UtilitiesList>? UtilitiesGridHelper;

        private string CodeTemplate { get; set; } = string.Empty;

        private IEnumerable<Utility> Utilities { get; set; } = Array.Empty<Utility>();
        private Dictionary<string, string>? UtilityNames { get; set; }

        private Utility CurrentUtility { get; set; } = new();
        private bool CurrentUtilityIsValid = false;
        private bool CurrentUtilityIsDirty = false;

        private object? CodeEditorRef = null;
        private MarkupString CodeAnalysisResults = new();
        private readonly Dictionary<string, Utility> ExternalUtilities = new();

        private ModalPrompt ModalPromptReference = null!;

        private bool IsUtilityValid = false;

        #endregion

        #region Lifecycle Methods

        protected override Task OnInitializedAsync()
        {
            _ = Task.Run(PopulateExternalUtilities);
            return base.OnInitializedAsync();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CodeTemplate = DbContextFactory.CreateDbContext().Settings.GetSetting("UtilityTemplate");
                await SetCurrentUtility(new Utility() { Code = CodeTemplate });

                UtilitiesGridHelper = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("initializeCodeEditor", CodeEditorRef, UtilitiesGridHelper);
                await UpdatePageState();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion

        #region UI Events

        private async Task UpdateProperty(string propertyName, object value)
        { 
            typeof(Utility).GetProperty(propertyName)!.SetValue(CurrentUtility, value);

            await UpdatePageState();
        }

        private Task PopulateExternalUtilities()
        {
            ExternalUtilities.Clear();
            if (DbContextFactory.CreateDbContext().Settings.GetSetting("ExternalUtilitiesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                foreach (FileInfo Utility in directoryInfo!.EnumerateFiles("*.cs", SearchOption.AllDirectories).OrderBy(file => file.Name))
                {
                    string UtilityCode = File.ReadAllText(Utility.FullName);
                    string UtilityName = Path.GetRelativePath(directoryInfo!.FullName, Utility.FullName); 
                    
                    ExternalUtilities.Add(UtilityName, new Utility
                    {
                        Name = UtilityName,
                        Code = UtilityCode
                    });
                }

            return Task.CompletedTask;
        }

        private async Task SetExternalUtility(string externalUtilityKey)
        {
            async void setCode()
            {
                CurrentUtility.Code = ExternalUtilities[externalUtilityKey].Code;
                await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentUtility.Code);
                await UpdatePageState();
                CompileCode();
            };

            if (CurrentUtility.Code != CodeTemplate)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing code changes!",
                    Message = $"Are you sure you want to replace the current code with the external utility \"{externalUtilityKey}\" and lose your changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = setCode
                });
            else
                setCode();
        }

        private async Task CreateUtility()
        {
            async void createAction()
            {
                await SetCurrentUtility(new Utility()
                {
                    Code = CodeTemplate
                });
            }

            if (CurrentUtilityIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to create a new utility and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = createAction
                });
            else
                createAction();
        }

        private async Task LoadUtility(string utilityId)
        {
            if (!int.TryParse(utilityId, out int id) && id == CurrentUtility.Id)
                return;

            Utility? utility = Utilities.FirstOrDefault(utility => utility.Id == id);

            if (utility == null)
                return;            

            async void loadAction()
            {
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentUtility((Utility)triggeredDbContext.Entry(utility).CurrentValues.Clone().ToObject());
            }

            if (CurrentUtilityIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to edit the utility \"{utility.Name}\" and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = loadAction
                });
            else
                loadAction();
        }

        private async Task SaveUtility()
        {
            using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            if (triggeredDbContext.Utilities.Any(Utility => Utility.Id == CurrentUtility.Id))
            {
                triggeredDbContext.Utilities.Update(CurrentUtility);
                await triggeredDbContext.SaveChangesAsync();
                await ModuleService.AnalyzeAndCompileUtilities();
                await MessagingService.AddMessage($"Succesfully updated Utility \"{CurrentUtility.Name}\"!", MessageCategory.Utility);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Utility updated!",
                    Message = $"Successfully updated and compiled the Utility \"{CurrentUtility.Name}\".",
                    CancelChoice = "Dismiss"
                });
            }
            else
            {
                triggeredDbContext.Utilities.Add(CurrentUtility);
                await triggeredDbContext.SaveChangesAsync();
                await ModuleService.AnalyzeAndCompileUtilities();
                await MessagingService.AddMessage($"Succesfully added new Utility \"{CurrentUtility.Name}\"!", MessageCategory.Utility);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Utility added!",
                    Message = $"Successfully added and compiled the Utility \"{CurrentUtility.Name}\".",
                    CancelChoice = "Dismiss"
                });
            }

            await UpdatePageState();
        }

        private async Task DeleteUtility(string UtilityId)
        {
            if (!int.TryParse(UtilityId, out int id))
                return;

            Utility? Utility = Utilities.FirstOrDefault(Utility => Utility.Id == id);

            if (Utility == null)
                return;

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Utility deletion!",
                Message = $"Are you sure you want to delete the Utility \"{Utility.Name}\"?",
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    if (CurrentUtility.Id == Utility.Id)
                        await CreateUtility();

                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                    triggeredDbContext.Remove(Utility);
                    await triggeredDbContext.SaveChangesAsync();
                    await ModuleService.AnalyzeAndCompileUtilities();
                    await MessagingService.AddMessage($"Succesfully removed the Utility \"{Utility.Name}\"!", MessageCategory.Utility);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Utility removed!",
                        Message = $"Successfully removed the Utility \"{Utility.Name}\".",
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });

        }

        #endregion

        #region JS Invokable

        [JSInvokable]
        public Task SetCode(string code)
        {
            CurrentUtility.Code = code;
            CompileCode();

            return Task.CompletedTask;
        }

        [JSInvokable]
        public Task<string> GetNetObjectMembers(string name)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(name) && 
                ModuleService.NetObjects.TryGetValue(name, out IEnumerable<(string name, string value, string kind)>? netObjectMembers) && 
                netObjectMembers != null ?
                    JsonConvert.SerializeObject(netObjectMembers) :
                    JsonConvert.SerializeObject(Array.Empty<string>()));
        }

        #endregion

        #region Helper Methods

        private async Task UpdatePageState()
        {
            CurrentUtilityIsValid =
                !string.IsNullOrWhiteSpace(CurrentUtility.Name) &&
                !string.IsNullOrWhiteSpace(CurrentUtility.Code) &&
                IsUtilityValid;

            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentUtilityIsDirty = (CurrentUtility.Id == null && !UtilityComparer.MemberwiseComparer.Equals(CurrentUtility, new Utility { Code = CodeTemplate })) ||
                (CurrentUtility.Id != null && !UtilityComparer.MemberwiseComparer.Equals(CurrentUtility, triggeredDbContext.Utilities.FirstOrDefault(Utility => Utility.Id == CurrentUtility.Id)));

            Utilities = triggeredDbContext.Utilities.ToList();
            UtilityNames = Utilities.OrderBy(Utility => Utility.Name).ToDictionary(Utility => Utility.Id.ToString()!, Utility => Utility.Name);
            
            await InvokeAsync(StateHasChanged);
        }

        private async Task SetCurrentUtility(Utility Utility)
        {
            CurrentUtility = Utility;
            await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentUtility.Code);
            await UpdatePageState();
            CompileCode();
        }

        private async Task ResetCode()
        {
            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Losing code changes!",
                Message = $"Are you sure you want to replace the current code with templated code and lose any changes?",
                CancelChoice = "Cancel",
                Choice = "Yes",
                ChoiceColour = "danger",
                ChoiceAction =  async () =>
                {
                    CurrentUtility.Code = CodeTemplate;
                    CompileCode();
                    await Task.CompletedTask;
                }
            });
        }

        private bool IsAnalysing = false;
        private bool IsWaiting = false;
        private void CompileCode()
        {
            _ = Task.Run(async () =>
            {
                if (!IsAnalysing)
                {
                    IsAnalysing = true;
                    string codeAnalysisResultsString = string.Empty;

                    if (string.IsNullOrWhiteSpace(CurrentUtility.Code))
                    {
                        codeAnalysisResultsString = "<span class=\"text-danger\"><strong>Code is empty!</strong></span>";
                    }
                    else
                    {
                        IEnumerable<string> warnings;
                        IEnumerable<string> errors;
                        (warnings, errors) = await ModuleService.AnalyzeAndCompileUtilities(CurrentUtility.Code);

                        codeAnalysisResultsString = string.Empty;
                        if (errors.Any())
                            codeAnalysisResultsString += $"<span class=\"text-danger\"><strong><u>Errors:</u></strong><br/>" +
                                $"{string.Join("<br/>", errors.Select(str => HttpUtility.HtmlEncode(str)))}</span><br/><br/>";
                        if (warnings.Any())
                            codeAnalysisResultsString += $"<span class=\"text-warning\"><strong><u>Warnings:</u></strong><br/>" +
                                $"{string.Join("<br/>", warnings.Select(str => HttpUtility.HtmlEncode(str)))}</span><br/><br/>";

                        if (!errors.Any() && !warnings.Any())
                        {
                            IsUtilityValid = true;
                            codeAnalysisResultsString = "<span class=\"text-success\"><strong>Code compilation was succesful!</strong></span>";
                        }
                    }

                    CodeAnalysisResults = new MarkupString(codeAnalysisResultsString);
                    await UpdatePageState();
                    IsAnalysing = false;
                }
                else if (!IsWaiting)
                {
                    IsWaiting = true;
                    while (IsAnalysing)
                    {
                        await Task.Delay(500);
                    }
                    IsWaiting = false;
                    CompileCode();
                }    
            });
        }

        #endregion
    }
}
