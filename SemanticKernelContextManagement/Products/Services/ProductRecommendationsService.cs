using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using SemanticKernelContextManagement.Models;

namespace SemanticKernelContextManagement.Products.Services
{
    public class ProductRecommendationsService(Kernel kernel, bool useSummarization, bool useObservationMasking)
    {
        private const string SystemPrompt = """
            You are a shop assistant that recommends products from our catalog.
            Use the Products plugin (GetProducts, GetProductDetails) to read real catalog data before suggesting items.
            Do not invent products, prices, or stock. If nothing matches, say so clearly.
            Match the user's language in your replies.
            """;

        private readonly IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        private readonly OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        public ChatHistory ChatHistory { get; } = CreateHistory();

        public async Task<string> GetRecommendationAsync(string userInput)
        {
            var beforeInteractionCount = ChatHistory.Count;
            ChatHistory.AddUserMessage(userInput);

            var result = await chatCompletionService.GetChatMessageContentAsync(
                ChatHistory,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            if (string.IsNullOrWhiteSpace(result.Content))
            {
                throw new InvalidOperationException($"{nameof(chatCompletionService)} did not return any content.");
            }

            ChatHistory.Add(result);

            NotifyTokenUsed(beforeInteractionCount);
            return result.Content;
        }

        private static ChatHistory CreateHistory()
        {
            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);
            return history;
        }

        private void NotifyTokenUsed(int beforeInteractionCount)
        {
            var newMessages = ChatHistory.Skip(beforeInteractionCount).ToList();

            var inputSum = 0;
            var outputSum = 0;
            var totalSum = 0;

            foreach (var message in newMessages)
            {
                if (message.Metadata?.TryGetValue("Usage", out var usageObj) == true
                    && usageObj is ChatTokenUsage usage)
                {
                    inputSum += usage.InputTokenCount;
                    outputSum += usage.OutputTokenCount;
                    totalSum += usage.TotalTokenCount;
                }
            }

            var tokenUsage = new TokenUsage(inputSum, outputSum, totalSum, "product-recommendation");
            TokenUsage.Publish(tokenUsage);
        }
    }
}