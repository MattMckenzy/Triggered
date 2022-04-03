namespace Triggered.Models
{
    public class CompiledModule
    {
        public CompiledModule(Module module, IEnumerable<Type> parameterTypes, Func<object[], Task<bool>> moduleFunction)
        {
            Id = module.Id;
            Name = module.Name;
            SubscriptionEvent = module.Event;
            ExecutionOrder = module.ExecutionOrder;
            StopEventExecution = module.StopEventExecution;
            IsEnabled = module.IsEnabled;
            ParameterTypes = parameterTypes;
            ModuleFunction = moduleFunction;
        }

        public int? Id { get; set; }

        public string Name { get; set; }

        public string SubscriptionEvent { get; set; }
                            
        public int ExecutionOrder { get; set; }

        public bool StopEventExecution { get; set; }

        public bool IsEnabled { get; set; }

        public IEnumerable<Type> ParameterTypes { get; set; }

        public Func<object[], Task<bool>> ModuleFunction { get; set; }

    }
}
