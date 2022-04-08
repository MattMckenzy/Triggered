using System.Collections.Concurrent;
using Triggered.Models;

namespace Triggered.Services
{
    public class QueueService
    {
        private readonly ConcurrentDictionary<string, (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>)> Queues = new();

        public MessagingService MessagingService { get; }

        public QueueService(MessagingService messagingService)
        {
            MessagingService = messagingService;
        }

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

            await RunQueue(queueKey);
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

        public Task Clear(string queueKey)
        {
            if (Queues.TryRemove(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<(Func<Task<bool>>, string?)>) queue))
            {
                queue.Item1.Cancel();
            }

            return Task.CompletedTask;
        }
    }
}
