<img align="center" height="100" src="https://raw.githubusercontent.com/MattMckenzy/Triggered/main/Resources/Triggered.png">

### React to Twitch and OBS events with dynamically compiled C# modules!

# Description

This application will provide a feature-rich, infinitely extendable way to react to any Twitch, OBS or Discord event offered by the platforms. 

At its core, it helpls you create, maintain and execute C# modules triggered to run by selected events. The modules are compiled dynamically during runtime and don't even require a web-page refresh to enable!

The web application also features a testing center, which can be used to mock event data and trigger all ready and compiled moodules created for that event. 

To aid in the creation and management of modules, here are some of the useful features that are included:
- Built-in, rich C# code editor with instant code analysis feedback. 
- Create and maintain C# utility methods that can be reused in any module.
- Ability to load external dll references.
- A dynamic data service to store and retrieve persistant information.
- Extendable module and utility templates.
- API available to execute any event or individual module.

To interact with Twitch, OBS and Discord, authentication and authorization credentials must be entered in its settings, which can be updated in the web application's configuration page.

# Installation

To install, simply download the Triggered.Launcher.exe file available [here](https://github.com/MattMckenzy/Triggered/releases), and launch it. It will download and update the web service, and give you a window displaying its console log as well as give you a tray icon that can be used to launch the website or shut down the web service.

The web application is coded platform-agnostic, so it will work on linux. The launcher however, is Windows only. For more instructions on getting this to run on Linux, please send me a message and I'll gladly help out!

# Initial Setup

Before launching and using the Triggered web application, **PLEASE READ THE FOLLOWING**:
- **IMPORTANT**: secure the app by randomly changing the following two keys found in appsettings.json:
  - The certificate password: used to secure the generated self-signed certificate used for HTTPS communication.
  - The database encryption key: used to secure very sensitive information such as client secrets and access tokens.

The appsettings.json file has a few other possible configurations:
- **"GenerateCertificate"**: *(Default: true)* Boolean: true to generate a self-signed certificate, false if you want to configure your own.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Url"**: *(Default: "https://localhost:7121")* Permits to set the service listening URL. The local address and port you will use to connect to the Triggered web app.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Path"**: *(Default "SelfSigned.pfx")* The path to the HTTPS certificate to use, or location to generate the self-signed one.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Password"**: The password used to generate the certificate, please make this truly random and secure!
- **"IPAccessList"**: *(Default: "127.0.0.1;::1;::ffff:127.0.0.1")* A semicolon-seperated list of client IP addresses that are allowed to use the Triggered web application.
- **"EncryptionKey"**: The password used to generate to encrypt sensitive information in the database, please make this truly random and secure!

<kbd><img align="center" height="500" src="https://raw.githubusercontent.com/MattMckenzy/Triggered/main/Resources/appsettings.png"></kbd>

# Launcher

The launcher provides a quick way to boot up the TR⚡GGERED service, and tuck it away in the notification area of the Taskbar. At the moment, it's only supported on Windows platforms.

Simply run the Triggered.Launcher.exe file available [here](https://github.com/MattMckenzy/Triggered/releases), and it will open a console window that you can use to update, monitor and run the service. 

Upon launch, it will check GitHub for any updated releases, download and update the service files automatically (while keeping any user-configured *.db or *.cs files the service folder contains), and launch the service.

Closing the window will simply hide it. While running, the Launcher will display a ⚡ icon in the Notification area. Right-clicking it will give you the following options:
- **Open Triggered Application**: will open a browser page pointing to localhost with the port that lets you access and manage the running service.
- **Show Launcher Console**: shows the launcher console window, can be used to make sure no unhandled exceptions have occured.
- **Hide Launcher Console**: hides the launcher console window.
- **Close Service And Exit**: Closes the service and exits the launcher.

# TR⚡GGERED Web Application

The web application will let you manage all aspects of TR⚡GGERED. 

The left hand side (or top on smaller screens) has the navigation menu and includes the following pages:
- **Home**: page that contains service start/stop buttons, as well as a filterable list of messages from the service.
- **Modules**: page used to create and manage modules that will be triggered by events.
- **Utilities**: page used to create and manage reusable code that can be consumed from any module.
- **Testing Center**: page used to create, manage and execute tests on any event supported by the service.
- **Data**: page used to create and manage persisted data objects, for use in modules and utilities. Used in conjuction with the Data Service.
- **Configuration**: page that lets you change default configuration settings, as well as create and maintain custom settings that can be used in any module.
- **README**: page showing this README.

The top section contains buttons used to login and logout of your Twitch (and Twitch Bot, if enabled) accounts. You must login with your Twitch broadcast account, once the client ID and client secret have been set, to have Twitch events available.

## Home Page

The home page contains a series of buttons used to turn off and on all event services that TR⚡GGERED supports. While the service is turning on, the button will be yellow and non-interactable. When the service is running, the button will be red and can be clicked to have it stop. When the service is stopped, the button will be green and can be clicked to have it start.

It also contains a list of messages that will alert of any trace, debug, information, warning and errors that the authentication, services and modules encounter. The list can be filtered to view any combination of message severity. There are buttons that can turn on a new message notification sound as well delete all messages currently filtered.

## Modules Page

The modules page will let you create and maintain C# modules that can be used to react to any supported event. The left side will display all created modules, sorted by service and event type. The right side will display the currently selected module's properties.

The modules list on the left, contains a plus button, that can be used to create a new module. The currently selected module will be hightlighted, with its properties populated on the right side. Each module in the list also has an **X** button that can be used to permanently delete it.

The section on the right, also contains a button on the top, that can be used to recompile all modules. This is useful if there have been any changes to utilities that are referenced in the modules. The modules need to be recompiled to have these changes reflected.

The modules properties are where you can configure all aspects of a module. Mandatory properties will be highlighted in green if valid and red if invalid. The edit code button will also be highlighted red if the code could not compile. 
A module has the following properties:
- **Name**: the name of the module, only used for descriptive purposes.
- **Event**: the event that will trigger the module. The field is a filterable text box drop down menu, categorized by service that calls the event. You can type in the box to filter the available events, use the arrows to highlight one, enter to select it and escape to close the menu.
- **Entry Method**: the name of the method from which the module will be executed. This method needs to exist in the module's code and will not compile if it is absent.
- **Execution Order**: the order the module will be executed when the event is triggered. Lower will be executed first. All modules with the same number will be executed in alphabetical order by name.
- **Stop Event Execution**: boolean value that describes if the triggered event should stop executing modules once this one is complete.
- **Is Enabled**: boolean value that can be used to enable or disable a module, changing it's availability for execution.
- **Edit Code**: a button that is used to bring up the code editor. Used to add and edit a module's execution code. The code editor is further described below.
- **Template Code**: a button that can be used to replace the current code with the module code template. This is destructive!
- **External Modules**: a filterable text box drop down menu field that contains any module file found in the configured external module directory. Clicking on an item in the list will prompt to replace the current code with the one found in the external module.
- **Save Module**: a button to save the current module's properties. Will be disabled if some are invalid or the code cannot be compiled.

## Utilities Page

The utilities page will let you create and maintain C# utilities that can be consumed in any module. The left side will display all created utilities, sorted by name. The right side will display the currently selected utility's properties.

The utilities list on the left, contains a plus button, that can be used to create a new utility. The currently selected utility will be hightlighted, with its properties populated on the right side. Each utility in the list also has an **X** button that can be used to permanently delete it.

The section on the right, also contains a button on the top, that can be used to recompile all utilities.

The utilities properties are where you can configure all aspects of a utility. Mandatory properties will be highlighted in green if valid and red if invalid. The edit code button will also be highlighted red if the code could not compile. 
A utility has the following properties:
- **Name**: the name of the utility, only used for descriptive purposes.
- **Edit Code**: a button that is used to bring up the code editor. Used to add and edit a utility's execution code. The code editor is further described below.
- **Template Code**: a button that can be used to replace the current code with the utility code template. This is destructive!
- **External Utilities**: a filterable text box drop down menu field that contains any utility file found in the configured external utility directory. Clicking on an item in the list will prompt to replace the current code with the one found in the external utility.
- **Save Utility**: a button to save the current utilities's properties. Will be disabled if some are invalid or the code cannot be compiled.

## Code Editor

The code editor will let you edit the C# code of any module or utility. Opening the code editor will open a full-screen modal that can be dismissed by closing the **X** button in the top right. For modules, the top bar of the code editor will also display the name of the related event and event arguments type.

There are a couple of things to consider when creating modules:
- **Entry Method**: the entry method must be `public static Task<bool>`, its name must be defined as the module's entry method and supplied arguments must be in the list of TR⚡GGERED's supported arguments. More information on supported arguments can be found below.
- **Return Value**: the return value is used to determine if the event trigger should continue executing modules. Returning `true` will continue module execution and `false` will stop it.

The code editor has a couple of features meant to ease coding.
- **Autocomplete**: some .NET methods, types, fields and namespaces are available in the autocomplete and can be peeked by using a dot syntax on the name of the parent member.
- **Code Analysis**: the bottom section of the code editor will display all errors and warnings that were encountered during code compilation. These are updated live as soon as the code is changed, as well as when certain properties of the module or utility are updated. The code analysis will also display any unsupported entry arguments as well as if the module's defined entry method is not found.

### Supported Module Entry Arguments

TR⚡GGERED offers a suite of services that can be used in modules. Simply add them as arguments to the entry method, and the calling event will inject them into it.

Here's a quick list and description of the available services. Descriptions of their public properties and methods can be found below:
- `TwitchService`: this service provides access to a `TwitchAPI` instance authenticated and connected through the configured broadcaster account. `TwitchAPI` can be used to easily call any of Twitch's available API methods.
- `TwitchChatService`: this service provides access to a `TwitchClient` instance, as well as a different `TwitchAPI` instance, both connected either through the secondary bot account, if configured, or the main broadcaster account. `TwitchClient` can be used to interact with any channel's twitch chat.
- `ObsService`: this service provides access to a `OBSWebsocket` instance, which can be used to access all OBS websocket methods. Provides means to modify and control OBS.
- `DiscordService`: this service provides access to three instances used to interact with Discord through the configured bot account. The `DiscordSocketClient`, `InteractionService` and `CommandService`.
- `FileWatchingService`: Coming soon...
- `DataService`: this service provides a way to save and retrieve `dynamic` `ExpandoObject` objects. Very useful to persist any type of serializable object accross module calls.
- `QueueService`: this service provides a mean to categorize and queue functions, to ensure they don't get executed simultaneous across multiple triggered events and module calls.
- `ModuleService`: this service provides means to execute modules, in a couple of ways. You can trigger an event with custom event arguments, or execute an individual module with its ID. 
- `MessagingService`: this service provides some methods to add and manage messages that are visible on the Home page of the web app. See messages above.
- `IDbContextFactory<TriggeredDbContext>`: this service can create an instance of `TriggeredDbContext`, which is used to access TR⚡GGERED's database. Useful to store annd retrieve persisted settings.
- `MemoryCache`: this service provides a quick way to cache and retrieve non-persisted information as bytes.

Below goes into detail describing eacch service with some more descriptions, a list of their useful public properties and an example of its use. For more examples, please have a look at the available Modules and Utilities in the ModuleMaker project.

#### TwitchService

The `TwitchService` instance will be the main way you can interact with Twitch. You can use the public instance of the `TwitchAPI` to quickly call API methods ranging from creating polls to ban users. If you would like to interact with the Twitch chat, please see the `TwitchChatService` and its `TwitchClient` below.

The following are its useful public properties and an example:
```csharp
/// <summary>
/// Class representing interactions with the Twitch PubSub. Please see the TwitchLib PubSub documentation here: https://swiftyspiffy.com/TwitchLib/PubSub/index.html
/// </summary>
public TwitchPubSub TwitchPubSub { get; set; } = new();

/// <summary>
/// Class representing interactions with the Twitch EventSub. Please see the TwitchLib EventSub GitHub page here: https://github.com/TwitchLib/TwitchLib.EventSub.Webhooks
/// </summary>
public ITwitchEventSubWebhooks TwitchEventSubWebhooks { get; set; } = null!;

/// <summary>
/// Class offering easy way to authenticate and consume Twitch API. Please see the TwitchLib API documentation here: https://swiftyspiffy.com/TwitchLib/Api/index.html
/// </summary>
public TwitchAPI TwitchAPI { get; private set; } = new();
            
/// <summary>
/// Event handler that is invoked when the service is stopped, starting and started.
/// </summary>
public event EventHandler<EventArgs>? ServiceStatusChanged;

/// <summary>
/// Returns true if the Twitch Service is active.
/// </summary>
public bool? IsActive { get; set; } = false;

/// <summary>
/// If the Twitch Service is logged in, will be populated with the logged in user's information.
/// </summary>
public User? User { get; set; }

/// <summary>
/// If the Twitch Service is logged in, will be populated with the configured channel's information.
/// </summary>
public ChannelInformation? ChannelInformation { get; set; }

/// <summary>
/// The name of the channel, taken from configuration settings (key: TwitchChannel).
/// </summary>
public string ChannelName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"TwitchChannelName"); } }

/// <summary>
/// The name of the user, taken from configuration settings (key: TwitchUserName).
/// </summary>
public string UserName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"TwitchUserName"); } }

/// Example of TwitchAPI use. Passing the access token to TwitchAPI methods is unecessary as it can pull it from the database when needed.
public ExpandoObject GetTwitchUSer(string userId)
{            
    ExpandoObject? user;
    GetUsersResponse usersResponse = await twitchService.TwitchAPI.Helix.Users.GetUsersAsync(new List<string> { userId });
    User? twitchUser = usersResponse.Users.FirstOrDefault();
    if (twitchUser != null)
    {
        user = new ExpandoObject();
        ((dynamic)user).Expires = DateTime.Now + TimeSpan.FromHours(1);
        ((dynamic)user).Name = twitchUser.DisplayName;
        ((dynamic)user).Id = twitchUser.Id;
        ((dynamic)user).Login = twitchUser.Login;
        ((dynamic)user).ProfileImageUrl = twitchUser.ProfileImageUrl;
        ((dynamic)user).Type = twitchUser.Type;
    }

    return user;
}
```

### TwitchChatService

The `TwitchChatService` instance will be the main way you can interact with Twitch Chat. You can use the public instance of the `TwitchClient` to send messages to any joined channel.

The following are its useful public properties and an example:
```csharp
/// <summary>
/// Class offering easy way to interact with Twitch Chat. Please see the TwitchLib Client documentation here: https://swiftyspiffy.com/TwitchLib/Client/index.html
/// </summary>
public TwitchClient TwitchClient { get; set; } = new();

/// <summary>
/// Class offering easy way to authenticate and consume Twitch API.  Please see the TwitchLib API documentation here: https://swiftyspiffy.com/TwitchLib/Api/index.html
/// </summary>
public TwitchAPI TwitchAPI { get; private set; } = new();
            
/// <summary>
/// Event handler that is invoked when the service is stopped, starting and started.
/// </summary>
public event EventHandler<EventArgs>? ServiceStatusChanged;

/// <summary>
/// Returns true if the Twitch service has been started.
/// </summary>
public bool? IsActive { get; set; } = false;

/// <summary>
/// If the Twitch Service is logged in, will be populated with the logged in user's information.
/// </summary>
public User? User { get; set; }

/// <summary>
/// If the Twitch Service is logged in, will be populated with the configured channel's information.
/// </summary>
public ChannelInformation? ChannelInformation { get; set; }

/// <summary>
/// The name of the channel, taken from configuration settings (key: TwitchChatChannelName).
/// </summary>
public string ChannelName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"TwitchChatChannelName"); } }

/// <summary>
/// The name of the user, taken from configuration settings (key: TwitchChatUserName).
/// </summary>
public string UserName { get { return _dbContextFactory.CreateDbContext().Settings.GetSetting($"TwitchChatUserName"); } }

/// Example of TwitchClient use. If you wish to send a message to a channel other than the one currerntly defined in ChannelName, you must join it as shown below.
public static Task<bool> SendMessage(OnChatCommandReceivedArgs onChatCommandReceivedArgs, TwitchChatService chatService)
{
    if (onChatCommandReceivedArgs.Command.CommandText.Equals("bananeorange", StringComparison.InvariantCultureIgnoreCase))
    {
        chatService.TwitchClient.JoinChannel("MattMckenzy");
        chatService.TwitchClient.SendMessage("MattMckenzy", "🍌🍊", false);
    }

    return Task.FromResult(true);
}
```

### ObsService

The `ObsService` instance will be the main way you can interact with OBS. It offers ways to change any scene and source item along with all of their properties. 

The following are its useful public properties and an example:
```csharp
/// <summary>
/// Class offering ways to interact and control OBS through it's scene and source items and their properties. See this page for more information: https://github.com/BarRaider/obs-websocket-dotnet
/// </summary>
public OBSWebsocket OBSWebsocket { get; } = new();

/// <summary>
/// Event handler that is invoked when the service is stopped, starting and started.
/// </summary>
public event EventHandler<EventArgs>? ServiceStatusChanged;

/// <summary>
/// Returns true if the OBS service has been started.
/// </summary>
public bool? IsActive { get; set; } = false;

// Example of OBSWebsoccket use. The following method sets a new TextGDIPlus text as well as a new image in their respective scene items.
public Task ShowFollowSplash(string userName, ObsService obsService)
{
    TriggeredDbContext triggeredDbContext = await triggeredDbContextFactory.CreateDbContextAsync();

    TextGDIPlusProperties properties = obsService.OBSWebsocket.GetTextGDIPlusProperties("FollowText");
    properties.Text = $"Thank you for the follow!\r\nMerci beaucoup pour le suivi!\r\n{userName}";
    obsService.OBSWebsocket.SetTextGDIPlusProperties(properties);

    SourceSettings mediaSourceSettings = obsService.OBSWebsocket.GetSourceSettings("FollowImage", "image_source");
    mediaSourceSettings.Settings["file"] = "followimage.png";
    obsService.OBSWebsocket.SetSourceSettings("FollowImage", mediaSourceSettings.Settings);

    await OBSSceneUtilities.PlayMediaSource(obsService, chosenSound.FullName, "FollowSound", "FollowSplash", "Animations");
}
```

### DiscordService

The `DiscordService` instance will be the main way you can interact with Discord. It offers a connected instance of `DiscordSocketClient` which will be your main way of manipulating any Discord entity the connected bot has access to. 

The following are its useful public properties and an example:
```csharp
    /// <summary>
    /// Class offering methods to interact with Discord guilds, channels, users and more, by means of an authenticated bot token. View the page here for more information: https://discordnet.dev/api/Discord.WebSocket.DiscordSocketClient.html
    /// </summary>
    public DiscordSocketClient DiscordSocketClient { get; }

    /// <summary>
    /// Event handler that is invoked when the service is stopped, starting and started.
    /// </summary>
    public event EventHandler<EventArgs>? ServiceStatusChanged;

    /// <summary>
    /// Returns true if the Discord Service has been started.
    /// </summary>
    public bool? IsActive { get; set; } = false;

    // Example of DisccordSocketClient use.
    public static async Task<bool> SendToDiscord(OnMessageReceivedArgs eventArgs, DiscordService discordSevice, DataService dataService, TwitchService twitchService, TwitchChatService twitchChatService, MessagingService messagingService, IDbContextFactory<TriggeredDbContext> triggeredDbContextFactory)
    {
        if (eventArgs.ChatMessage.Username.Equals(twitchChatService.UserName, StringComparison.InvariantCultureIgnoreCase) && eventArgs.ChatMessage.Message.StartsWith("From Discord user"))
            return true;

        SocketTextChannel? channel = discordSevice.DiscordSocketClient.GetGuild(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordGuildId")))?
                .GetTextChannel(ulong.Parse(triggeredDbContext.Settings.GetSetting("DiscordSyncTextChannelId")));

        if (channel != null)
        {
            ExpandoObject? user = await TwitchUserUtilities.GetTwitchUser(dataService, twitchService, messagingService, eventArgs.ChatMessage.UserId);

            if (user != null)
            {
                await channel.SendMessageAsync(embed: new EmbedBuilder()
                    .WithUrl($"https://twitch.tv/{twitchService.ChannelName}")
                    .WithColor(Color.DarkTeal)
                    .WithDescription(eventArgs.ChatMessage.Message)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName(eventArgs.ChatMessage.DisplayName)
                        .WithUrl($"https://twitch.tv/{(string)((dynamic)user).Login}")
                        .WithIconUrl((string)((dynamic)user).ProfileImageUrl))
                    .Build());
            }               
        }

        return true;
    }
```

### FileWatcherServie

Coming Soon...

### DataService

The `DataService` instance can you help you persist data, letting you store and retrieve them through a dot-syntax key (i.e "Twitch.Users.MattMckenzy").  The key syntax can be userd to create parent-child relationships with the persisted data and easily retrieve all children of a specific object (i.e. "Twitch.Users" retrieves "Twitch.Users.MattMckenzy" and "Twitch.Users.SirSquad"). 

The following are its useful public properties and an example:
```csharp
    /// <summary>
    /// Saves the given <see cref="ExpandoObject"/> under the given dot-syntax key.
    /// </summary>
    /// <param name="key">Dot-syntax key (i.e. "Twitch.Users.MattMckenzy").</param>
    /// <param name="expandoObject">The <see cref="ExpandoObject"/> to save under the given key.</param>
    public async Task SetObject(string key, ExpandoObject expandoObject) {...}

    /// <summary>
    /// Returns the <see cref="ExpandoObject"/> stored at the given dot-syntax key, or null if it doesn't exist.
    /// </summary>
    /// <param name="key">The dot-syntax key used to retrieve the <see cref="ExpandoObject"/> (i.e. "Twitch.Users.MattMckenzy").</param>
    /// <returns>The <see cref="ExpandoObject"/>, or null if nothing was found at the given key.</returns>
    public async Task<ExpandoObject?> GetObject(string key) {...}

    /// <summary>
    /// Retrieves all <see cref="ExpandoObject"/>s that are children of the given dot-syntax key.
    /// </summary>
    /// <param name="key">The dot-syntax key from which to retrieve children. (i.e. "Twitch.Users" retrieves "Twitch.Users.MattMckenzy" and "Twitch.Users.SirSquad").</param>
    /// <returns>An enumerable of children <see cref="ExpandoObject"/>.</returns>
    public async Task<IEnumerable<ExpandoObject?>> GetChildren(string key) {...}

    /// <summary>
    /// Removes the <see cref="ExpandoObject"/> stored at the given dot-syntax key, or does nothing if it doesn't exist.
    /// </summary>
    /// <param name="key">The dot-syntax key used of the <see cref="ExpandoObject"/> to remove (i.e. "Twitch.Users.MattMckenzy").</param>
    public async Task RemoveObject(string key) {...}

// Example of using the DataService to cache and retrieve a Twitch user
public static async Task<ExpandoObject?> GetTwitchUser(DataService dataService, TwitchService twitchService, MessagingService messagingService, string userId)
{
    ExpandoObject? user = await dataService.GetObject($"Twitch.Users.{userId}");
    if (user == null || ((dynamic)user).Expires < DateTime.Now)
    {
        GetUsersResponse usersResponse = await twitchService.TwitchAPI.Helix.Users.GetUsersAsync(new List<string> { userId });
        User? twitchUser = usersResponse.Users.FirstOrDefault();
        if (twitchUser != null)
        {
            user = new ExpandoObject();
            ((dynamic)user).Expires = DateTime.Now + TimeSpan.FromHours(1);
            ((dynamic)user).Name = twitchUser.DisplayName;
            ((dynamic)user).Id = twitchUser.Id;
            ((dynamic)user).Login = twitchUser.Login;
            ((dynamic)user).ProfileImageUrl = twitchUser.ProfileImageUrl;
            ((dynamic)user).Type = twitchUser.Type;
            await dataService.SetObject($"Twitch.Users.{userId}", user);
        }
        else
        {
            await messagingService.AddMessage("Could not retrieve user from Twitch.", MessageCategory.Module, LogLevel.Error);
            return null;
        }
    }

    return user;
}
```

### QueueService

The `QueueService` instance will be the main way you can interact with Discord. It offers a connected instance of `DiscordSocketClient` which will be your main way of manipulating any Discord entity the connected bot has access to. 

The following are its useful public methods and an example:
```csharp
/// <summary>
/// Adds a delegate function to a new or existing queue denoted by the given queue key.
/// </summary>
/// <param name="queueKey">The unique key describing under which queue the delegate function should be added.</param>
/// <param name="func">The delegate function to queue. It cannot accept arguments and must return Task<bool>. The boolean return will decide whether the queue continues to execute functions (true) or is cancelled and all queue items are cleared (false).</param>
/// <param name="exceptionPreamble">String that begins any exception messages sent to the messaging service when a queued function encounters an unhandled exception.</param>
public async Task Add(string queueKey, Func<Task<bool>> func, string? exceptionPreamble = null) {...}
             
/// <summary>
/// Clears the queue found under the given queue key. If no queue was found, returns without exception.
/// </summary>
/// <param name="queueKey">The unique key describing which queue should be cleared.</param>
public Task Clear(string queueKey) {...}

/// <summary>
/// A list of all active queues and their current queued function counts.
/// </summary>
public Dictionary<string, int> QueueCounts { get { ... } }
                
/// <summary>
/// An event that is invoked whenever queue counts are changed. 
/// </summary>
public event EventHandler? QueueCountsUpdated;

// Example of queue usage. This function queues an OBS scene change under "SceneChange", so that any subsequent scene changes happen after this one is completed. It returns true so that any subsequently queued functions are not cancelled.
public static async Task<bool> ShowCamReward(ChannelPointsCustomRewardRedemptionArgs eventArgs, QueueService queueService, ObsService obsService)
{
    if (!eventArgs.Notification.Event.Reward.Title.Equals("Full Screen Cam", StringComparison.InvariantCultureIgnoreCase))
        return true;

    await queueService.Add("SceneChange", async () =>
    {
        string currentScene = obsService.OBSWebsocket.GetCurrentScene().Name;
        obsService.OBSWebsocket.SetCurrentScene("Full Camera");
        await Task.Delay(10000);
        obsService.OBSWebsocket.SetCurrentScene(currentScene);

        return true;
    });

    return false;
}
````

### ModuleService

The `ModuleService` instance will be the main way you can interact with Discord. It offers a connected instance of `DiscordSocketClient` which will be your main way of manipulating any Discord entity the connected bot has access to. 

The following are its useful public properties and an example:
```csharp
```

### MessagingService

The `MessagingService` instance will be the main way you can interact with Discord. It offers a connected instance of `DiscordSocketClient` which will be your main way of manipulating any Discord entity the connected bot has access to. 

The following are its useful public properties and an example:
```csharp
```

### IDbContextFactory<TriggeredDbContext>

The `IDbContextFactory<TriggeredDbContext>` instance will be the main way you can interact with Discord. It offers a connected instance of `DiscordSocketClient` which will be your main way of manipulating any Discord entity the connected bot has access to. 

The following are its useful public properties and an example:
```csharp
```

### MemoryCache

The `MemoryCache` instance will provides an easy way to store and retrieve any object, as bytes, that last for the duration of the service's runtime. MemoryCache documentation and examples can be seen here: https://docs.microsoft.com/en-us/dotnet/api/system.runtime.caching.memorycache?view=dotnet-plat-ext-6.0

## Testing Center Page

Coming soon...

## Data Page

Coming soon...

## Configuration Page

Coming soon...

## README Page

Coming soon...

# Module Maker

Coming soon...

# Proxy

Coming soon...

# Linux and Containerization.

The service is cross-platform and can be run on Linux. Simply make sure you turn off certificate generation as well as configure the IPAccessList to be able to connect through the containers.

# Donate

If you appreciate my work and feel like helping me realize other projects, you can donate at <a href="https://paypal.me/MattMckenzy">https://paypal.me/MattMckenzy</a>!
