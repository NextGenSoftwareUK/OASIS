#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["NextGenSoftware.OASIS.API.ONODE.WebApp/NextGenSoftware.OASIS.API.ONODE.WebApp.csproj", "NextGenSoftware.OASIS.API.ONODE.WebApp/"]
RUN dotnet restore "NextGenSoftware.OASIS.API.ONODE.WebApp/NextGenSoftware.OASIS.API.ONODE.WebApp.csproj"
COPY . .
WORKDIR "/src/NextGenSoftware.OASIS.API.ONODE.WebApp"
RUN dotnet build "NextGenSoftware.OASIS.API.ONODE.WebApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NextGenSoftware.OASIS.API.ONODE.WebApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NextGenSoftware.OASIS.API.ONODE.WebApp.dll"]