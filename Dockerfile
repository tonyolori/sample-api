#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER app
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SampleAPI.csproj", "."]
RUN dotnet restore "./././SampleAPI.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "./SampleAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build --secret id=secret,src=/etc/secrets/secrets.json .

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SampleAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false --mount=type=secret,id=secrets.json,dst=/etc/secrets/secrets.json

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SampleAPI.dll"]