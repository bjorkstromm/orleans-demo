param location string = resourceGroup().location

var name = 'mb-orleans-demo'
var shortName = replace(name, '-', '')
var acrPullRbacResourceId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
var storageTableContributorRbacResourceId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '0a9a7e1f-b9d0-4cc4-a60d-0319b160aaa3')
var storageBlobContributorRbacResourceId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'ba92f5b4-2d11-453d-a403-e96b0029c9fe')
var keyVaultCryptoServiceEncryptionUserRbacResourceId = subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'e147488a-f6f5-4113-8e2d-b22465e65bf6')

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${name}-logs'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: '${name}-ai'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource storageAccount 'Microsoft.Storage/storageAccounts@2021-04-01' = {
  name: '${shortName}sa'
  location: location
  kind: 'StorageV2'
  sku: {
    name: 'Standard_LRS'
  }
}

resource storageAccountBlobServices 'Microsoft.Storage/storageAccounts/blobServices@2023-01-01' = {
  parent: storageAccount
  name: 'default'
}

resource dataprotectionContainer 'Microsoft.Storage/storageAccounts/blobServices/containers@2023-01-01' = {
  parent: storageAccountBlobServices
  name: 'dataprotection'
}

resource keyVault 'Microsoft.KeyVault/vaults@2023-02-01' = {
  name: '${shortName}kv'
  location: location
  properties: {
    sku: {
      name: 'standard'
      family: 'A'
    }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true
  }
}

resource dataprotection 'Microsoft.KeyVault/vaults/keys@2023-02-01' = {
  parent: keyVault
  name: 'dataprotection'
  properties: {
    keySize: 2048
    kty: 'RSA'
  }
}

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: '${shortName}cr'
  location: location
  sku: {
    name: 'Standard'
  }
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: '${name}-mi'
  location: location
}

resource rbacAcrPull 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: containerRegistry
  name: guid(containerRegistry.id, managedIdentity.id, acrPullRbacResourceId)
  properties: {
    roleDefinitionId: acrPullRbacResourceId
    principalId: managedIdentity.properties.principalId
  }
}

resource rbacStorageTableContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, managedIdentity.id, storageTableContributorRbacResourceId)
  properties: {
    roleDefinitionId: storageTableContributorRbacResourceId
    principalId: managedIdentity.properties.principalId
  }
}

resource rbacStorageBlobContributor 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: storageAccount
  name: guid(storageAccount.id, managedIdentity.id, storageBlobContributorRbacResourceId)
  properties: {
    roleDefinitionId: storageBlobContributorRbacResourceId
    principalId: managedIdentity.properties.principalId
  }
}

resource keyVaultCryptoServiceEncryptionUser 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: keyVault
  name: guid(keyVault.id, managedIdentity.id, keyVaultCryptoServiceEncryptionUserRbacResourceId)
  properties: {
    roleDefinitionId: keyVaultCryptoServiceEncryptionUserRbacResourceId
    principalId: managedIdentity.properties.principalId
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2021-08-01' = {
  name: '${name}-vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    subnets: [
      {
        name: '${name}-subnet'
        properties: {
          addressPrefix: '10.0.0.0/21'
        }
      }
    ]
  }
}

resource subnet 'Microsoft.Network/virtualNetworks/subnets@2020-06-01' existing = {
  parent: vnet
  name: '${name}-subnet'
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2022-03-01' = {
  name: '${name}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsWorkspace.properties.customerId
        sharedKey: logAnalyticsWorkspace.listKeys().primarySharedKey
      }
    }
    vnetConfiguration: {
      infrastructureSubnetId: subnet.id
      internal: false
    }
  }
}
