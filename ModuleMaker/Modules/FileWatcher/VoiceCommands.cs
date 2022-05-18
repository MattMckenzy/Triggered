using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;
using Triggered.Models;
using Triggered.Services;

namespace ModuleMaker.Modules.FileWatcher
{
    public class VoiceCommands
    {
        public static async Task<bool> ActionVoiceCommand(FileSystemEventArgs eventArgs, MessagingService messagingService)
        {
            await messagingService.AddMessage($"Created file: \"{eventArgs.FullPath}\"", MessageCategory.Module, LogLevel.Debug);
                
            return true;
        }
    }
}