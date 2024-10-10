# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER root
WORKDIR /app
EXPOSE 8000
ENV ASPNETCORE_URLS=http://+:8000

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

# Install necessary packages
RUN apt-get update && \
    apt-get install -y iproute2 wireguard wireguard-tools curl unzip nano iptables wget && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*


 # Create necessary directories
RUN mkdir -p /usr/local/etc/v2ray
RUN mkdir -p /usr/local/share/v2ray

RUN cd /usr/local/share/v2ray && \
     wget https://github.com/XTLS/Xray-core/releases/download/v24.9.30/Xray-linux-64.zip && \
     unzip Xray-linux-64.zip && \
     rm Xray-linux-64.zip

COPY ["./Services/Api/Wireguard.Api/config.json", "/usr/local/etc/v2ray/"]

ENTRYPOINT ["/bin/bash", "-c", "iptables -F && \
      iptables -t nat -F && \
      iptables -X && \
      iptables -t nat -A PREROUTING -i wg0 -p tcp -j REDIRECT --to-ports 10808 && \
      iptables -t nat -A PREROUTING -i wg0 -p udp -j REDIRECT --to-ports 10808 && \
      echo 1 > /proc/sys/net/ipv4/ip_forward && \
      iptables -t nat -A POSTROUTING -o ens192 -j MASQUERADE && \
      iptables -A FORWARD -i wg0 -o ens192 -j ACCEPT && \
      iptables -A FORWARD -i ens192 -o wg0 -m state --state RELATED,ESTABLISHED -j ACCEPT && \
      /usr/local/share/v2ray/xray run -config /usr/local/etc/v2ray/config.json & \
      dotnet Wireguard.Api.dll"]