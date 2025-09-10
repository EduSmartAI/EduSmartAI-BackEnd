FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Services/QuizService/QuizService.API/QuizService.API.csproj", "Services/QuizService/QuizService.API/"]
COPY ["Services/BaseService/BaseService.API/BaseService.API.csproj", "Services/BaseService/BaseService.API/"]
COPY ["Services/BaseService/BaseService.Application/BaseService.Application.csproj", "Services/BaseService/BaseService.Application/"]
COPY ["Services/BaseService/BaseService.Common/BaseService.Common.csproj", "Services/BaseService/BaseService.Common/"]
COPY ["Services/QuizService/QuizService.Application/QuizService.Application.csproj", "Services/QuizService/QuizService.Application/"]
COPY ["BuildingBlocks/BuildingBlocks.Messaging/BuildingBlocks.Messaging.csproj", "BuildingBlocks/BuildingBlocks.Messaging/"]
COPY ["BuildingBlocks/BuildingBlocks/BuildingBlocks.csproj", "BuildingBlocks/BuildingBlocks/"]
COPY ["Services/QuizService/QuizService.Infrastructure/QuizService.Infrastructure.csproj", "Services/QuizService/QuizService.Infrastructure/"]
COPY ["Services/BaseService/BaseService.Infrastructure/BaseService.Infrastructure.csproj", "Services/BaseService/BaseService.Infrastructure/"]
COPY ["Services/BaseService/BaseService.Domain/BaseService.Domain.csproj", "Services/BaseService/BaseService.Domain/"]
COPY ["Services/QuizService/QuizService.Domain/QuizService.Domain.csproj", "Services/QuizService/QuizService.Domain/"]
RUN dotnet restore "Services/QuizService/QuizService.API/QuizService.API.csproj"

COPY Services/QuizService/ Services/QuizService/
WORKDIR "/src/Services/QuizService/QuizService.API"
RUN dotnet build "./QuizService.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./QuizService.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /quizservice /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish/quizservice .
ENTRYPOINT ["dotnet", "QuizService.API.dll"]
