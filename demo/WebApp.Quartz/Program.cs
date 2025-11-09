using EasyCore.Quartz;

namespace WebApp.Quartz
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Use EasyCore.Quartz
            builder.Services.EasyCoreQuartz(options =>
            {
                options.EasyCoreQuartzMySql(mysql =>
                {
                    mysql.ConnectionString = "server=localhost;port=3307;user id=root;password=123;database=EasyCoreDtm;";
                });

                options.TimeZoneOffsetHours = +8;

                //options.EasyCoreQuartzSqlServer(sqlserver =>
                //{
                //    sqlserver.ConnectionString = "Server=192.168.157.142;Database=EasyCoreDtm;User Id=sa;Password=Sa123456;TrustServerCertificate=True;Connect Timeout=10;";
                //});
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();

                app.UseSwaggerUI();
            }

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
