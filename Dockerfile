FROM mcr.microsoft.com/dotnet/sdk:8.0 AS publish
WORKDIR /app
COPY . .
RUN dotnet publish "src/HtmlTemplateEngineExample.sln" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENTRYPOINT dotnet HtmlTemplateEngineExample.dll templateFilePath="data/template.html" dataFilePath="data/data.json" outputFilePath="data/output.html"