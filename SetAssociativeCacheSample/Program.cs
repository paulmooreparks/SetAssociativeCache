using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using NUnit.Framework;
using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheSample {
    class Program {
        static void Main(string[] args) {
            CelebCouples();
        }

        static void CelebCouples() {
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

            var bradPartner = coupleCache.GetValueOrDefault("Brad");
            bool tryAddResult = coupleCache.TryAdd("Kanye", "Edna");
            // var immDict = coupleCache.ToImmutableDictionary();

            IDictionary<string, string> dict = coupleCache;

            dict.Remove("Kanye", out string kanyePartner);

            Console.WriteLine($"Out of all couples added, {coupleCache.Count} couples remain in the cache");
            Console.WriteLine();

            foreach (var couple in coupleCache) {
                Console.WriteLine($"{couple.Key} loves {couple.Value}");
            }
        }

        static void LruHamAndEggs() {
            string value;

            var cache = new LruCache<string, string>(1, 2);

            cache["Eggs"] = "Ham";
            Assert.IsTrue(cache.Count == 1);

            cache["Sam"] = "Iam";

            Assert.IsTrue(cache.Count == 2);

            Assert.IsTrue(cache.TryGetEvictKey("Green", out string evictKey));
            Assert.IsTrue(evictKey.Equals("Eggs"));

            cache["Green"] = "EggsAndHam";

            Assert.IsTrue(cache.Count == 2);

            Assert.IsTrue(cache.TryGetValue("Sam", out value));

            Assert.IsTrue(cache.TryGetValue("Green", out value));

            Assert.IsFalse(cache.ContainsKey("Eggs"));
            Assert.IsTrue(cache.ContainsKey("Sam"));
            Assert.IsFalse(cache.Contains(new KeyValuePair<string, string>("Eggs", "Ham")));
            Assert.IsTrue(cache.Contains(new KeyValuePair<string, string>("Sam", "Iam")));

            Assert.IsTrue(cache.ContainsKey("Green"));

            Assert.IsFalse(cache.Remove("Eggs"));
            Assert.IsTrue(cache.Remove("Sam"));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("Green", "EggsAndHam")));

            Assert.IsTrue(cache.Count == 0);
            Assert.IsTrue(cache.Capacity == 2);
        }

    }
}
