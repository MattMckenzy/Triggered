using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    public class MessagingService
    {        
        private int _currentMessageId = 0;
        private readonly ConcurrentDictionary<int, Message> _messages = new();
        private readonly IDbContextFactory<TriggeredDbContext> _dBContextFactory;

        public MessagingService(IDbContextFactory<TriggeredDbContext> dBContextFactory)
        {
            _dBContextFactory = dBContextFactory;
        }

        public Task AddMessage(string text, MessageCategory messageCategory = MessageCategory.Undefined, LogLevel logLevel = LogLevel.Information)
        {
            int limit = int.TryParse(_dBContextFactory.CreateDbContext().Settings.GetSetting("MessagesLimit"), out int messageLimit) ? messageLimit : 1000;
            
            Message message;
            lock (_messages)
            {
                while (_messages.Count > limit)
                    _messages.TryRemove(_messages.MinBy(keyValuePair =>  keyValuePair.Key).Key, out _);
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
