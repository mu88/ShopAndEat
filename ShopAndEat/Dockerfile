#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BASE_IMAGE=mcr.microsoft.com/dotnet/aspnet:7.0.1-bullseye-slim-arm64v8
FROM ${BASE_IMAGE} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0.101 AS build
WORKDIR /src
COPY ["ShopAndEat/ShopAndEat.csproj", "ShopAndEat/"]
COPY ["BizDbAccess/BizDbAccess.csproj", "BizDbAccess/"]
COPY ["DataLayer/DataLayer.csproj", "DataLayer/"]
COPY ["ServiceLayer/ServiceLayer.csproj", "ServiceLayer/"]
COPY ["DTO/DTO.csproj", "DTO/"]
COPY ["BizLogic/BizLogic.csproj", "BizLogic/"]

RUN dotnet restore "ShopAndEat/ShopAndEat.csproj"
COPY . .
WORKDIR "/src/ShopAndEat"
RUN dotnet build "ShopAndEat.csproj" -c Release

FROM build AS publish
RUN dotnet publish "ShopAndEat.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ShopAndEat.dll"]
