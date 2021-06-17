FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env
WORKDIR /app

COPY ./repeater.csproj ./repeater/
RUN dotnet restore repeater

# Copy everything else and build
COPY . ./
RUN dotnet publish repeater.csproj \
    -c Release \
    -r linux-x64 \
    -o out \
    --self-contained

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
RUN apt-get update && apt-get install -y libc6-dev
COPY --from=build-env /app/out .

VOLUME /data

ENTRYPOINT ["dotnet", "repeater.dll"]
