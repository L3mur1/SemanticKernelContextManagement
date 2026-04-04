using SemanticKernelContextManagement.Models;

namespace SemanticKernelContextManagement.Analysis
{
    public sealed class TokenUsageService : IDisposable
    {
        private readonly string resultsPath = Path.Combine(AppContext.BaseDirectory, "Results");
        private readonly DateTimeOffset sessionTimeStamp = DateTimeOffset.UtcNow;
        private IDisposable? sub;
        private int currentTurnIndex;

        public void Dispose()
        {
            sub?.Dispose();
            GC.SuppressFinalize(this);
        }

        public void RecordTurnUsage(int turnIndex)
        {
            currentTurnIndex = turnIndex;
            sub ??= TokenUsage.TokenUsageStream.Subscribe(SaveTokenUsage);
        }

        private void SaveTokenUsage(TokenUsage usage)
        {
            string resultsFileName = $"tokenUsage_{sessionTimeStamp:yyyyMMdd_HHmmss}_{currentTurnIndex}.csv";
            var filePath = Path.Combine(resultsPath, resultsFileName);

            Directory.CreateDirectory(resultsPath);
            if (!File.Exists(filePath))
            {
                File.AppendAllText(filePath, "TurnIndex,InputTokens,OutputTokens,TotalTokens,Description");
            }

            File.AppendAllText(
                filePath,
                $"{Environment.NewLine}{currentTurnIndex},{usage.InputTokens},{usage.OutputTokens},{usage.TotalTokens},{usage.Description}");
        }
    }
}