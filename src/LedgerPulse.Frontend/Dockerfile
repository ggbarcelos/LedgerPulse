FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/LedgerPulse.Frontend/LedgerPulse.Frontend.csproj -c Release -o /app/publish

FROM nginx:alpine AS final
COPY src/LedgerPulse.Frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY src/LedgerPulse.Frontend/wait-for-api.sh /docker-entrypoint.d/30-wait-for-api.sh
RUN chmod +x /docker-entrypoint.d/30-wait-for-api.sh
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
EXPOSE 80
