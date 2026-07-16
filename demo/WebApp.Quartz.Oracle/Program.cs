using EasyCore.Quartz;
using WebApp.Quartz.Oracle.Jobs;

namespace WebApp.Quartz.Oracle;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
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
            options.EasyCoreQuartzDashboard(dash =>
            {
                dash.PathMatch = "/easy-quartz";
                dash.Username = "admin";
                dash.Password = "admin123";
            });
        });

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapControllers();
        app.Run();
    }
}
