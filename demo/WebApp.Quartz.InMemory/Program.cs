using EasyCore.Quartz;
using WebApp.Quartz.InMemory.Jobs;

namespace WebApp.Quartz.InMemory;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // In-memory (RAM) job store � no database package required.
        builder.Services.AddEasyCoreQuartz(options =>
        {
            options.AddAssemblyFrom<SampleJob>();
            options.TimeZoneOffsetHours = +8;
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
