using SemanticKernelContextManagement.Analysis.Models;

namespace SemanticKernelContextManagement.Analysis
{
    public sealed class TokenUsageService(string experimentName) : IDisposable
    {
        private readonly string resultsPath = Path.Combine(AppContext.BaseDirectory, "Results");
        private readonly DateTimeOffset sessionTimeStamp = DateTimeOffset.UtcNow;
        private int currentTurnIndex;
        private IDisposable? sub;

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
            string resultsFileName = $"tokenUsage_{experimentName}_{sessionTimeStamp:yyyyMMdd_HHmmss}_{currentTurnIndex}.csv";
            var filePath = Path.Combine(resultsPath, resultsFileName);

            Directory.CreateDirectory(resultsPath);
            if (!File.Exists(filePath))
            {
                File.AppendAllText(filePath, "Experiment,Session,TurnIndex,InputTokens,OutputTokens,TotalTokens,Description");
            }

            File.AppendAllText(
                filePath,
                $"{Environment.NewLine}{experimentName},{sessionTimeStamp:yyyyMMdd_HHmmss},{currentTurnIndex},{usage.InputTokens},{usage.OutputTokens},{usage.TotalTokens},{usage.Description}");
        }
    }
}