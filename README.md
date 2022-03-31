# Town Bulletin

<img align="left" height="100" src="TownBulletin/wwwroot/assets/TownBulletin.ico">

<br />
This web application will assist in automating Twitch and OBS events, through the means of dynamically compiled C# modules.

<br />
<br />

## Description

This application will provide a simple, mobile-friendly means to keep track of any number of cash accounts. It supports automatic-reccurring transactions, net worth, balance history, balance history chart and claimable bounties.

The idea is to create seperate cash accounts for each category of spending you want to budget. No dealing with setting a category for each transaction and you can quickly see how much money you have left to spend in each account. 

You can also use cash accounts to keep track of Net Worth by adding any non-budgeting accounts as well.

All configuration is done via the web app by clicking on the pencil icon in the top-right.

## Installation

You can self-host it on any server, as it is cross-platform, but I designed it with Docker in mind. See below for an example Docker configuration.

As an alternative, I've added a "Release" folder at root that contains the published website. You can use this to host the website directly on any server or system without the use of Docker. You can configure the contained "Casheesh.bat" file to modify the port or culture that Casheesh will use. Make sure you also install .NET 5.0 runtime, it's necessary to have it work! You can find it here: https://dotnet.microsoft.com/download.

# Donate

If you appreciate my work and feel like helping me realize other projects, you can donate at <a href="https://paypal.me/MattMckenzy">https://paypal.me/MattMckenzy</a>!