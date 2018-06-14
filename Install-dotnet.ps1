$url="https://raw.githubusercontent.com/dotnet/cli/release/2.1.3xx/scripts/obtain/dotnet-install.ps1"
$output="$PSScriptRoot\dotnet-install.ps1"
$installDir = Join-Path $PSScriptRoot ".dotnet"

(New-Object System.Net.WebClient).DownloadFile($url, $output)
Invoke-Expression "& `"$output`" -InstallDir $installDir -Channel release/2.1.3xxx -Version 2.1.300"
