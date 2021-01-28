using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Server.Kestrel.Core.Features;
using System.Net.Http;
using Love_Bot;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace LoveBot {
    public class Startup {
        public void Configure(IApplicationBuilder app) {
            var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();

            app.UseStaticFiles();

            app.Run(async (context) => {
                context.Request.ContentType = "application/json";
                IFormCollection col = await context.Request.ReadFormAsync();
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonConvert.SerializeObject(new WebsiteConfig()));
                //await context.Response
                //    .WriteAsync(new WebsiteConfig().ToString);

                //if (serverAddressesFeature != null) {
                //    await context.Response
                //        .WriteAsync("<p>Listening on the following addresses: " +
                //            string.Join(", ", serverAddressesFeature.Addresses) +
                //            "</p>");
                //}

                //await context.Response.WriteAsync("<p>Request URL: " +
                //    $"{context.Request.GetDisplayUrl()}<p>");
            });
        }

    }
}