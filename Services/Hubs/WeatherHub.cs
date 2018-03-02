using Microsoft.AspNetCore.SignalR;
using RestSharp;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Vue2SpaSignalR.Services.Hubs
{
    public class WeatherHub : Hub
    {

    }

    public class Weather : HostedService
    {
        public Weather(IHubContext<WeatherHub> context)
        {
            Clients = context.Clients;
        }

        private IHubClients Clients { get; }

        private List<WeatherForecast> _forecast = new List<WeatherForecast>();
        private DateTime _lastrun = DateTime.Now;

        private static readonly string[] Summaries = {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public async Task UpdateWeatherForecasts()
        {
            if (DateTime.Now.Subtract(_lastrun).TotalMinutes >= 10)
            {
                await UpdateForecast("18040");
            }

            await Clients.All.InvokeAsync("weather", _forecast);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            await UpdateForecast("18040");

            while (true)
            {
                await UpdateWeatherForecasts();

                var task = Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                try
                {
                    await task;
                }
                catch (TaskCanceledException)
                {
                    return;
                }
            }
        }

        private async Task UpdateForecast(string zip)
        {
            RestClient client = new RestClient("https://api.openweathermap.org");
            RestRequest request = new RestRequest("data/2.5/weather?zip={zip},us&APPID={apikey}", Method.GET);

            request.AddUrlSegment("zip", zip);
            request.AddUrlSegment("apikey", "");

            var response = await client.ExecuteTaskAsync(request);

            var json = JsonConvert.DeserializeObject<dynamic>(response.Content);

            int temp = (int)(json.main.temp - 273.15);

            _forecast = new List<WeatherForecast>
            {
                new WeatherForecast {
                    DateFormatted = DateTime.Now.ToString(),
                    TemperatureC = temp,
                    Summary = json.weather[0].main + " - " + json.weather[0].description
                }
            };

            _lastrun = DateTime.Now;
        }

        public class WeatherForecast
        {
            public string DateFormatted { get; set; }
            public int TemperatureC { get; set; }
            public string Summary { get; set; }

            public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
        }
    }
}
