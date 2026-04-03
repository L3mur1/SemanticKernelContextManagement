using Microsoft.SemanticKernel;

namespace SemanticKernelContextManagement.Framework.Kernel.Filters
{
    public class MetricsChatFilter : IFunctionInvocationFilter
    {
        public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
        {
            throw new NotImplementedException();
        }
    }
}