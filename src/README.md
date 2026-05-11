# Strawhats Framework Templates

Templates for creating Strawhats framework services with `dotnet new`.

## Install

Install from NuGet after the package is published:

```powershell
dotnet new install StrawhatsCompany.SHFramework.Templates
```

Or install from a local package:

```powershell
dotnet new install .\artifacts\packages\StrawhatsCompany.SHFramework.Templates.1.0.0.nupkg
```

You can also register this source folder directly during local development:

```powershell
dotnet new install .
```

## Usage

Create a new service:

```powershell
dotnet new shf -n Strawhats.Authentication -o .\authentication\src
```

This creates the framework solution under `authentication\src` and renames `SHFramework.slnx` to `Strawhats.Authentication.slnx`.

## Build Package

Create the NuGet package:

```powershell
dotnet pack .\StrawhatsCompany.SHFramework.Templates.csproj -c Release -o .\artifacts\packages
```

## Publish Package

Publish to NuGet:

```powershell
dotnet nuget push .\artifacts\packages\StrawhatsCompany.SHFramework.Templates.1.0.0.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json
```
