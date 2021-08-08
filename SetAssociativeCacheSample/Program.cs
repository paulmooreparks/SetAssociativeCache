using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading.Tasks;

using NUnit.Framework;
using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheSample {
    public class Program {
        static void Main(string[] args) {
            new WebSample().Run(args);
        }

        public class CelebCouples {
            public static void ReadmeSample() {
                /* Create a cache that maps string keys to string values, with 2 sets of 4 elements, or "ways". 
                In this example, we use the LruCache implementation, which removes the least-recently used 
                item (LRU) when a new item is added to a full set. Slots are tracked in an array rather than a 
                linked list, in order to keep it CPU-cache friendly. 

                If we decide later that a least-frequently used cache (LFU) cache is more appropriate, we can 
                change LruCache to LfuCache. We could also add new classes with other implementations. */

                var coupleCache = new LruCache<string, string>(sets: 2, ways: 4);

                Console.WriteLine($"There is room for {coupleCache.Capacity} couples. Let the games begin....");

                coupleCache["Brad"] = "Angelina";
                coupleCache["Ben"] = "Jennifer";
                coupleCache["Kanye"] = "Kim";
                coupleCache["Sonny"] = "Cher";
                coupleCache["Desi"] = "Lucy";
                coupleCache["Donald"] = "Ivana";
                coupleCache["Burt"] = "Loni";
                coupleCache["Johnny"] = "June";
                coupleCache["John"] = "Yoko";
                coupleCache["Kurt"] = "Goldie";

                Console.WriteLine($"Out of 10 couples added, {coupleCache.Count} couples remain in the cache");
                Console.WriteLine();

                foreach (var couple in coupleCache) {
                    Console.WriteLine($"{couple.Key} loves {couple.Value}");
                }

                /* Note that Brangelina and Bennifer have been evicted from their respective sets. */
            }

            public static void ArticlePart1Sample() {
                /* Create a cache that maps string keys to string values, with 3 sets of 3 elements, or "ways". 
                The concrete type implements the specific cache-eviction policy. In this example, we use LruCache, 
                which removes the least-recently used item (LRU) when a new item is added to a full set. Slots 
                are tracked in an array rather than a linked list, in order to keep it CPU-cache friendly. 
                If we decide later that a least-frequently used cache (LFU) cache is more appropriate, we can 
                change LruCache to LfuCache. We could also add new classes with other implementations. */
                var coupleCache = new LruCache<string, string>(sets: 3, ways: 3);

                Console.WriteLine($"There is room for {coupleCache.Capacity} couples. Let the games begin....");

                coupleCache["Brad"] = "Angelina";
                coupleCache["Kanye"] = "Kim";
                coupleCache["Ben"] = "Jennifer";
                coupleCache["Burt"] = "Loni";
                coupleCache["Kurt"] = "Goldie";
                coupleCache["Sonny"] = "Cher";
                coupleCache["Desi"] = "Lucy";
                coupleCache["Johnny"] = "June";
                coupleCache["John"] = "Yoko";
                coupleCache["Tom"] = "Rita";

                //coupleCache["David"] = "Victoria";
                //coupleCache["Will"] = "Jada";
                //coupleCache["Kevin"] = "Kyra";
                //coupleCache["Keith"] = "Nicole";

                Console.WriteLine($"Out of all couples added, {coupleCache.Count} couples remain in the cache");
                Console.WriteLine();

                foreach (var couple in coupleCache) {
                    Console.WriteLine($"{couple.Key} loves {couple.Value}");
                }

                var bradPartner = coupleCache.GetValueOrDefault("Brad");
                bool tryAddResult = coupleCache.TryAdd("Kanye", "Edna");
                // var immDict = coupleCache.ToImmutableDictionary();

                IDictionary<string, string> dict = coupleCache;

                dict.Remove("Kanye", out string kanyePartner);
            }
        }
    }


    class WebSample {

        readonly HttpClient client = new HttpClient();
        ISetAssociativeCache<string, string> responseCache;

        public void Run(string[] args) {
            responseCache = new LruCache<string, string>(sets: 2, ways: 3);
            string line;

            Console.Write("URL: ");

            while (!string.IsNullOrEmpty(line = Console.In.ReadLine())) {
                string url = line.Trim();
                UriBuilder uri = new UriBuilder(url);
                DateTime startTime = DateTime.Now;
                GetRequest(uri.ToString()).Wait();
                DateTime endTime = DateTime.Now;
                TimeSpan duration = endTime - startTime;

                Console.WriteLine();
                Console.WriteLine($"Retrieval time: {duration.ToString("c")}");
                Console.WriteLine();
                Console.Write("URL: ");
            }
        }

        async Task GetRequest(string url) {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try {
                if (!responseCache.TryGetValue(url, out string headers)) {
                    HttpResponseMessage response = await client.GetAsync(url);
                    headers = response.Headers.ToString();
                    response.EnsureSuccessStatusCode();
                    _ = await response.Content.ReadAsStringAsync();
                    responseCache.TryAdd(url, headers);
                }

                Console.WriteLine(headers);
            }
            catch (HttpRequestException e) {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }
    }

}
