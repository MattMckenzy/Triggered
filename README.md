# Town Bulletin

<img align="left" height="130" src="https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/TownBulletin/wwwroot/assets/TownBulletin.ico">

<br />
<br />
React to Twitch and OBS events with dynamically compiled C# modules!

<br />
<br />
<br />

## Description

This application will provide a feature-rich, infinitely extendable way to react to any Twitch or OBS event offered by the platforms. 

At its core, it helpls you create, maintain and execute C# modules triggered to run on selected events. The modules are compiled dynamically during runtime and don't even require a web-page refresh to enable!

The web application also features a testing center, which can be used to mock event data and trigger all ready and compiled moodules created for that event. 

To aid in the creation and management of modules, here are some of the useful features that are included:
- Built-in, rich C# code editor with instant code analysis feedback. 
- Ability to load external dll references.
- A dynamic data service to store and retrieve persistant information.
- Extendable module templates.

To interact with Twitch and OBS, authentication and authorization credentials must be entered in its settings, which can be updated in the web appllication's configuration page.

## Installation

To install, simply download the TownBulletin-Launcher.exe file available here, and launch it. It will handle downloading and updating the web service, and give you a  window displaying its console log as well as give you a tray icon that can be used to launch the website or shut down the web service.

The web application is coded platform-agnostic, so it will work on linux. The launcher however, is Windows only. For more instructions on getting this to run on Linux, please send me a message and I'll gladly help out!

## Initial Setup

Before launching and using the Town Bulletin web application, **PLEASE READ THE FOLLOWING**:
- **IMPORTANT**: secure the app by randomly changing the following two keys found in appsettings.json:
  - The certificate password: used to secure the generated self-signed certificate used for HTTPS communication.
  - The database encryption key: used to secure very sensitive information such as client secrets and access tokens.
- Optional: to receive Twitch EventSub webhook triggers, you must forward port 443 on your network to the system running Town Bulletin.

The appsettings.json file has a few other possible configurations:
- **"GenerateCertificate"**: *(Default: true)* Boolean: true to generate a self-signed certificate, false if you want to configure your own.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Url"**: *(Default: "https://+:443")* Permits to set the service listening URL, make sure its listening on external 443 for Twitch EventSub Webhook.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Path"**: *(Default "SelfSigned.pfx")* The path to the HTTPS certificate to use, or location to generate the self-signed one.
- **"Kestrel:Endpoints:HttpsInlineCertFile:Certificate:Password"**: The password used to generate the certificate, please make this truly random and secure!
- **"IPAccessList"**: *(Default: "127.0.0.1;::1;::ffff:127.0.0.1")* A semicolon-seperated list of IP addresses that are allowed to use the TownBulletin web application. Used to keep it secure and disallow external intruders!
- **"EncryptionKey"**: The password used to generate to encrypt sensitive information in the database, please make this truly random and secure!

<kbd><img align="center" height="500" src="https://raw.githubusercontent.com/MattMckenzy/TownBulletin/main/Resources/appsettings.png"></kbd>

## Usage

### Launcher

Coming soon...

### Home Page

Coming soon...

### Modules Page

Coming soon...

### Code Editor

Coming soon...

### Testing Center Page

Coming soon...

### Configuration Page

Coming soon...

### README Page

Coming soon...

# Donate

If you appreciate my work and feel like helping me realize other projects, you can donate at <a href="https://paypal.me/MattMckenzy">https://paypal.me/MattMckenzy</a>!
