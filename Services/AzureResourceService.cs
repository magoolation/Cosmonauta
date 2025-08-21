using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Resources;
using Cosmonauta.Models;

namespace Cosmonauta.Services;

public class AzureResourceService
{
    private readonly ArmClient _armClient;

    public AzureResourceService(ArmClient armClient)
    {
        _armClient = armClient;
    }

    public async Task<List<ResourceGroupInfo>> GetResourceGroupsAsync()
    {
        var resourceGroups = new List<ResourceGroupInfo>();
        var subscription = await _armClient.GetDefaultSubscriptionAsync();
        
        await foreach (var resourceGroup in subscription.GetResourceGroups())
        {
            resourceGroups.Add(new ResourceGroupInfo
            {
                Name = resourceGroup.Data.Name,
                Location = resourceGroup.Data.Location,
                Id = resourceGroup.Id.ToString()
            });
        }

        return resourceGroups;
    }

    public async Task<List<CosmosAccountInfo>> GetCosmosAccountsAsync(string resourceGroupName)
    {
        var cosmosAccounts = new List<CosmosAccountInfo>();
        var subscription = await _armClient.GetDefaultSubscriptionAsync();
        var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);

        await foreach (var account in resourceGroup.Value.GetCosmosDBAccounts())
        {
            var keys = await account.GetKeysAsync();
            
            cosmosAccounts.Add(new CosmosAccountInfo
            {
                Name = account.Data.Name,
                Location = account.Data.Location,
                DocumentEndpoint = account.Data.DocumentEndpoint?.ToString() ?? string.Empty,
                Id = account.Id.ToString(),
                ResourceGroup = resourceGroupName,
                PrimaryMasterKey = keys.Value.PrimaryMasterKey
            });
        }

        return cosmosAccounts;
    }

    public async Task<List<CosmosAccountInfo>> GetAllCosmosAccountsAsync()
    {
        var cosmosAccounts = new List<CosmosAccountInfo>();
        var subscription = await _armClient.GetDefaultSubscriptionAsync();

        await foreach (var account in subscription.GetCosmosDBAccountsAsync())
        {
            var keys = await account.GetKeysAsync();
            var resourceGroupName = account.Id.ResourceGroupName;
            
            cosmosAccounts.Add(new CosmosAccountInfo
            {
                Name = account.Data.Name,
                Location = account.Data.Location,
                DocumentEndpoint = account.Data.DocumentEndpoint?.ToString() ?? string.Empty,
                Id = account.Id.ToString(),
                ResourceGroup = resourceGroupName ?? string.Empty,
                PrimaryMasterKey = keys.Value.PrimaryMasterKey
            });
        }

        return cosmosAccounts;
    }
}