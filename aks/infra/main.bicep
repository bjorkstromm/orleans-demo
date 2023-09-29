param location string = resourceGroup().location

var name = 'mb-aks'
var shortName = replace(name, '-', '')
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

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: '${shortName}acr'
  location: location
  sku: {
    name: 'Standard'
  }
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2018-11-30' = {
  name: 'orleans-workload-identity'
  location: location
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
