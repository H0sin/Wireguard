# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000


RUN apt-get update && \
    apt-get install -y iproute2 wireguard wireguard-tools curl unzip nano iptables && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

\        
# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/Api/Wireguard.Api/Wireguard.Api.csproj", "Services/Api/Wireguard.Api/"]
RUN dotnet restore "./Services/Api/Wireguard.Api/Wireguard.Api.csproj"
COPY . .
WORKDIR "/src/Services/Api/Wireguard.Api"
RUN dotnet build "./Wireguard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build
    
# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Wireguard.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Wireguard.Api.dll"]

RUN mkdir -p /usr/local/etc/v2ray && mkdir -p /usr/local/etc/v2ray && cd /usr/local/share/v2ray && \
    curl -L -o v2ray.zip https://github.com/v2fly/v2ray-core/releases/latest/download/v2ray-linux-64.zip \
    && unzip v2ray.zip \
    && rm -r v2ray.zip 

COPY ["Services/Api/Wireguard.Api/config.template.json","/usr/local/etc/v2ray/config.json"]

RUN -config /usr/local/etc/v2ray/config.json & 

RUN iptables -F && \
    iptables -t nat -F && \
    iptables -X && \

RUN iptables -t nat -A PREROUTING -i wg0 -p tcp -j REDIRECT --to-ports 10808 && \
    iptables -t nat -A PREROUTING -i wg0 -p udp -j REDIRECT --to-ports 10808

RUN echo 1 > /proc/sys/net/ipv4/ip_forward

RUN iptables -t nat -A POSTROUTING -o ens192 -j MASQUERADE

RUN iptables -A FORWARD -i wg0 -o ens192 -j ACCEPT

RUN iptables -A FORWARD -i ens192 -o wg0 -m state --state RELATED,ESTABLISHED -j ACCEPT