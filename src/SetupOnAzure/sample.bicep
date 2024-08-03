param name string = 'sample'
param location string = resourceGroup().location
param vmCount int = 2
param vmSize string = 'Standard_DS2_v2'

@description('Windows Server 2022 or later is required since the SampleAppWithListener depends on .Net Framework 4.8, which is available by default since Windows Server 2022.')
param vmImage object = {
  publisher: 'MicrosoftWindowsServer'
  offer: 'WindowsServer'
  sku: '2022-Datacenter'
  version: 'latest'
}

param userName string
@secure()
param password string

var uniqStr = substring(uniqueString(resourceGroup().id), 0, 10)
var prefix = '${name}-${uniqStr}-'

resource workSpace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${prefix}workspace'
  location: location
}

module logIngestion 'log-ingestion.bicep' = {
  name: '${prefix}logIngestion'
  params: {
    workSpaceName: workSpace.name
    prefix: prefix
  }
}

resource vnet 'Microsoft.Network/virtualNetworks@2023-11-01' = {
  name: '${prefix}vnet'
  location: location
  properties: {
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
  }
}

resource defaulSubnet 'Microsoft.Network/virtualNetworks/subnets@2023-11-01' = {
  parent: vnet
  name: '${prefix}defaultSubnet'
  properties: {
    addressPrefix: '10.0.0.0/22'
  }
}

var tags = {
  LA_MiClientId: logIngestion.outputs.userMiClientId
  LA_DcrId: logIngestion.outputs.dcrRunId
  LA_DcrStream: logIngestion.outputs.dcrStreamName
  LA_DceUrl: logIngestion.outputs.logsIngestionEndpoint
}

//NOTE: Windows computer name cannot be more than 15 characters long. So we don't put a long prefix for the name here.
module nodes 'node.bicep' = [for idx in range(1, vmCount): {
  name: 'node-${idx}'
  params: {
    name: 'node-${idx}'
    subnetResId: defaulSubnet.id
    vmSize: vmSize
    vmImage: vmImage
    vmDiskSizeInGB: 128
    vmTags: tags
    userName: userName
    password: password
    userMiResId: logIngestion.outputs.userMiResId
  }
  dependsOn: [
    logIngestion
  ]
}]
