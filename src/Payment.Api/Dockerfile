FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["Directory.Build.props", "./"]
COPY ["Booking.sln", "./"]
COPY ["src/Payment.Api/Payment.Api.csproj", "src/Payment.Api/"]
COPY ["src/Payment.Application/Payment.Application.csproj", "src/Payment.Application/"]
COPY ["src/Payment.Domain/Payment.Domain.csproj", "src/Payment.Domain/"]
COPY ["src/Payment.Infrastructure/Payment.Infrastructure.csproj", "src/Payment.Infrastructure/"]
RUN dotnet restore "src/Payment.Api/Payment.Api.csproj"
COPY src/ src/
RUN dotnet publish "src/Payment.Api/Payment.Api.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payment.Api.dll"]
