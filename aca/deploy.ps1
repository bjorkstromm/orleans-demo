$resourceGroup = "orleans-demo"

# deploy infra
Push-Location "$PSScriptRoot"
az deployment group create `
    --resource-group $resourceGroup `
    --template-file main.bicep

# build and push images
Pop-Location
Push-Location "$PSScriptRoot/../"

# Restore tools
dotnet tool restore

# Run minver
$version = $(dotnet minver -v e)
Write-Host "Start deployment of version: $version"

.\build.ps1 -acrName "mborleansdemocr" -version $version

# Parameters needed for AAD authentication in Admin app
Push-Location "$PSScriptRoot"

$appId = $(az ad sp list --filter "displayname eq 'Orleans Booking Demo Admin'" --query "[0].appId" -o tsv)
$domain = $(az rest --method get --url 'https://graph.microsoft.com/v1.0/domains?$select=id' --query "value[0].id" -o tsv)

# Deploy
az deployment group create `
    --resource-group $resourceGroup `
    --template-file app.bicep `
    --parameters "version=$version" `
                 "aadClientId=$appId" `
                 "aadDomain=$domain"

Pop-Location