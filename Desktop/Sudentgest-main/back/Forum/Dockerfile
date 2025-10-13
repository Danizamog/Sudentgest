# Usar la imagen oficial de .NET 10 SDK para build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar csproj(s) y restaurar para aprovechar cache de capas
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Escuchar en todos los interfaces en el puerto 5006
ENV ASPNETCORE_URLS=http://+:5006
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5006

# Copiar artefactos publicados
COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Forum.dll"]