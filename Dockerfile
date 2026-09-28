# Compila y publica la aplicación en modo Release.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["UniLink.csproj", "./"]
RUN dotnet restore "UniLink.csproj"
COPY . .
RUN dotnet publish "UniLink.csproj" -c Release -o /app/publish --no-restore

# Ejecuta la aplicación con el runtime de ASP.NET Core.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "UniLink.dll"]
