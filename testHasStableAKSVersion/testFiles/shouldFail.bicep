resource aksClusterTwo 'Microsoft.ContainerService/managedClusters@2025-06-02-preview' = {
  name: 'aks-cluster-two'
  location: 'northeurope'
  sku: {
    name: 'Standard'
    tier: 'Paid'
  }
  properties: {
    kubernetesVersion: '1.28.5'
    dnsPrefix: 'aksclustertwo'
    agentPoolProfiles: [
      {
        name: 'nodepool2'
        count: 2
        vmSize: 'Standard_B4ms'
        osType: 'Linux'
        mode: 'User'
      }
    ]
    enableRBAC: true
    networkProfile: {
      networkPlugin: 'kubenet'
      outboundType: 'loadBalancer'
    }
  }
}
