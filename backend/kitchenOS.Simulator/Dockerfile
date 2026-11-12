FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src

COPY ["kitchenOS_Scada.Contract/kitchenOS_Scada.Contract.csproj", "kitchenOS_Scada.Contract/"]
COPY ["kitchenOS.Simulator/kitchenOS.Simulator.csproj", "kitchenOS.Simulator/"]
RUN dotnet restore "kitchenOS.Simulator/kitchenOS.Simulator.csproj"

COPY kitchenOS_Scada.Contract/ kitchenOS_Scada.Contract/
COPY kitchenOS.Simulator/ kitchenOS.Simulator/

WORKDIR /src/kitchenOS.Simulator
CMD ["dotnet", "watch", "run", "--no-launch-profile"]
