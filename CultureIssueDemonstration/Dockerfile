# Stage 1 (Build)
FROM microsoft/dotnet:2.1-sdk as build
WORKDIR /app
COPY . . 
RUN dotnet publish --output /app
COPY . .
ENTRYPOINT ["dotnet", "CultureIssueDemonstration.dll"]