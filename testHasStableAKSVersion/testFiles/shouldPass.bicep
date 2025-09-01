resource aksClusterOne 'Microsoft.ContainerService/managedClusters@2025-06-02-preview' = {
  name: 'aks-cluster-one'
  location: 'westeurope'
  sku: {
    name: 'Basic'
    tier: 'Free'
  }
  properties: {
    kubernetesVersion: '1.32.5'
    dnsPrefix: 'aksclusterone'
    agentPoolProfiles: [
      {
        name: 'nodepool1'
        count: 3
        vmSize: 'Standard_DS2_v2'
        osType: 'Linux'
        mode: 'System'
      }
    ]
    networkProfile: {
      networkPlugin: 'azure'
      loadBalancerSku: 'standard'
    }
  }
}
