using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Google.Cloud.Firestore;
using System.Linq;
using System.Timers;

namespace DatabaseProject
{
    /// <summary>
    ///  Main class of the database component.
    /// </summary>
    internal class Program
    {
        // Timer for 24 hours (unused)
        //private static Timer aTimer = new System.Timers.Timer(24 * 60 * 60 * 1000);

        public static FirestoreDb firestoreDb = FirestoreDb.Create("bdo-developer-test");
        public static List<CryptoPair> cryptoPairs;
        public static List<CryptoPair> cryptoPairsDB = new List<CryptoPair>();

        /// <summary>
        ///  This is the entry point of the database component.
        /// </summary>
        static void Main(string[] args)
        {
            // Firestore credentials
            var path = AppDomain.CurrentDomain.BaseDirectory + @"bdo-developer-test-firebase-adminsdk-l4qp3-ec36aec082.json";
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", path);

            // Here i use GetAwaiter as a workaround so I can do asynchronous operations in the main method
            cryptoPairs = fetchCryptoPairs().GetAwaiter().GetResult();
            CollectionReference cRef = firestoreDb.Collection("CryptoPairs");

            // Iterating through all the CryptoPair objects from twelvedata and adding them to Firestore
            foreach (var item in cryptoPairs)
            {

                DocumentReference dRef = cRef.Document($"{item.Currency_base} to {item.Currency_quote}");

                // Firestore add method, accepts custom objects directly
                cRef.AddAsync(item).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        ///  Fetches the cryptocurrency pairs list to be uploaded to the database. Compares results from the API and the database, and removes duplicate entries.
        /// </summary>
        /// <returns>Task<List<CryptoPair>></returns>
        async static Task<List<CryptoPair>> fetchCryptoPairs()
        {

            List<CryptoPair> cryptoPairsAPI;

            // Fetch the currency pair list from the API and store them in a list
            using var client = new HttpClient();
            client.BaseAddress = new Uri("https://api.twelvedata.com");
            client.DefaultRequestHeaders.Add("User-Agent", "C# console program");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var url = "/cryptocurrencies";

            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var resp = await response.Content.ReadAsStringAsync();

            Root rawData = JsonConvert.DeserializeObject<Root>(resp);
            cryptoPairsAPI = rawData.data;

            // Fetch the already stored currency pairs from the DB and add them to another list
            Query query = firestoreDb.Collection("CryptoPairs");
            QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

            foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
            {
                CryptoPair cryptoPair = documentSnapshot.ConvertTo<CryptoPair>();
                cryptoPairsDB.Add(cryptoPair);
            }

            // Combine the lists from the API and the DB
            List<CryptoPair> result = cryptoPairsAPI.Union(cryptoPairsDB).ToList();

            // Remove duplicate entries based on the Symbol property (unique key)
            List<CryptoPair> trimmedList = result.GroupBy(x => x.Symbol).Select(x => x.First()).ToList();

            return trimmedList;
        }
    }

    // The Root class is needed in order for the DeserializeObject method to function properly.
    // This is because the response from twelvedata is not just the requred jSON array, but rather an object containing it.

    /// <summary>
    ///  Root class needed in order for the DeserializeObject method to function properly. Acts as a wrapper class for the list of cryptocurrency pairs from the API.
    /// </summary>

    public class Root
    {
        public List<CryptoPair> data { get; set; }
        public string status { get; set; }
    }

    /// <summary>
    ///  Model class for the cryptocurrency pairs fetched from the API.
    /// </summary>
    [FirestoreData]
    public class CryptoPair
    {
        [FirestoreProperty]
        /// <summary>
        ///  The cryptocurrency pair symbol.
        /// </summary>
        public string Symbol { get; set; }
        [FirestoreProperty]
        /// <summary>
        ///  The exchanges in which the cryptocurrency pair can be traded.
        /// </summary>
        public List<string> Available_exchanges { get; set; }
        [FirestoreProperty]
        /// <summary>
        ///  The currency that is being converted.
        /// </summary>
        public string Currency_base { get; set; }
        [FirestoreProperty]
        /// <summary>
        ///  The currency that is being converted to.
        /// </summary>
        public string Currency_quote { get; set; }
    }
}
