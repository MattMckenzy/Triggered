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
    public partial class ModulesList
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
        private readonly Dictionary<string, Module> ExternalModules = new();

        private ModalPrompt ModalPromptReference = null!;

        #endregion

        #region Lifecycle Methods

        protected override Task OnInitializedAsync()
        {
            _ = Task.Run(PopulateExternalModules);
            return base.OnInitializedAsync();
        }

        protected async override Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                CodeTemplate = DbContextFactory.CreateDbContext().Settings.GetSetting("ModuleTemplate");
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

            if (propertyName.Equals(nameof(Module.ExecutionOrder)))
                typeof(Module).GetProperty(propertyName)!.SetValue(CurrentModule, int.TryParse((string)value, out int intValue) ? intValue : 0);
            else
                 typeof(Module).GetProperty(propertyName)!.SetValue(CurrentModule, value);


            if (new string[] { nameof(Module.Event), nameof(Module.EntryMethod) }.Contains(propertyName))
                CompileCode();

            await UpdatePageState();
        }

        private Task PopulateExternalModules()
        {
            ExternalModules.Clear();
            if (DbContextFactory.CreateDbContext().Settings.GetSetting("ExternalModulesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                foreach (FileInfo module in directoryInfo!.EnumerateFiles("*.cs", SearchOption.AllDirectories).OrderBy(file => file.Name))
                {
                    string moduleCode = File.ReadAllText(module.FullName);
                    string? eventName = ModuleService.GetCodeEvent(moduleCode);

                    string moduleName = Path.GetRelativePath(directoryInfo!.FullName, module.FullName); 
                    
                    ExternalModules.Add(moduleName, new Module
                    {
                        Name = moduleName,
                        Code = moduleCode,
                        Event = eventName ?? string.Empty
                    });
                }

            return Task.CompletedTask;
        }

        private async Task SetExternalModule(string externalModuleKey)
        {
            async void setCode()
            {
                CurrentModule.Code = ExternalModules[externalModuleKey].Code;
                await ReplaceCodeTemplates();
                CompileCode();
            };

            if (CurrentModule.Code != await ReplaceCodeTemplatesInternal(CodeTemplate))
                await ModalPromptReference.ShowModalPrompt(new()
                {
                    Title = "WARNING: Losing code changes!",
                    Message = $"Are you sure you want to replace the current code with the external module \"{externalModuleKey}\" and lose your changes?",
                    CancelChoice = "Cancel",
                    Choice = "Yes",
                    ChoiceColour = "danger",
                    ChoiceAction = setCode
                });
            else
                setCode();
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
                using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

                await SetCurrentModule((Module)triggeredDbContext.Entry(module).CurrentValues.Clone().ToObject());
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
            using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            if (triggeredDbContext.Modules.Any(module => module.Id == CurrentModule.Id))
            {
                triggeredDbContext.Modules.Update(CurrentModule);
                await triggeredDbContext.SaveChangesAsync();
                await ModuleService.UpdateModule(CurrentModule);
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
                triggeredDbContext.Modules.Add(CurrentModule);
                await triggeredDbContext.SaveChangesAsync();
                await ModuleService.AddModule(CurrentModule);
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

                    using TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();
                    triggeredDbContext.Remove(module);
                    await triggeredDbContext.SaveChangesAsync();
                    await ModuleService.RemoveModule((int)module.Id!, CurrentModule.Event);
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
            CurrentModule.Code = await ReplaceCodeTemplatesInternal(CurrentModule.Code);
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

        private Task<string> ReplaceCodeTemplatesInternal(string code)
        {
            code = code
            .Replace(@"/*EventName*/", ModuleService.SupportedEvents[CurrentModule.Event])
            .Replace(@"/*EventArgs*/", ModuleService.EventArgumentTypes[CurrentModule.Event].Name);

            if (!string.IsNullOrWhiteSpace(CurrentModule.Name))
                code =
                    code.Replace(@"/*ModuleName*/", CurrentModule.EntryMethod.Equals(CurrentModule.Name) ? $"{ModuleService.SupportedEvents[CurrentModule.Event]}{CurrentModule.Name}" : CurrentModule.Name);

            if (!string.IsNullOrWhiteSpace(CurrentModule.EntryMethod))
                code = code.Replace(@"/*EntryMethod*/", CurrentModule.EntryMethod);

            return Task.FromResult(code);
        }

        private async Task UpdatePageState()
        {
            CurrentModuleIsValid =
                !string.IsNullOrWhiteSpace(CurrentModule.Name) &&
                !string.IsNullOrWhiteSpace(CurrentModule.EntryMethod) &&
                !string.IsNullOrWhiteSpace(CurrentModule.Code) &&
                CompiledModule != null;

            TriggeredDbContext triggeredDbContext = await DbContextFactory.CreateDbContextAsync();

            CurrentModuleIsDirty = (CurrentModule.Id == null && !ModuleComparer.MemberwiseComparer.Equals(CurrentModule, new Module { Code = CodeTemplate })) ||
                (CurrentModule.Id != null && !ModuleComparer.MemberwiseComparer.Equals(CurrentModule, triggeredDbContext.Modules.FirstOrDefault(module => module.Id == CurrentModule.Id)));

            Modules = triggeredDbContext.Modules.ToList();
            ModuleNames = Modules.OrderBy(module => module.Name).ToDictionary(module => module.Id.ToString()!, module => module.Name);
            CategoryModules = Modules.OrderBy(module => module.Event).ThenBy(module => module.Name).GroupBy(module => module.Event).ToDictionary(category => category.Key, category => category.Select(module => module.Id.ToString()!));

            await InvokeAsync(StateHasChanged);
        }

        private async Task SetCurrentModule(Module module)
        {
            CurrentModule = module;
            await JSRuntime.InvokeAsync<string>("setCode", CodeEditorRef, CurrentModule.Code);
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
                ChoiceAction = async () =>
                {
                    CurrentModule.Code = CodeTemplate;
                    await ReplaceCodeTemplates();
                    CompileCode();
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

                    if (string.IsNullOrWhiteSpace(CurrentModule.Code))
                    {
                        codeAnalysisResultsString = "<span class=\"text-danger\"><strong>Code is empty!</strong></span>";
                    }
                    else
                    {
                        IEnumerable<string> warnings;
                        IEnumerable<string> errors;
                        (CompiledModule, warnings, errors) = ModuleService.AnalyzeAndCompileModule(CurrentModule);

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
