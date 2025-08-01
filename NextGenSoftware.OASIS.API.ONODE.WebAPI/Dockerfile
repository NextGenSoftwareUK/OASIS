#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
#COPY ["README.md", ""]
COPY ["NextGenSoftware.Logging/NextGenSoftware.Logging.csproj", "NextGenSoftware.Logging/"]
COPY ["NextGenSoftware.Logging.NLog/NextGenSoftware.Logging.NLog.csproj", "NextGenSoftware.Logging.NLog/"]
COPY ["NextGenSoftware.Utilities/NextGenSoftware.Utilities.csproj", "NextGenSoftware.Utilities/"]
COPY ["NextGenSoftware.CLI.Engine/NextGenSoftware.CLI.Engine.csproj", "NextGenSoftware.CLI.Engine/"]
COPY ["NextGenSoftware.ErrorHandling/NextGenSoftware.ErrorHandling.csproj", "NextGenSoftware.ErrorHandling/"]
COPY ["NextGenSoftware.Holochain.HoloNET.Client/NextGenSoftware.Holochain.HoloNET.Client.csproj", "NextGenSoftware.Holochain.HoloNET.Client/"]
COPY ["NextGenSoftware.Holochain.HoloNET.ORM/NextGenSoftware.Holochain.HoloNET.ORM.csproj", "NextGenSoftware.Holochain.HoloNET.ORM/"]
COPY ["NextGenSoftware.WebSocket/NextGenSoftware.WebSocket.csproj", "NextGenSoftware.WebSocket/"]
COPY ["External Libs/net-ipfs-http-client-master/src/IpfsHttpClient.csproj", "External Libs/net-ipfs-http-client-master/src/"]
COPY ["NextGenSoftware.OASIS.Common/NextGenSoftware.OASIS.Common.csproj", "NextGenSoftware.OASIS.Common/"]
COPY ["NextGenSoftware.OASIS.API.Core/NextGenSoftware.OASIS.API.Core.csproj", "NextGenSoftware.OASIS.API.Core/"]
COPY ["NextGenSoftware.OASIS.API.Core.Apollo.Server/NextGenSoftware.OASIS.API.Core.Apollo.Server.csproj", "NextGenSoftware.OASIS.API.Core.Apollo.Server/"]
COPY ["NextGenSoftware.OASIS.API.DNA/NextGenSoftware.OASIS.API.DNA.csproj", "NextGenSoftware.OASIS.API.DNA/"]
COPY ["NextGenSoftware.OASIS.OASISBootLoader/NextGenSoftware.OASIS.OASISBootLoader.csproj", "NextGenSoftware.OASIS.OASISBootLoader/"]
COPY ["NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj", "NextGenSoftware.OASIS.API.ONODE.WebAPI/"]
COPY ["NextGenSoftware.OASIS.API.ONODE.Core/NextGenSoftware.OASIS.API.ONODE.Core.csproj", "NextGenSoftware.OASIS.API.ONODE.Core/"]
COPY ["NextGenSoftware.OASIS.API.Providers.HoloOASIS/NextGenSoftware.OASIS.API.Providers.HoloOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.HoloOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.IPFSOASIS/NextGenSoftware.OASIS.API.Providers.IPFSOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.IPFSOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/NextGenSoftware.OASIS.API.Providers.SOLIDOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.SOLIDOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.BlockStackOASIS/NextGenSoftware.OASIS.API.Providers.BlockStackOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.BlockStackOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.CargoOASIS/NextGenSoftware.OASIS.API.Providers.CargoOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.CargoOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.SEEDSOASIS/NextGenSoftware.OASIS.API.Providers.SEEDSOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.SEEDSOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/NextGenSoftware.OASIS.API.Providers.EOSIOOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.EOSIOOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.TelosOASIS/NextGenSoftware.OASIS.API.Providers.TelosOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.TelosOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.HoloWeb/NextGenSoftware.OASIS.API.Providers.HoloWebOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.HoloWeb/"]
COPY ["NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity/NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity.csproj", "NextGenSoftware.OASIS.API.Providers.HoloOASIS.Unity/"]
COPY ["NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS/NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.ActivityPubOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.Neo4jOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.TRONOASIS/NextGenSoftware.OASIS.API.Providers.TRONOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.TRONOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/NextGenSoftware.OASIS.API.Providers.HashgraphOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.HashgraphOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.PLANOASIS/NextGenSoftware.OASIS.API.Providers.PLANOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.PLANOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.EthereumOASIS/NextGenSoftware.OASIS.API.Providers.EthereumOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.EthereumOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS/NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.ScuttlebuttOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.SOLANAOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS/NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS/NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.ChainLinkOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.MongoOASIS/NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.MongoOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/NextGenSoftware.OASIS.API.Providers.LocalFileOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.LocalFileOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura/NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura.csproj", "NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.Aura/"]
COPY ["NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.ArbitrumOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.PolygonOASIS/NextGenSoftware.OASIS.API.Providers.PolygonOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.PolygonOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.RootstockOASIS/NextGenSoftware.OASIS.API.Providers.RootstockOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.RootstockOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS/"]
COPY ["NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.csproj", "NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS/"]


RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj"
COPY . .
WORKDIR "/src/NextGenSoftware.OASIS.API.ONODE.WebAPI"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebAPI.dll"]