using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;

namespace Cosmonauta.Services;

public class AzureAuthService
{
    private readonly DefaultAzureCredential _credential;
    private readonly ArmClient _armClient;

    public AzureAuthService()
    {
        _credential = new DefaultAzureCredential();
        _armClient = new ArmClient(_credential);
    }

    public TokenCredential GetCredential() => _credential;
    
    public ArmClient GetArmClient() => _armClient;

    public async Task<string> GetCurrentSubscriptionIdAsync()
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync();
        return subscription.Data.SubscriptionId;
    }

    public async Task<string> GetCurrentSubscriptionNameAsync()
    {
        var subscription = await _armClient.GetDefaultSubscriptionAsync();
        return subscription.Data.DisplayName;
    }
}