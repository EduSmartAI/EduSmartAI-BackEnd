FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy và restore chỉ project files trước (layer caching tốt hơn)
COPY Services/QuizService/QuizService.Domain/*.csproj Services/QuizService/QuizService.Domain/
COPY Services/QuizService/QuizService.Application/*.csproj Services/QuizService/QuizService.Application/
COPY Services/QuizService/QuizService.Infrastructure/*.csproj Services/QuizService/QuizService.Infrastructure/
COPY Services/QuizService/QuizService.API/*.csproj Services/QuizService/QuizService.API/

# Copy shared dependencies (sẽ ��ược cache nếu không thay đổi)
COPY BuildingBlocks/BuildingBlocks/*.csproj BuildingBlocks/BuildingBlocks/
COPY BuildingBlocks/BuildingBlocks.Messaging/*.csproj BuildingBlocks/BuildingBlocks.Messaging/
COPY Services/BaseService/BaseService.Domain/*.csproj Services/BaseService/BaseService.Domain/
COPY Services/BaseService/BaseService.Common/*.csproj Services/BaseService/BaseService.Common/
COPY Services/BaseService/BaseService.Application/*.csproj Services/BaseService/BaseService.Application/
COPY Services/BaseService/BaseService.Infrastructure/*.csproj Services/BaseService/BaseService.Infrastructure/
COPY Services/BaseService/BaseService.API/*.csproj Services/BaseService/BaseService.API/

# Restore dependencies (layer riêng biệt để cache tốt hơn)
RUN dotnet restore Services/QuizService/QuizService.API/QuizService.API.csproj

# Copy source code (layer này sẽ thay đổi thường xuyên nhất)
COPY BuildingBlocks/ BuildingBlocks/
COPY Services/BaseService/ Services/BaseService/
COPY Services/QuizService/ Services/QuizService/

# Build application
WORKDIR /src/Services/QuizService/QuizService.API
RUN dotnet build QuizService.API.csproj -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish QuizService.API.csproj -c $BUILD_CONFIGURATION -o /app/publish/quizservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/quizservice .
ENTRYPOINT ["dotnet", "QuizService.API.dll"]
