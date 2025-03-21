using TestApi.Controllers;

namespace TestApi;

public class SelfHosting
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        builder.Services.AddSingleton<IJobStore, JobStore>();
        var app = builder.Build();
        app.MapControllers();
        app.Run();
    }
}

