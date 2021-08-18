using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

using NUnit.Framework;
using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheSample {
    public class Program {
        static void Main(string[] args) {
            WebSample.Run(args);
            // PerfTest.Run(args);
            // CelebCouples.ArticlePart1Sample();
            // CelebCouples.ReadmeSample();
        }
        class WebSample {

            readonly static HttpClient client = new HttpClient();
            static TlruCache<string, HttpResponseMessage> responseCache;

            static void Help() {
                Console.WriteLine("Web request cache tester. NOTE that real caching is quite a bit");
                Console.WriteLine("more complicated than this. This is just a simple example of how");
                Console.WriteLine("to use the TLRU cache implementation.");
                Console.WriteLine();
                Console.WriteLine("get <url>                Get response from URL");
                Console.WriteLine("timeout <seconds>        Set default timeout for new responses");
                Console.WriteLine("update <url> <seconds>   Set timeout for cached response");
                Console.WriteLine("dump                     List all cached responses");
                Console.WriteLine("now                      Print the current time");
                Console.WriteLine("quit                     Quit");
                Console.WriteLine("help                     Print help");
                Console.WriteLine();
            }

            public static void Run(string[] args) {
                responseCache = new(sets: 1, ways: 3);
                responseCache.DefaultTTU = 10; // 10 seconds is low, but it's just to make a point.
                string line;

                Help();
                Console.Write("> ");

                while (true) {
                    line = Console.In.ReadLine();
                    string[] tokens = line.Split(' ');
                    int i = 0;

                    while (i < tokens.Length) {
                        switch (tokens[i].ToLower()) {
                        case "get":
                        case "g":
                            ++i;

                            if (i < tokens.Length) {
                                string url = tokens[i];
                                UriBuilder uri = new(url);
                                GetRequest(uri.ToString()).Wait();
                            }
                            else {
                                Console.WriteLine("Error: No URL specified");
                            }

                            break;

                        case "update":
                        case "u":
                            ++i;

                            if (i < tokens.Length) {
                                string url = tokens[i];
                                UriBuilder uri = new(url);
                                var key = uri.ToString();
                                ++i;

                                if (i < tokens.Length) {
                                    try {
                                        long ttu = int.Parse(tokens[i]);
                                        responseCache.SetTimeout(key, ttu);
                                    }
                                    catch (FormatException) {
                                        Console.WriteLine($"Error: Cannot parse timeout value: {tokens[i]}");
                                    }
                                    catch (OverflowException) {
                                        Console.WriteLine($"Error: Cannot parse timeout value: {tokens[i]}");
                                    }
                                    catch (KeyNotFoundException) {
                                        Console.WriteLine($"Error: Cannot find key: {key}. It may have expired already.");
                                    }
                                }
                                else {
                                    Console.WriteLine("Error: ");
                                }
                            }
                            else {
                                Console.WriteLine("Error: ");
                            }

                            break;

                        case "now":
                        case "n":
                            Console.WriteLine(DateTime.Now.ToString());
                            break;

                        case "dump":
                        case "d":
                            foreach (string key in responseCache.Keys) {
                                Console.WriteLine($"URL: {key}, Expires: {responseCache.GetExpirationTime(key).ToLocalTime()}");
                            }

                            break;

                        case "timeout":
                        case "t":
                            ++i;

                            if (i < tokens.Length) {
                                try {
                                    long ttu = int.Parse(tokens[i]);
                                    responseCache.DefaultTTU = ttu;
                                }
                                catch (Exception) {
                                    Console.WriteLine($"Error: Cannot parse timeout value: {tokens[i]}");
                                }
                            }
                            else {
                                Console.WriteLine("Error: No timeout specified");
                            }

                            break;

                        case "quit":
                        case "exit":
                        case "q":
                            return;

                        case "help":
                        case "h":
                        case "?":
                            Help();
                            break;

                        default:
                            break;
                        }

                        ++i;
                    }

                    Console.Write("> ");
                }
            }

            static async Task GetRequest(string url) {
                try {
                    DateTime startTime = DateTime.Now;

                    if (!responseCache.TryGetValue(url, out HttpResponseMessage response)) {
                        response = await client.GetAsync(url);
                        response.EnsureSuccessStatusCode();
                        responseCache[url] = response;

                        if (response.Headers.CacheControl != null) {
                            var cacheControl = response.Headers.CacheControl;
                            var seconds = cacheControl.MaxAge;

                            if (seconds.HasValue) {
                                Console.WriteLine($"max-age: {seconds}");
                            }
                            else {
                                Console.WriteLine("No max-age");
                            }
                        }
                    }

                    string headers = response.Headers.ToString();
                    string responseContent = await response.Content.ReadAsStringAsync();
                    DateTime endTime = DateTime.Now;
                    TimeSpan duration = endTime - startTime;

                    Console.WriteLine(headers);
                    Console.WriteLine(responseContent);
                    Console.WriteLine();
                    Console.WriteLine($"Retrieval time: {duration:c}");
                }
                catch (HttpRequestException e) {
                    Console.WriteLine("Message :{0} ", e.Message);
                }
            }
        }

        public class PerfTest {
            public static void Run(string[] args) {
                InitRandomStringGen();

                // LruCache<int, int> cache = new (sets: 1, ways: 500);
                LruCache<string, string> cache = new (sets: 1000, ways: 4);
                int genCount = 250000;

                DateTime startTime = DateTime.Now;
                TimeSpan randGenTime = TimeSpan.Zero;

                while (genCount-- > 0) {
                    DateTime randGenStartTime = DateTime.Now;
                    // int rand = GenerateRandomInt();
                    string rand = GenerateRandomAlphanumericString();
                    randGenTime += DateTime.Now - randGenStartTime;
                    cache.Add(rand, rand);
                }

                TimeSpan duration = (DateTime.Now - startTime) - randGenTime;

                Console.WriteLine($"Run time: {duration:c}");
                Console.WriteLine($"String gen time: {randGenTime:c}");
            }

            /// <summary>
            /// Generates a random alphanumeric string.
            /// </summary>
            /// <param name="length">The desired length of the string</param>
            /// <returns>The string which has been generated</returns>
            /// <remarks>
            /// Credit: https://jonathancrozier.com/blog/how-to-generate-a-random-string-with-c-sharp
            /// </remarks>
            public static string GenerateRandomAlphanumericString(int length = 50) {
                const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%^&*()-={}[]|\\,<.>/?`~";
                string randomString = new string(Enumerable.Repeat(chars, length).Select(s => s[randGen.Next(s.Length)]).ToArray());
                return randomString;
            }

            public static void InitRandomStringGen() {
                stringGenSet = new();
            }

            public static void InitRandomIntGen() {
                intGenSet = new();
            }

            public static int GenerateRandomInt() {
                return randGen.Next(int.MaxValue);
            }

            static HashSet<string> stringGenSet;
            static HashSet<int> intGenSet;
            static Random randGen = new Random();
        }

        public class GreenEggsAndHam {
            public static void LfuHamAndEggs() {
                string value;

                var cache = new LfuCache<string, string>(1, 2);

                cache["Eggs"] = "Ham";
                Assert.IsTrue(cache.Count == 1);

                cache["Sam"] = "Iam";

                Assert.IsTrue(cache.Count == 2);

                Assert.IsTrue(cache.ContainsKey("Eggs"));
                Assert.IsTrue(cache.TryGetEvictKey("Green", out string evictKey));
                Assert.IsFalse(evictKey.Equals("Eggs"));

                cache["Green"] = "EggsAndHam";

                Assert.IsTrue(cache.Count == 2);

                Assert.IsFalse(cache.TryGetValue("Sam", out value));

                Assert.IsTrue(cache.TryGetValue("Green", out value));

                Assert.IsTrue(cache.ContainsKey("Eggs"));
                Assert.IsFalse(cache.ContainsKey("Sam"));
                Assert.IsTrue(cache.Contains(new KeyValuePair<string, string>("Eggs", "Ham")));
                Assert.IsFalse(cache.Contains(new KeyValuePair<string, string>("Sam", "Iam")));

                Assert.IsTrue(cache.ContainsKey("Green"));

                Assert.IsTrue(cache.Remove("Eggs"));
                Assert.IsFalse(cache.Remove("Sam"));
                Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("Green", "EggsAndHam")));

                Assert.IsTrue(cache.Count == 0);
                Assert.IsTrue(cache.Capacity == 2);
            }

            public static void LfuTest3() {
                var cache = new LfuCache<int, string>(4, 2);

                cache[1] = "value01";
                cache[2] = "value02";
                cache[3] = "value03";
                cache[4] = "value04";
                cache[5] = "value05";
                cache[6] = "value06";
                cache[7] = "value07";
                cache[8] = "value08";
                cache[9] = "value09";
                cache[10] = "value10";
                cache[11] = "value11";
                cache[12] = "value12";

                KeyValuePair<int, string>[] pairArray = new KeyValuePair<int, string>[cache.Capacity];
                cache.CopyTo(pairArray, 0);

                Assert.IsTrue(pairArray[0].Key == 10);
                Assert.IsTrue(pairArray[1].Key == 9);
                Assert.IsTrue(pairArray[2].Key == 6);
                Assert.IsTrue(pairArray[3].Key == 2);
                Assert.IsTrue(pairArray[4].Key == 7);
                Assert.IsTrue(pairArray[5].Key == 12);
                Assert.IsTrue(pairArray[6].Key == 8);
                Assert.IsTrue(pairArray[7].Key == 11);

                Assert.IsTrue(pairArray[0].Value == "value10");
                Assert.IsTrue(pairArray[1].Value == "value09");
                Assert.IsTrue(pairArray[2].Value == "value06");
                Assert.IsTrue(pairArray[3].Value == "value02");
                Assert.IsTrue(pairArray[4].Value == "value07");
                Assert.IsTrue(pairArray[5].Value == "value12");
                Assert.IsTrue(pairArray[6].Value == "value08");
                Assert.IsTrue(pairArray[7].Value == "value11");

                Assert.IsTrue(cache.Remove(9));
                Assert.IsTrue(cache.Remove(10));
                Assert.IsTrue(cache.Remove(new KeyValuePair<int, string>(11, "value11")));
                Assert.IsTrue(cache.Remove(new KeyValuePair<int, string>(12, "value12")));

                Assert.IsTrue(cache.Count == 4);

                Array.Clear(pairArray, 0, pairArray.Length);
                cache.CopyTo(pairArray, 0);

                Assert.IsTrue(pairArray[0].Key == 2);
                Assert.IsTrue(pairArray[1].Key == 6);
                Assert.IsTrue(pairArray[2].Key == 3);
                Assert.IsTrue(pairArray[3].Key == 4);

                Assert.IsTrue(pairArray[0].Value == "value02");
                Assert.IsTrue(pairArray[1].Value == "value06");
                Assert.IsTrue(pairArray[2].Value == "value03");
                Assert.IsTrue(pairArray[3].Value == "value04");

                Assert.IsFalse(cache.Remove(08));
                Assert.IsFalse(cache.Remove(10));
                Assert.IsFalse(cache.Remove(new KeyValuePair<int, string>(11, "value11")));
                Assert.IsFalse(cache.Remove(new KeyValuePair<int, string>(12, "value12")));

                cache[13] = "value13";

                Array.Clear(pairArray, 0, pairArray.Length);
                cache.CopyTo(pairArray, 0);

                Assert.IsTrue(pairArray[0].Key == 13);
                Assert.IsTrue(pairArray[1].Key == 2);
                Assert.IsTrue(pairArray[2].Key == 3);
                Assert.IsTrue(pairArray[3].Key == 4);

                Assert.IsTrue(pairArray[0].Value == "value13");
                Assert.IsTrue(pairArray[1].Value == "value02");
                Assert.IsTrue(pairArray[2].Value == "value03");
                Assert.IsTrue(pairArray[3].Value == "value04");

                Assert.IsTrue(cache.Count == 4);

                cache[2] = "value02value02";
                Assert.IsTrue(cache.Count == 4);

                string v;
                v = cache[4];
                Assert.IsTrue(v.Equals("value04"));
                v = cache[4];
                Assert.IsTrue(v.Equals("value04"));
                v = cache[4];
                Assert.IsTrue(v.Equals("value04"));
                v = cache[4];
                Assert.IsTrue(v.Equals("value04"));
                v = cache[13];
                Assert.IsTrue(v.Equals("value13"));
                v = cache[13];
                Assert.IsTrue(v.Equals("value13"));
                v = cache[13];
                Assert.IsTrue(v.Equals("value13"));
                v = cache[3];
                Assert.IsTrue(v.Equals("value03"));
                v = cache[3];
                Assert.IsTrue(v.Equals("value03"));
                v = cache[3];
                Assert.IsTrue(v.Equals("value03"));
                v = cache[2];
                Assert.IsTrue(v.Equals("value02value02"));

                cache[14] = "value14";
                cache[15] = "value15";
                cache[16] = "value16";
                cache[17] = "value17";
                cache[18] = "value18";

                Array.Clear(pairArray, 0, pairArray.Length);
                cache.CopyTo(pairArray, 0);

                Assert.IsTrue(pairArray[0].Key == 14);
                Assert.IsTrue(pairArray[1].Key == 18);
                Assert.IsTrue(pairArray[2].Key == 13);
                Assert.IsTrue(pairArray[3].Key == 17);
                Assert.IsTrue(pairArray[4].Key == 3);
                Assert.IsTrue(pairArray[5].Key == 16);
                Assert.IsTrue(pairArray[6].Key == 4);
                Assert.IsTrue(pairArray[7].Key == 15);

                cache.Clear();

                Assert.IsTrue(cache.Sets == 4);
                Assert.IsTrue(cache.Ways == 2);
                Assert.IsTrue(cache.Capacity == cache.Sets * cache.Ways);
                Assert.IsTrue(cache.Count == 0);
                Assert.IsTrue(cache.Keys.Count == 0);
                Assert.IsTrue(cache.Values.Count == 0);
                Assert.IsFalse(cache.IsReadOnly);

                cache[1] = "value01";
                cache[2] = "value02";
                cache[3] = "value03";
                cache[4] = "value04";

                Assert.DoesNotThrow(() => {
                    foreach (var item in cache) {
                        Console.WriteLine(item);
                    }
                });

                cache[5] = "value05";
                cache[6] = "value06";
                cache[7] = "value07";
                cache[8] = "value08";
                cache[9] = "value09";
                cache[10] = "value10";
                cache[11] = "value11";
                cache[12] = "value12";

                Array.Clear(pairArray, 0, pairArray.Length);
                cache.CopyTo(pairArray, 0);

                Assert.IsTrue(pairArray[0].Key == 9);
                Assert.IsTrue(pairArray[1].Key == 10);
                Assert.IsTrue(pairArray[2].Key == 2);
                Assert.IsTrue(pairArray[3].Key == 6);
                Assert.IsTrue(pairArray[4].Key == 12);
                Assert.IsTrue(pairArray[5].Key == 3);
                Assert.IsTrue(pairArray[6].Key == 11);
                Assert.IsTrue(pairArray[7].Key == 4);

                Assert.IsTrue(pairArray[0].Value == "value09");
                Assert.IsTrue(pairArray[1].Value == "value10");
                Assert.IsTrue(pairArray[2].Value == "value02");
                Assert.IsTrue(pairArray[3].Value == "value06");
                Assert.IsTrue(pairArray[4].Value == "value12");
                Assert.IsTrue(pairArray[5].Value == "value03");
                Assert.IsTrue(pairArray[6].Value == "value11");
                Assert.IsTrue(pairArray[7].Value == "value04");
            }

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

}
