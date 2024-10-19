# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Api/Wireguard.Api/Wireguard.Api.csproj", "Services/Api/Wireguard.Api/"]
COPY ["BuildingBlocks/EventBus.Messages/EventBus.Messages.csproj", "EventBus.Messages/"]

RUN dotnet restore "./Services/Api/Wireguard.Api/Wireguard.Api.csproj"
COPY . .

WORKDIR "/src/Services/Api/Wireguard.Api"
RUN dotnet build "./Wireguard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
    
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Wireguard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

RUN apt-get update && \
    apt-get install -y iproute2 wireguard wireguard-tools iptables
#    apt-get clean && \
#    rm -rf /var/lib/apt/lists/*

#RUN apt upgrade -y && \
#sudo modprobe ip_gre && \
#lsmod | grep gre \

ENTRYPOINT ["/bin/bash", "-c", "iptables -F && \
      dotnet Wireguard.Api.dll && \
      chmod +x /etc/rc.local && \
      bash /etc/rc.local"]
      
      