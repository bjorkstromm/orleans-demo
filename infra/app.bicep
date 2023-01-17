param version string
param location string = resourceGroup().location

var name = 'mb-orleans-demo'
var shortName = replace(name, '-', '')

resource appInsights 'Microsoft.Insights/components@2020-02-02' existing = {
  name: '${name}-ai'
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' existing = {
  name: '${name}-env'
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' existing = {
  name: '${shortName}sa'
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
  name: '${shortName}cr'
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' existing = {
  name: '${name}-mi'
}

// Silo
resource siloContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-silo'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/booking.silo:${version}'
          name: 'booking-silo'
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'AZURE_STORAGE_NAME'
              value: storageAccount.name
            }
            {
              name: 'MANAGEDIDENTITY_CLIENTID'
              value: managedIdentity.properties.clientId
            }
          ]
        }
      ]
    }
  }
}

// Dashboard
resource dashboardContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-dashboard'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: managedIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/booking.dashboard:${version}'
          name: 'booking-dashboard'
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'AZURE_STORAGE_NAME'
              value: storageAccount.name
            }
            {
              name: 'MANAGEDIDENTITY_CLIENTID'
              value: managedIdentity.properties.clientId
            }
          ]
        }
      ]
    }
  }
}

// Web
resource webContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-web'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      registries: [
        {
          server: containerRegistry.properties.loginServer
          identity: managedIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/booking.web:${version}'
          name: 'booking-web'
          env: [
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: appInsights.properties.ConnectionString
            }
            {
              name: 'AZURE_STORAGE_NAME'
              value: storageAccount.name
            }
            {
              name: 'MANAGEDIDENTITY_CLIENTID'
              value: managedIdentity.properties.clientId
            }
          ]
        }
      ]
    }
  }
}
