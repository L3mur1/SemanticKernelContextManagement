using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace SemanticKernelContextManagement.Services
{
    public class ProductRecommendationsService(Kernel kernel)
    {
        private readonly IChatCompletionService chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        private readonly ChatHistory history = [];

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
    }
}