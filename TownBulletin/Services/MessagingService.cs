using Microsoft.AspNetCore.Components;
using System.Collections.Concurrent;
using TownBulletin.Models;

namespace TownBulletin.Services
{
    public class MessagingService
    {        
        //TODO: make list bounded, with configurable size.
        private int _currentMessageId = 0;
        private readonly ConcurrentDictionary<int, Message> _messages = new();

        public Task AddMessage(string text, MessageCategory messageCategory = MessageCategory.Undefined, LogLevel logLevel = LogLevel.Information)
        {
            Message message;
            lock (_messages)
            {
                message = new(text, _currentMessageId++, messageCategory, logLevel);
                _messages.TryAdd(message.Id, message);
            }

            MessagesChanged?.Invoke(this, new() { Value = message });
            return Task.CompletedTask;
        }


        public Task<Message> GetMessage(int id)
        {
            _messages.TryGetValue(id, out Message? message);

            if (message == null)
                throw new KeyNotFoundException();

            return Task.FromResult(message);
        }


        public Task<IEnumerable<Message>> GetMessages()
        {
            lock (_messages)
            {
                return Task.FromResult(_messages.Select(pair => pair.Value));
            }
        }

        public Task DeleteMessage(int id)
        {
            lock (_messages)
            {
                _messages.TryRemove(id, out Message _);
            }

            MessagesChanged?.Invoke(this, new());
            return Task.CompletedTask;
        }

        internal Task DeleteMessages()
        {
            lock (_messages)
            {
                _messages.Clear();
            }

            MessagesChanged?.Invoke(this, new());
            return Task.CompletedTask;
        }

        public event EventHandler<ChangeEventArgs>? MessagesChanged;
    }
}
