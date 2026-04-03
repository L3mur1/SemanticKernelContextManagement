using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelContextManagement.Products.Services
{
    public class ProductRecommendationsService(Kernel kernel)
    {
        private const string SystemPrompt = """
            You are a shop assistant that recommends products from our catalog.
            Use the Products plugin (GetProducts, GetProductDetails) to read real catalog data before suggesting items.
            Do not invent products, prices, or stock. If nothing matches, say so clearly.
            Match the user's language in your replies.
            """;

        private readonly IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        private readonly ChatHistory history = CreateHistory();

        private readonly OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        public async Task<string> GetRecommendationAsync(string userInput)
        {
            history.AddUserMessage(userInput);

            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            if (string.IsNullOrWhiteSpace(result.Content))
            {
                throw new InvalidOperationException($"{nameof(chatCompletionService)} did not return any content.");
            }

            history.AddAssistantMessage(result.Content);
            return result.Content;
        }

        private static ChatHistory CreateHistory()
        {
            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);
            return history;
        }
    }
}