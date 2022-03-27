using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using System.Web;
using TownBulletin.Extensions;
using TownBulletin.Models;
using TownBulletin.Services;

namespace TownBulletin.Components
{
    public partial class ModulesList
    {
        #region Services Injection

        [Inject]
        private IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        private IDbContextFactory<TownBulletinDbContext> DbContextFactory { get; set; } = null!;

        [Inject]
        private ModuleService ModuleService { get; set; } = null!;

        [Inject]
        private MessagingService MessagingService { get; set; } = null!;

        [Inject]
        private IWebHostEnvironment IWebHostEnvironment { get; set; } = null!;

        #endregion

        #region Private Variables

        private DotNetObjectReference<ModulesList>? ModulesGridHelper;

        private string CodeTemplate { get; set; } = string.Empty;

        private IEnumerable<Module> Modules { get; set; } = Array.Empty<Module>();
        private Dictionary<string, string>? ModuleNames { get; set; }
        private Dictionary<string, IEnumerable<string>>? CategoryModules { get; set; }

        private Module CurrentModule { get; set; } = new();
        private bool CurrentModuleIsValid = false;
        private bool CurrentModuleIsDirty = false;

        private object? CodeEditorRef = null;
        private MarkupString CodeAnalysisResults = new();
        private CompiledModule? CompiledModule = null;

        private ModalPrompt ModalPromptReference = null!;

        #endregion

        #region Lifecycle Methods

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CodeTemplate = await File.ReadAllTextAsync(Path.Combine(IWebHostEnvironment.ContentRootPath, "Resources", "ModuleTemplate.cs"));
                await SetCurrentModule(new Module() { Code = CodeTemplate });

                ModulesGridHelper = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("initializeCodeEditor", CodeEditorRef, ModulesGridHelper);
                await UpdatePageState();
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        #endregion

        #region UI Events

        private async Task UpdateProperty(string propertyName, object value)
        {
            if (propertyName == nameof(Module.Event) && value is string valueString && string.IsNullOrWhiteSpace(valueString))
                return;

            typeof(Module).GetProperty(propertyName)!.SetValue(CurrentModule, value);

            if (new string[] { nameof(CurrentModule.Event), nameof(CurrentModule.EntryMethod) }.Contains(propertyName))
                CompileCode();

            await UpdatePageState();
        }

        private async Task CreateModule()
        {
            async void createAction()
            {
                await SetCurrentModule(new Module()
                {
                    Code = CodeTemplate
                });
            }

            if (CurrentModuleIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to create a new module and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = createAction
                });
            else
                createAction();
        }

