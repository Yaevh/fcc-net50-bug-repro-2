using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Szlem.AspNetCore.Common;

namespace Szlem.AspNetCore
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = HostBuilderFactory.BuildNLogFactory().GetCurrentClassLogger();
            try
            {
                logger.Info("init main");

                var host = CreateHostBuilder(args).Build();

                logger.Debug("host built, running initialization code");

                await host.InitAsync();

                logger.Debug("initialization code completed, running host");

                await host.RunAsync();
            }
            catch (Exception ex)
            {
                // NLog: catch setup and initialization errors
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                logger.Info("stopping application");
                // NLog: Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            new HostBuilderFactory().Create<Startup>(args);
    }
}
