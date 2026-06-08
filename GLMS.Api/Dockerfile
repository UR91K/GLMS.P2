FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GLMS.Api/GLMS.Api.csproj", "GLMS.Api/"]
COPY ["GLMS.Shared/GLMS.Shared.csproj", "GLMS.Shared/"]
RUN dotnet restore "GLMS.Api/GLMS.Api.csproj"
COPY . .
RUN dotnet publish "GLMS.Api/GLMS.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GLMS.Api.dll"]
