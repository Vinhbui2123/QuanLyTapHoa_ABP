FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

RUN dotnet tool install -g Microsoft.Web.LibraryManager.Cli
RUN ln -s ~/.dotnet/tools/libman /usr/local/bin/libman

WORKDIR /src
COPY ["src/InternProject.Web.Mvc/InternProject.Web.Mvc.csproj", "src/InternProject.Web.Mvc/"]
COPY ["src/InternProject.Web.Core/InternProject.Web.Core.csproj", "src/InternProject.Web.Core/"]
COPY ["src/InternProject.Application/InternProject.Application.csproj", "src/InternProject.Application/"]
COPY ["src/InternProject.Core/InternProject.Core.csproj", "src/InternProject.Core/"]
COPY ["src/InternProject.EntityFrameworkCore/InternProject.EntityFrameworkCore.csproj", "src/InternProject.EntityFrameworkCore/"]
WORKDIR "/src/src/InternProject.Web.Mvc"
RUN dotnet restore 

WORKDIR /src
COPY ["src/InternProject.Web.Mvc", "src/InternProject.Web.Mvc"]
COPY ["src/InternProject.Web.Core", "src/InternProject.Web.Core"]
COPY ["src/InternProject.Application", "src/InternProject.Application"]
COPY ["src/InternProject.Core", "src/InternProject.Core"]
COPY ["src/InternProject.EntityFrameworkCore", "src/InternProject.EntityFrameworkCore"]
WORKDIR "/src/src/InternProject.Web.Mvc"
RUN libman restore
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "InternProject.Web.Mvc.dll"]
