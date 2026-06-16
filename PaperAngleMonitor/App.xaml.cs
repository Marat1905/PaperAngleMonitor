using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaperAngleMonitor.Services;
using PaperAngleMonitor.ViewModels;
using PaperAngleMonitor.Views;
using System.Windows;
using Microsoft.Extensions.Configuration;
using PaperAngleMonitor.Models;

namespace PaperAngleMonitor
{
    public partial class App : Application
    {
        private IHost? _host;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder()
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddLogging(configure =>
                    {
                        configure.AddConsole();
                        configure.AddDebug();
                    });

                    // Read BaslerSettings from configuration
                    services.AddSingleton(provider =>
                    {
                        var config = provider.GetRequiredService<IConfiguration>();
                        var settings = new BaslerSettings();
                        config.GetSection("BaslerSettings").Bind(settings);
                        return settings;
                    });

                    services.AddSingleton<IVideoService, BaslerVideoService>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MainWindow>();
                })
                .Build();

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _host?.Dispose();
            base.OnExit(e);
        }
    }
}