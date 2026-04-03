using SemanticKernelContextManagement.Metrics.Models;

namespace SemanticKernelContextManagement.Metrics.Services
{
    public class TurnMetricsService
    {
        private readonly List<TurnMetrics> turnMetrics = [];

        public void BeginTurn() => turnMetrics.Add(new TurnMetrics()
        {
            Turn = turnMetrics.Count + 1,
        });
    }
}