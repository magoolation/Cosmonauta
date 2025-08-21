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

        try
        {
            var subscriptionName = await _authService.GetCurrentSubscriptionNameAsync();
            AnsiConsole.MarkupLine($"[green]Conectado à subscription:[/] [yellow]{subscriptionName}[/]");
            AnsiConsole.WriteLine();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Erro ao conectar ao Azure: {ex.Message}[/]");
            AnsiConsole.MarkupLine("[yellow]Certifique-se de estar logado no Azure CLI (az login)[/]");
            return;
        }

        while (true)
        {
            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]Menu Principal[/]")
                    .AddChoices(new[]
                    {
                        "Explorar por Resource Group",
                        "Listar todas as Contas CosmosDB",
                        "Conectar diretamente (endpoint + key)",
                        "Sair"
                    }));

            switch (choice)
            {
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