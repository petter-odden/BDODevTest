using DatabaseProject;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace UserApplication
{
    /// <summary>
    ///  Main class of the user application component.
    /// </summary>
    internal class Program
    {
        // Should ideally be stored more securely
        private static string twelveDataAPIKey = "c4799342270e4ea09fc50c6512f38d16";

        public static List<CryptoPair> cryptoPairsDB = new List<CryptoPair>();
        public static FirestoreDb firestoreDb = FirestoreDb.Create("bdo-developer-test");

        /// <summary>
        ///  The entry point of the user application component.
        ///  Handles user input and processing of that input.
        /// </summary>
        static void Main(string[] args)
        {
            // Firestore credentials
            var path = AppDomain.CurrentDomain.BaseDirectory + @"bdo-developer-test-firebase-adminsdk-l4qp3-ec36aec082.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // Fetch the already stored currency pairs from the DB and add them to another list
            Query query = firestoreDb.Collection("CryptoPairs");
            QuerySnapshot querySnapshot = query.GetSnapshotAsync().GetAwaiter().GetResult();

            foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
            {
                CryptoPair cryptoPair = documentSnapshot.ConvertTo<CryptoPair>();
                cryptoPairsDB.Add(cryptoPair);
            }

            // Alphabetically ordering of the list for an easier user experience
            IEnumerable<CryptoPair> orderedCryptoPairList = cryptoPairsDB.OrderBy(x => x.Currency_base);

            // Take user input and process accordingly
            while (true)
            {
                Console.WriteLine("Please enter a crypto pair in the format BTC/ETH etc. For a list of available pairs, type 'list'. To quit, type 'quit'\n");
                var userInput = Console.ReadLine().ToString();
                bool containsItem = cryptoPairsDB.Any(item => item.Symbol == userInput);
                if (userInput == "list")
                {
                    foreach (var item in orderedCryptoPairList)
                    {
                        Console.WriteLine($"{ item.Symbol}      {item.Currency_base} to {item.Currency_quote}");
                        continue;
                    }
                }
                else if (userInput == "quit")
                {
                    break;
                }
                else if (!containsItem)
                {
                    Console.WriteLine("Invalid crypto pair.\n");
                    continue;
                }
                else
                {
                    while (true)
                    {
                        Console.WriteLine("Please enter amount (whole numbers)\n");
                        try
                        {
                            var amount = int.Parse(Console.ReadLine());
                            var exchangeRate = getExchangeRate(userInput, amount).GetAwaiter().GetResult();

                            Console.WriteLine($"\nSymbol: {exchangeRate.symbol}\nExchange rate: {exchangeRate.rate}\nBase currency amount: {amount}\nQuote currency amount: {exchangeRate.amount}\nUnix timestamp: {exchangeRate.timestamp}\n");

                            break;
                        }
                        catch (FormatException)
                        {
                            Console.WriteLine("Invalid amount\n");
                            continue;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  Method for fetching the exchange rates of a cryptocurrency pair.
        ///  <param name="symbol">The symbol of the cryptocurrency pair in which the exchange rate is wanted</param>
        ///  <param name="amount">The amount of currency to be converted</param>
        /// </summary>
        /// <returns>Task<ExchangeRate></returns>
        async static Task<ExchangeRate> getExchangeRate(string symbol, double amount = 1)
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.twelvedata.com");
            client.DefaultRequestHeaders.Add("User-Agent", "C# console program");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Build the url
            var url = $"currency_conversion?symbol={symbol}&amount={amount}&apikey={twelveDataAPIKey}";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();

            ExchangeRate exchangeRate = JsonConvert.DeserializeObject<ExchangeRate>(resp);
            
            return exchangeRate;
        }
    }

    /// <summary>
    ///  Model class of the exchange rates fetched from the API.
    /// </summary>
    public class ExchangeRate
    {
        /// <summary>
        ///  Symbol of the cryptocurrency pair in which the exchange rate is wanted 
        /// </summary>
        public string symbol { get; set; }
        /// <summary>
        ///  Real-time exchange rate for the corresponding symbol
        /// </summary>
        public double rate { get; set; }
        /// <summary>
        ///  Amount of converted currency
        /// </summary>
        public double amount { get; set; }
        /// <summary>
        ///  Unix timestamp of the rate
        /// </summary>
        public int timestamp { get; set; }
    }

}
