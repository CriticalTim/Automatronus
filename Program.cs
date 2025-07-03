using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Automatronus.Data;
using Automatronus.Services;

#if WINDOWS
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using System.Windows.Forms;
#endif

namespace Automatronus
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            var host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
#if WINDOWS
                    services.AddBlazorWebView();
#endif
                    services.AddDbContext<AutomatronusContext>(options =>
                        options.UseSqlite("Data Source=automatronus.db"));
                    services.AddScoped<ProfileService>();
                    services.AddScoped<ProjectService>();
                    services.AddScoped<WebScrapingService>();
                    services.AddScoped<PdfService>();
                })
                .Build();

            // Initialize database
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AutomatronusContext>();
                context.Database.EnsureCreated();
            }

#if WINDOWS
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm(host.Services));
#else
            Console.WriteLine("Automatronus Console Mode");
            Console.WriteLine("Web scraping and data management functionality available.");
            Console.WriteLine("Press Ctrl+C to exit.");
            
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
            };
            
            try
            {
                cancellationTokenSource.Token.WaitHandle.WaitOne();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Application shutting down...");
            }
#endif
        }
    }
}