using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OBSWebsocketDotNet;
using TownBulletin.Models;
using TownBulletin.Services;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace TownBulletin.Modules./*EventName*/
{
    public class /*ModuleName*/
    {
        public static Task<bool> /*EntryMethod*/(/*EventArgs*/ eventArgs)
        {
            return Task.FromResult(true);
        }
    }
}
