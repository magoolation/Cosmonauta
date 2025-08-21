using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;

namespace Cosmonauta.Services;

public class AzureAuthService
{
    private readonly DefaultAzureCredential _credential;
    private readonly ArmClient _armClient;
    private SubscriptionResource? _currentSubscription;

    public AzureAuthService()
    {
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ExcludeEnvironmentCredential = false,
            ExcludeManagedIdentityCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeAzureCliCredential = false,
            ExcludeAzurePowerShellCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeInteractiveBrowserCredential = true
        };
        
        _credential = new DefaultAzureCredential(credentialOptions);
        _armClient = new ArmClient(_credential);
    }

    public TokenCredential GetCredential() => _credential;
    
    public ArmClient GetArmClient() => _armClient;

    public async Task<SubscriptionResource> GetCurrentSubscriptionAsync()
    {
        if (_currentSubscription == null)
        {
            try
            {
                _currentSubscription = await _armClient.GetDefaultSubscriptionAsync();
            }
            catch
            {
                var subscriptions = _armClient.GetSubscriptions();
                await foreach (var sub in subscriptions)
                {
                    _currentSubscription = sub;
                    break;
                }
                
                if (_currentSubscription == null)
                    throw new InvalidOperationException("Nenhuma subscription Azure encontrada. Verifique se está logado no Azure CLI (az login)");
            }
        }
        
        return _currentSubscription;
    }

    public async Task<string> GetCurrentSubscriptionIdAsync()
    {
        var subscription = await GetCurrentSubscriptionAsync();
        return subscription.Data.SubscriptionId;
    }

    public async Task<string> GetCurrentSubscriptionNameAsync()
    {
        var subscription = await GetCurrentSubscriptionAsync();
        return subscription.Data.DisplayName;
    }
    
    public async Task<List<SubscriptionResource>> GetAllSubscriptionsAsync()
    {
        var subscriptions = new List<SubscriptionResource>();
        await foreach (var sub in _armClient.GetSubscriptions())
        {
            subscriptions.Add(sub);
        }
        return subscriptions;
    }
}