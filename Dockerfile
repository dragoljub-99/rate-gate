FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY RateGate.sln ./
COPY RateGate.Api/RateGate.Api.csproj RateGate.Api/
COPY RateGate.Domain/RateGate.Domain.csproj RateGate.Domain/
COPY RateGate.Infrastructure/RateGate.Infrastructure.csproj RateGate.Infrastructure/
COPY RateGate.ConsoleDemo/RateGate.ConsoleDemo.csproj RateGate.ConsoleDemo/

RUN dotnet restore RateGate.sln

COPY . .

RUN dotnet publish RateGate.Api -c Release -o /out/api --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

COPY --from=build /out/api ./

EXPOSE 5000
ENTRYPOINT ["dotnet", "RateGate.Api.dll"]
