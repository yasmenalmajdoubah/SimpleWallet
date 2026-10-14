FROM node:24-bookworm-slim AS web
WORKDIR /src/client/wallet-web
COPY client/wallet-web/package.json client/wallet-web/package-lock.json ./
RUN npm ci
COPY client/wallet-web/ ./
RUN npx ng build --configuration production

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ ./src/
RUN dotnet publish src/SimpleWallet.Api/SimpleWallet.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=web /src/client/wallet-web/dist/wallet-web/browser ./wwwroot
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "SimpleWallet.Api.dll"]
