namespace SemanticKernelContextManagement.Metrics.Models
{
    public class TurnMetrics
    {
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public int TotalTokens => InputTokens + OutputTokens;
        public int Turn { get; set; }
    }
}