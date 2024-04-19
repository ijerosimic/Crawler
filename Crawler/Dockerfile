FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /Crawler

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /Crawler
COPY --from=build-env /Crawler/out .
ENTRYPOINT ["dotnet", "Crawler.dll"]