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
- **Modules**: page used to create and maintain modules that will be triggered by events.
- **Utilities**: page used to create and maintain reusable code that can be consumed from any module.
- **Testing Center**: page used to create, maintain and execute tests on any event supported by the service.
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

TwitchService
TwitchChatService
ObsService
DiscordService
DataService
QueueService
MemoryCache
ModuleService
MessagingService
IDbContextFactory<TriggeredDbContext>  

## Testing Center Page

Coming soon...

## Configuration Page

Coming soon...

## README Page

Coming soon...

# Module Maker

# Proxy

Coming soon...

# Donate

If you appreciate my work and feel like helping me realize other projects, you can donate at <a href="https://paypal.me/MattMckenzy">https://paypal.me/MattMckenzy</a>!
