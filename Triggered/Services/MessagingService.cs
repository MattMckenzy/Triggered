using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using Triggered.Extensions;
using Triggered.Models;

namespace Triggered.Services
{
    /// <summary>
    /// A singleton service that handles posting and reading of runtime <see cref="Message"/>s, used on the home page to keep track of services, modules, issues and more.
    /// </summary>
    public class MessagingService
    {        
        private int _currentMessageId = 0;
        private readonly ConcurrentDictionary<int, Message> _messages = new();
        private readonly IDbContextFactory<TriggeredDbContext> _dBContextFactory;

        public MessagingService(IDbContextFactory<TriggeredDbContext> dBContextFactory)
        {
            _dBContextFactory = dBContextFactory;
        }

        /// <summary>
        /// Creates and adds a <see cref="Message"/> to the service, with the given text and optional filter parameters. If the <see cref="Message"/>s limit, configured with the <see cref="Setting"/> "MessagesLimit", is hit, the oldest <see cref="Message"/> will be removed.
        /// </summary>
        /// <param name="text">The text of the <see cref="Message"/>.</param>
        /// <param name="messageCategory">The <see cref="MessageCategory"/> of the <see cref="Message"/>, with a default value of <see cref="MessageCategory.Undefined"/>. Only used as a description and for filtering.</param>
        /// <param name="logLevel">The <see cref="LogLevel"/> of the <see cref="Message"/>, with a default value of <see cref="LogLevel.Information"/>. used as a description and for filtering.</param>
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

        /// <summary>
        /// Retrieves the <see cref="Message"/> with the given <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="Message"/>.</param>
        /// <returns>The retrieved <see cref="Message"/>.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a <see cref="Message"/> with the given <paramref name="id"/> was not found.</exception>
        public Task<Message> GetMessage(int id)
        {
            _messages.TryGetValue(id, out Message? message);

            if (message == null)
                throw new KeyNotFoundException();

            return Task.FromResult(message);
        }

        /// <summary>
        /// Returns all <see cref="Message"/>s currently stored in the service.
        /// </summary>
        /// <returns>All <see cref="Message"/>s in the service, an <see cref="IEnumerable{T}"/> of <see cref="Message"/>.</returns>
        public Task<IEnumerable<Message>> GetMessages()
        {
            lock (_messages)
            {
                return Task.FromResult(_messages.Select(pair => pair.Value));
            }
        }

        /// <summary>
        /// Deletes the <see cref="Message"/> with the given Id. If none found, nothing happens.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="Message"/>.</param>
        public Task DeleteMessage(int id)
        {
            lock (_messages)
            {
                _messages.TryRemove(id, out Message _);
            }

            MessagesChanged?.Invoke(this, new());
            return Task.CompletedTask;
        }

        /// <summary>
        /// Deletes all <see cref="Message"/>s in the service.
        /// </summary>
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
