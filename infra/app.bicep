param version string
param aadClientId string
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

// Scaler
resource scalerContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-scaler'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  dependsOn: [
    redisContainerApp
  ]
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
        allowInsecure: true
        transport: 'http2'
      }
    }
    template: {
      containers: [
        {
          image: '${containerRegistry.properties.loginServer}/booking.scaler:${version}'
          name: 'booking-scaler'
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
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
          ]
        }
        {
          image: 'docker.io/rogeralsing/tracelens:amd64'
          name: 'tracelens-collector'
          env: [
            {
              name: 'Redis__Server'
              value: 'redis'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
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
  dependsOn: [
    redisContainerApp
  ]
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
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
          ]
        }
        {
          image: 'docker.io/rogeralsing/tracelens:amd64'
          name: 'tracelens-collector'
          env: [
            {
              name: 'Redis__Server'
              value: 'redis'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 10
        rules: [
          {
            name: 'scaler'
            custom: {
              type: 'external'
              metadata: {
                scalerAddress: '${scalerContainerApp.properties.configuration.ingress.fqdn}:80'
                graintype: 'timeslot'
                upperbound: '300'
              }
            }
          }
        ]
      }
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
  dependsOn: [
    redisContainerApp
  ]
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
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
          ]
        }
        {
          image: 'docker.io/rogeralsing/tracelens:amd64'
          name: 'tracelens-collector'
          env: [
            {
              name: 'Redis__Server'
              value: 'redis'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// Web
resource adminContainerApp 'Microsoft.App/containerApps@2022-03-01' = {
  name: 'booking-admin'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  dependsOn: [
    redisContainerApp
  ]
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
          image: '${containerRegistry.properties.loginServer}/booking.admin:${version}'
          name: 'booking-admin'
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
            {
              name: 'OTEL_EXPORTER_OTLP_ENDPOINT'
              value: 'http://localhost:4317'
            }
          ]
        }
        {
          image: 'docker.io/rogeralsing/tracelens:amd64'
          name: 'tracelens-collector'
          env: [
            {
              name: 'Redis__Server'
              value: 'redis'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
  resource authConfig 'authConfigs' = {
    name: 'current'
    properties: {
      platform: {
        enabled: true
      }
      globalValidation: {
        unauthenticatedClientAction: 'RedirectToLoginPage'
        redirectToProvider: 'azureactivedirectory'
      }
      identityProviders: {
        azureActiveDirectory: {
          registration: {
            openIdIssuer: 'https://sts.windows.net/${subscription().tenantId}/v2.0'
            clientId: aadClientId
          }
          validation: {
            allowedAudiences: []
          }
        }
      }
      login: {
        preserveUrlFragmentsForLogins: false
      }
    }
  }
}

// Redis
resource redisContainerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'redis'
  location: location
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: false
        targetPort: 6379
        transport: 'tcp'
      }
    }
    template: {
      containers: [
        {
          image: 'redis:latest'
          name: 'redis'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

// TracelensUI
resource tracelensUIContainerApp 'Microsoft.App/containerApps@2022-06-01-preview' = {
  name: 'tracelens-ui'
  location: location
  dependsOn: [
    redisContainerApp
  ]
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 5001
        transport: 'auto'
      }
    }
    template: {
      containers: [
        {
          image: 'docker.io/rogeralsing/tracelens:amd64'
          name: 'tracelens-ui'
          env: [
            {
              name: 'PlantUml__RemoteUrl'
              value: 'http://localhost:8080'
            }
            {
              name: 'Redis__Server'
              value: 'redis'
            }
          ]
        }
        {
          image: 'plantuml/plantuml-server:tomcat'
          name: 'plantuml'
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}
