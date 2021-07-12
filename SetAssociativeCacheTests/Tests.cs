using System.Linq;
using System;

using NUnit.Framework;

using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheTests {
    /*
    This is not an exhaustive set of tests yet. I'll add more when I get some time to loop back around to this project.
    */
    public class Tests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void InitTests() {
            var cache1 = new LfuCache<string, string>(64, 4);
            Assert.IsTrue(cache1.Capacity == 64 * 4);
            Assert.IsTrue(cache1.Sets == 64);
            Assert.IsTrue(cache1.Ways == 4);
            Assert.IsTrue(cache1.Count == 0);
            Assert.IsTrue(cache1.Keys.Count == 0);
            Assert.IsTrue(cache1.Values.Count == 0);

            var cache2 = new LruCache<string, string>(64, 4);
            Assert.IsTrue(cache2.Capacity == 64 * 4);
            Assert.IsTrue(cache2.Sets == 64);
            Assert.IsTrue(cache2.Ways == 4);
            Assert.IsTrue(cache2.Count == 0);
            Assert.IsTrue(cache2.Keys.Count == 0);
            Assert.IsTrue(cache2.Values.Count == 0);

            var cache3 = new MruCache<string, string>(64, 4);
            Assert.IsTrue(cache3.Capacity == 64 * 4);
            Assert.IsTrue(cache3.Sets == 64);
            Assert.IsTrue(cache3.Ways == 4);
            Assert.IsTrue(cache3.Count == 0);
            Assert.IsTrue(cache3.Keys.Count == 0);
            Assert.IsTrue(cache3.Values.Count == 0);

        }

        [Test]
        public void LruTest1() {
            var cache = new LruCache<int, int>(16, 4);
            Assert.IsTrue(cache.Capacity == (16 * 4));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue(123, out int value));
        }

        [Test]
        public void LruTest2() {
            var cache = new LruCache<string, string>(6, 6);
            Assert.IsTrue(cache.Capacity == (6 * 6));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue("abc", out string value));
        }

        [Test]
        public void LruTest3() {
            var cache = new LruCache<int, int>(4, 4);
            cache.Add(38, 9);
            Assert.IsTrue(cache.Count == 1);
            cache.Add(34, 123);
            Assert.IsTrue(cache.Count == 2);
            Assert.IsTrue(cache.ContainsKey(38));
            Assert.IsTrue(cache.ContainsKey(34));
            Assert.IsFalse(cache.ContainsKey(88));
        }


        [Test]
        public void LfuTest1() {
            var cache = new LfuCache<int, int>(16, 4);
            Assert.IsFalse(cache.TryGetValue(123, out int value));
        }

        [Test]
        public void LfuTest2() {
            var cache = new LfuCache<string, string>(6, 6);
            Assert.IsTrue(cache.Capacity == (6 * 6));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue("abc", out string value));
        }

        [Test]
        public void LfuTest3() {
            var cache = new LfuCache<int, int>(4, 4);
            cache.Add(38, 9);
            Assert.IsTrue(cache.Count == 1);
            cache.Add(34, 123);
            Assert.IsTrue(cache.Count == 2);
            Assert.IsTrue(cache.ContainsKey(38));
            Assert.IsTrue(cache.ContainsKey(34));
            Assert.IsFalse(cache.ContainsKey(88));
        }

        [Test]
        public void LruHamAndEggs() {
            string value;

            //1, 2, LRUReplacementAlgo
            var cache = new LruCache<string, string>(1, 2);

            //Set, Eggs, Ham
            cache["Eggs"] = "Ham";
            //Set, Sam, Iam
            cache["Sam"] = "Iam";

            Assert.IsTrue(cache.TryGetEvictKey("Green", out string evictKey));
            Assert.IsTrue(evictKey.Equals("Eggs"));

            //Set, Green, EggsAndHam
            cache["Green"] = "EggsAndHam";

            //Get, Sam
            Assert.IsTrue(cache.TryGetValue("Sam", out value));

            //Get, Green
            Assert.IsTrue(cache.TryGetValue("Green", out value));

            //ContainsKey, Eggs
            Assert.IsFalse(cache.ContainsKey("Eggs"));
            //ContainsKey, Sam
            Assert.IsTrue(cache.ContainsKey("Sam"));
            //ContainsKey, Green
            Assert.IsTrue(cache.ContainsKey("Green"));
        }

        [Test]
        public void MruHamAndEggs() {
            string value;

            //1, 2, MRUReplacementAlgo
            var cache = new MruCache<string, string>(1, 2);

            //Set, Eggs, Ham
            cache["Eggs"] = "Ham";
            //Set, Sam, Iam
            cache["Sam"] = "Iam";
            //Set, Green, EggsAndHam
            cache["Green"] = "EggsAndHam";

            //Get, Eggs
            Assert.IsTrue(cache.TryGetValue("Eggs", out value));
            //Get, Green
            Assert.IsTrue(cache.TryGetValue("Green", out value));

            //ContainsKey, Eggs
            Assert.IsTrue(cache.ContainsKey("Eggs"));
            //ContainsKey, Sam
            Assert.IsFalse(cache.ContainsKey("Sam"));
            //ContainsKey, Green
            Assert.IsTrue(cache.ContainsKey("Green"));
        }

    }
}
