using Eltons.ReflectionKit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using TownBulletin.Models;

namespace TownBulletin.Services
{
    public class ModuleService
    {
        #region Private Variables

        private readonly IDbContextFactory<TownBulletinDbContext> _dbContextFactory;
        private readonly MessagingService _messagingService;

        private Dictionary<string, List<CompiledModule>> EventModules { get; } = new();

        #endregion

        #region Public Properties

        public Dictionary<Type, object> EventObjects { get; } = new();

        public Dictionary<string, string> SupportedEvents { get; } = new();

        public Dictionary<string, Type> EventArgumentTypes { get; } = new();

        public Dictionary<string, Type> SupportedArgumentTypes { get; } = new();

        public Dictionary<string, IEnumerable<(string name, string value, string kind)>> NetObjects { get; } = new();

        #endregion

        #region Constructor

        public ModuleService(IDbContextFactory<TownBulletinDbContext> dbContextFactory, MessagingService messagingService, DataService dataService, QueueService queueService)
        {
            _dbContextFactory = dbContextFactory;
            _messagingService = messagingService;

            SupportedEvents.Add("TownBulletin.Custom", "Custom");
            EventArgumentTypes.Add("TownBulletin.Custom", typeof(CustomEventArgs));
            SupportedArgumentTypes.Add("CustomEventArgs", typeof(CustomEventArgs));

            IntializeNetObjects();

            RegisterParameterObjects(new (string, Type, object)[]
            {
                (nameof(DataService), typeof(DataService), dataService),
                (nameof(QueueService), typeof(QueueService), queueService),
                (nameof(ModuleService), typeof(ModuleService), this),
                (nameof(MessagingService), typeof(MessagingService), _messagingService),
                (nameof(IDbContextFactory<TownBulletinDbContext>), typeof(IDbContextFactory<TownBulletinDbContext>), dbContextFactory)
            });
        }

        public void IntializeNetObjects()   
        {
            // TODO: Make autocomplete smarter.
            IEnumerable<Assembly> assemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
            .Select(item => Assembly.Load(item))
            .Union(AppDomain.CurrentDomain.GetAssemblies());

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

        public void CompileModules(string subscriptionEvent)
        {
            using TownBulletinDbContext townBulletinDbContext = _dbContextFactory.CreateDbContext();

            List<CompiledModule> returnModules = new();

            foreach (Models.Module module in townBulletinDbContext.Modules
                .Where(module => module.Event == subscriptionEvent)
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
                EventModules[subscriptionEvent] = returnModules;
            }
        }

        public void ClearModules(string subscriptionEvent)
        {
            lock (EventModules)
            {
                EventModules[subscriptionEvent] = new();
            }
        }

        public (CompiledModule? CompiledModule, IEnumerable<string> Warnings, IEnumerable<string> Errors) CompileAndAnalyzeModule(Models.Module module)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(module.Code);

            string assemblyName = Path.GetRandomFileName();
            MetadataReference[] references = Assembly.GetExecutingAssembly().GetReferencedAssemblies()
                .Select((item) => MetadataReference.CreateFromFile(Assembly.Load(item).Location))
                .Union(((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                    .Select(path => MetadataReference.CreateFromFile(path))).ToArray();

            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                syntaxTrees: new[] { syntaxTree },
                references: references,
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            IEnumerable<MethodDeclarationSyntax> methods = compilation.SyntaxTrees
                .SelectMany(tree => tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().ToList());

            using var memoryStream = new MemoryStream();
            EmitResult result = compilation.Emit(memoryStream);

            CompiledModule? compiledModule = null;
            List<string> compilationWarnings = new();
            List<string> compilationErrors = new();

            IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                diagnostic.IsWarningAsError ||
                diagnostic.Severity == DiagnosticSeverity.Error);
            compilationErrors.AddRange(failures.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));

            IEnumerable<Diagnostic> warnings = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
            compilationWarnings.AddRange(warnings.Select(failure => $"{failure.Id}: {failure.GetMessage()}"));

            SemanticModel semanticModel = compilation.GetSemanticModel(syntaxTree, true);
            MethodDeclarationSyntax? methodDeclarationSyntax = methods.FirstOrDefault(method => method.Identifier.Text.Equals(module.EntryMethod));

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

            return (compiledModule, compilationWarnings, compilationErrors);
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

        public void RemoveModule(int id, string subscriptionEvent)
        {
            lock (EventModules)
            {
                if (EventModules.TryGetValue(subscriptionEvent, out List<CompiledModule>? eventModules) && eventModules != null)
                {
                    CompiledModule? compiledModule = eventModules.FirstOrDefault(compiledModule => compiledModule.Id == id);

                    if (compiledModule != null && eventModules.Remove(compiledModule))
                    {
                        EventModules[subscriptionEvent] = eventModules;
                    }
                }
            }
        }

        public async Task ExecuteModules(string subscriptionEvent, object eventArgs)
        {
            if (EventModules.TryGetValue(subscriptionEvent, out List<CompiledModule>? compiledModules) &&
                compiledModules != null)
            {
                foreach (CompiledModule compiledModule in compiledModules)
                {
                    if (!compiledModule.IsEnabled)
                        continue;

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
                            break;
                    }
                    catch (Exception exception)
                    {
                        await _messagingService.AddMessage($"{subscriptionEvent} module {compiledModule.Name} exception: {exception.InnerException?.Message ?? exception.Message} in {exception.InnerException?.StackTrace ?? exception.StackTrace }", MessageCategory.Event, LogLevel.Error);
                    }

                    if (compiledModule.StopEventExecution)
                        break;
                }
            }
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

        #endregion
    }
}
