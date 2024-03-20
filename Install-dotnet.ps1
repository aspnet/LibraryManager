$url="https://dotnet.microsoft.com/download/dotnet/scripts/v1/dotnet-install.ps1"
$output="$PSScriptRoot\dotnet-install.ps1"
$installDir = Join-Path $PSScriptRoot ".dotnet"

(New-Object System.Net.WebClient).DownloadFile($url, $output)
Invoke-Expression "& `"$output`" -InstallDir $installDir -Channel 8.0.1xx -Quality ga"
