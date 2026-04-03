using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SemanticKernelContextManagement.Products.Plugins;
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

var recommendationService = new ProductRecommendationsService(kernel);

while (true)
{
    Console.Write("User > ");
    var userInput = Console.ReadLine();
    if (userInput == null)
    {
        Console.WriteLine("Exiting...");
        break;
    }

    var recommendation = await recommendationService.GetRecommendationAsync(userInput);
    Console.WriteLine("Assistant > " + recommendation);
}