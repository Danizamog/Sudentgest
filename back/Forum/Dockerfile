FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Forum.csproj", "."]
RUN dotnet restore "Forum.csproj"
COPY . .
RUN dotnet build "Forum.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Forum.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Forum.dll"]