# Creates a ZIP for Azure App Service; does not provision or deploy cloud resources.
$ErrorActionPreference = 'Stop'
$taskRoot = $PSScriptRoot
$publishDirectory = Join-Path $taskRoot 'artifacts\azure\publish'
$packagePath = Join-Path $taskRoot 'artifacts\azure\restaurant-web.zip'
$projectPath = Join-Path $taskRoot 'RestaurantManagement.Web\RestaurantManagement.Web.csproj'

& dotnet publish $projectPath -c Release --no-self-contained -p:UseAppHost=false -p:BuildInParallel=false -m:1 -o $publishDirectory
if ($LASTEXITCODE -ne 0) { throw 'Publish failed; no deployment package was created.' }
if (!(Test-Path -LiteralPath (Join-Path $publishDirectory 'web.config'))) { throw 'Missing IIS web.config.' }
if (!(Test-Path -LiteralPath (Join-Path $publishDirectory 'RestaurantManagement.Web.dll'))) { throw 'Missing Web entry assembly.' }

Compress-Archive -Path (Join-Path $publishDirectory '*') -DestinationPath $packagePath -Force
Write-Output "Azure ZIP: $packagePath"
Write-Output 'Set ConnectionStrings__DefaultConnection in Azure. Do not enable WEBSITE_RUN_FROM_PACKAGE for the current upload implementation.'
