namespace Triggered.Models
{
    /// <summary>
    /// A class
    /// </summary>
    public class LongRunningTask
    {
        /// <summary>
        /// The long running task. Used to enable cancellation from the home page. The module service will wait for this task before releasing it from the list of currently executing modules. 
        /// </summary>
        public Task? Task { get; set; } = null;

        /// <summary>
        /// The cancellation token. It can be called to cancel on the home page. Please ensure to handle module cancellation through this token.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; set; } = new();
    }
}
