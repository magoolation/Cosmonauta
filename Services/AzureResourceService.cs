using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB;
using Azure.ResourceManager.Resources;
using Cosmonauta.Models;

namespace Cosmonauta.Services;

public class AzureResourceService
{
    private readonly ArmClient _armClient;
    private readonly AzureAuthService _authService;

    public AzureResourceService(ArmClient armClient, AzureAuthService authService = null!)
    {
        _armClient = armClient;
        _authService = authService ?? new AzureAuthService();
    }

    public async Task<OperationResult<List<ResourceGroupInfo>>> GetResourceGroupsAsync()
    {
        var result = new OperationResult<List<ResourceGroupInfo>>();
        var resourceGroups = new List<ResourceGroupInfo>();
        
        try
        {
            var subscription = await _authService.GetCurrentSubscriptionAsync();
            
            result.AddLog($"Listando Resource Groups da subscription: {subscription.Data.DisplayName}");
            
            await foreach (var resourceGroup in subscription.GetResourceGroups())
            {
                resourceGroups.Add(new ResourceGroupInfo
                {
                    Name = resourceGroup.Data.Name,
                    Location = resourceGroup.Data.Location,
                    Id = resourceGroup.Id.ToString()
                });
            }
            
            result.AddLog($"Encontrados {resourceGroups.Count} Resource Groups");
            result.Success = true;
            result.Data = resourceGroups;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Erro ao listar Resource Groups: {ex.Message}";
            if (ex.InnerException != null)
            {
                result.AddLog($"Detalhes: {ex.InnerException.Message}");
            }
        }

        return result;
    }

    public async Task<List<CosmosAccountInfo>> GetCosmosAccountsAsync(string resourceGroupName)
    {
        var cosmosAccounts = new List<CosmosAccountInfo>();
        
        try
        {
            var subscription = await _authService.GetCurrentSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(resourceGroupName);

            if (!resourceGroup.HasValue)
            {
                return cosmosAccounts;
            }

            await foreach (var account in resourceGroup.Value.GetCosmosDBAccounts())
            {
                try
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
                catch
                {
                    // Ignora erros de chaves individuais
                }
            }
        }
        catch
        {
            // Retorna lista vazia em caso de erro
        }

        return cosmosAccounts;
    }

    public async Task<List<CosmosAccountInfo>> GetAllCosmosAccountsAsync()
    {
        var cosmosAccounts = new List<CosmosAccountInfo>();
        
        try
        {
            var subscription = await _authService.GetCurrentSubscriptionAsync();

            await foreach (var account in subscription.GetCosmosDBAccountsAsync())
            {
                try
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
                catch
                {
                    // Ignora erros de chaves individuais
                }
            }
        }
        catch
        {
            // Retorna lista vazia em caso de erro
        }

        return cosmosAccounts;
    }
}