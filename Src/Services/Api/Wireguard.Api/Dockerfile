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
    apt-get install -y iproute2 wireguard wireguard-tools iptables && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

 # Create necessary directories

ENTRYPOINT ["/bin/bash", "-c", "iptables -F && \
      dotnet Wireguard.Api.dll"]
      
      