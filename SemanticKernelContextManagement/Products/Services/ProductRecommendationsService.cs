using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using OpenAI.Chat;
using SemanticKernelContextManagement.Models;

namespace SemanticKernelContextManagement.Products.Services
{
    public class ProductRecommendationsService(Kernel kernel, bool useSummarization, bool useObservationMasking)
    {
        private const string ObservationMaskedPlaceholder = "[TOOL_OBSERVATION_MASKED]";

        private const string SummarizationSystemPrompt = """
            You compress conversation transcripts for a product-recommendation assistant.
            Preserve concrete facts the user asked about, product names, prices, stock, and language the user used.
            Do not invent catalog data. Output a concise third-person summary suitable as context for continuing the chat.
            """;

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

        private readonly OpenAIPromptExecutionSettings summarizationPromptExecutionSettings = new()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.None()
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
            NotifyProductRecommendationsTokenUsed(beforeInteractionCount);

            if (useObservationMasking)
            {
                MaskObservations();
            }

            if (useSummarization)
            {
                var numberOfTurnsWithUser = ChatHistory.Count(c => c.Role == AuthorRole.User);
                if (numberOfTurnsWithUser % 10 == 0)
                {
                    await SummarizeChatAsync();
                }
            }

            return result.Content;
        }

        private static ChatHistory CreateHistory()
        {
            var history = new ChatHistory();
            history.AddSystemMessage(SystemPrompt);
            return history;
        }

        private static string FormatConversationForSummary(ChatHistory history)
        {
            var blocks = new List<string>();
            foreach (var message in history)
            {
                if (message.Role == AuthorRole.System)
                {
                    continue;
                }

                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(message.Content))
                {
                    parts.Add(message.Content.Trim());
                }

                foreach (var item in message.Items)
                {
                    switch (item)
                    {
                        case FunctionCallContent call:
                            parts.Add($"[called {call.PluginName}.{call.FunctionName} with arguments: {call.Arguments}]");
                            break;

                        case FunctionResultContent result:
                            parts.Add($"[result from {result.PluginName}.{result.FunctionName}: {FormatResultObject(result.Result)}]");
                            break;

                        case TextContent text when !string.IsNullOrWhiteSpace(text.Text):
                            parts.Add(text.Text.Trim());
                            break;
                    }
                }

                var body = parts.Count > 0 ? string.Join(" ", parts) : "(no text)";
                blocks.Add($"{message.Role}: {body}");
            }

            return string.Join(Environment.NewLine + Environment.NewLine, blocks);
        }

        private static string FormatResultObject(object? result) => result switch
        {
            null => string.Empty,
            string s => s,
            _ => result.ToString() ?? string.Empty,
        };

        private static void NotifySummarizationTokenUsed(Microsoft.SemanticKernel.ChatMessageContent summaryMessage)
        {
            if (summaryMessage.Metadata?.TryGetValue("Usage", out var usageObj) != true || usageObj is not ChatTokenUsage usage)
            {
                return;
            }

            TokenUsage.Publish(new TokenUsage(
                usage.InputTokenCount,
                usage.OutputTokenCount,
                usage.TotalTokenCount,
                "conversation-summarization"));
        }

        private void MaskObservations()
        {
            var toolMessages = ChatHistory.Where(m => m.Role == AuthorRole.Tool);
            foreach (var message in toolMessages)
            {
                message.Content = ObservationMaskedPlaceholder;

                if (message.Items.Count == 0)
                {
                    message.Items.Add(new TextContent(ObservationMaskedPlaceholder));
                    continue;
                }

                var originals = message.Items.ToArray();
                message.Items.Clear();

                foreach (var item in originals)
                {
                    if (item is FunctionResultContent functionResult)
                    {
                        message.Items.Add(new FunctionResultContent(
                            functionResult.FunctionName,
                            functionResult.PluginName,
                            functionResult.CallId,
                            ObservationMaskedPlaceholder));
                    }
                    else
                    {
                        message.Items.Add(new TextContent(ObservationMaskedPlaceholder));
                    }
                }
            }
        }

        private void NotifyProductRecommendationsTokenUsed(int beforeInteractionCount)
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

        private async Task SummarizeChatAsync()
        {
            var systemMessage = ChatHistory.FirstOrDefault(m => m.Role == AuthorRole.System);
            if (systemMessage is null)
            {
                return;
            }

            var transcript = FormatConversationForSummary(ChatHistory);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                return;
            }

            var summarizationChat = new ChatHistory();
            summarizationChat.AddSystemMessage(SummarizationSystemPrompt);
            summarizationChat.AddUserMessage(
                "Summarize the following conversation transcript.\n\n" + transcript);

            var summaryResponse = await chatCompletionService.GetChatMessageContentAsync(
                summarizationChat,
                executionSettings: summarizationPromptExecutionSettings,
                kernel: null);

            if (string.IsNullOrWhiteSpace(summaryResponse.Content))
            {
                throw new InvalidOperationException("Summarization did not return any content.");
            }

            NotifySummarizationTokenUsed(summaryResponse);

            var shopSystemText = systemMessage.Content ?? string.Empty;
            ChatHistory.Clear();
            ChatHistory.AddSystemMessage(shopSystemText);
            ChatHistory.AddUserMessage("Summary of the conversation so far:\n" + summaryResponse.Content);
        }
    }
}