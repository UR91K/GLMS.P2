FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["GLMS.Web/GLMS.Web.csproj", "GLMS.Web/"]
COPY ["GLMS.Shared/GLMS.Shared.csproj", "GLMS.Shared/"]
RUN dotnet restore "GLMS.Web/GLMS.Web.csproj"
COPY . .
RUN dotnet publish "GLMS.Web/GLMS.Web.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "GLMS.Web.dll"]
