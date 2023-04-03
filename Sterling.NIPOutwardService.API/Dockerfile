FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 5000
COPY ["Sterling.NIPOutwardService.API/SSMKeys/public.key", "/app/SSMKeys/"]
COPY ["Sterling.NIPOutwardService.API/SSMKeys/private.key", "/app/SSMKeys/"]

ENV ASPNETCORE_ENVIRONMENT="Development"

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Sterling.NIPOutwardService.API/Sterling.NIPOutwardService.API.csproj", "Sterling.NIPOutwardService.API/"]
COPY ["Sterling.NIPOutwardService.Data/Sterling.NIPOutwardService.Data.csproj", "Sterling.NIPOutwardService.Data/"]
COPY ["Sterling.NIPOutwardService.Domain/Sterling.NIPOutwardService.Domain.csproj", "Sterling.NIPOutwardService.Domain/"]
COPY ["Sterling.NIPOutwardService.Service/Sterling.NIPOutwardService.Service.csproj", "Sterling.NIPOutwardService.Service/"]
RUN dotnet restore "Sterling.NIPOutwardService.API/Sterling.NIPOutwardService.API.csproj"
COPY . .
WORKDIR "/src/Sterling.NIPOutwardService.API"
RUN dotnet build "Sterling.NIPOutwardService.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sterling.NIPOutwardService.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sterling.NIPOutwardService.API.dll"]
