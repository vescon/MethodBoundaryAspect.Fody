# Builds the project with the nuget package created by "dotnet pack ... -o Artifacts/Packages" (as a user would use it,
# with the weaver from the package) and runs it on every target framework.
param([string]$PackageFolder = (Join-Path $PSScriptRoot '..\..\Artifacts\Packages'))

$ErrorActionPreference = 'Stop'
$package = Get-ChildItem $PackageFolder -Filter 'MethodBoundaryAspect.Fody.*.nupkg' | Where-Object { $_.Name -notlike '*.snupkg' } | Select-Object -First 1
if (-not $package) { throw "No package in $PackageFolder" }
$version = $package.BaseName.Substring('MethodBoundaryAspect.Fody.'.Length)
Write-Host "Package: $($package.Name)"

# a folder of its own, so that no earlier version of the package is used from the global cache
$packages = Join-Path ([IO.Path]::GetTempPath()) ("mba-smoke-" + [Guid]::NewGuid().ToString('N'))
try {
    Push-Location $PSScriptRoot
    Remove-Item bin, obj -Recurse -Force -ErrorAction SilentlyContinue
    $config = Join-Path $packages 'nuget.config'
    New-Item $packages -ItemType Directory -Force | Out-Null
    @"
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$($package.DirectoryName)" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content $config -Encoding utf8
    dotnet restore -p:MbaVersion=$version --packages $packages --configfile $config
    if ($LASTEXITCODE -ne 0) { throw 'restore failed' }
    dotnet build --no-restore -p:MbaVersion=$version -c Release
    if ($LASTEXITCODE -ne 0) { throw 'build failed' }

    foreach ($framework in 'net8.0', 'net48') {
        $exe = if ($framework -eq 'net48') { "bin\Release\$framework\MethodBoundaryAspect.Fody.PackageSmokeTest.exe" } else { $null }
        Write-Host "Running $framework"
        if ($exe) { & $exe } else { dotnet "bin\Release\$framework\MethodBoundaryAspect.Fody.PackageSmokeTest.dll" }
        if ($LASTEXITCODE -ne 0) { throw "The woven program failed on $framework" }
    }
}
finally {
    Pop-Location
    Remove-Item $packages -Recurse -Force -ErrorAction SilentlyContinue
}
