#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BASE_IMAGE=mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim-arm32v7
FROM ${BASE_IMAGE} AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
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
RUN dotnet build "ShopAndEat.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ShopAndEat.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ShopAndEat.dll"]