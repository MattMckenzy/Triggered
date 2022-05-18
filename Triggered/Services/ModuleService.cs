using Eltons.ReflectionKit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Reflection;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that contains methods to analyze, compile and execute dynamic C# <see cref="Module"/> code. IT also contains delegates to all event handlers, as well as dictionaries of supported events and arguments.
    /// </summary>
    public class ModuleService
    {
        #region Private Variables

        private IDbContextFactory<TriggeredDbContext> DbContextFactory { get; }
        private MessagingService MessagingService { get; }

        private ConcurrentDictionary<string, List<CompiledModule>> EventModules { get; } = new();

        private readonly string UtilitiesAssemblyName = Path.GetRandomFileName();

        private byte[]? UtilitiesAssembly { get; set; } = null;

        private readonly Dictionary<string, Action<object, object>> EventHandlers = new();

        #endregion

        #region Public Properties

        /// <summary>
        /// Holds the <see cref="Type"/> and instances of services and <see cref="object"/>s that can be injected into a compiled entry method during <see cref="Module"/> execution.
        /// </summary>
        public Dictionary<Type, object> EventObjects { get; } = new();

        /// <summary>
        /// Defines the list of Triggered's supported events (i.e. "ModuleService.OnCustomEvent" and "Custom").
        /// </summary>
        public Dictionary<string, string> SupportedEvents { get; } = new();

        /// <summary>
        /// Defines all event names and their event arguments object <see cref="Type"/> (i.e. "ModuleService.OnCustomEvent" and <see cref="CustomEventArgs"/>).
        /// </summary>
        public Dictionary<string, Type> EventArgumentTypes { get; } = new();

        /// <summary>
        /// Defines all supported event arguments object names and types <see cref="Type"/> (i.e. "CustomEventArgs", <see cref="CustomEventArgs"/>).
        /// </summary>
        public Dictionary<string, Type> SupportedArgumentTypes { get; } = new();

        /// <summary>
        /// Defines all .NET objects used in relation to their parent. Used for autocompletion with the C# code editor.
        /// </summary>
        public Dictionary<string, IEnumerable<(string name, string value, string kind)>> NetObjects { get; } = new();

        /// <summary>
        /// Defines all currently executing <see cref="Module"/>s, used to keep track of and cancel <see cref="Module"/> execution on the home page.
        /// </summary>
        public Dictionary<CancellationTokenSource, (CompiledModule executingModule, DateTime startTime)> ExecutingModules { get; } = new();
        
        /// <summary>
        /// Event that is invoked whenever <see cref="ExecutingModules"/> is changed.
        /// </summary>
        public event EventHandler? ExecutingModulesStateChanged;

        /// <summary>
        /// Custom event that can be invoked and consumed in <see cref="Module"/>s.
        /// </summary>
#pragma warning disable 67 // OnCustomEvent might be used in dynamic modules.
        public event EventHandler<CustomEventArgs>? OnCustomEvent;
#pragma warning restore 67 // OnCustomEvent might be used in dynamic modules.

        #endregion

        #region Constructor

        /// <summary>
        /// Default constructor with injected services.
        /// </summary>
        /// <param name="dbContextFactory">Injected <see cref="IDbContextFactory{TContext}"/> of <see cref="TriggeredDbContext"/>.</param>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        /// <param name="dataService">Injected <see cref="DataService"/>.</param>
        /// <param name="queueService">Injected <see cref="QueueService"/>.</param>
        /// <param name="memoryCache">Injected <see cref="MemoryCache"/>.</param>
        /// <param name="encryptionService">Injected <see cref="EncryptionService"/>.</param>
        public ModuleService(IDbContextFactory<TriggeredDbContext> dbContextFactory, MessagingService messagingService, DataService dataService, QueueService queueService, MemoryCache memoryCache, EncryptionService encryptionService)
        {
            DbContextFactory = dbContextFactory;
            MessagingService = messagingService;

            IntializeNetObjects();

            RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(DataService), typeof(DataService), dataService),
                (nameof(QueueService), typeof(QueueService), queueService),
                (nameof(MemoryCache), typeof(MemoryCache), memoryCache),
                (nameof(EncryptionService), typeof(EncryptionService), encryptionService),
                (nameof(ModuleService), typeof(ModuleService), this),
                (nameof(Services.MessagingService), typeof(MessagingService), MessagingService),
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

            OnCustomEvent?.GetInvocationList();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Adds built-in and custom events and arguments to the <see cref="ModuleService"/>.
        /// </summary>
        /// <remarks>This should be for internal use only.</remarks>
        public async Task AddUtilities()
        {
            SupportedEvents.Add("ModuleService.OnCustomEvent", "Custom");
            EventArgumentTypes.Add("ModuleService.OnCustomEvent", typeof(CustomEventArgs));
            SupportedArgumentTypes.Add("CustomEventArgs", typeof(CustomEventArgs));
            await RegisterEvents(this);

            SupportedArgumentTypes.Add("LongRunningTask", typeof(LongRunningTask));

            await AnalyzeAndCompileUtilities();
        }

        /// <summary>
        /// Registers all given parameter objects, with their name <see cref="string"/>, their <see cref="Type"/> and their <see cref="object"/> instance, making them supported and ready for injection into executed <see cref="Module"/>s.
        /// </summary>
        /// <param name="parameterObjects">The list of parameter objects to register.</param>
        public Task RegisterParameterObjects(IEnumerable<(string name, Type type, object instance)> parameterObjects)
        {
            foreach ((string name, Type type, object instance) in parameterObjects)
            {
                EventObjects.Add(type, instance);
                SupportedArgumentTypes.Add(name, type);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Initializes the given <see cref="object"/> instance for <see cref="Module"/> exection by adding all of its direct children events and their event arguments to the <see cref="ModuleService"/>'s dictionaries.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the given <see cref="object"/> instance.</typeparam>
        /// <param name="classInstance">The <see cref="object"/> instance from which to add support.</param>
        public Task InitializeSupportedEventsAndParameters<T>(T classInstance) where T : class
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

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates and registers event handlers for all of the <see cref="object"/> instance's direct children events.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the given <see cref="object"/> instance.</typeparam>
        /// <param name="classInstance">The <see cref="object"/> instance from which to create and register event handlers.</param>
        public async Task RegisterEvents<T>(T classInstance) where T : class
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

                    if (!EventHandlers.TryAdd(eventKey, eventHandler))
                        EventHandlers[eventKey] = eventHandler;

                    eventInfo.AddEventHandler(classInstance, Delegate.CreateDelegate(
                        eventType,
                        eventHandler.Target,
                        eventHandler.Method));

                    await CompileModules(eventKey);
                }
            }
        }

        /// <summary>
        /// Removes event handlers for all of the <see cref="object"/> instance's direct children events.
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/> of the given <see cref="object"/> instance.</typeparam>
        /// <param name="classInstance">The <see cref="object"/> instance from which to remove event handlers.</param>
        public Task DeregisterEvents<T>(T classInstance) where T : class
        {
            foreach (EventInfo eventInfo in classInstance.GetType().GetEvents())
            {
                string eventKey = $"{typeof(T).Name}.{eventInfo!.Name}";
                Type? eventType = eventInfo.EventHandlerType;

                if (eventType != null)
                {
                    if (EventHandlers.TryGetValue(eventKey, out Action<object, object>? eventHandler) && eventHandler != null)
                    {
                        EventHandlers.Remove(eventKey);

                        if (eventHandler != null)
                        {
                            eventInfo.RemoveEventHandler(classInstance, Delegate.CreateDelegate(
                                eventType,
                                eventHandler.Target,
                                eventHandler.Method));
                        }
                    }

                    EventModules.AddOrUpdate(eventKey, (_) => new(), (_, __) => new());
                }
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Analyzes the given code and retrieves all unique event names (i.e. "ModuleService.OnCustomEvent") for the first found supported event argument used as a method parameter.
        /// </summary>
        /// <param name="code">The code to analyze and from which to retrieve the event names.</param>
        /// <returns>The unique event names.</returns>
        public IEnumerable<string> GetCodeEvents(string code)
        {
            List<string> matchedEvents = new();

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
                        {
                            matchedEvents.AddRange(
                                EventArgumentTypes
                                    .Where(keyValuePair => keyValuePair.Value.Equals(parameterType))
                                    .Select(keyValuePair => keyValuePair.Key));

                            if (matchedEvents.Any())
                                return matchedEvents;
                        }
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

            return matchedEvents;
        }

        /// <summary>
        /// Compiles all <see cref="Module"/>s found under all supported events.
        /// </summary>
        public async Task CompileAllModules()
        {
            foreach (string eventName in SupportedEvents.Keys)
                await CompileModules(eventName);            
        }

        /// <summary>
        /// Compiles a <see cref="Module"/>, returning any errors and warnings that the compilation emitted, as well as the resulting <see cref="CompiledModule"/>.
        /// </summary>
        /// <param name="module">The <see cref="Module"/> to analyze and compile.</param>
        /// <returns>The resulting <see cref="CompiledModule"/>, as well as a list of warnings and a list of errors that the compilation emitted.</returns>
        public (CompiledModule? CompiledModule, IEnumerable<string> Warnings, IEnumerable<string> Errors) AnalyzeAndCompileModule(Models.Module module)
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


        /// <summary>
        /// Compiles all or a single <see cref="Utility"/>, returning any errors and warnings that the compilation emitted.
        /// </summary>
        /// <param name="code">Optionally, a single <see cref="Utility"/> code to compile. If this is null, all <see cref="Utility"/> entities from the database will be compiled.</param>
        /// <returns>A list of warnings and a list of errors that the compilation emitted.</returns>
        public async Task<(List<string>, List<string>)> AnalyzeAndCompileUtilities(string? code = null)
        {
            List<string> compilationWarnings = new();
            List<string> compilationErrors = new();
            List<SyntaxTree> syntaxTrees = new();

            if (code != null)
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(code));
            else
                foreach (Utility utility in (await DbContextFactory.CreateDbContextAsync()).Utilities)
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
               
        /// <summary>
        /// Compiles and adds a <see cref="Module"/> to the <see cref="ModuleService"/>, making it ready to be executed and awaiting event triggers.
        /// </summary>
        /// <param name="module">The <see cref="Module"/> to compile and make ready.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="Models.Module.Id"/> is null. The <see cref="Module"/> has not been saved to the database and is not ready for compilation.</exception>
        public Task AddModule(Models.Module module)
        {
            if (module.Id == null)
                throw new ArgumentNullException(nameof(module));

            CompiledModule? compiledModule = CompileModule(module);

            if (compiledModule != null)
            {
                EventModules.AddOrUpdate(compiledModule.Module.Event, 
                    (_) => new() { compiledModule }, 
                    (_, compiledModules) =>
                    {
                        compiledModules.Add(compiledModule);
                        return compiledModules;
                    });
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Compiles and updates a <see cref="Module"/> in the <see cref="ModuleService"/>, updating it and making the changes ready to be executed and awaiting event triggers.
        /// </summary>
        /// <param name="module">The <see cref="Module"/> to update, compile and make ready.</param>
        /// <exception cref="ArgumentNullException">Thrown if the <see cref="Models.Module.Id"/> is null. The <see cref="Module"/> has not been saved to the database and is not ready for compilation.</exception>
        public async Task UpdateModule(Models.Module module)
        {
            if (module.Id == null)
                throw new ArgumentNullException(nameof(module));

            CompiledModule? removingModule = EventModules.Values.SelectMany(compiledModule => compiledModule).FirstOrDefault(compiledModule => compiledModule.Module.Id == module.Id);
                
            if (removingModule != null)
                await RemoveModule((int)module.Id!, removingModule.Module.Event);

            await AddModule(module);
        }

        /// <summary>
        /// Removes a <see cref="Module"/> from the <see cref="ModuleService"/>, making it unavailable for execution.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="Module"/>.</param>
        /// <param name="eventName">The unique name of the event (i.e. "ModuleService.OnCustomEvent"),\ in which the <see cref="Module"/> belongs.</param>
        /// <returns></returns>
        public Task RemoveModule(int id, string eventName)
        {
            if (EventModules.TryGetValue(eventName, out List<CompiledModule>? eventModules) && eventModules != null)
            {
                CompiledModule? compiledModule = eventModules.FirstOrDefault(compiledModule => compiledModule.Module.Id == id);

                if (compiledModule != null)
                    eventModules.Remove(compiledModule);                
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Executes all modules for a given event with the given event arguments <see cref="object"/>.
        /// </summary>
        /// <param name="eventName">The unique name of the event (i.e. "ModuleService.OnCustomEvent").</param>
        /// <param name="eventArgs">The event arguments <see cref="object"/> that will be injected into executed <see cref="Module"/>s.</param>
        public async Task ExecuteModules(string eventName, object eventArgs)
        {
            if (EventModules.TryGetValue(eventName, out List<CompiledModule>? compiledModules) &&
            compiledModules != null && compiledModules.Any())
            {
                foreach (CompiledModule compiledModule in compiledModules
                    .OrderBy(compiledModule => compiledModule.Module.ExecutionOrder)
                    .ThenBy(compiledModules => compiledModules.Module.Name)
                    .ToList())
                {
                    if (!await ExecuteModule(eventName, compiledModule, eventArgs))
                        break;
                }
            }
        }

        /// <summary>
        /// Executes all modules for a given event with the given event arguments <see cref="string"/>.
        /// </summary>
        /// <param name="eventName">The unique name of the event (i.e. "ModuleService.OnCustomEvent").</param>
        /// <param name="stringArguments">The event arguments JSON <see cref="string"/> that will be injected into executed <see cref="Module"/>s.</param>
        /// <exception cref="ArgumentException">Thrown if the unique event name did not correspond to a supported event, or if the JSON string could not be parsed into the appropriate event <see cref="object"/> <see cref="Type"/>.</exception>
        public Task ExecuteModulesAsync(string eventName, string stringArguments)
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

        /// <summary>
        /// Executes the <see cref="Module"/> found with the given unique identifier, with the given JSON event arguments <see cref="string"/>.
        /// </summary>
        /// <param name="moduleId">The unique identifier of the <see cref="Module"/> to execute.</param>
        /// <param name="stringArguments">The event arguments JSON <see cref="string"/> that will be injected into the executed <see cref="Module"/>.</param>
        /// <exception cref="ArgumentException">Thrown if the <see cref="Module"/> with the given identifier was not found or is not valid, the unique event name did not correspond to a supported event, or if the JSON string could not be parsed into the appropriate event <see cref="object"/> <see cref="Type"/>.</exception>
        public async Task ExecuteModuleAsync(int moduleId, string stringArguments)
        {
            Models.Module? module = (await DbContextFactory.CreateDbContextAsync()).Modules.FirstOrDefault(eventTest => eventTest.Id == moduleId);

            if (module == null)
                throw new ArgumentException($"Could not find module with ID {moduleId}");

            if (module.Event == null || !EventArgumentTypes.TryGetValue(module.Event, out Type? argumentType) || argumentType == null)
                throw new ArgumentException("The event name supplied could not be found in the list of supported events! Please verify it and try again.");

            object? eventArgs;
            Newtonsoft.Json.Serialization.ErrorEventArgs? errorEventArgs = null;
            if (string.IsNullOrWhiteSpace(stringArguments))
                eventArgs = new EventArgs();
            else
                eventArgs = JsonConvert.DeserializeObject(stringArguments, argumentType, new JsonSerializerSettings { Error = (_, eventArgs) => { errorEventArgs = eventArgs; } });

            if (eventArgs == null)
                throw new ArgumentException($"The JSON data could not be parsed as \"{nameof(argumentType)}\". Please make sure the JSON data is accurate and try again: {errorEventArgs?.ErrorContext.Error.Message ?? "N/A"}");

            CompiledModule? compiledModule = EventModules.Values.SelectMany(list => list).FirstOrDefault(compiledModule => compiledModule.Module.Id == module.Id);

            if (compiledModule == null)
                throw new ArgumentException($"The supplied module with ID {moduleId} is not a validly compiled one. Please check module for compilation status.");

            _ = Task.Run(async () => await ExecuteModule(module.Event, compiledModule, eventArgs));            
        }


        #endregion

        #region Private Helpers

        private void IntializeNetObjects()
        {
            // TODO: Make autocomplete smarter.

            List<Assembly> assemblies = new();
            assemblies.AddRange(Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .Select(item => Assembly.Load(item)));

            assemblies.AddRange(AppDomain.CurrentDomain.GetAssemblies());

            if (DbContextFactory.CreateDbContext().Settings.GetSetting("ExternalResourcesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
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

        private async Task CompileModules(string eventName)
        {
            using TriggeredDbContext triggeredDbContext = DbContextFactory.CreateDbContext();

            List<CompiledModule> returnModules = new();

            foreach (Models.Module module in triggeredDbContext.Modules
                .Where(module => module.Event == eventName)
                .OrderBy(module => module.ExecutionOrder))
            {
                CompiledModule? compiledModule = CompileModule(module);
                if (compiledModule != null)
                {
                    returnModules.Add(compiledModule);
                    await MessagingService.AddMessage($"Module \"{module.Name}\" compiled succesfully!", MessageCategory.Module, LogLevel.Debug);
                }
            }

            EventModules.AddOrUpdate(eventName, (_) => returnModules, (_, __) => returnModules);
        }

        private CompiledModule? CompileModule(Models.Module module)
        {
            CompiledModule? compiledModule;
            IEnumerable<string>? warnings;
            IEnumerable<string>? errors;
            (compiledModule, warnings, errors) = AnalyzeAndCompileModule(module);

            if (compiledModule == null)
            {
                MessagingService.AddMessage($"Compilation of \"{module.Name}\" failed. " +
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

            if (DbContextFactory.CreateDbContext().Settings.GetSetting("ExternalResourcesPath").TryCreateDirectory(out DirectoryInfo? directoryInfo))
                references.AddRange(directoryInfo!.GetFiles("*.dll", SearchOption.AllDirectories).Select(referenceFile => MetadataReference.CreateFromFile(referenceFile.FullName)));

            if (addUtilities && UtilitiesAssembly != null)
                references.Add(MetadataReference.CreateFromImage(UtilitiesAssembly));

            return references;
        }

        private async Task<bool> ExecuteModule(string subscriptionEvent, CompiledModule compiledModule, object eventArgs)
        {
            if (!compiledModule.Module.IsEnabled)
                return true;

            await MessagingService.AddMessage($"Executing {subscriptionEvent} module {compiledModule.Module.Name}", MessageCategory.Event, LogLevel.Debug);

            LongRunningTask moduleLongRunningTask = new();

            ExecutingModules.Add(moduleLongRunningTask.CancellationTokenSource, (compiledModule, DateTime.Now));
            ExecutingModulesStateChanged?.Invoke(this, new());

            try
            {
                List<object> arguments = new();
                foreach (Type parameterType in compiledModule.ParameterTypes)
                {
                    if (parameterType.Equals(eventArgs.GetType()))
                        arguments.Add(eventArgs);
                    else if (EventObjects.TryGetValue(parameterType, out object? obj) && obj != null)
                        arguments.Add(obj);
                    else if (parameterType.Equals(typeof(LongRunningTask)))
                        arguments.Add(moduleLongRunningTask);               
                }

                if (!await compiledModule.ModuleFunction(arguments.ToArray()) ||
                    compiledModule.Module.StopEventExecution)
                    return false;
            }
            catch (Exception exception)
            {
                await MessagingService.AddMessage($"{subscriptionEvent} module {compiledModule.Module.Name} exception: {exception.InnerException?.Message ?? exception.Message} in {exception.InnerException?.StackTrace ?? exception.StackTrace }", MessageCategory.Event, LogLevel.Error);
            }
            finally
            {
                if (moduleLongRunningTask.Task == null)
                    moduleLongRunningTask.Task = Task.CompletedTask;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await moduleLongRunningTask.Task!.ConfigureAwait(false);
                    }
                    catch (TaskCanceledException)
                    {
                        await MessagingService.AddMessage($"{subscriptionEvent} module {compiledModule.Module.Name} was canceled.", MessageCategory.Event, LogLevel.Debug);
                    }
                    finally
                    {
                        ExecutingModules.Remove(moduleLongRunningTask.CancellationTokenSource);
                        ExecutingModulesStateChanged?.Invoke(this, new());
                    }
                });
            }

            return true;
        }

        #endregion
    }
}
