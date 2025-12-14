# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy file csproj và restore các thư viện
COPY *.csproj ./
RUN dotnet restore

# Copy toàn bộ code và build
COPY . ./
RUN dotnet publish -c Release -o out

# 2. Run Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Thiết lập biến môi trường để ứng dụng chạy trên port mà Render cung cấp
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "OrdersManager.dll"] 
# LƯU Ý: Thay "GoogleSheetOMS.dll" bằng tên project của bạn nếu khác (xem trong bin/Debug/...)