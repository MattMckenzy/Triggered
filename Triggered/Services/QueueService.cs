using System.Collections.Concurrent;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that provides means to queue tasks and delegate functions under then-keyed queues, either instantly created or retrieved and used.
    /// </summary>
    public class QueueService
    {
        private MessagingService MessagingService { get; }

        private readonly ConcurrentDictionary<string, (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>)> Queues = new();

        /// <summary>
        /// A list of all active queues and their current queued function counts.
        /// </summary>
        public Dictionary<string, int> QueueCounts { get { return Queues.Keys.ToDictionary(key => key, key => Queues[key].Item3.Count); } }
        
        /// <summary>
        /// An event that is invoked whenever queue counts are changed. 
        /// </summary>
        public event EventHandler? QueueCountsUpdated;

        /// <summary>
        /// Default constructor with injected <see cref="Services.MessagingService"/>.
        /// </summary>
        /// <param name="messagingService">Injected <see cref="Services.MessagingService"/>.</param>
        public QueueService(MessagingService messagingService)
        {
            MessagingService = messagingService;
        }

        /// <summary>
        /// Adds a delegate function to a new or existing queue denoted by the given queue key.
        /// </summary>
        /// <param name="queueKey">The unique key describing under which queue the delegate function should be added.</param>
        /// <param name="func">The delegate function to queue. It cannot accept arguments and must return Task<bool>. The boolean return will decide whether the queue continues to execute functions (true) or is cancelled and all queue items are cleared (false).</param>
        /// <param name="exceptionPreamble">String that begins any exception messages sent to the messaging service when a queued function encounters an unhandled exception.</param>
        public async Task Add(string queueKey, Func<Task<bool>> func, string? exceptionPreamble = null)
        {
            ConcurrentQueue<(Func<Task<bool>>, string?)> newQueue = new();
            newQueue.Enqueue((func, exceptionPreamble));

            SemaphoreSlim newSemaphore = new(1);

            if (!Queues.TryAdd(queueKey, (new(), new SemaphoreSlim(1), newQueue)) && Queues.TryGetValue(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>) queue))
            {
                await newSemaphore.WaitAsync();
                queue.Item3.Enqueue((func, exceptionPreamble));
                newSemaphore.Release();
            }

            QueueCountsUpdated?.Invoke(this, EventArgs.Empty);
            await RunQueue(queueKey);
        }
             
        /// <summary>
        /// Clears the queue found under the given queue key. If no queue was found, returns without exception.
        /// </summary>
        /// <param name="queueKey">The unique key describing which queue should be cleared.</param>
        public Task Clear(string queueKey)
        {
            if (Queues.TryRemove(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>) queue))
            {
                queue.Item1.Cancel();
            }

            QueueCountsUpdated?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        private Task RunQueue(string queueKey)
        {
            if (Queues.TryGetValue(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>) queue))
            {
                _ = Task.Run(async () =>
                {
                    await queue.Item2.WaitAsync();

                    try
                    {
                        while (queue.Item3.TryDequeue(out (Func<Task<bool>>, string?) func))
                        {
                            try
                            {
                                if (!queue.Item1.IsCancellationRequested && !await func.Item1())
                                    break;

                                QueueCountsUpdated?.Invoke(this, EventArgs.Empty);
                            }
                            catch (Exception ex)
                            {
                                string exceptionPreamble = func.Item2 ?? $"Exception in queue \"{queueKey}\"";
                                await MessagingService.AddMessage($"{exceptionPreamble}: {ex.Message}", MessageCategory.Module, LogLevel.Error);
                            }
                        }
                    }
                    finally
                    {
                        await Clear(queueKey);

                        queue.Item2.Release();
                    }

                },
                queue.Item1.Token);
            }

            return Task.CompletedTask;
        }
    }
}
