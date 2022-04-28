namespace Triggered.Models
{
    /// <summary>
    /// Defines a module that has been compiled and is ready to be executed.
    /// </summary>
    public class CompiledModule
    {
        /// <summary>
        /// Default constructer with mandatory properties.
        /// </summary>
        /// <param name="module">The <see cref="Module"/> that this <see cref="CompiledModule"/> was built from.</param>
        /// <param name="parameterTypes">A list of <see cref="Type"/>s that is used to build the object array of entry method arguments.</param>
        /// <param name="moduleFunction">The invokable compiled entry method.</param>
        public CompiledModule(Module module, IEnumerable<Type> parameterTypes, Func<object[], Task<bool>> moduleFunction)
        {
            Module = module;
            ParameterTypes = parameterTypes;
            ModuleFunction = moduleFunction;
        }

        /// <summary>
        /// The <see cref="Models.Module"/> that this <see cref="CompiledModule"/> was built from.
        /// </summary>
        public Module Module { get; set; }

        /// <summary>
        /// A list of <see cref="Type"/>s that is used to build the object array of entry method arguments.
        /// </summary>
        public IEnumerable<Type> ParameterTypes { get; set; }

        /// <summary>
        /// The invokable compiled entry method.
        /// </summary>
        public Func<object[], Task<bool>> ModuleFunction { get; set; }

    }
}
