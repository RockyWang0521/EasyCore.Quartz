using EasyCore.Quartz;
using WebApp.Quartz.SqlServer.Jobs;

namespace WebApp.Quartz.SqlServer;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var connectionString = builder.Configuration.GetConnectionString("Quartz")
            ?? throw new InvalidOperationException("ConnectionStrings:Quartz is required for the SQL Server demo.");

        builder.Services.AddEasyCoreQuartz(options =>
        {
            options.AddAssemblyFrom<SampleJob>();
            options.TimeZoneOffsetHours = +8;
            options.AutoCreateSchema = true;
            options.UseSqlServer(sql => sql.ConnectionString = connectionString);
            options.UseEasyCoreQuartzDashboard(dash =>
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
