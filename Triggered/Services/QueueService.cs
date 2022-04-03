using System.Collections.Concurrent;

namespace Triggered.Services
{
    public class QueueService
    {
        private readonly ConcurrentDictionary<string, (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<Func<Task<bool>>>)> Queues = new();

        public async Task Add(string queueKey, Func<Task<bool>> func)
        {
            ConcurrentQueue<Func<Task<bool>>> newQueue = new();
            newQueue.Enqueue(func);

            SemaphoreSlim newSemaphore = new(1);

            if (!Queues.TryAdd(queueKey, (new(), new SemaphoreSlim(1), newQueue)) && Queues.TryGetValue(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<Func<Task<bool>>>) queue))
            {
                await newSemaphore.WaitAsync();

                queue.Item3.Enqueue(func);

                newSemaphore.Release();
            }   

            await RunQueue(queueKey);
        }

        private Task RunQueue(string queueKey)
        {
            if (Queues.TryGetValue(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<Func<Task<bool>>>) queue))
            {
                _ = Task.Run(async () => 
                {
                    await queue.Item2.WaitAsync();

                    try
                    {
                        while (queue.Item3.TryDequeue(out Func<Task<bool>>? func))
                        {
                             if (func != null && !queue.Item1.IsCancellationRequested && !await func())
                                break;
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
            if (Queues.TryRemove(queueKey, out (CancellationTokenSource, SemaphoreSlim, ConcurrentQueue<Func<Task<bool>>>) queue))
            {
                queue.Item1.Cancel();
            }

            return Task.CompletedTask;
        }
    }
}
