using Microsoft.SemanticKernel;

namespace SemanticKernelContextManagement.Metrics.Services
{
    public class TurnMetricsFunctionInvocationFilter : IFunctionInvocationFilter
    {
        public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }
}