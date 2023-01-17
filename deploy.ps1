# acr name
$acrName = "mborleansdemocr"
$resourceGroup = "orleans-demo"

# Restore tools
dotnet tool restore

# Run minver
$version = $(dotnet minver -v e)

Write-Host "Start deployment of version: $version"

# Build silo
docker build -t "$acrName.azurecr.io/booking.silo:$version" -f ./src/Booking.Silo/Dockerfile .

# Build dashboard
docker build -t "$acrName.azurecr.io/booking.dashboard:$version" -f ./src/Booking.Dashboard/Dockerfile .

# Build web
docker build -t "$acrName.azurecr.io/booking.web:$version" -f ./src/Booking.Web/Dockerfile .

# ACR login
az acr login --name $acrName

# Push images
docker push "$acrName.azurecr.io/booking.silo:$version"
docker push "$acrName.azurecr.io/booking.dashboard:$version"
docker push "$acrName.azurecr.io/booking.web:$version"

# ACR logout
docker logout "$acrName.azurecr.io"

# Deploy
az deployment group create `
    --resource-group $resourceGroup `
    --template-file infra/app.bicep
