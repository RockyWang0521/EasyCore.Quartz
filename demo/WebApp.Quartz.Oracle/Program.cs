using EasyCore.Quartz;
using EasyCore.Quartz.Dashboard;
using WebApp.Quartz.Shared.Jobs;

namespace WebApp.Quartz.Oracle;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers()
            .AddApplicationPart(typeof(SampleJob).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connectionString = builder.Configuration.GetConnectionString("Quartz")
            ?? throw new InvalidOperationException("ConnectionStrings:Quartz is required for the Oracle demo.");

        builder.Services.EasyCoreQuartz(options =>
        {
            options.AddAssemblyFrom<SampleJob>();
            options.TimeZoneOffsetHours = +8;
            options.AutoCreateSchema = true;
            options.UseOracle(ora => ora.ConnectionString = connectionString);
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseEasyCoreQuartzDashboard("/easy-quartz", options =>
        {
            options.Authorization.Add(new LocalRequestsOnlyAuthorizationFilter());
        });

        app.MapControllers();
        app.Run();
    }
}
