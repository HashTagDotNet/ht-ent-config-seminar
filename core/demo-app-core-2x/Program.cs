using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace demo_app_core_2x
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, configBuilder) =>
                {
                    var settings = configBuilder.Build();
                    configBuilder.AddHTConfigurationSource(settings["ConnectionStrings:ConfigServiceUrl"]);                    
                })
                .UseStartup<Startup>();
    }

    public static class HTConfigurationExtensions
    {
        public static IConfigurationBuilder AddHTConfigurationSource(this IConfigurationBuilder configBuilder, string connectionString)
        {
            configBuilder.Sources.Add(new HTConfigurationSource(connectionString));
            return configBuilder;
        }
    }
    public class HTConfigurationSource:IConfigurationSource
    {
        private object connnectionString;

        public HTConfigurationSource(string connectionString)
        {
            var _cnString = connnectionString;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            throw new NotImplementedException();
        }
        
    }
    public class HTConfigurationProvider : ConfigurationProvider
    {
        private string _cnString;

        public HTConfigurationProvider(string connectionString)
        {
            _cnString = connectionString;
        }

        public override void Load()
        {
            using(var httpClient = new HttpClient())
            {
                httpClient.BaseAddress = new Uri(_cnString, UriKind.RelativeOrAbsolute);
                httpClient.DefaultRequestHeaders.Accept.Clear();
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

              //  var contentToSend = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

             //   var httpResult = httpClient.PostAsync("api/settings", contentToSend).ConfigureAwait(false).GetAwaiter().GetResult();
            }
            base.Load();
        }
    }

}
