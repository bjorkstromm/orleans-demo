$resourceGroup = "aks-rg"

# deploy infra
Push-Location "$PSScriptRoot/infra/"
.\deploy.ps1 -resourceGroup $resourceGroup

# build and push images
Pop-Location
Push-Location "$PSScriptRoot/../"

.\build.ps1 -acrName "mbaksacr" -version "dev"

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
kubectl apply -f .\booking-scaler.yml
kubectl apply -f .\booking-silo.yml
kubectl apply -f .\booking-web.yml
kubectl apply -f .\booking-admin.yml

Pop-Location