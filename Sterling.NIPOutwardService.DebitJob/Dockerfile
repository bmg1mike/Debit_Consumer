FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
COPY ["Sterling.NIPOutwardService.DebitJob/SSMKeys/public.key", "/app/SSMKeys/"]
COPY ["Sterling.NIPOutwardService.DebitJob/SSMKeys/private.key", "/app/SSMKeys/"]

#ENV ASPNETCORE_ENVIRONMENT="Development"

# Creates a non-root user with an explicit UID and adds permission to access the /app folder
# For more info, please refer to https://aka.ms/vscode-docker-dotnet-configure-containers
#RUN adduser -u 5678 --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Sterling.NIPOutwardService.DebitJob/Sterling.NIPOutwardService.DebitJob.csproj", "Sterling.NIPOutwardService.DebitJob/"]
COPY ["Sterling.NIPOutwardService.Data/Sterling.NIPOutwardService.Data.csproj", "Sterling.NIPOutwardService.Data/"]
COPY ["Sterling.NIPOutwardService.Domain/Sterling.NIPOutwardService.Domain.csproj", "Sterling.NIPOutwardService.Domain/"]
COPY ["Sterling.NIPOutwardService.Service/Sterling.NIPOutwardService.Service.csproj", "Sterling.NIPOutwardService.Service/"]
RUN dotnet restore "Sterling.NIPOutwardService.DebitJob/Sterling.NIPOutwardService.DebitJob.csproj"
COPY . .
WORKDIR "/src/Sterling.NIPOutwardService.DebitJob"
RUN dotnet build "Sterling.NIPOutwardService.DebitJob.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sterling.NIPOutwardService.DebitJob.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sterling.NIPOutwardService.DebitJob.dll"]
