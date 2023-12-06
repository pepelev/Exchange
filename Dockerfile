# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk as build
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
WORKDIR /repo
COPY . .
RUN ["dotnet", "publish", "src/Exchange/Exchange.csproj", "--output", "/output", "--runtime", "linux-x64"]


FROM mcr.microsoft.com/dotnet/sdk
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
ENV ASPNETCORE_HTTP_PORTS=5000
WORKDIR /app
COPY --from=build /output .
CMD ["dotnet", "Exchange.dll"]