param (
    $resourceGroup
)

az deployment group create `
    --resource-group $resourceGroup `
    --template-file "$PSScriptRoot/main.bicep"