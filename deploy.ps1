$resourceGroup = "orleans-demo"

# Restore tools
dotnet tool restore

# Run minver
$version = $(dotnet minver -v e)
Write-Host "Start deployment of version: $version"

.\build.ps1 -acrName "mborleansdemocr" -version $version

# Parameters needed for AAD authentication in Admin app
$appId = $(az ad sp list --filter "displayname eq 'Orleans Booking Demo Admin'" --query "[0].appId" -o tsv)

# Deploy
az deployment group create `
    --resource-group $resourceGroup `
    --template-file infra/app.bicep `
    --parameters "version=$version" `
                 "aadClientId=$appId"
