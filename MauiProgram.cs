using budget.models;
using Microsoft.Extensions.Logging;
using CommunityToolkit.Maui;

namespace budget
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder.UseMauiApp<App>().ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            }).UseMauiCommunityToolkit();
            string dbPath = Path.Combine(FileSystem.AppDataDirectory, "budget_local_db.db3");
            builder.Services.AddSingleton<AppDbContext>(new AppDbContext());
            var app = builder.Build();
            using (var scope = app.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                Task.Run(() => dbContext.SeedData()).Wait(); 
            }

            return app;
        }
    }
}