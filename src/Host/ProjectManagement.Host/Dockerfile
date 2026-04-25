FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ProjectManagement.slnx ./
COPY src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj src/Host/ProjectManagement.Host/
COPY src/Modules/Auth/ProjectManagement.Auth.Api/ProjectManagement.Auth.Api.csproj src/Modules/Auth/ProjectManagement.Auth.Api/
COPY src/Modules/Auth/ProjectManagement.Auth.Application/ProjectManagement.Auth.Application.csproj src/Modules/Auth/ProjectManagement.Auth.Application/
COPY src/Modules/Auth/ProjectManagement.Auth.Domain/ProjectManagement.Auth.Domain.csproj src/Modules/Auth/ProjectManagement.Auth.Domain/
COPY src/Modules/Auth/ProjectManagement.Auth.Infrastructure/ProjectManagement.Auth.Infrastructure.csproj src/Modules/Auth/ProjectManagement.Auth.Infrastructure/
COPY src/Shared/ProjectManagement.Shared.Domain/ProjectManagement.Shared.Domain.csproj src/Shared/ProjectManagement.Shared.Domain/
COPY src/Shared/ProjectManagement.Shared.Infrastructure/ProjectManagement.Shared.Infrastructure.csproj src/Shared/ProjectManagement.Shared.Infrastructure/
RUN dotnet restore src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj

COPY src/ src/
RUN dotnet publish src/Host/ProjectManagement.Host/ProjectManagement.Host.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "ProjectManagement.Host.dll"]

