using System.Reactive.Subjects;

namespace SemanticKernelContextManagement.Models
{
    public record TokenUsage(
        int InputTokens,
        int OutputTokens,
        int TotalTokens,
        string Description)
    {
        private static readonly Subject<TokenUsage> subject = new Subject<TokenUsage>();

        public static IObservable<TokenUsage> TokenUsageStream => subject;

        public static void Publish(TokenUsage tokenUsage) => subject.OnNext(tokenUsage);
    }
}