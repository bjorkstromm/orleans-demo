param(
    [int]$replicas
)

$resourceGroup = "orleans-demo"
$appName = "booking-silo"

az containerapp update -n $appName -g $resourceGroup `
    --min-replicas $replicas --max-replicas $replicas