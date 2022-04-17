using Eltons.ReflectionKit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    public class ModuleService
    {
        #region Private Variables

        private readonly IDbContextFactory<TriggeredDbContext> _dbContextFactory;
        private readonly MessagingService _messagingService;

        private Dictionary<string, List<CompiledModule>> EventModules { get; } = new();

        readonly string UtilitiesAssemblyName = Path.GetRandomFileName();
        private byte[]? UtilitiesAssembly { get; set; } = null;

        #endregion

        #region Public Properties

        public Dictionary<Type, object> EventObjects { get; } = new();

        public Dictionary<string, string> SupportedEvents { get; } = new();

        public Dictionary<string, Type> EventArgumentTypes { get; } = new();

        public Dictionary<string, Type> SupportedArgumentTypes { get; } = new();

        public Dictionary<string, IEnumerable<(string name, string value, string kind)>> NetObjects { get; } = new();

        public event EventHandler<CustomEventArgs>? OnCustomEvent;

        #endregion

        #region Constructor

        public ModuleService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, DataService dataService, QueueService queueService, MemoryCache memoryCache)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;

            IntializeNetObjects();

            RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(DataService), typeof(DataService), dataService),
                (nameof(QueueService), typeof(QueueService), queueService),
                (nameof(MemoryCache), typeof(MemoryCache), memoryCache),
                (nameof(ModuleService), typeof(ModuleService), this),
                (nameof(MessagingService), typeof(MessagingService), _messagingService),
                (nameof(IDbContextFactory<TriggeredDbContext>), typeof(IDbContextFactory<TriggeredDbContext>), dbContextFactory)
            });

            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (new AssemblyName(args.Name).Name == UtilitiesAssemblyName)
                { 
                    return Assembly.Load(UtilitiesAssembly!.ToArray()); 
                }

                return null;
            };
        }

        public void IntializeNetObjects()   
        {
            // TODO: Make autocomplete smarter.

            List<Assembly> assemblies = new();
            assemblies.AddRange(Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .Select(item => Assembly.Load(item)));

            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            if (_dbContextFactory.CreateDbContext().Settings.GetSetting("ExternalResourcesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                assemblies.AddRange(directoryInfo!.GetFiles("*.dll", SearchOption.AllDirectories).Select(referenceFile => Assembly.LoadFile(referenceFile.FullName)));

            foreach (Type type in assemblies.SelectMany(assembly => assembly.GetTypes()))
            {
                List<(string name, string value, string kind)> memberObjects = new();

                IEnumerable<MemberInfo> memberInfos =
                    type.GetMembers(BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.Public);

                memberObjects.AddRange(memberInfos.Where(memberInfo => !memberInfo.MemberType.ToString().Equals("Method"))
                    .Select(memberInfo => (memberInfo.Name, memberInfo.Name, memberInfo.MemberType.ToString())));

                IEnumerable<MethodInfo> methodInfos =
                    type.GetMethods(BindingFlags.Instance |
                        BindingFlags.Static |
                        BindingFlags.Public);

                memberObjects.AddRange(methodInfos
                    .Select(methodInfo => (methodInfo.GetSignature(true), methodInfo.GetSignature(true), "Method")));

                if (!NetObjects.TryAdd(type.Name, memberObjects))
                    NetObjects[type.Name] = NetObjects[type.Name].Union(memberObjects);

                if (type.Namespace != null)
                {
                    IEnumerable<string> namespaceStrings = type.Namespace.Split(".");
                    for (int i = 0; i < namespaceStrings.Count() - 1; i++)
                    {
                        string name = namespaceStrings.ElementAt(i + 1);
                        if (!NetObjects.TryAdd(namespaceStrings.ElementAt(i), new List<(string, string, string)> {
                            (name, name, "Namespace") }))
                        {
                            NetObjects[namespaceStrings.ElementAt(i)] = NetObjects[namespaceStrings.ElementAt(i)].Union(new List<(string, string, string)> {
                            (name, name, "Namespace") });
                        }
                    }

                    if (!NetObjects.TryAdd(namespaceStrings.Last(), new List<(string, string, string)> {
                        ( type.Name, type.Name, "Type") }))
                    {
                        NetObjects[namespaceStrings.Last()] = NetObjects[namespaceStrings.Last()].Union(new List<(string, string, string)> {
                            ( type.Name, type.Name, "Type") });
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        public async Task AddUtilities()
        {
            SupportedEvents.Add("ModuleService.OnCustomEvent", "Custom");
            EventArgumentTypes.Add("ModuleService.OnCustomEvent", typeof(CustomEventArgs));
            SupportedArgumentTypes.Add("CustomEventArgs", typeof(CustomEventArgs));
            RegisterEvents(this);

            await CompileUtilities();
        }

        public async Task<(List<string>, List<string>)> CompileUtilities(string? code = null)
        {
            List<string> compilationWarnings = new();
            List<string> compilationErrors = new();
            List<SyntaxTree> syntaxTrees = new();

            if (code != null)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code));
            else
                foreach (Utility utility in (await _dbContextFactory.CreateDbContextAsync()).Utilities)
                {
                    syntaxTrees.Add(CSharpSyntaxTree.ParseText(utility.Code));
                }

            if (syntaxTrees.Any())
            {
                List<MetadataReference> references = GetReferences(false);

                CSharpCompilation compilation = CSharpCompilation.Create(
                    UtilitiesAssemblyName,
                    syntaxTrees: syntaxTrees,
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, 
                        metadataReferenceResolver: new MissingResolver(),
                        nullableContextOptions: NullableContextOptions.Enable));

                using MemoryStream memoryStream = new();
                EmitResult result = compilation.Emit(memoryStream);

                IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                    diagnostic.IsWarningAsError ||
                    diagnostic.Severity == DiagnosticSeverity.Error);
                compilationErrors.AddRange(failures.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));

                IEnumerable<Diagnostic> warnings = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
                compilationWarnings.AddRange(warnings.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));
                
                if (result.Success && code == null)
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    UtilitiesAssembly = memoryStream.ToArray();
                }
            }

            return (compilationWarnings, compilationErrors);        
        }

        public void RegisterParameterObjects(IEnumerable<(string name, Type type, object instance)> parameterObjects)
        {
            foreach ((string name, Type type, object instance) in parameterObjects)
            {
                EventObjects.Add(type, instance);
                SupportedArgumentTypes.Add(name, type);
            }
        }

        public void InitializeSupportedEventsAndParameters<T>(T classInstance) where T : class
        {
            foreach (EventInfo eventInfo in classInstance.GetType().GetEvents().OrderBy(eventInfo => eventInfo.Name))
            {
                string eventKey = $"{typeof(T).Name}.{eventInfo!.Name}";

                Type? eventType = eventInfo.EventHandlerType;

                if (eventType != null)
                {
                    Type? eventArgumentType = eventType.GenericTypeArguments.FirstOrDefault();
                    if (eventArgumentType != null)
                    {
                        SupportedEvents.Add(eventKey, eventInfo.Name);
                        EventArgumentTypes.Add(eventKey, eventArgumentType);
                        SupportedArgumentTypes.TryAdd(eventArgumentType.Name, eventArgumentType);
                    }
                }
            }
        }

        private readonly Dictionary<string, Action<object, object>> _eventHandlers = new();

        public void RegisterEvents<T>(T classInstance) where T : class
        {
            foreach (EventInfo eventInfo in classInstance.GetType().GetEvents())
            {
                string eventKey = $"{typeof(T).Name}.{eventInfo!.Name}";
                Type? eventType = eventInfo.EventHandlerType;

                if (eventType != null)
                {
                    Action<object, object> eventHandler = async (object _, object eventArgs) =>
                    {
                        await ExecuteModules(eventKey, eventArgs);
                    };

                    if (!_eventHandlers.TryAdd(eventKey, eventHandler))
                        _eventHandlers[eventKey] = eventHandler;

                    eventInfo.AddEventHandler(classInstance, Delegate.CreateDelegate(
                        eventType,
                        eventHandler.Target,
                        eventHandler.Method));

                    CompileModules(eventKey);
                }
            }
        }

        public void DeregisterEvents<T>(T classInstance) where T : class
        {
            foreach (EventInfo eventInfo in classInstance.GetType().GetEvents())
            {
                string eventKey = $"{typeof(T).Name}.{eventInfo!.Name}";
                Type? eventType = eventInfo.EventHandlerType;

                if (eventType != null)
                {
                    if (_eventHandlers.TryGetValue(eventKey, out Action<object, object>? eventHandler) && eventHandler != null)
                    {
                        _eventHandlers.Remove(eventKey);

                        if (eventHandler != null)
                        {
                            eventInfo.RemoveEventHandler(classInstance, Delegate.CreateDelegate(
                                eventType,
                                eventHandler.Target,
                                eventHandler.Method));
                        }
                    }


                    ClearModules(eventKey);
                }
            }
        }

        public void CompileAllModules()
        {
            foreach (string eventName in SupportedEvents.Keys)
                CompileModules(eventName);            
        }

        public void CompileModules(string eventName)
        {
            using TriggeredDbContext triggeredDbContext = _dbContextFactory.CreateDbContext();

            List<CompiledModule> returnModules = new();

            foreach (Models.Module module in triggeredDbContext.Modules
                .Where(module => module.Event == eventName)
                .OrderBy(module => module.ExecutionOrder))
            {
                CompiledModule? compiledModule = CompileModule(module);
                if (compiledModule != null)
                {
                    returnModules.Add(compiledModule);
                    _messagingService.AddMessage($"Module \"{module.Name}\" compiled succesfully!", MessageCategory.Module, LogLevel.Debug);
                }
            }

            lock (EventModules)
            {
                EventModules[eventName] = returnModules;
            }
        }

        public void ClearModules(string eventName)
        {
            lock (EventModules)
            {
                EventModules[eventName] = new();
            }
        }

        public (CompiledModule? CompiledModule, IEnumerable<string> Warnings, IEnumerable<string> Errors) CompileAndAnalyzeModule(Models.Module module)
        {
            CompiledModule? compiledModule = null;
            List<string> compilationWarnings = new();
            List<string> compilationErrors = new();

            CompileCode(module.Code, out CSharpCompilation compilation, out SemanticModel semanticModel, out IEnumerable<MethodDeclarationSyntax> methodDeclarationSyntaxes);

            try
            {
                MethodDeclarationSyntax? methodDeclarationSyntax = methodDeclarationSyntaxes.FirstOrDefault(method => method.Identifier.Text.Equals(module.EntryMethod));

                if (methodDeclarationSyntax != null)
                {
                    List<Type> parameterTypes = new();
                    foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
                    {
                        string? typeName = semanticModel.GetDeclaredSymbol(parameterSyntax)?.Type.Name;
                        if (typeName == null || !SupportedArgumentTypes.TryGetValue(typeName, out Type? parameterType) || parameterType == null)
                            compilationErrors.Add($"Module entry method parameter type \"{typeName ?? "N/A"}\" is not a supported type.");
                        else
                            parameterTypes.Add(parameterType);
                    }

                    IMethodSymbol? methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclarationSyntax);
                    INamedTypeSymbol? returnTypeSymbol = (INamedTypeSymbol?)methodSymbol?.ReturnType;
                    INamedTypeSymbol? returnTypeArgumentSymbol = (INamedTypeSymbol?)returnTypeSymbol?.TypeArguments.FirstOrDefault();

                    if (!compilationErrors.Any() &&
                        methodSymbol != null &&
                        returnTypeSymbol != null &&
                        returnTypeSymbol.Name.Equals("Task") &&
                        @returnTypeSymbol.TypeArguments.Length == 1 &&
                        returnTypeArgumentSymbol != null &&
                        returnTypeArgumentSymbol.Name.Equals("Boolean"))
                    {
                        using MemoryStream memoryStream = new();
                        EmitResult result = compilation.Emit(memoryStream);

                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);
                        compilationErrors.AddRange(failures.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));

                        IEnumerable<Diagnostic> warnings = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
                        compilationWarnings.AddRange(warnings.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));

                        if (result.Success)
                        {
                            memoryStream.Seek(0, SeekOrigin.Begin);
                            Assembly assembly = Assembly.Load(memoryStream.ToArray());

                            Type? methodType = assembly.GetType(methodSymbol.ContainingSymbol.ToDisplayString());

                            if (methodType != null)
                            {
                                object? classInstance = Activator.CreateInstance(methodType);

                                compiledModule = new CompiledModule(
                                    module,
                                    parameterTypes,
                                    (object[] arguments) => (Task<bool>?)methodType.InvokeMember(module.EntryMethod,
                                        BindingFlags.Default | BindingFlags.InvokeMethod,
                                    null,
                                    classInstance,
                                    arguments) ?? Task.FromResult(false));
                            }
                            else
                                compilationErrors.Add($"Module entry method type \"{methodSymbol.ToDisplayString()}\" was not able to discerned from the code.");
                        }
                    }
                    else
                        compilationErrors.Add($"Module entry method return type needs to be of type \"Task<bool>\".");
                }
                else
                    compilationErrors.Add($"Module entry method \"{module.EntryMethod}\" was not found in the code.");
            }
            finally
            {
                methodDeclarationSyntaxes = null!;
                semanticModel = null!;
                compilation = null!;
                GC.Collect();
            }        

            return (compiledModule, compilationWarnings, compilationErrors);
        }

        public string? GetCodeEvent(string code)
        {
            string? returnEvent = null;

            CompileCode(code, out CSharpCompilation compilation, out SemanticModel semanticModel, out IEnumerable<MethodDeclarationSyntax> methodDeclarationSyntaxes);

            try
            {
                foreach (MethodDeclarationSyntax methodDeclarationSyntax in methodDeclarationSyntaxes)
                {
                    List<Type> parameterTypes = new();
                    foreach (ParameterSyntax parameterSyntax in methodDeclarationSyntax.ParameterList.Parameters)
                    {
                        string? typeName = semanticModel.GetDeclaredSymbol(parameterSyntax)?.Type.Name;
                        if (typeName != null && 
                            SupportedArgumentTypes.TryGetValue(typeName, out Type? parameterType) &&
                            parameterType != null)
                            returnEvent = EventArgumentTypes.FirstOrDefault(eventArgumentType => eventArgumentType.Value.Equals(parameterType)).Key;                    
                        else
                            continue;

                        if (!string.IsNullOrEmpty(returnEvent))
                            return returnEvent;
                    }
                }
            }
            finally
            {
                methodDeclarationSyntaxes = null!;
                semanticModel = null!;
                compilation = null!;
                GC.Collect();
            }

            return returnEvent;
        }

        public void AddModule(Models.Module module)
        {
            if (module.Id == null)
                throw new ArgumentNullException(nameof(module));

            CompiledModule? compiledModule = CompileModule(module);

            if (compiledModule != null)
            {
                lock (EventModules)
                {
                    if (EventModules.TryGetValue(compiledModule.SubscriptionEvent, out List<CompiledModule>? eventModules))
                    {
                        if (eventModules == null)
                            eventModules = new();

                        eventModules.Add(compiledModule);
                        EventModules[compiledModule.SubscriptionEvent] = eventModules.OrderBy(compiledModule => compiledModule.ExecutionOrder).ToList();
                    }
                    else
                    {
                        EventModules.Add(compiledModule.SubscriptionEvent, new List<CompiledModule> { compiledModule });
                    }
                }
            }
        }

        public void UpdateModule(Models.Module module)
        {
            if (module.Id == null)
                throw new ArgumentNullException(nameof(module));

            lock (EventModules)
            {
                CompiledModule? removingModule = EventModules.Values.SelectMany(compiledModule => compiledModule).FirstOrDefault(compiledModule => compiledModule.Id == module.Id);
                
                if (removingModule != null)
                    RemoveModule((int)module.Id!, removingModule.SubscriptionEvent);

                AddModule(module);
            }
        }

        public void RemoveModule(int id, string eventName)
        {
            lock (EventModules)
            {
                if (EventModules.TryGetValue(eventName, out List<CompiledModule>? eventModules) && eventModules != null)
                {
                    CompiledModule? compiledModule = eventModules.FirstOrDefault(compiledModule => compiledModule.Id == id);

                    if (compiledModule != null && eventModules.Remove(compiledModule))
                    {
                        EventModules[eventName] = eventModules;
                    }
                }
            }
        }

        public async Task ExecuteModules(string eventName, object eventArgs)
        {
            if (EventModules.TryGetValue(eventName, out List<CompiledModule>? compiledModules) &&
                compiledModules != null && compiledModules.Any())
            {
                foreach (CompiledModule compiledModule in compiledModules)
                {
                    if (!await ExecuteModule(eventName, compiledModule, eventArgs))
                        break;
                }
            }
        }

        public Task ExecuteModules(string eventName, string stringArguments)
        {
            if (!EventArgumentTypes.TryGetValue(eventName, out Type? argumentType) || argumentType == null)
                throw new ArgumentException("The event name supplied could not be found in the list of supported events! Please verify it and try again.");

            object? eventArgs;
            Newtonsoft.Json.Serialization.ErrorEventArgs? errorEventArgs = null;
            if (string.IsNullOrWhiteSpace(stringArguments))
                eventArgs = new EventArgs();
            else
                eventArgs = JsonConvert.DeserializeObject(stringArguments, argumentType, new JsonSerializerSettings { Error = (_, eventArgs) => { errorEventArgs = eventArgs; } });

            if (eventArgs == null)
                throw new ArgumentException($"The JSON data could not be parsed as \"{nameof(argumentType)}\". Please make sure the JSON data is accurate and try again: {errorEventArgs?.ErrorContext.Error.Message ?? "N/A"}");

            _ = Task.Run(async () => await ExecuteModules(eventName, eventArgs));

            return Task.CompletedTask;
        }

        public async Task ExecuteModule(int moduleId, string stringArguments)
        {
            Models.Module? module = (await _dbContextFactory.CreateDbContextAsync()).Modules.FirstOrDefault(eventTest => eventTest.Id == moduleId);

            if (module == null)
                throw new ArgumentException($"Could not find module with ID {moduleId}");

            if (module.Event == null || !EventArgumentTypes.TryGetValue(module.Event, out Type? argumentType) || argumentType == null)
                throw new ArgumentException("The event name supplied could not be found in the list of supported events! Please verify it and try again.");

            CompiledModule? compiledModule = EventModules.Values.SelectMany(list => list).FirstOrDefault(compiledModule => compiledModule.Id == module.Id);
            if (compiledModule == null)
                throw new ArgumentException($"The supplied module with ID {moduleId} is not a validly compiled one. Please check module for compilation status.");

            object? eventArgs;
            Newtonsoft.Json.Serialization.ErrorEventArgs? errorEventArgs = null;
            if (string.IsNullOrWhiteSpace(stringArguments))
                eventArgs = new EventArgs();
            else
                eventArgs = JsonConvert.DeserializeObject(stringArguments, argumentType, new JsonSerializerSettings { Error = (_, eventArgs) => { errorEventArgs = eventArgs; } });

            if (eventArgs == null)
                throw new ArgumentException($"The JSON data could not be parsed as \"{nameof(argumentType)}\". Please make sure the JSON data is accurate and try again: {errorEventArgs?.ErrorContext.Error.Message ?? "N/A"}");

            _ = Task.Run(async () => await ExecuteModule(module.Event, compiledModule, eventArgs));            
        }


        #endregion

        #region Private Helpers

        private CompiledModule? CompileModule(Models.Module module)
        {
            CompiledModule? compiledModule;
            IEnumerable<string>? warnings;
            IEnumerable<string>? errors;
            (compiledModule, warnings, errors) = CompileAndAnalyzeModule(module);

            if (compiledModule == null)
            {
                _messagingService.AddMessage($"Compilation of \"{module.Name}\" failed. " +
                    $"{(warnings.Any() ? $"Warnings: {string.Join(Environment.NewLine, warnings)}; " : string.Empty)}" +
                    $"{(errors.Any() ? $"Errors: {string.Join(Environment.NewLine, errors)}; " : string.Empty)}",
                    MessageCategory.Module, LogLevel.Error);
            }

            return compiledModule;
        }

        private void CompileCode(string moduleCode, out CSharpCompilation compilation, out SemanticModel semanticModel, out IEnumerable<MethodDeclarationSyntax> methodDeclarationSyntaxes)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(moduleCode);

            string assemblyName = Path.GetRandomFileName();
            List<MetadataReference> references = GetReferences();

            compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,                    
                    metadataReferenceResolver: new MissingResolver(),
                nullableContextOptions: NullableContextOptions.Enable));

            methodDeclarationSyntaxes = compilation.SyntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToList());

            semanticModel = compilation.GetSemanticModel(syntaxTree, true);
        }

        private List<MetadataReference> GetReferences(bool addUtilities = true)
        {
            List<MetadataReference> references = new();

            references.AddRange(Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .Select((item) => MetadataReference.CreateFromFile(Assembly.Load(item).Location)));

            references.AddRange(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                    .Select(path => MetadataReference.CreateFromFile(path)));

            if (_dbContextFactory.CreateDbContext().Settings.GetSetting("ExternalResourcesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                references.AddRange(directoryInfo!.GetFiles("*.dll", SearchOption.AllDirectories).Select(referenceFile => MetadataReference.CreateFromFile(referenceFile.FullName)));

            if (addUtilities && UtilitiesAssembly != null)
                references.Add(MetadataReference.CreateFromImage(UtilitiesAssembly));

            return references;
        }

        private async Task<bool> ExecuteModule(string subscriptionEvent, CompiledModule compiledModule, object eventArgs)
        {
            if (!compiledModule.IsEnabled)
                return true;

            await _messagingService.AddMessage($"Executing {subscriptionEvent} module {compiledModule.Name}", MessageCategory.Event, LogLevel.Debug);

            try
            {
                List<object> arguments = new();
                foreach (Type parameterType in compiledModule.ParameterTypes)
                {
                    if (parameterType.Equals(eventArgs.GetType()))
                        arguments.Add(eventArgs);
                    else
                    {
                        if (EventObjects.TryGetValue(parameterType, out object? obj) && obj != null)
                            arguments.Add(obj);
                    }
                }

                if (!await compiledModule.ModuleFunction(arguments.ToArray()))
                    return false;
            }
            catch (Exception exception)
            {
                await _messagingService.AddMessage($"{subscriptionEvent} module {compiledModule.Name} exception: {exception.InnerException?.Message ?? exception.Message} in {exception.InnerException?.StackTrace ?? exception.StackTrace }", MessageCategory.Event, LogLevel.Error);
            }

            if (compiledModule.StopEventExecution)
                return false;

            return true;
        }

        #endregion
    }

    public class MissingResolver : MetadataReferenceResolver
    {
        public override bool Equals(object? other)
        {
            throw new NotImplementedException();
        }

        public override int GetHashCode()
        {
            throw new NotImplementedException();
        }

        public override bool ResolveMissingAssemblies => false;

        public override ImmutableArray<PortableExecutableReference> ResolveReference(string reference, string? baseFilePath, MetadataReferenceProperties properties)
        {
            throw new NotImplementedException();
        }
    }

    public class CollectibleAssemblyLoadContext : AssemblyLoadContext
    {
        public CollectibleAssemblyLoadContext() : base(isCollectible: true)
        { }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            return null;
        }

        public void Dispose()
        {
            Unload();
        }
    }

}
