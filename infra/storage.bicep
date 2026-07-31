// storage.bicep — Azure Storage Account for CodeLab session persistence
// Cost: Cheapest LRS Standard storage (~$0.018/GB/month for table storage).
// Deploy via: az deployment group create --resource-group <rg> --template-file storage.bicep

@description('Globally unique storage account name (3-24 lowercase alphanumeric).')
param storageAccountName string = 'codelabsessions${uniqueString(resourceGroup().id)}'

@description('Azure region for the storage account.')
param location string = resourceGroup().location

@description('Storage SKU — Standard_LRS is the cheapest option.')
@allowed(['Standard_LRS', 'Standard_GRS', 'Standard_ZRS'])
param sku string = 'Standard_LRS'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: storageAccountName
  location: location
  kind: 'StorageV2'
  sku: {
    name: sku
  }
  properties: {
    accessTier: 'Hot'
    allowBlobPublicAccess: false
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    networkAcls: {
      defaultAction: 'Allow'
    }
  }
}

// The CodeLabSessions table is created by the app on startup via TableClient.CreateIfNotExists().
// No additional table resource is needed here.

output storageAccountName string = storageAccount.name
output storageAccountId string = storageAccount.id

// Retrieve the primary connection string from the deployed account (useful in pipelines)
output connectionString string = 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
