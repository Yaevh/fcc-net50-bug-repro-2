using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Szlem.AspNetCore.Common
{
    public class HostBuilderFactory
    {
        public IHostBuilder Create<TStartup>(string[] args) where TStartup : class
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webhostBuilder => webhostBuilder.UseStartup<TStartup>())
                .ConfigureAppConfiguration((hostingContext, configBuilder) =>
                {
                    configBuilder
                        .AddJsonFile(BuildRelativeDataPath("../../../config/appsettings.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildRelativeDataPath($@"../../../config/appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildRelativeDataPath("../config/appsettings.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildRelativeDataPath($@"../config/appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildAppDataPath("appsettings.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildAppDataPath($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json"), optional: true, reloadOnChange: true)
                        .AddJsonFile(BuildAppDataPath("hosting.json"), optional: true, reloadOnChange: true);
                })
                .UseNLog();
        }

        public static string BuildAppDataPath(string fileName)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Szlem",
                fileName
            );
        }

        public static string BuildRelativeDataPath(string fileName)
        {
            return Path.GetFullPath(Directory.GetCurrentDirectory() + Path.DirectorySeparatorChar + fileName);
        }

        public static NLog.LogFactory BuildNLogFactory()
        {
            NLog.LayoutRenderers.LayoutRenderer.Register("appData", (logEventInfo, logConfig) => Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

            var nlogConfigPath = BuildAppDataPath("nlog.config");
            if (!File.Exists(nlogConfigPath))
                nlogConfigPath = Path.GetFullPath(Directory.GetCurrentDirectory() + "/../config/nlog.config");
            if (!File.Exists(nlogConfigPath))
                nlogConfigPath = Path.GetFullPath(Directory.GetCurrentDirectory() + "/../../../config/nlog.config");

            return NLogBuilder.ConfigureNLog(nlogConfigPath);
        }
    }
}
