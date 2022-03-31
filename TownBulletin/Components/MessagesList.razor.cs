using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using TownBulletin.Extensions;
using TownBulletin.Models;
using TownBulletin.Services;

namespace TownBulletin.Components
{
    public partial class MessagesList : ComponentBase, IDisposable
    {
        [Inject]
        public IJSRuntime JSRuntime { get; set; } = null!;

        [Inject]
        public TownBulletinDbContext TownBulletinDbContext { get; set; } = null!;

        [Inject]
        public MessagingService MessagingService { get; set; } = null!;

        private IList<Message> Messages { get; set; } = null!;

        private IEnumerable<LogLevel> FilteredLogLevels  = Array.Empty<LogLevel>();

        private bool IsMuted = true;
        private double Volume = 0.25;
        private bool IsRendered = false;
        private bool DisposedValue = false;

        //TODO: Add Sort, Search list.
        protected override async Task OnInitializedAsync()
        {
            IsMuted = TownBulletinDbContext.Settings.GetSetting("MessageNotificationsEnabled")
                .Equals("False", StringComparison.InvariantCultureIgnoreCase);

            Volume = double.TryParse(TownBulletinDbContext.Settings.GetSetting("MessageNotificationVolume"), out double volume) ?
                Math.Min(Math.Max(volume, 0), 1) : 0.25;

            FilteredLogLevels = TownBulletinDbContext.Settings.GetSetting("MessageLevels")
                .Split(new char[] { ':', ';', ',', ' ', '/', '\\' }, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(logLevelString => Enum.TryParse(logLevelString, out LogLevel result) ? result : LogLevel.None);

            await UpdateMessages();
            MessagingService.MessagesChanged += MessagingService_MessagesChanged;

            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);
            IsRendered = true;
        }

        private async void MessagingService_MessagesChanged(object? sender, ChangeEventArgs changeEventArgs)
        {
            await UpdateMessages();

            if (changeEventArgs.Value != null && 
                changeEventArgs.Value is Message message &&
                FilteredLogLevels.Contains(message.Severity))
            {
                if (IsRendered && !DisposedValue)
                    await JSRuntime.InvokeVoidAsync("audio.play", IsMuted ? 0 : Volume.ToString());
            }
        }

        private async Task UpdateMessages()
        {
            Messages = (await MessagingService.GetMessages())
                   .Where(message => FilteredLogLevels.Contains(message.Severity))
                   .OrderByDescending(message => message.TimeStamp)
                   .ToList();

            await InvokeAsync(StateHasChanged);
        }

        private async Task DeleteMessage(Message message)
        {
            await MessagingService.DeleteMessage(message.Id);
            Messages.Remove(message);

            await InvokeAsync(StateHasChanged);
        }

        private async Task DeleteAllMessages()
        {
            foreach(Message message in Messages.ToArray())
            {
                await MessagingService.DeleteMessage(message.Id);
            }

            await InvokeAsync(StateHasChanged);
        }

        private async Task OnAudioClick()
        {
            IsMuted = !IsMuted;
            TownBulletinDbContext.Settings.SetSetting("MessageNotificationsEnabled", (!IsMuted).ToString());
            await JSRuntime.InvokeVoidAsync("audio.mute");
        }

        private static string GetMessageStyle(Message message)
        {
            return message.Severity switch
            {
                LogLevel.Trace or LogLevel.Debug => "list-group-item-light",
                LogLevel.Information => "list-group-item-info",
                LogLevel.Warning => "list-group-item-warning",
                LogLevel.Error or LogLevel.Critical => "list-group-item-danger",
                _ => "",
            };
        }

        private async Task FilterMessages(ChangeEventArgs changeEventArgs)
        {
            if (changeEventArgs?.Value != null && changeEventArgs.Value is IEnumerable<LogLevel> selectedLogLevels)
            {
                TownBulletinDbContext.Settings.SetSetting("MessageLevels", string.Join(", ", selectedLogLevels));
                FilteredLogLevels = selectedLogLevels;

                await UpdateMessages();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!DisposedValue)
            {
                DisposedValue = true;
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}