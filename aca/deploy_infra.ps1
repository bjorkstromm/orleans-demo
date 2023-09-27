$resourceGroup = "orleans-demo"

az deployment group create `
    --resource-group $resourceGroup `
    --template-file main.bicep