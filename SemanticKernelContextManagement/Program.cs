using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SemanticKernelContextManagement.Analysis;
using SemanticKernelContextManagement.Products.SemanticsKernel;
using SemanticKernelContextManagement.Products.Services;

IConfigurationRoot config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

string? modelId = config["modelId"];
if (string.IsNullOrWhiteSpace(modelId))
{
    throw new InvalidOperationException("Please provide a valid modelId in the settings file.");
}

string? endpoint = config["endpoint"];
if (string.IsNullOrWhiteSpace(endpoint))
{
    throw new InvalidOperationException("Please provide a valid endpoint in the settings file.");
}

string? apiKey = config["apiKey"];
if (string.IsNullOrWhiteSpace(apiKey))
{
    throw new InvalidOperationException("Please provide a valid apiKey in the settings file.");
}

IKernelBuilder kernelBuilder = Kernel
    .CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelId, endpoint, apiKey);

Kernel kernel = kernelBuilder.Build();
kernel.Plugins.AddFromType<ProductsPlugin>("Products");

var tokenUsageService = new TokenUsageService();
var recommendationService = new ProductRecommendationsService(kernel);

Console.WriteLine("Welcome to agentic shop assistant demo!");
Console.WriteLine("Available token management strategies are:");
Console.WriteLine("1. No management (default)");
Console.WriteLine("2. LM Summarization every 5 turns");
Console.WriteLine("3. Observation masking");
Console.WriteLine("4. LM Summarization every 5 turns and observation masking");
int strategy = 0;
while (strategy < 1 || strategy > 4)
{
    Console.Write("Enter the number of the strategy you want to use: ");
    var input = Console.ReadLine();
    if (!int.TryParse(input, out strategy) || strategy < 1 || strategy > 4)
    {
        Console.WriteLine("Invalid input. Please enter a number between 1 and 4.");
    }
}

Console.WriteLine($"You have selected strategy {strategy}.");
Console.WriteLine("Name a product or ask for a recommendation in any language.");

int turnIndex = 0;
while (true)
{
    turnIndex++;
    tokenUsageService.RecordTurnUsage(turnIndex);

    Console.Write("User > ");
    var userInput = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(userInput) || userInput.Equals("q", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Exiting...");
        break;
    }

    var recommendation = await recommendationService.GetRecommendationAsync(userInput);
    Console.WriteLine("Assistant > " + recommendation);
}

tokenUsageService.Dispose();