using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet.Types;
using System;
using System.Collections.Generic;

namespace OBSWebsocketDotNet
{
    /// <summary>
    /// Scene event arguments.
    /// </summary>
    /// <param name="SceneName">Name of the scene</param>
    public class SceneEventArgs : EventArgs
    {
        public string SceneName { get; set; }
    }

    /// <summary>
    /// Scene item event arguments.
    /// </summary>
    /// <param name="SceneName">Name of the scene where the item is</param>
    /// <param name="ItemName">Name of the concerned item</param>
    public class SceneItemEventArgs : EventArgs
    {
        public string SceneName { get; set; }
        public string ItemName { get; set; }
    }

    /// <summary>
    /// Scene item visibility event arguments.
    /// </summary>
    /// <param name="SceneName">Name of the scene where the item is</param>
    /// <param name="ItemName">Name of the concerned item</param>
    /// <param name="IsVisible">Visibility of the item</param>
    public class SceneItemVisibilityEventArgs : EventArgs
    {
        public string SceneName { get; set; }
        public string ItemName { get; set; }
        public bool IsVisible { get; set; }
    }

    /// <summary>
    /// Scene item lock event arguments.
    /// </summary>
    /// <param name="SceneName">Name of the scene where the item is</param>
    /// <param name="ItemName">Name of the concerned item</param>
    /// <param name="ItemId">Id of the concerned item</param>
    /// <param name="IsLocked">Lock status of the item</param>
    public class SceneItemLockEventArgs : EventArgs
    {
        public string SceneName { get; set; }
        public string ItemName { get; set; }
        public int ItemId { get; set; }
        public bool IsLocked { get; set; }
    }

    /// <summary>
    /// Transition event arguments.
    /// </summary>
    /// <param name="TransitionName">Name of the concerned transition</param>
    public class TransitionEventArgs : EventArgs
    {
        public string TransitionName { get; set; }
    }

    /// <summary>
    /// Transition duration event arguments.
    /// </summary>
    /// <param name="Duration">Name of the transition duration (in milliseconds)</param>
    public class TransitionDurationEventArgs : EventArgs
    {
        public int Duration { get; set; }
    }

    /// <summary>
    /// Transition begin event arguments.
    /// </summary>
    /// <param name="TransitionName">Name of the concerned transition</param>   
    /// <param name="TransitionType">Transition type</param>
    /// <param name="Duration">Transition duration (in milliseconds). Will be -1 for any transition with a fixed duration, such as a Stinger, due to limitations of the OBS API</param>
    /// <param name="FromScene">Source scene of the transition</param>
    /// <param name="ToScene">Destination scene of the transition</param>
    public class TransitionFromToEventArgs : EventArgs
    {
        public string TransitionName { get; set; }
        public string TransitionType { get; set; }
        public int Duration { get; set; }
        public string FromScene { get; set; }
        public string ToScene { get; set; }
    }

    /// <summary>
    /// Transition end event arguments.
    /// </summary>
    /// <param name="TransitionName">Name of the concerned transition</param>   
    /// <param name="TransitionType">Transition type</param>
    /// <param name="Duration">Transition duration (in milliseconds). Will be -1 for any transition with a fixed duration, such as a Stinger, due to limitations of the OBS API</param>
    /// <param name="ToScene">Destination scene of the transition</param>
    public class TransitionToEventArgs : EventArgs
    {
        public string TransitionName { get; set; }
        public string TransitionType { get; set; }
        public int Duration { get; set; }
        public string ToScene { get; set; }
    }

    /// <summary>
    /// Output state event arguments.
    /// </summary>
    /// <param name="type">Output state</param>
    public class OutputStateEventArgs : EventArgs
    {
        public OutputState OutputState { get; set; }
    }

    /// <summary>
    /// Stream status event arguments.
    /// </summary>
    /// <param name="status">Stream status data</param>
    public class StreamStatusEventArgs : EventArgs
    {
        public StreamStatus StreamStatus { get; set; }
    }

    /// <summary>
    /// Studio mode event arguments.
    /// </summary>
    /// <param name="enabled">Studio Mode status</param>
    public class StudioModeEventArgs : EventArgs
    {
        public bool Enabled { get; set; }
    }

    /// <summary>
    /// Heartbeat event arguments.
    /// </summary>
    /// <param name="Heartbeat">heartbeat data</param>
    public class HeartbeatEventArgs : EventArgs
    {
        public Heartbeat Heartbeat { get; set; }
    }

