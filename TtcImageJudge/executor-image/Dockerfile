# escape=`
FROM mcr.microsoft.com/dotnet/sdk:10.0-windowsservercore-ltsc2025
SHELL ["powershell", "-NoLogo", "-Command"]
RUN New-Item -ItemType Directory C:\warm | Out-Null; `
    Set-Content C:\warm\warm.csproj '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0-windows</TargetFramework></PropertyGroup><ItemGroup><PackageReference Include="System.Drawing.Common" Version="10.0.0" /></ItemGroup></Project>'; `
    dotnet restore C:\warm\warm.csproj; `
    Remove-Item C:\warm -Recurse -Force
WORKDIR C:\task
USER ContainerUser
