using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json;

namespace Foundatio.Caching.Demo
{
    class Program
    {
        private static ICacheClient _cache;
        private static HttpClient _httpClient;

        static void Main()
        {
            _cache = new InMemoryCacheClient
            {
                MaxItems = 3
            };
        
            _httpClient = new HttpClient();    

            while (true)
            {
                Console.WriteLine("Please enter a Date [yyyy-mm-dd]: ");
                var dateStr = Console.ReadLine();
                DateTime date;

                if (DateTime.TryParse(dateStr, out date) == false)
                {
                    Console.WriteLine("Invalid Date.");
                    continue;
                }

                if (date.Date > DateTime.UtcNow.Date)
                {
                    Console.WriteLine("Cannot specify a date in the future.");
                    continue;
                }

                var exchange = GetCurrencyRate(date);
                    decimal canadian;
                    exchange.Rates.TryGetValue("CAD", out canadian);
                    Console.WriteLine($"USD to CAD = {canadian}");
                
            }
        }

        private static CurrencyExchange GetCurrencyRate(DateTime date)
        {
            var key = date.Date.ToString("yyyy-MM-dd");

            var cachedExchange = _cache.GetAsync<CurrencyExchange>(key).Result;

            if (cachedExchange.HasValue)
            {
                Console.WriteLine("Found in cache");
                return cachedExchange.Value;
            }

            Console.WriteLine("Fetching from service");

            var response = _httpClient.GetAsync("http://api.fixer.io/"+key+"?base=USD").Result;
            var json = response.Content.ReadAsStringAsync().Result;

            var exchange = JsonConvert.DeserializeObject<CurrencyExchange>(json);
            _cache.AddAsync(key, exchange).Wait();
            
            return exchange;
        }
    }
       
    public class CurrencyExchange
    {
        public string Base {get; set; }
        public DateTime Date { get; set; }
        public Dictionary<string, decimal> Rates { get; set; }
    }
}