    /// <summary>
    /// Scene item ID event arguments.
    /// </summary>
    /// <param name="SceneName">Name of the scene where the item is</param>
    /// <param name="ItemName">Name of the concerned item</param>
    /// <param name="ItemId">Id of the concerned item</param>
    public class SceneItemIdEventArgs : EventArgs
    {
        public string SceneName { get; set; }
        public string ItemName { get; set; }
        public string ItemId { get; set; }
    }

    /// <summary>
    /// Scene item transform event arguments.
    /// </summary>
    /// <param name="Transform">Transform data</param>
    public class SceneItemTransformEventArgs : EventArgs
    {
        public SceneItemTransformInfo Transform { get; set; }
    }

    /// <summary>
    /// Source audio mixer event arguments.
    /// </summary>
    /// <param name="MixerInfo">Mixer information</param>
    public class SourceAudioMixersEventArgs : EventArgs
    {
        public AudioMixersChangedInfo MixerInfo { get; set; }
    }

    /// <summary>
    /// Source audio sync offset event arguments.
    /// </summary>
    /// <param name="SourceName">Name of the source for the offset change</param>
    /// <param name="SyncOffset">Sync offset value</param>
    public class SourceAudioSyncOffsetEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public int SyncOffset { get; set; }
    }

    /// <summary>
    /// Source settings event arguments.
    /// </summary>
     /// <param name="Settings">Source settings</param>
    public class SourceSettingsEventArgs : EventArgs
    {
        public SourceSettings Settings { get; set; }
    }

    /// <summary>
    /// Source type event arguments.
    /// </summary>
    /// <param name="SourceName">Name of the source for the offset change</param>
    /// <param name="SourceKind">Kind of source</param>
    /// <param name="SourceType">Type of source</param>
    public class SourceTypeEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string SourceType { get; set; }
        public string SourceKind { get; set; }
    }

    /// <summary>
    /// Source renamed event arguments.
    /// </summary>
    /// <param name="NewName">New name of source</param>
    /// <param name="PreviousName">Previous name of source</param>
    public class SourceRenamedEventArgs : EventArgs
    {
        public string NewName { get; set; }
        public string PreviousName { get; set; }
    }

    /// <summary>
    /// Source muted event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="Muted">Current mute state of source</param>
    public class SourceMuteEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public bool Muted { get; set; }
    }

    /// <summary>
    /// Source event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    public class SourceEventArgs : EventArgs
    {
        public string SourceName { get; set; }
    }

    /// <summary>
    /// Source volume event arguments.
    /// </summary>
    /// <param name="Volume">Current volume levels of source</param>
    public class SourceVolumeEventArgs : EventArgs
    {
        public SourceVolume Volume { get; set; }
    }

    /// <summary>
    /// Source filter event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="FilterName">Name of filter</param>
    public class SourceFilterEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string FilterName { get; set; }
    }


    /// <summary>
    /// Source filter event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="FilterName">Name of filter</param>
    /// <param name="FilterType">Type of filter</param>
    /// <param name="FilterSettings">Settings for filter</param>
    public class SourceFilterTypeEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string FilterName { get; set; }
        public string FilterType { get; set; }
        public JObject FilterSettings { get; set; }
    }

    /// <summary>
    /// Source filter order event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="Filters">Current order of filters for source</param>
    public class SourceFilterOrderEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public List<FilterReorderItem> Filters { get; set; }
    }

    /// <summary>
    /// Source filter visibility event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="FilterName">Name of filter</param>    
    /// <param name="FilterEnabled">Filter visibility</param>
    public class SourceFilterVisibilityEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string FilterName { get; set; }
        public bool FilterEnabled { get; set; }
    }

    /// <summary>
    /// Custom broadcast event args.
    /// </summary>
    /// <param name="Realm">Identifier provided by the sender</param>
    /// <param name="Data">User-defined data</param>
    public class CustomBroadcastEventArgs : EventArgs
    {
        public string Realm { get; set; }
        public JObject Data { get; set; }
    }

    /// <summary>
    /// Media event arguments.
    /// </summary>
    /// <param name="SourceName">Name of source</param>
    /// <param name="SourceKind">Kind of source</param>
    public class MediaEventArgs : EventArgs
    {
        public string SourceName { get; set; }
        public string SourceKind { get; set; }
    }

}