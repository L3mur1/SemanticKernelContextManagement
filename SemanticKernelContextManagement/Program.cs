using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernelContextManagement.Plugins;
using SemanticKernelContextManagement.Services;

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
IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
kernel.Plugins.AddFromType<ProductsPlugin>("Products");

OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var recommendationService = new ProductRecommendationsService(kernel);

var history = new ChatHistory();

while (true)
{
    Console.Write("User > ");
    var userInput = Console.ReadLine();
    if (userInput == null)
    {
        Console.WriteLine("Exiting...");
        break;
    }

    history.AddUserMessage(userInput);

    var result = await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);

    Console.WriteLine("Assistant > " + result);
    history.AddMessage(result.Role, result.Content ?? string.Empty);
}