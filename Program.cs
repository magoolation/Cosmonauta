using Cosmonauta.Services;
using Cosmonauta.UI;
using Spectre.Console;

try
{
    AnsiConsole.Clear();
    
    var authService = new AzureAuthService();
    var resourceService = new AzureResourceService(authService.GetArmClient(), authService);
    var dataService = new CosmosDataService();
    var curlGenerator = new CurlGeneratorService();
    
    var ui = new ConsoleUI(authService, resourceService, dataService, curlGenerator);
    await ui.RunAsync();
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    AnsiConsole.MarkupLine("[red]Erro fatal na aplicação[/]");
    AnsiConsole.MarkupLine("[yellow]Certifique-se de:[/]");
    AnsiConsole.MarkupLine("[yellow]1. Estar logado no Azure CLI: az login[/]");
    AnsiConsole.MarkupLine("[yellow]2. Ter permissões adequadas na subscription[/]");
    AnsiConsole.MarkupLine("[yellow]3. Ter o Azure CLI instalado e atualizado[/]");
    Environment.Exit(1);
}
