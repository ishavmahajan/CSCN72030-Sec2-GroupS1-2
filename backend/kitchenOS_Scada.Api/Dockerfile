# ==========================================
# Development stage
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS development

WORKDIR /app

COPY ["kitchenOS_Scada.Api.csproj", "./"]

RUN dotnet restore "kitchenOS_Scada.Api.csproj"

COPY . .

EXPOSE 8080

CMD ["dotnet", "watch", "run", "--no-launch-profile"]


# ==========================================
# Build stage
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["kitchenOS_Scada.Api.csproj", "./"]

RUN dotnet restore "kitchenOS_Scada.Api.csproj"

COPY . .

RUN dotnet publish \
    "kitchenOS_Scada.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


# ==========================================
# Production stage
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS production

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "kitchenOS_Scada.Api.dll"]