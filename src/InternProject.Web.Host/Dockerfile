FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/InternProject.Web.Host/InternProject.Web.Host.csproj", "src/InternProject.Web.Host/"]
COPY ["src/InternProject.Web.Core/InternProject.Web.Core.csproj", "src/InternProject.Web.Core/"]
COPY ["src/InternProject.Application/InternProject.Application.csproj", "src/InternProject.Application/"]
COPY ["src/InternProject.Core/InternProject.Core.csproj", "src/InternProject.Core/"]
COPY ["src/InternProject.EntityFrameworkCore/InternProject.EntityFrameworkCore.csproj", "src/InternProject.EntityFrameworkCore/"]
WORKDIR "/src/src/InternProject.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/InternProject.Web.Host", "src/InternProject.Web.Host"]
COPY ["src/InternProject.Web.Core", "src/InternProject.Web.Core"]
COPY ["src/InternProject.Application", "src/InternProject.Application"]
COPY ["src/InternProject.Core", "src/InternProject.Core"]
COPY ["src/InternProject.EntityFrameworkCore", "src/InternProject.EntityFrameworkCore"]
WORKDIR "/src/src/InternProject.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "InternProject.Web.Host.dll"]
