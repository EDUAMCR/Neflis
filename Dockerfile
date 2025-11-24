# Imagen base para ASP.NET Core (runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Imagen para compilar el proyecto
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiamos todo el código
COPY . .

# Restaura paquetes y publica en modo Release
RUN dotnet restore "./Neflis.csproj"
RUN dotnet publish "./Neflis.csproj" -c Release -o /app/publish

# Imagen final
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Ejecutar la app
ENTRYPOINT ["dotnet", "Neflis.dll"]
