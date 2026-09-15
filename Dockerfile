FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/Woodpecker.Api/Woodpecker.Api.csproj", "src/Woodpecker.Api/"]
COPY ["src/Woodpecker.Application/Woodpecker.Application.csproj", "src/Woodpecker.Application/"]
COPY ["src/Woodpecker.Domain/Woodpecker.Domain.csproj", "src/Woodpecker.Domain/"]
COPY ["src/Woodpecker.Infrastructure/Woodpecker.Infrastructure.csproj", "src/Woodpecker.Infrastructure/"]
RUN dotnet restore "src/Woodpecker.Api/Woodpecker.Api.csproj"

COPY src/ src/
RUN dotnet publish "src/Woodpecker.Api/Woodpecker.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Woodpecker.Api.dll"]
