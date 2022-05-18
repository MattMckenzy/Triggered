#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Triggered/Triggered.csproj", "Triggered/"]
COPY ["Discord.Net/src/Discord.Net.Interactions/Discord.Net.Interactions.csproj", "Discord.Net/src/Discord.Net.Interactions/"]
COPY ["Discord.Net/src/Discord.Net.Core/Discord.Net.Core.csproj", "Discord.Net/src/Discord.Net.Core/"]
COPY ["Discord.Net/src/Discord.Net.Rest/Discord.Net.Rest.csproj", "Discord.Net/src/Discord.Net.Rest/"]
COPY ["Discord.Net/src/Discord.Net.WebSocket/Discord.Net.WebSocket.csproj", "Discord.Net/src/Discord.Net.WebSocket/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api/TwitchLib.Api.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Helix/TwitchLib.Api.Helix.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Helix/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Helix.Models/TwitchLib.Api.Helix.Models.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Helix.Models/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Interfaces/TwitchLib.Api.Core.Interfaces.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Interfaces/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Enums/TwitchLib.Api.Core.Enums.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Enums/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Core/TwitchLib.Api.Core.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Core/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Models/TwitchLib.Api.Core.Models.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.Core.Models/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.V5/TwitchLib.Api.V5.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.V5/"]
COPY ["TwitchLib/TwitchLib.Api/TwitchLib.Api.V5.Models/TwitchLib.Api.V5.Models.csproj", "TwitchLib/TwitchLib.Api/TwitchLib.Api.V5.Models/"]
COPY ["obs-websocket-dotnet/obs-websocket-dotnet/obs-websocket-dotnet.csproj", "obs-websocket-dotnet/obs-websocket-dotnet/"]
COPY ["TwitchLib.EventSub.Webhooks/TwitchLib.EventSub.Webhooks/TwitchLib.EventSub.Webhooks.csproj", "TwitchLib.EventSub.Webhooks/TwitchLib.EventSub.Webhooks/"]
COPY ["Discord.Net/src/Discord.Net.Commands/Discord.Net.Commands.csproj", "Discord.Net/src/Discord.Net.Commands/"]
COPY ["TwitchLib/TwitchLib.Client/TwitchLib.Client/TwitchLib.Client.csproj", "TwitchLib/TwitchLib.Client/TwitchLib.Client/"]
COPY ["TwitchLib/TwitchLib.Client/TwitchLib.Client.Models/TwitchLib.Client.Models.csproj", "TwitchLib/TwitchLib.Client/TwitchLib.Client.Models/"]
COPY ["TwitchLib/TwitchLib.Client/TwitchLib.Client.Enums/TwitchLib.Client.Enums.csproj", "TwitchLib/TwitchLib.Client/TwitchLib.Client.Enums/"]
COPY ["Discord.Net/src/Discord.Net.Webhook/Discord.Net.Webhook.csproj", "Discord.Net/src/Discord.Net.Webhook/"]
COPY ["TwitchLib/TwitchLib.PubSub/TwitchLib.PubSub/TwitchLib.PubSub.csproj", "TwitchLib/TwitchLib.PubSub/TwitchLib.PubSub/"]
RUN dotnet restore "Triggered/Triggered.csproj"
COPY . .
WORKDIR "/src/Triggered"
RUN dotnet build "Triggered.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Triggered.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Triggered.dll"]