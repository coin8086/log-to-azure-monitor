param name string
param subnetResId string
param vmSize string
param vmImage object
param vmDiskSizeInGB int
param vmTags object = {}
param userName string
@secure()
param password string

@description('The user assigned managed identity for DCR')
param userMiResId string

param timestamp string = utcNow()

var location = resourceGroup().location

resource ip 'Microsoft.Network/publicIPAddresses@2023-11-01' = {
  name: '${name}-ip'
  location: location
  properties: {
    publicIPAllocationMethod: 'Dynamic'
  }
}

resource nic 'Microsoft.Network/networkInterfaces@2023-11-01' = {
  name: '${name}-nic'
  location: location
  properties: {
    ipConfigurations: [
      {
        name: 'IPConfig'
        properties: {
          subnet: {
            id: subnetResId
          }
          privateIPAllocationMethod: 'Dynamic'
          publicIPAddress: {
            id: ip.id
          }
        }
      }
    ]
  }
}

resource vm 'Microsoft.Compute/virtualMachines@2024-03-01' = {
  name: name
  location: location
  tags: vmTags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userMiResId}': {}
    }
  }
  properties: {
    hardwareProfile: {
      vmSize: vmSize
    }
    osProfile: {
      computerName: name
      adminUsername: userName
      adminPassword: password
    }
    storageProfile: {
      imageReference: vmImage
      osDisk: {
        name: '${name}-osdisk'
        createOption: 'FromImage'
        diskSizeGB: vmDiskSizeInGB
        caching: 'ReadOnly'
        managedDisk: {
          storageAccountType: 'StandardSSD_LRS'
        }
      }
    }
    networkProfile: {
      networkInterfaces: [
        {
          id: nic.id
        }
      ]
    }
  }
}

resource runSample 'Microsoft.Compute/virtualMachines/extensions@2024-03-01' = {
  name: '${name}-runSample'
  parent: vm
  location: location
  properties: {
    publisher: 'Microsoft.Compute'
    type: 'CustomScriptExtension'
    typeHandlerVersion: '1.10'
    autoUpgradeMinorVersion: true
    settings: {
      fileUris: [
        'https://raw.githubusercontent.com/coin8086/log-to-azure-monitor/main/sample/SampleAppWithListener.zip'
      ]
      //NOTE: SampleAppWithListener runs endlessly so we need to start it with "start /b" to let the extension finish.
      commandToExecute: 'powershell -command "Expand-Archive .\\SampleAppWithListener.zip -Force" && cd SampleAppWithListener && start /b SampleAppWithListener.exe'
      timestamp: dateTimeToEpoch(timestamp)
    }
  }
}
