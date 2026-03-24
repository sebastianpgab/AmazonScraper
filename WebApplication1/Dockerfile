FROM mcr.microsoft.com/playwright/dotnet:v1.41.0-jammy

WORKDIR /app

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o out

WORKDIR /app/out

CMD ["dotnet", "WebApplication1.dll"]
