using Spectre.Console;
using Cosmonauta.Models;
using Cosmonauta.Services;
using Newtonsoft.Json;

namespace Cosmonauta.UI;

public class ConsoleUI
{
    private readonly AzureAuthService _authService;
    private readonly AzureResourceService _resourceService;
    private readonly CosmosDataService _dataService;
    private readonly CurlGeneratorService _curlGenerator;

    public ConsoleUI(AzureAuthService authService, AzureResourceService resourceService, 
                     CosmosDataService dataService, CurlGeneratorService curlGenerator)
    {
        _authService = authService;
        _resourceService = resourceService;
        _dataService = dataService;
        _curlGenerator = curlGenerator;
    }

    public async Task RunAsync()
    {
        AnsiConsole.Write(new FigletText("Cosmonauta")
            .LeftJustified()
            .Color(Color.Blue));

        AnsiConsole.MarkupLine("[bold cyan]Azure CosmosDB Explorer[/]");
        AnsiConsole.WriteLine();

        // Tentar obter subscription atual
        var currentSubscription = await _authService.GetCurrentSubscriptionNameAsync();
        if (!string.IsNullOrEmpty(currentSubscription))
        {
            AnsiConsole.MarkupLine($"[green]Subscription atual:[/] [yellow]{currentSubscription}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Nenhuma subscription selecionada[/]");
        }
        AnsiConsole.WriteLine();

        while (true)
        {
            var menuChoices = new List<string>
            {
                "Selecionar/Alterar Subscription",
                "Explorar por Resource Group",
                "Listar todas as Contas CosmosDB",
                "Conectar diretamente (endpoint + key)",
                "Sair"
            };

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Menu Principal[/]")
                    .AddChoices(menuChoices));

            switch (choice)
            {
                case "Selecionar/Alterar Subscription":
                    await SelectSubscriptionAsync();
                    break;
                case "Explorar por Resource Group":
                    await ExploreByResourceGroupAsync();
                    break;
                case "Listar todas as Contas CosmosDB":
                    await ListAllCosmosAccountsAsync();
                    break;
                case "Conectar diretamente (endpoint + key)":
                    await ConnectDirectlyAsync();
                    break;
                case "Sair":
                    return;
            }
        }
    }

    private async Task SelectSubscriptionAsync()
    {
        var subscriptions = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Carregando subscriptions disponíveis...", async ctx =>
            {
                try
                {
                    return await _authService.GetAllSubscriptionsAsync();
                }
                catch (Exception ex)
                {
                    ctx.Status($"[red]Erro: {ex.Message}[/]");
                    return new List<Azure.ResourceManager.Resources.SubscriptionResource>();
                }
            });

        if (!subscriptions.Any())
        {
            AnsiConsole.MarkupLine("[red]Nenhuma subscription encontrada.[/]");
            AnsiConsole.MarkupLine("[yellow]Certifique-se de estar logado no Azure CLI (az login)[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[green]Encontradas {subscriptions.Count} subscription(s)[/]");
        AnsiConsole.WriteLine();

        // Opções de seleção
        var selectionMethod = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Como deseja selecionar a subscription?[/]")
                .AddChoices(new[]
                {
                    "Buscar por nome/ID",
                    "Listar todas",
                    "Digitar ID exato",
                    "Cancelar"
                }));

        if (selectionMethod == "Cancelar")
            return;

        Azure.ResourceManager.Resources.SubscriptionResource? selectedSub = null;

        switch (selectionMethod)
        {
            case "Buscar por nome/ID":
                selectedSub = await SearchSubscriptionAsync(subscriptions);
                break;
            
            case "Listar todas":
                selectedSub = await ListAllSubscriptionsAsync(subscriptions);
                break;
            
            case "Digitar ID exato":
                selectedSub = await EnterSubscriptionIdAsync(subscriptions);
                break;
        }

        if (selectedSub != null)
        {
            _authService.SetCurrentSubscription(selectedSub);
            
            AnsiConsole.Clear();
            AnsiConsole.Write(new FigletText("Cosmonauta")
                .LeftJustified()
                .Color(Color.Blue));
            AnsiConsole.MarkupLine("[bold cyan]Azure CosmosDB Explorer[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine($"[green]Subscription selecionada:[/] [yellow]{selectedSub.Data.DisplayName}[/]");
            AnsiConsole.WriteLine();
        }
    }

    private async Task<Azure.ResourceManager.Resources.SubscriptionResource?> SearchSubscriptionAsync(
        List<Azure.ResourceManager.Resources.SubscriptionResource> subscriptions)
    {
        var searchTerm = AnsiConsole.Ask<string>("Digite parte do [cyan]nome[/] ou [cyan]ID[/] da subscription:");
        
        var filtered = subscriptions.Where(s => 
            (s.Data.DisplayName?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false) ||
            s.Data.SubscriptionId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        if (!filtered.Any())
        {
            AnsiConsole.MarkupLine($"[yellow]Nenhuma subscription encontrada com o termo '{searchTerm}'[/]");
            return null;
        }

        if (filtered.Count == 1)
        {
            var sub = filtered.First();
            if (AnsiConsole.Confirm($"Selecionar [cyan]{sub.Data.DisplayName}[/] ({sub.Data.SubscriptionId})?"))
                return sub;
            return null;
        }

        // Se houver múltiplos resultados, mostrar lista filtrada
        return await ListAllSubscriptionsAsync(filtered);
    }

    private Task<Azure.ResourceManager.Resources.SubscriptionResource?> ListAllSubscriptionsAsync(
        List<Azure.ResourceManager.Resources.SubscriptionResource> subscriptions)
    {
        var table = new Table();
        table.AddColumn("Nome");
        table.AddColumn("ID");
        table.AddColumn("Estado");

        foreach (var sub in subscriptions)
        {
            table.AddRow(
                sub.Data.DisplayName ?? "Sem nome",
                sub.Data.SubscriptionId,
                sub.Data.State?.ToString() ?? "Desconhecido"
            );
        }

        AnsiConsole.Write(table);

        // Para listas grandes, usar paginação
        var pageSize = 20;
        if (subscriptions.Count > pageSize)
        {
            AnsiConsole.MarkupLine($"[dim]Mostrando seleção com busca interativa (use as setas e digite para filtrar)[/]");
        }

        var subscriptionChoices = subscriptions
            .Select(s => $"{s.Data.DisplayName} ({s.Data.SubscriptionId})")
            .Append("Cancelar")
            .ToList();

        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[cyan]Selecione uma subscription:[/]")
                .PageSize(Math.Min(pageSize, subscriptionChoices.Count))
                .EnableSearch() // Habilita busca interativa
                .SearchPlaceholderText("Digite para filtrar...")
                .AddChoices(subscriptionChoices));

        if (selected != "Cancelar")
        {
            return Task.FromResult<Azure.ResourceManager.Resources.SubscriptionResource?>(
                subscriptions.First(s => 
                    $"{s.Data.DisplayName} ({s.Data.SubscriptionId})" == selected));
        }

        return Task.FromResult<Azure.ResourceManager.Resources.SubscriptionResource?>(null);
    }

    private Task<Azure.ResourceManager.Resources.SubscriptionResource?> EnterSubscriptionIdAsync(
        List<Azure.ResourceManager.Resources.SubscriptionResource> subscriptions)
    {
        var subscriptionId = AnsiConsole.Ask<string>("Digite o [cyan]ID completo[/] da subscription:");
        
        var sub = subscriptions.FirstOrDefault(s => 
            s.Data.SubscriptionId.Equals(subscriptionId, StringComparison.OrdinalIgnoreCase));
        
        if (sub == null)
        {
            AnsiConsole.MarkupLine($"[red]Subscription com ID '{subscriptionId}' não encontrada[/]");
            return Task.FromResult<Azure.ResourceManager.Resources.SubscriptionResource?>(null);
        }

        AnsiConsole.MarkupLine($"[green]Encontrada:[/] {sub.Data.DisplayName} ({sub.Data.SubscriptionId})");
        if (AnsiConsole.Confirm("Confirma seleção?"))
            return Task.FromResult<Azure.ResourceManager.Resources.SubscriptionResource?>(sub);
        
        return Task.FromResult<Azure.ResourceManager.Resources.SubscriptionResource?>(null);
    }

    private async Task ExploreByResourceGroupAsync()
    {
        var result = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Carregando Resource Groups...", async ctx =>
            {
                return await _resourceService.GetResourceGroupsAsync();
            });

        // Exibir logs após a operação
        foreach (var log in result.Logs)
        {
            AnsiConsole.MarkupLine($"[dim]{log}[/]");
        }

        if (!result.Success || result.Data == null || !result.Data.Any())
        {
            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                AnsiConsole.MarkupLine($"[red]{result.ErrorMessage}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Nenhum Resource Group encontrado[/]");
            }
            return;
        }

        var resourceGroups = result.Data;
        
        var table = new Table();
        table.AddColumn("Nome");
        table.AddColumn("Localização");
        
        foreach (var rg in resourceGroups)
        {
            table.AddRow(rg.Name, rg.Location);
        }
        
        AnsiConsole.Write(table);

        var selectedRg = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Selecione um Resource Group:")
                .AddChoices(resourceGroups.Select(rg => rg.Name).Append("Voltar")));

        if (selectedRg != "Voltar")
        {
            await ExploreResourceGroupAsync(selectedRg);
        }
    }

    private async Task ExploreResourceGroupAsync(string resourceGroupName)
    {
        var accounts = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Carregando contas CosmosDB em {resourceGroupName}...", async ctx =>
            {
                return await _resourceService.GetCosmosAccountsAsync(resourceGroupName);
            });
        
        if (!accounts.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Nenhuma conta CosmosDB encontrada neste Resource Group[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Nome");
        table.AddColumn("Localização");
        table.AddColumn("Endpoint");
        
        foreach (var account in accounts)
        {
            table.AddRow(account.Name, account.Location, account.DocumentEndpoint);
        }
        
        AnsiConsole.Write(table);

        var selectedAccount = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Selecione uma conta CosmosDB:")
                .AddChoices(accounts.Select(a => a.Name).Append("Voltar")));

        if (selectedAccount != "Voltar")
        {
            var account = accounts.First(a => a.Name == selectedAccount);
            
            AnsiConsole.MarkupLine($"[dim]Conectando ao endpoint: {account.DocumentEndpoint}[/]");
            
            if (_dataService.Initialize(account.DocumentEndpoint, account.PrimaryMasterKey))
            {
                AnsiConsole.MarkupLine("[green]Conexão estabelecida com sucesso![/]");
                await ExploreDatabasesAsync();
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{_dataService.LastError}[/]");
            }
        }
    }

    private async Task ListAllCosmosAccountsAsync()
    {
        var accounts = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Carregando todas as contas CosmosDB...", async ctx =>
            {
                return await _resourceService.GetAllCosmosAccountsAsync();
            });
        
        if (!accounts.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Nenhuma conta CosmosDB encontrada[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Nome");
        table.AddColumn("Resource Group");
        table.AddColumn("Localização");
        table.AddColumn("Endpoint");
        
        foreach (var account in accounts)
        {
            table.AddRow(account.Name, account.ResourceGroup, account.Location, account.DocumentEndpoint);
        }
        
        AnsiConsole.Write(table);

        var selectedAccount = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Selecione uma conta CosmosDB:")
                .AddChoices(accounts.Select(a => a.Name).Append("Voltar")));

        if (selectedAccount != "Voltar")
        {
            var account = accounts.First(a => a.Name == selectedAccount);
            
            AnsiConsole.MarkupLine($"[dim]Conectando ao endpoint: {account.DocumentEndpoint}[/]");
            
            if (_dataService.Initialize(account.DocumentEndpoint, account.PrimaryMasterKey))
            {
                AnsiConsole.MarkupLine("[green]Conexão estabelecida com sucesso![/]");
                await ExploreDatabasesAsync();
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]{_dataService.LastError}[/]");
            }
        }
    }

    private async Task ConnectDirectlyAsync()
    {
        AnsiConsole.MarkupLine("[cyan]Conexão direta ao CosmosDB[/]");
        AnsiConsole.MarkupLine("[dim]Exemplo de endpoint: https://sua-conta.documents.azure.com:443/[/]");
        
        var endpoint = AnsiConsole.Ask<string>("Digite o [cyan]endpoint[/] do CosmosDB:");
        var key = AnsiConsole.Prompt(
            new TextPrompt<string>("Digite a [cyan]chave primária[/]:")
                .PromptStyle("red")
                .Secret());

        AnsiConsole.MarkupLine($"[dim]Conectando ao endpoint: {endpoint}[/]");
        
        if (_dataService.Initialize(endpoint, key))
        {
            AnsiConsole.MarkupLine("[green]Conexão estabelecida com sucesso![/]");
            await ExploreDatabasesAsync();
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{_dataService.LastError}[/]");
            AnsiConsole.MarkupLine("[yellow]Dicas:[/]");
            AnsiConsole.MarkupLine("[yellow]1. Verifique se o endpoint está correto (ex: https://conta.documents.azure.com:443/)[/]");
            AnsiConsole.MarkupLine("[yellow]2. Verifique se a chave primária está correta[/]");
            AnsiConsole.MarkupLine("[yellow]3. Verifique se o firewall do CosmosDB permite seu IP[/]");
        }
    }

    private async Task ExploreDatabasesAsync()
    {
        var databases = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Carregando databases...", async ctx =>
            {
                return await _dataService.GetDatabasesAsync();
            });
        
        if (!databases.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Nenhum database encontrado[/]");
            return;
        }

        while (true)
        {
            var table = new Table();
            table.AddColumn("Database ID");
            
            foreach (var db in databases)
            {
                table.AddRow(db.Id);
            }
            
            AnsiConsole.Write(table);

            var choices = databases.Select(d => d.Id).ToList();
            choices.Add("Gerar exemplo cURL (listar databases)");
            choices.Add("Voltar");

            var selectedDb = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Selecione um database:")
                    .AddChoices(choices));

            if (selectedDb == "Voltar")
            {
                break;
            }
            else if (selectedDb == "Gerar exemplo cURL (listar databases)")
            {
                ShowCurlExample(_curlGenerator.GenerateListDatabasesCurl(
                    _dataService.GetCurrentEndpoint(), 
                    _dataService.GetCurrentKey()));
            }
            else
            {
                await ExploreCollectionsAsync(selectedDb);
            }
        }
    }

    private async Task ExploreCollectionsAsync(string databaseId)
    {
        var collections = await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync($"Carregando collections de {databaseId}...", async ctx =>
            {
                return await _dataService.GetCollectionsAsync(databaseId);
            });
        
        if (!collections.Any())
        {
            AnsiConsole.MarkupLine("[yellow]Nenhuma collection encontrada[/]");
            return;
        }

        while (true)
        {
            var table = new Table();
            table.AddColumn("Collection ID");
            table.AddColumn("Partition Key");
            
            foreach (var coll in collections)
            {
                table.AddRow(coll.Id, coll.PartitionKey);
            }
            
            AnsiConsole.Write(table);

            var choices = collections.Select(c => c.Id).ToList();
            choices.Add("Gerar exemplo cURL (listar collections)");
            choices.Add("Voltar");

            var selectedColl = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Selecione uma collection:")
                    .AddChoices(choices));

            if (selectedColl == "Voltar")
            {
                break;
            }
            else if (selectedColl == "Gerar exemplo cURL (listar collections)")
            {
                ShowCurlExample(_curlGenerator.GenerateListCollectionsCurl(
                    _dataService.GetCurrentEndpoint(),
                    databaseId,
                    _dataService.GetCurrentKey()));
            }
            else
            {
                await ExploreDocumentsAsync(databaseId, selectedColl);
            }
        }
    }

    private async Task ExploreDocumentsAsync(string databaseId, string collectionId)
    {
        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"[cyan]Database: {databaseId} / Collection: {collectionId}[/]")
                    .AddChoices(new[]
                    {
                        "Listar documentos",
                        "Executar query SQL",
                        "Gerar exemplo cURL para query",
                        "Voltar"
                    }));

            switch (choice)
            {
                case "Listar documentos":
                    await ListDocumentsAsync(databaseId, collectionId);
                    break;
                case "Executar query SQL":
                    await ExecuteQueryAsync(databaseId, collectionId);
                    break;
                case "Gerar exemplo cURL para query":
                    await GenerateCurlForQueryAsync(databaseId, collectionId);
                    break;
                case "Voltar":
                    return;
            }
        }
    }

    private async Task ListDocumentsAsync(string databaseId, string collectionId)
    {
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Carregando documentos...", async ctx =>
            {
                var maxItems = AnsiConsole.Ask("Quantos documentos deseja listar?", 10);
                var documents = await _dataService.GetDocumentsAsync(databaseId, collectionId, maxItems);
                
                if (!documents.Any())
                {
                    AnsiConsole.MarkupLine("[yellow]Nenhum documento encontrado[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"[green]Encontrados {documents.Count} documentos:[/]");
                
                foreach (var doc in documents)
                {
                    var panel = new Panel(JsonConvert.SerializeObject(doc, Formatting.Indented))
                    {
                        Header = new PanelHeader($"[cyan]Document ID: {doc.Id}[/]"),
                        Border = BoxBorder.Rounded
                    };
                    AnsiConsole.Write(panel);
                }

                AnsiConsole.Prompt(new TextPrompt<string>("[grey]Pressione Enter para continuar[/]")
                    .AllowEmpty());
            });
    }

    private async Task ExecuteQueryAsync(string databaseId, string collectionId)
    {
        var query = AnsiConsole.Ask<string>("Digite a query SQL (ex: SELECT * FROM c WHERE c.type = 'user'):");
        
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Star)
            .StartAsync("Executando query...", async ctx =>
            {
                try
                {
                    var queryRequest = new QueryRequest
                    {
                        Query = query,
                        MaxItemCount = 100
                    };

                    var documents = await _dataService.ExecuteQueryAsync(databaseId, collectionId, queryRequest);
                    
                    if (!documents.Any())
                    {
                        AnsiConsole.MarkupLine("[yellow]Nenhum resultado encontrado[/]");
                        return;
                    }

                    AnsiConsole.MarkupLine($"[green]Encontrados {documents.Count} resultados:[/]");
                    
                    foreach (var doc in documents)
                    {
                        var panel = new Panel(JsonConvert.SerializeObject(doc, Formatting.Indented))
                        {
                            Header = new PanelHeader($"[cyan]Document ID: {doc.Id}[/]"),
                            Border = BoxBorder.Rounded
                        };
                        AnsiConsole.Write(panel);
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Erro ao executar query: {ex.Message}[/]");
                }

                AnsiConsole.Prompt(new TextPrompt<string>("[grey]Pressione Enter para continuar[/]")
                    .AllowEmpty());
            });
    }

    private Task GenerateCurlForQueryAsync(string databaseId, string collectionId)
    {
        var query = AnsiConsole.Ask<string>("Digite a query SQL para gerar o exemplo cURL:");
        
        var curlExample = _curlGenerator.GenerateQueryDocumentsCurl(
            _dataService.GetCurrentEndpoint(),
            databaseId,
            collectionId,
            _dataService.GetCurrentKey(),
            query);

        ShowCurlExample(curlExample);
        return Task.CompletedTask;
    }

    private void ShowCurlExample(CurlExample example)
    {
        var panel = new Panel(example.CurlCommand)
        {
            Header = new PanelHeader("[cyan]Exemplo cURL[/]"),
            Border = BoxBorder.Double
        };
        
        AnsiConsole.Write(panel);
        
        if (AnsiConsole.Confirm("Copiar para a área de transferência?"))
        {
            try
            {
                TextCopy.ClipboardService.SetText(example.CurlCommand);
                AnsiConsole.MarkupLine("[green]Copiado para a área de transferência![/]");
            }
            catch
            {
                AnsiConsole.MarkupLine("[yellow]Não foi possível copiar para a área de transferência[/]");
            }
        }

        AnsiConsole.Prompt(new TextPrompt<string>("[grey]Pressione Enter para continuar[/]")
            .AllowEmpty());
    }
}