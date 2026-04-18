FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props NuGet.config ./
COPY Fortuno.sln ./
COPY Fortuno.API/Fortuno.API.csproj Fortuno.API/
COPY Fortuno.Application/Fortuno.Application.csproj Fortuno.Application/
COPY Fortuno.Domain/Fortuno.Domain.csproj Fortuno.Domain/
COPY Fortuno.DTO/Fortuno.DTO.csproj Fortuno.DTO/
COPY Fortuno.GraphQL/Fortuno.GraphQL.csproj Fortuno.GraphQL/
COPY Fortuno.Infra/Fortuno.Infra.csproj Fortuno.Infra/
COPY Fortuno.Infra.Interfaces/Fortuno.Infra.Interfaces.csproj Fortuno.Infra.Interfaces/

RUN dotnet restore Fortuno.API/Fortuno.API.csproj

COPY . .
RUN dotnet publish Fortuno.API/Fortuno.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Fortuno.API.dll"]
