FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /app



# Copiar csproj y restaurar

COPY *.csproj ./

RUN dotnet restore



# Copiar todo y compilar

COPY . ./

RUN dotnet publish -c Release -o out



# Etapa final con runtime

FROM mcr.microsoft.com/dotnet/aspnet:9.0

WORKDIR /app

COPY --from=build /app/out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Roles.dll"]