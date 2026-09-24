FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/PicPay.Domain/PicPay.Domain.csproj src/PicPay.Domain/
COPY src/PicPay.Application/PicPay.Application.csproj src/PicPay.Application/
COPY src/PicPay.Infrastructure/PicPay.Infrastructure.csproj src/PicPay.Infrastructure/
COPY src/PicPay.Api/PicPay.Api.csproj src/PicPay.Api/
RUN dotnet restore src/PicPay.Api/PicPay.Api.csproj

COPY src/ src/
RUN dotnet publish src/PicPay.Api/PicPay.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "PicPay.Api.dll"]
