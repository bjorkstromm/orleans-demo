$resourceGroup = "aks-rg"

# deploy infra
Push-Location "$PSScriptRoot/infra/"
.\deploy.ps1 -resourceGroup $resourceGroup

# build and push images
Pop-Location
Push-Location "$PSScriptRoot/../"

# Restore tools
dotnet tool restore

# Run minver
$BOOKERIZER_VERSION = $(dotnet minver -v e)
Write-Host "Start deployment of version: $BOOKERIZER_VERSION"

.\build.ps1 -acrName "mbaksacr" -version $BOOKERIZER_VERSION

# deploy manifests
Pop-Location
Push-Location "$PSScriptRoot/manifest/"

$APPLICATIONINSIGHTS_CONNECTION_STRING = $(az resource show -g $resourceGroup -n mb-aks-ai --resource-type "microsoft.insights/components" --query properties.ConnectionString -o tsv)
$AZURE_STORAGE_NAME = "mbakssa"
$MANAGEDIDENTITY_CLIENTID = $(az identity show -g $resourceGroup -n orleans-workload-identity --query clientId -o tsv)

kubectl apply -f .\namespace.yml
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\service-account.yml | Out-String)) | kubectl apply -f -
kubectl apply -f .\pod-reader-role.yml
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\booking-config.yml | Out-String)) | kubectl apply -f -
kubectl apply -f .\tracelens.yml
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\booking-scaler.yml | Out-String)) | kubectl apply -f -
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\booking-silo.yml | Out-String)) | kubectl apply -f -
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\booking-web.yml | Out-String)) | kubectl apply -f -
$ExecutionContext.InvokeCommand.ExpandString((Get-Content .\booking-admin.yml | Out-String)) | kubectl apply -f -

Pop-Location