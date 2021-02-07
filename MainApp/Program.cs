using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MainApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args)
        {
            var webhost = WebHost
                .CreateDefaultBuilder(args)
                //kestrel работает, только что-то с контекстами, не получает пользователя
                //.UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    //TODO Пока стоити фильтер, только на SQL, собственные сформированные сообщения не отображаются
                    logging.AddFilter((category, level) => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information).AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                })
                .UseIISIntegration()
                .UseStartup<Startup>();
            //    .UseHttpSys(options =>
            //{
            //    options.Authentication.Schemes = AuthenticationSchemes.NTLM;
            //    options.Authentication.AllowAnonymous = false;
            //});

            //if (string.IsNullOrEmpty(webhost.GetSetting("IIS_HTTPAUTH")))
            //{
            //    webhost = webhost.UseHttpSys(options =>
            //    {
            //        options.Authentication.Schemes = AuthenticationSchemes.NTLM | AuthenticationSchemes.Negotiate;
            //        options.Authentication.AllowAnonymous = false;
            //    });
            //}
            return webhost;
        }
    }
}