        private async Task LoadModule(string moduleId)
        {
            if (!int.TryParse(moduleId, out int id) && id == CurrentModule.Id)
                return;

            Module? module = Modules.FirstOrDefault(module => module.Id == id);

            if (module == null)
                return;            

            async void loadAction()
            {
                using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentModule((Module)townBulletinDbContext.Entry(module).CurrentValues.Clone().ToObject());
            }

            if (CurrentModuleIsDirty)
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing changes!",
                    Message = $"Are you sure you want to edit the module \"{module.Name}\" and dismiss the unsaved changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = loadAction
                });
            else
                loadAction();
        }

        private async Task SaveModule()
        {
            using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

            if (townBulletinDbContext.Modules.Any(module => module.Id == CurrentModule.Id))
            {
                townBulletinDbContext.Modules.Update(CurrentModule);
                await townBulletinDbContext.SaveChangesAsync();
                ModuleService.UpdateModule(CurrentModule);
                await MessagingService.AddMessage($"Succesfully updated Module \"{CurrentModule.Name}\"!", MessageCategory.Module);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Module updated!",
                    Message = $"Successfully updated and compiled the module \"{CurrentModule.Name}\".",
                    CancelChoice = "Dismiss"
                });
            }
            else
            {
                townBulletinDbContext.Modules.Add(CurrentModule);
                await townBulletinDbContext.SaveChangesAsync();
                ModuleService.AddModule(CurrentModule);
                await MessagingService.AddMessage($"Succesfully added new Module \"{CurrentModule.Name}\"!", MessageCategory.Module);

                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "Module added!",
                    Message = $"Successfully added and compiled the module \"{CurrentModule.Name}\".",
                    CancelChoice = "Dismiss"
                });
            }

            await UpdatePageState();
        }

        private async Task DeleteModule(string moduleId)
        {
            if (!int.TryParse(moduleId, out int id))
                return;

            Module? module = Modules.FirstOrDefault(module => module.Id == id);

            if (module == null)
                return;

            await ModalPromptReference.ShowModalPrompt(new()
            {
                Title = "WARNING: Module deletion!",
                Message = $"Are you sure you want to delete the module \"{module.Name}\"?",
                CancelChoice = "Cancel",
                Choice = "Delete",
                ChoiceColour = "danger",
                ChoiceAction = async () =>
                {
                    if (CurrentModule.Id == module.Id)
                        await CreateModule();

                    using TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();
                    townBulletinDbContext.Remove(module);
                    await townBulletinDbContext.SaveChangesAsync();
                    ModuleService.RemoveModule((int)module.Id!, CurrentModule.Event);
                    await MessagingService.AddMessage($"Succesfully removed the Module \"{module.Name}\"!", MessageCategory.Module);

                    await ModalPromptReference.ShowModalPrompt(new()
                    {
                        Title = "Module removed!",
                        Message = $"Successfully removed the module \"{module.Name}\".",
                        CancelChoice = "Dismiss"
                    });

                    await UpdatePageState();
                }
            });

        }

        private async Task ReplaceCodeTemplates()
        {
            //TODO: Create template list

            CurrentModule.Code = CurrentModule.Code
                .Replace(@"/*EventName*/", ModuleService.SupportedEvents[CurrentModule.Event])
                .Replace(@"/*EventArgs*/", ModuleService.EventArgumentTypes[CurrentModule.Event].Name);

            if (!string.IsNullOrWhiteSpace(CurrentModule.Name))
                CurrentModule.Code =
                    CurrentModule.Code.Replace(@"/*ModuleName*/", CurrentModule.EntryMethod.Equals(CurrentModule.Name) ? $"{ModuleService.SupportedEvents[CurrentModule.Event]}{CurrentModule.Name}" : CurrentModule.Name);

            if (!string.IsNullOrWhiteSpace(CurrentModule.EntryMethod))
                CurrentModule.Code =
                    CurrentModule.Code.Replace(@"/*EntryMethod*/", CurrentModule.EntryMethod);

            await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentModule.Code);
            await UpdatePageState();
        }

        #endregion

        #region JS Invokable

        [JSInvokable]
        public Task SetCode(string code)
        {
            CurrentModule.Code = code;
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
            CurrentModuleIsValid =
                !string.IsNullOrWhiteSpace(CurrentModule.Name) &&
                !string.IsNullOrWhiteSpace(CurrentModule.EntryMethod) &&
                !string.IsNullOrWhiteSpace(CurrentModule.Code) &&
                CompiledModule != null;

            TownBulletinDbContext townBulletinDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentModuleIsDirty = (CurrentModule.Id == null && !ModuleComparer.MemberwiseComparer.Equals(CurrentModule, new Module { Code = CodeTemplate })) ||
                (CurrentModule.Id != null && !ModuleComparer.MemberwiseComparer.Equals(CurrentModule, townBulletinDbContext.Modules.FirstOrDefault(module => module.Id == CurrentModule.Id)));

            Modules = townBulletinDbContext.Modules.ToList();
            ModuleNames = Modules.OrderBy(module => module.Name).ToDictionary(module => module.Id.ToString()!, module => module.Name);
            CategoryModules = Modules.OrderBy(module => module.Event).ThenBy(module => module.Name).GroupBy(module => module.Event).ToDictionary(category => category.Key.Split(".").Last(), category => category.Select(module => module.Id.ToString()!));

            await InvokeAsync(StateHasChanged);
        }


        private async Task SetCurrentModule(Module module)
        {
            CurrentModule = module;
            await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentModule.Code);
            await UpdatePageState();
            CompileCode();
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

                    if (string.IsNullOrWhiteSpace(CurrentModule.Code))
                    {
                        codeAnalysisResultsString = "<span class=\"text-danger\"><strong>Code is empty!</strong></span>";
                    }
                    else
                    {
                        IEnumerable<string> warnings;
                        IEnumerable<string> errors;
                        (CompiledModule, warnings, errors) = ModuleService.CompileAndAnalyzeModule(CurrentModule);

                        codeAnalysisResultsString = string.Empty;
                        if (errors.Any())
                            codeAnalysisResultsString += $"<span class=\"text-danger\"><strong><u>Errors:</u></strong><br/>" +
                                $"{string.Join("<br/>", errors.Select(str => HttpUtility.HtmlEncode(str)))}</span><br/><br/>";
                        if (warnings.Any())
                            codeAnalysisResultsString += $"<span class=\"text-warning\"><strong><u>Warnings:</u></strong><br/>" +
                                $"{string.Join("<br/>", warnings.Select(str => HttpUtility.HtmlEncode(str)))}</span><br/><br/>";

                        if (!errors.Any() && !warnings.Any())
                            codeAnalysisResultsString = "<span class=\"text-success\"><strong>Code compilation was succesful!</strong></span>";
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
