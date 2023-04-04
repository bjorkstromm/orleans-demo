# acr name
param (
    $acrName,
    $version
)

# Build silo
docker build -t "$acrName.azurecr.io/booking.silo:$version" -f ./src/Booking.Silo/Dockerfile .

# Build web
docker build -t "$acrName.azurecr.io/booking.web:$version" -f ./src/Booking.Web/Dockerfile .

# Build admin
docker build -t "$acrName.azurecr.io/booking.admin:$version" -f ./src/Booking.Admin/Dockerfile .

# Build scaler
docker build -t "$acrName.azurecr.io/booking.scaler:$version" -f ./src/Booking.Scaler/Dockerfile .

# ACR login
az acr login --name $acrName

# Push images
docker push "$acrName.azurecr.io/booking.silo:$version"
docker push "$acrName.azurecr.io/booking.web:$version"
docker push "$acrName.azurecr.io/booking.admin:$version"
docker push "$acrName.azurecr.io/booking.scaler:$version"

# ACR logout
docker logout "$acrName.azurecr.io"