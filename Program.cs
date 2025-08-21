using Cosmonauta.Services;
using Cosmonauta.UI;
using Spectre.Console;

try
{
    var authService = new AzureAuthService();
    var resourceService = new AzureResourceService(authService.GetArmClient());
    var dataService = new CosmosDataService();
    var curlGenerator = new CurlGeneratorService();
    
    var ui = new ConsoleUI(authService, resourceService, dataService, curlGenerator);
    await ui.RunAsync();
}
catch (Exception ex)
{
    AnsiConsole.WriteException(ex, ExceptionFormats.ShortenEverything);
    AnsiConsole.MarkupLine("[red]Erro fatal na aplicação[/]");
    Environment.Exit(1);
}
