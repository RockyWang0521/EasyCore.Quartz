using EasyCore.Invocation;

namespace WebApp.Quartz.SqlServer.Invocations;

public sealed class AuditAttribute : InvocationAttribute<AuditInvocation>
{
    public AuditAttribute()
    {
        Order = 0;
    }
}
