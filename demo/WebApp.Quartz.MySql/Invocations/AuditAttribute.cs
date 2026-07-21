using EasyCore.Invocation;

namespace WebApp.Quartz.MySql.Invocations;

public sealed class AuditAttribute : InvocationAttribute<AuditInvocation>
{
    public AuditAttribute()
    {
        Order = 0;
    }
}
