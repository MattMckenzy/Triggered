using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using WebSocketSharp;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using OBSWebsocketDotNet.Types;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace OBSWebsocketDotNet
{
    public partial class OBSWebsocket
    {
        #region Events
        /// <summary>
        /// Triggered when switching to another scene
        /// </summary>
        public event EventHandler<SceneEventArgs> SceneChanged;

        /// <summary>
        /// Triggered when a scene is created, deleted or renamed
        /// </summary>
        public event EventHandler SceneListChanged;

        /// <summary>
        /// Triggered when the scene item list of the specified scene is reordered
        /// </summary>
        public event EventHandler<SceneEventArgs> SourceOrderChanged;

        /// <summary>
        /// Triggered when a new item is added to the item list of the specified scene
        /// </summary>
        public event EventHandler<SceneItemEventArgs> SceneItemAdded;

        /// <summary>
        /// Triggered when an item is removed from the item list of the specified scene
        /// </summary>
        public event EventHandler<SceneItemEventArgs> SceneItemRemoved;

        /// <summary>
        /// Triggered when the visibility of a scene item changes
        /// </summary>
        public event EventHandler<SceneItemVisibilityEventArgs> SceneItemVisibilityChanged;

        /// <summary>
        /// Triggered when the lock status of a scene item changes
        /// </summary>
        public event EventHandler<SceneItemLockEventArgs> SceneItemLockChanged;      

        /// <summary>
        /// Triggered when switching to another scene collection
        /// </summary>
        public event EventHandler SceneCollectionChanged;

        /// <summary>
        /// Triggered when a scene collection is created, deleted or renamed
        /// </summary>
        public event EventHandler SceneCollectionListChanged;

        /// <summary>
        /// Triggered when switching to another transition
        /// </summary>
        public event EventHandler<TransitionEventArgs> TransitionChanged;

        /// <summary>
        /// Triggered when the current transition duration is changed
        /// </summary>
        public event EventHandler<TransitionDurationEventArgs> TransitionDurationChanged;

        /// <summary>
        /// Triggered when a transition is created or removed
        /// </summary>
        public event EventHandler TransitionListChanged;

        /// <summary>
        /// Triggered when a transition between two scenes starts. Followed by <see cref="SceneChanged"/>
        /// </summary>
        public event EventHandler<TransitionFromToEventArgs> TransitionBegin;

        /// <summary>
        /// Triggered when a transition (other than "cut") has ended. Please note that the from-scene field is not available in TransitionEnd
        /// </summary>
        public event EventHandler<TransitionToEventArgs> TransitionEnd;

        /// <summary>
        /// Triggered when a stinger transition has finished playing its video
        /// </summary>
        public event EventHandler<TransitionFromToEventArgs> TransitionVideoEnd;

        /// <summary>
        /// Triggered when switching to another profile
        /// </summary>
        public event EventHandler ProfileChanged;

        /// <summary>
        /// Triggered when a profile is created, imported, removed or renamed
        /// </summary>
        public event EventHandler ProfileListChanged;

        /// <summary>
        /// Triggered when the streaming output state changes
        /// </summary>
        public event EventHandler<OutputStateEventArgs> StreamingStateChanged;

        /// <summary>
        /// Triggered when the recording output state changes
        /// </summary>
        public event EventHandler<OutputStateEventArgs> RecordingStateChanged;

        /// <summary>
        /// Triggered when the recording output is paused
        /// </summary>
        public event EventHandler RecordingPaused;

        /// <summary>
        /// Triggered when the recording output is resumed
        /// </summary>
        public event EventHandler RecordingResumed;

        /// <summary>
        /// Triggered when state of the replay buffer changes
        /// </summary>
        public event EventHandler<OutputStateEventArgs> ReplayBufferStateChanged;

        /// <summary>
        /// Triggered every 2 seconds while streaming is active
        /// </summary>
        public event EventHandler<StreamStatusEventArgs> StreamStatus;

        /// <summary>
        /// Triggered when the preview scene selection changes (Studio Mode only)
        /// </summary>
        public event EventHandler<SceneEventArgs> PreviewSceneChanged;

        /// <summary>
        /// Triggered when Studio Mode is turned on or off
        /// </summary>
        public event EventHandler<StudioModeEventArgs> StudioModeSwitched;

        /// <summary>
        /// Triggered when OBS exits
        /// </summary>
        public event EventHandler OBSExit;

        /// <summary>
        /// Triggered when connected successfully to an obs-websocket server
        /// </summary>
        public event EventHandler Connected;

        /// <summary>
        /// Triggered when disconnected from an obs-websocket server
        /// </summary>
        public event EventHandler Disconnected;

        /// <summary>
        /// Emitted every 2 seconds after enabling it by calling SetHeartbeat
        /// </summary>
        public event EventHandler<HeartbeatEventArgs> Heartbeat;

        /// <summary>
        /// A scene item is deselected
        /// </summary>
        public event EventHandler<SceneItemIdEventArgs> SceneItemDeselected;

        /// <summary>
        /// A scene item is selected
        /// </summary>
        public event EventHandler<SceneItemIdEventArgs> SceneItemSelected;

        /// <summary>
        /// A scene item transform has changed
        /// </summary>
        public event EventHandler<SceneItemTransformEventArgs> SceneItemTransformChanged;

        /// <summary>
        /// Audio mixer routing changed on a source
        /// </summary>
        public event EventHandler<SourceAudioMixersEventArgs> SourceAudioMixersChanged;

        /// <summary>
        /// The audio sync offset of a source has changed
        /// </summary>
        public event EventHandler<SourceAudioSyncOffsetEventArgs> SourceAudioSyncOffsetChanged;

        /// <summary>
        /// A source has been created. A source can be an input, a scene or a transition.
        /// </summary>
        public event EventHandler<SourceSettingsEventArgs> SourceCreated;

        /// <summary>
        /// A source has been destroyed/removed. A source can be an input, a scene or a transition.
        /// </summary>
        public event EventHandler<SourceTypeEventArgs> SourceDestroyed;

        /// <summary>
        /// A filter was added to a source
        /// </summary>
        public event EventHandler<SourceFilterTypeEventArgs> SourceFilterAdded;

        /// <summary>
        /// A filter was removed from a source
        /// </summary>
        public event EventHandler<SourceFilterEventArgs> SourceFilterRemoved;

        /// <summary>
        /// Filters in a source have been reordered
        /// </summary>
        public event EventHandler<SourceFilterOrderEventArgs> SourceFiltersReordered;

        /// <summary>
        /// Triggered when the visibility of a filter has changed
        /// </summary>
        public event EventHandler<SourceFilterVisibilityEventArgs> SourceFilterVisibilityChanged;

        /// <summary>
        /// A source has been muted or unmuted
        /// </summary>
        public event EventHandler<SourceMuteEventArgs> SourceMuteStateChanged;

        /// <summary>
        /// A source has been muted or unmuted
        /// </summary>
        public event EventHandler<SourceEventArgs> SourceAudioDeactivated;

        /// <summary>
        /// A source has been muted or unmuted
        /// </summary>
        public event EventHandler<SourceEventArgs> SourceAudioActivated;

        /// <summary>
        /// A source has been renamed
        /// </summary>
        public event EventHandler<SourceRenamedEventArgs> SourceRenamed;

        /// <summary>
        /// The volume of a source has changed
        /// </summary>
        public event EventHandler<SourceVolumeEventArgs> SourceVolumeChanged;

        /// <summary>
        /// A custom broadcast message was received
        /// </summary>
        public event EventHandler<CustomBroadcastEventArgs> BroadcastCustomMessageReceived;

        /// <summary>
        /// These events are emitted by the OBS sources themselves. For example when the media file ends. The behavior depends on the type of media source being used.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaEnded;

        /// <summary>
        /// These events are emitted by the OBS sources themselves. For example when the media file starts playing. The behavior depends on the type of media source being used.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaStarted;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaPrevious;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaNext;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaStopped;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaRestarted;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaPaused;

        /// <summary>
        /// This event is only emitted when something actively controls the media/VLC source. In other words, the source will never emit this on its own naturally.
        /// </summary>
        public event EventHandler<MediaEventArgs> MediaPlaying;

        /// <summary>
        /// The virtual camera has been started.
        /// </summary>
        public event EventHandler VirtualCameraStarted;

        /// <summary>
        /// The virtual camera has been stopped.
        /// </summary>
        public event EventHandler VirtualCameraStopped;

        #endregion

        /// <summary>
        /// WebSocket request timeout, represented as a TimeSpan object
        /// </summary>
        public TimeSpan WSTimeout
        {
            get
            {
                return WSConnection?.WaitTime ?? wsTimeout;
            }
            set
            {
                wsTimeout = value;

                if (WSConnection != null)
                {
                    WSConnection.WaitTime = wsTimeout;
                }
            }
        }

        #region Private Members
        private const string WEBSOCKET_URL_PREFIX = "ws://";
        private TimeSpan wsTimeout = TimeSpan.FromSeconds(10);

        // Random should never be created inside a function
        private static readonly Random random = new Random();

        #endregion

        /// <summary>
        /// Current connection state
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return (WSConnection != null && WSConnection.IsAlive);
            }
        }

        /// <summary>
        /// Underlying WebSocket connection to an obs-websocket server. Value is null when disconnected.
        /// </summary>
        public WebSocket WSConnection { get; private set; }

        private delegate void RequestCallback(OBSWebsocket sender, JObject body);
        private readonly ConcurrentDictionary<string, TaskCompletionSource<JObject>> responseHandlers;

        /// <summary>
        /// Constructor
        /// </summary>
        public OBSWebsocket()
        {
            responseHandlers = new ConcurrentDictionary<string, TaskCompletionSource<JObject>>();
        }

        /// <summary>
        /// Connect this instance to the specified URL, and authenticate (if needed) with the specified password
        /// </summary>
        /// <param name="url">Server URL in standard URL format.</param>
        /// <param name="password">Server password</param>
        public void Connect(string url, string password)
        {
            if (!url.ToLower().StartsWith(WEBSOCKET_URL_PREFIX))
            {
                throw new ArgumentException($"Invalid url, must start with '{WEBSOCKET_URL_PREFIX}'");
            }

            if (WSConnection != null && WSConnection.IsAlive)
            {
                Disconnect();
            }

            WSConnection = new WebSocket(url)
            {
                WaitTime = wsTimeout
            };
            WSConnection.OnMessage += WebsocketMessageHandler;
            WSConnection.OnClose += (s, e) =>
            {
                Disconnected?.Invoke(this, e);
            };
            WSConnection.Connect();

            if (!WSConnection.IsAlive)
                return;

            OBSAuthInfo authInfo = GetAuthInfo();

            if (authInfo.AuthRequired)
            {
                Authenticate(password, authInfo);
            }

            Connected?.Invoke(this, null);
        }

        /// <summary>
        /// Disconnect this instance from the server
        /// </summary>
        public void Disconnect()
        {
            if (WSConnection != null)
            {
                // Attempt to both close and dispose the existing connection
                try
                {
                    WSConnection.Close();
                    ((IDisposable)WSConnection).Dispose();
                }
                catch { }
                WSConnection = null;
            }
            
            var unusedHandlers = responseHandlers.ToArray();
            responseHandlers.Clear();
            foreach (var cb in unusedHandlers)
            {
                var tcs = cb.Value;
                tcs.TrySetCanceled();
            }
        }



        // This callback handles incoming JSON messages and determines if it's
        // a request response or an event ("Update" in obs-websocket terminology)
        private void WebsocketMessageHandler(object sender, MessageEventArgs e)
        {
            if (!e.IsText)
                return;

            JObject body = JObject.Parse(e.Data);

            if (body["message-id"] != null)
            {
                // Handle a request :
                // Find the response handler based on
                // its associated message ID
                string msgID = (string)body["message-id"];

                if (responseHandlers.TryRemove(msgID, out TaskCompletionSource<JObject> handler))
                {
                    // Set the response body as Result and notify the request sender
                    handler.SetResult(body);
                }
            }
            else if (body["update-type"] != null)
            {
                // Handle an event
                string eventType = body["update-type"].ToString();
                Task.Run(() => { ProcessEventType(eventType, body); });
            }
        }

        /// <summary>
        /// Sends a message to the websocket API with the specified request type and optional parameters
        /// </summary>
        /// <param name="requestType">obs-websocket request type, must be one specified in the protocol specification</param>
        /// <param name="additionalFields">additional JSON fields if required by the request type</param>
        /// <returns>The server's JSON response as a JObject</returns>
        public JObject SendRequest(string requestType, JObject additionalFields = null)
        {
            string messageID;

            // Build the bare-minimum body for a request
            var body = new JObject
            {
                { "request-type", requestType }
            };

            // Add optional fields if provided
            if (additionalFields != null)
            {
                _ = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union
                };

                body.Merge(additionalFields);
            }

            // Prepare the asynchronous response handler
            var tcs = new TaskCompletionSource<JObject>();
            do
            {
                // Generate a random message id
                messageID = NewMessageID();
                if (responseHandlers.TryAdd(messageID, tcs))
                {
                    body.Add("message-id", messageID);
                    break;
                }
                // Message id already exists, retry with a new one.
            } while (true);
            // Send the message and wait for a response
            // (received and notified by the websocket response handler)
            WSConnection.Send(body.ToString());
            tcs.Task.Wait();

            if (tcs.Task.IsCanceled)
                throw new ErrorResponseException("Request canceled");

            // Throw an exception if the server returned an error.
            // An error occurs if authentication fails or one if the request body is invalid.
            var result = tcs.Task.Result;

            if ((string)result["status"] == "error")
                throw new ErrorResponseException((string)result["error"]);

            return result;
        }

        /// <summary>
        /// Requests version info regarding obs-websocket, the API and OBS Studio
        /// </summary>
        /// <returns>Version info in an <see cref="OBSVersion"/> object</returns>
        public OBSVersion GetVersion()
        {
            JObject response = SendRequest("GetVersion");
            return new OBSVersion(response);
        }

        /// <summary>
        /// Request authentication data. You don't have to call this manually.
        /// </summary>
        /// <returns>Authentication data in an <see cref="OBSAuthInfo"/> object</returns>
        public OBSAuthInfo GetAuthInfo()
        {
            JObject response = SendRequest("GetAuthRequired");
            return new OBSAuthInfo(response);
        }

        /// <summary>
        /// Authenticates to the Websocket server using the challenge and salt given in the passed <see cref="OBSAuthInfo"/> object
        /// </summary>
        /// <param name="password">User password</param>
        /// <param name="authInfo">Authentication data</param>
        /// <returns>true if authentication succeeds, false otherwise</returns>
        public bool Authenticate(string password, OBSAuthInfo authInfo)
        {
            string secret = HashEncode(password + authInfo.PasswordSalt);
            string authResponse = HashEncode(secret + authInfo.Challenge);

            var requestFields = new JObject
            {
                { "auth", authResponse }
            };

            try
            {
                // Throws ErrorResponseException if auth fails
                SendRequest("Authenticate", requestFields);
            }
            catch (ErrorResponseException)
            {
                Disconnect();
                throw new AuthFailureException();
            }

            return true;
        }

        /// <summary>
        /// Update message handler
        /// </summary>
        /// <param name="eventType">Value of "event-type" in the JSON body</param>
        /// <param name="body">full JSON message body</param>
        protected void ProcessEventType(string eventType, JObject body)
        {
            StreamStatus status;

            switch (eventType)
            {
                case "SwitchScenes":
                    SceneChanged?.Invoke(this, new SceneEventArgs { SceneName = (string)body["scene-name"] });
                    break;

                case "ScenesChanged":
                    SceneListChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "SourceOrderChanged":
                    SourceOrderChanged?.Invoke(this, new SceneEventArgs { SceneName = (string)body["scene-name"] });
                    break;

                case "SceneItemAdded":
                    SceneItemAdded?.Invoke(this, new SceneItemEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"] });
                    break;

                case "SceneItemRemoved":
                    SceneItemRemoved?.Invoke(this, new SceneItemEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"] });
                    break;

                case "SceneItemVisibilityChanged":
                    SceneItemVisibilityChanged?.Invoke(this, new SceneItemVisibilityEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"], IsVisible = (bool)body["item-visible"] });
                    break;

                case "SceneItemLockChanged":
                    SceneItemLockChanged?.Invoke(this, new SceneItemLockEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"], ItemId = (int)body["item-id"], IsLocked = (bool)body["item-locked"] });
                    break;

                case "SceneCollectionChanged":
                    SceneCollectionChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "SceneCollectionListChanged":
                    SceneCollectionListChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "SwitchTransition":
                    TransitionChanged?.Invoke(this, new TransitionEventArgs { TransitionName = (string)body["transition-name"] });
                    break;

                case "TransitionDurationChanged":
                    TransitionDurationChanged?.Invoke(this, new TransitionDurationEventArgs { Duration = (int)body["new-duration"] });
                    break;

                case "TransitionListChanged":
                    TransitionListChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "TransitionBegin":
                    TransitionBegin?.Invoke(this, new TransitionFromToEventArgs { TransitionName = (string)body["name"], TransitionType = (string)body["type"], Duration = (int)body["duration"], FromScene = (string)body["from-scene"], ToScene = (string)body["to-scene"] });
                    break;

                case "TransitionEnd":
                    TransitionEnd?.Invoke(this, new TransitionToEventArgs { TransitionName = (string)body["name"], TransitionType = (string)body["type"], Duration = (int)body["duration"], ToScene = (string)body["to-scene"] });
                    break;

                case "TransitionVideoEnd":
                    TransitionVideoEnd?.Invoke(this, new TransitionFromToEventArgs { TransitionName = (string)body["name"], TransitionType = (string)body["type"], Duration = (int)body["duration"], FromScene = (string)body["from-scene"], ToScene = (string)body["to-scene"] });
                    break;

                case "ProfileChanged":
                    ProfileChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "ProfileListChanged":
                    ProfileListChanged?.Invoke(this, EventArgs.Empty);
                    break;

                case "StreamStarting":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Starting });
                    break;

                case "StreamStarted":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Started });
                    break;

                case "StreamStopping":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopping });
                    break;

                case "StreamStopped":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopped });
                    break;

                case "RecordingStarting":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Starting });
                    break;

                case "RecordingStarted":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Starting });
                    break;

                case "RecordingStopping":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopping });
                    break;

                case "RecordingStopped":
                    StreamingStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopped });
                    break;

                case "RecordingPaused":
                    RecordingPaused?.Invoke(this, EventArgs.Empty);
                    break;

                case "RecordingResumed":
                    RecordingResumed?.Invoke(this, EventArgs.Empty);
                    break;

                case "StreamStatus":
                    if (StreamStatus != null)
                    {
                        status = new StreamStatus(body);
                        StreamStatus(this, new StreamStatusEventArgs { StreamStatus = status });
                    }
                    break;

                case "PreviewSceneChanged":
                    PreviewSceneChanged?.Invoke(this, new SceneEventArgs { SceneName = (string)body["scene-name"] });
                    break;

                case "StudioModeSwitched":
                    StudioModeSwitched?.Invoke(this, new StudioModeEventArgs { Enabled = (bool)body["new-state"] });
                    break;

                case "ReplayStarting":
                    ReplayBufferStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Starting });
                    break;

                case "ReplayStarted":
                    ReplayBufferStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Started });
                    break;

                case "ReplayStopping":
                    ReplayBufferStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopping });
                    break;

                case "ReplayStopped":
                    ReplayBufferStateChanged?.Invoke(this, new OutputStateEventArgs { OutputState = OutputState.Stopped });
                    break;

                case "Exiting":
                    OBSExit?.Invoke(this, EventArgs.Empty);
                    break;

                case "Heartbeat":
                    Heartbeat?.Invoke(this, new HeartbeatEventArgs { Heartbeat = new Heartbeat(body) });
                    break;

                case "SceneItemDeselected":
                    SceneItemDeselected?.Invoke(this, new SceneItemIdEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"], ItemId = (string)body["item-id"] });
                    break;

                case "SceneItemSelected":
                    SceneItemSelected?.Invoke(this, new SceneItemIdEventArgs { SceneName = (string)body["scene-name"], ItemName = (string)body["item-name"], ItemId = (string)body["item-id"] });
                    break;

                case "SceneItemTransformChanged":
                    SceneItemTransformChanged?.Invoke(this, new SceneItemTransformEventArgs { Transform = new SceneItemTransformInfo(body) });
                    break;

                case "SourceAudioMixersChanged":
                    SourceAudioMixersChanged?.Invoke(this, new SourceAudioMixersEventArgs { MixerInfo = new AudioMixersChangedInfo(body) });
                    break;

                case "SourceAudioSyncOffsetChanged":
                    SourceAudioSyncOffsetChanged?.Invoke(this, new SourceAudioSyncOffsetEventArgs { SourceName = (string)body["sourceName"], SyncOffset = (int)body["syncOffset"] });
                    break;

                case "SourceCreated":
                    SourceCreated?.Invoke(this, new SourceSettingsEventArgs { Settings = new SourceSettings(body) });
                    break;

                case "SourceDestroyed":
                    SourceDestroyed?.Invoke(this, new SourceTypeEventArgs { SourceName = (string)body["sourceName"], SourceType = (string)body["sourceType"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "SourceRenamed":
                    SourceRenamed?.Invoke(this, new SourceRenamedEventArgs { NewName = (string)body["newName"], PreviousName = (string)body["previousName"] });
                    break;

                case "SourceMuteStateChanged":
                    SourceMuteStateChanged?.Invoke(this, new SourceMuteEventArgs { SourceName = (string)body["sourceName"], Muted = (bool)body["muted"] });
                    break;

                case "SourceAudioDeactivated":
                    SourceAudioDeactivated?.Invoke(this, new SourceEventArgs { SourceName = (string)body["sourceName"] });
                    break;

                case "SourceAudioActivated":
                    SourceAudioActivated?.Invoke(this, new SourceEventArgs { SourceName = (string)body["sourceName"] });
                    break;

                case "SourceVolumeChanged":
                    SourceVolumeChanged?.Invoke(this, new SourceVolumeEventArgs { Volume = new SourceVolume(body) });
                    break;

                case "SourceFilterAdded":
                    SourceFilterAdded?.Invoke(this, new SourceFilterTypeEventArgs { SourceName = (string)body["sourceName"], FilterName = (string)body["filterName"], FilterType = (string)body["filterType"], FilterSettings = (JObject)body["filterSettings"] });
                    break;

                case "SourceFilterRemoved":
                    SourceFilterRemoved?.Invoke(this, new SourceFilterEventArgs { SourceName = (string)body["sourceName"], FilterName = (string)body["filterName"] });
                    break;

                case "SourceFiltersReordered":
                    if (SourceFiltersReordered != null)
                    {
                        List<FilterReorderItem> filters = new List<FilterReorderItem>();
                        JsonConvert.PopulateObject(body["filters"].ToString(), filters);

                        SourceFiltersReordered?.Invoke(this, new SourceFilterOrderEventArgs { SourceName = (string)body["sourceName"], Filters = filters });
                    }
                    break;

                case "SourceFilterVisibilityChanged":
                    SourceFilterVisibilityChanged?.Invoke(this, new SourceFilterVisibilityEventArgs { SourceName = (string)body["sourceName"], FilterName = (string)body["filterName"], FilterEnabled = (bool)body["filterEnabled"] });
                    break;

                case "BroadcastCustomMessage":
                    BroadcastCustomMessageReceived?.Invoke(this, new CustomBroadcastEventArgs { Realm = (string)body["realm"], Data = (JObject)body["data"] });
                    break;

                case "MediaEnded":
                    MediaEnded?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaStarted":
                    MediaStarted?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaPrevious":
                    MediaPrevious?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaNext":
                    MediaNext?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaStopped":
                    MediaStopped?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaRestarted":
                    MediaRestarted?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaPaused":
                    MediaPaused?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "MediaPlaying":
                    MediaPlaying?.Invoke(this, new MediaEventArgs { SourceName = (string)body["sourceName"], SourceKind = (string)body["sourceKind"] });
                    break;

                case "VirtualCamStarted":
                    VirtualCameraStarted?.Invoke(this, EventArgs.Empty);
                    break;

                case "VirtualCamStopped":
                    VirtualCameraStopped?.Invoke(this, EventArgs.Empty);
                    break;

                default:
                        var message = $"Unsupported Event: {eventType}\n{body}";
                        Console.WriteLine(message);
                        Debug.WriteLine(message);
                        break;
            }
        }

        /// <summary>
        /// Encode a Base64-encoded SHA-256 hash
        /// </summary>
        /// <param name="input">source string</param>
        /// <returns></returns>
        protected string HashEncode(string input)
        {
            using var sha256 = new SHA256Managed();

            byte[] textBytes = Encoding.ASCII.GetBytes(input);
            byte[] hash = sha256.ComputeHash(textBytes);

            return System.Convert.ToBase64String(hash);
        }

        /// <summary>
        /// Generate a message ID
        /// </summary>
        /// <param name="length">(optional) message ID length</param>
        /// <returns>A random string of alphanumerical characters</returns>
        protected string NewMessageID(int length = 16)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";

            string result = "";
            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, pool.Length - 1);
                result += pool[index];
            }

            return result;
        }
    }
}
