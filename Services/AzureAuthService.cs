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
        // Configurar para priorizar Azure CLI
        var credentialOptions = new DefaultAzureCredentialOptions
        {
            // Desabilitar métodos que vêm antes do Azure CLI
            ExcludeEnvironmentCredential = true,
            ExcludeManagedIdentityCredential = true,
            ExcludeWorkloadIdentityCredential = true,
            
            // Manter Azure CLI habilitado (é o primeiro agora)
            ExcludeAzureCliCredential = false,
            
            // Outros métodos como fallback
            ExcludeAzurePowerShellCredential = false,
            ExcludeAzureDeveloperCliCredential = false,
            ExcludeVisualStudioCredential = false,
            ExcludeVisualStudioCodeCredential = false,
            ExcludeInteractiveBrowserCredential = true
        };
        
        _credential = new DefaultAzureCredential(credentialOptions);
        _armClient = new ArmClient(_credential);
    }

    public TokenCredential GetCredential() => _credential;
    
    public ArmClient GetArmClient() => _armClient;

    public async Task<SubscriptionResource?> GetCurrentSubscriptionAsync()
    {
        if (_currentSubscription == null)
        {
            try
            {
                _currentSubscription = await _armClient.GetDefaultSubscriptionAsync();
            }
            catch
            {
                // Se não houver subscription padrão, retorna null
                return null;
            }
        }
        
        return _currentSubscription;
    }
    
    public void SetCurrentSubscription(SubscriptionResource subscription)
    {
        _currentSubscription = subscription;
    }

    public async Task<string?> GetCurrentSubscriptionIdAsync()
    {
        var subscription = await GetCurrentSubscriptionAsync();
        return subscription?.Data.SubscriptionId;
    }

    public async Task<string?> GetCurrentSubscriptionNameAsync()
    {
        var subscription = await GetCurrentSubscriptionAsync();
        return subscription?.Data.DisplayName;
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