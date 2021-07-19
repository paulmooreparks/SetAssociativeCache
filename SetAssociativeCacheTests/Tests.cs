using System.Linq;
using System;

using NUnit.Framework;

using ParksComputing.SetAssociativeCache;
using System.Collections.Generic;

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
            Assert.IsTrue(cache1.Sets == 64);
            Assert.IsTrue(cache1.Ways == 4);
            Assert.IsTrue(cache1.Capacity == cache1.Sets * cache1.Ways);
            Assert.IsTrue(cache1.Count == 0);
            Assert.IsTrue(cache1.Keys.Count == 0);
            Assert.IsTrue(cache1.Values.Count == 0);
            Assert.IsFalse(cache1.IsReadOnly);

            var cache2 = new LruCache<string, string>(64, 4);
            Assert.IsTrue(cache2.Sets == 64);
            Assert.IsTrue(cache2.Ways == 4);
            Assert.IsTrue(cache1.Capacity == cache2.Sets * cache2.Ways);
            Assert.IsTrue(cache2.Count == 0);
            Assert.IsTrue(cache2.Keys.Count == 0);
            Assert.IsTrue(cache2.Values.Count == 0);
            Assert.IsFalse(cache2.IsReadOnly);

            var cache3 = new MruCache<string, string>(64, 4);
            Assert.IsTrue(cache3.Sets == 64);
            Assert.IsTrue(cache3.Ways == 4);
            Assert.IsTrue(cache1.Capacity == cache3.Sets * cache3.Ways);
            Assert.IsTrue(cache3.Count == 0);
            Assert.IsTrue(cache3.Keys.Count == 0);
            Assert.IsTrue(cache3.Values.Count == 0);
            Assert.IsFalse(cache3.IsReadOnly);

        }

        [Test]
        public void LruTest1() {
            var cache = new LruCache<string, int>(16, 4);
            Assert.Throws<ArgumentNullException>(() => {
                cache.Add(null, 0);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.Remove(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.ContainsKey(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                var x = cache[null];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetValue(null, out int value);
            });
            Assert.Throws<KeyNotFoundException>(() => {
                int value = cache[""];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetEvictKey(null, out string key);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.CopyTo(null, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                KeyValuePair<string,int>[] dest = new KeyValuePair<string, int>[cache.Capacity];
                cache.CopyTo(dest, -1);
            });
        }

        [Test]
        public void LruTest2() {
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
        public void LruTest3() {
            var cache = new LruCache<string, string>(4, 2);

            cache["key01"] = "value01";
            cache["key02"] = "value02";
            cache["key03"] = "value03";
            cache["key04"] = "value04";
            cache["key05"] = "value05";
            cache["key06"] = "value06";
            cache["key07"] = "value07";
            cache["key08"] = "value08";
            cache["key09"] = "value09";
            cache["key10"] = "value10";
            cache["key11"] = "value11";
            cache["key12"] = "value12";

            KeyValuePair<string, string>[] pairArray = new KeyValuePair<string, string>[cache.Capacity];
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key08");
            Assert.IsTrue(pairArray[1].Key == "key04");
            Assert.IsTrue(pairArray[2].Key == "key10");
            Assert.IsTrue(pairArray[3].Key == "key07");
            Assert.IsTrue(pairArray[4].Key == "key11");
            Assert.IsTrue(pairArray[5].Key == "key06");
            Assert.IsTrue(pairArray[6].Key == "key12");
            Assert.IsTrue(pairArray[7].Key == "key09");

            Assert.IsTrue(pairArray[0].Value == "value08");
            Assert.IsTrue(pairArray[1].Value == "value04");
            Assert.IsTrue(pairArray[2].Value == "value10");
            Assert.IsTrue(pairArray[3].Value == "value07");
            Assert.IsTrue(pairArray[4].Value == "value11");
            Assert.IsTrue(pairArray[5].Value == "value06");
            Assert.IsTrue(pairArray[6].Value == "value12");
            Assert.IsTrue(pairArray[7].Value == "value09");

            Assert.IsTrue(cache.Remove("key08"));
            Assert.IsTrue(cache.Remove("key10"));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            Assert.IsTrue(cache.Count == 4);

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key07");
            Assert.IsTrue(pairArray[2].Key == "key06");
            Assert.IsTrue(pairArray[3].Key == "key09");

            Assert.IsTrue(pairArray[0].Value == "value04");
            Assert.IsTrue(pairArray[1].Value == "value07");
            Assert.IsTrue(pairArray[2].Value == "value06");
            Assert.IsTrue(pairArray[3].Value == "value09");

            Assert.IsFalse(cache.Remove("key08"));
            Assert.IsFalse(cache.Remove("key10"));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            cache["key13"] = "value13";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key13");
            Assert.IsTrue(pairArray[1].Key == "key04");
            Assert.IsTrue(pairArray[2].Key == "key07");
            Assert.IsTrue(pairArray[3].Key == "key06");
            Assert.IsTrue(pairArray[4].Key == "key09");

            Assert.IsTrue(pairArray[0].Value == "value13");
            Assert.IsTrue(pairArray[1].Value == "value04");
            Assert.IsTrue(pairArray[2].Value == "value07");
            Assert.IsTrue(pairArray[3].Value == "value06");
            Assert.IsTrue(pairArray[4].Value == "value09");

            Assert.IsTrue(cache.Count == 5);

            string v;
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key06"];
            Assert.IsTrue(v.Equals("value06"));
            v = cache["key07"];
            Assert.IsTrue(v.Equals("value07"));
            v = cache["key09"];
            Assert.IsTrue(v.Equals("value09"));

            cache["key14"] = "value14";
            cache["key15"] = "value15";
            cache["key16"] = "value16";
            cache["key17"] = "value17";
            cache["key18"] = "value18";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key17");
            Assert.IsTrue(pairArray[1].Key == "key04");
            Assert.IsTrue(pairArray[2].Key == "key18");
            Assert.IsTrue(pairArray[3].Key == "key14");
            Assert.IsTrue(pairArray[4].Key == "key15");
            Assert.IsTrue(pairArray[5].Key == "key06");
            Assert.IsTrue(pairArray[6].Key == "key16");
            Assert.IsTrue(pairArray[7].Key == "key09");
        }

        [Test]
        public void LfuTest1() {
            var cache = new LfuCache<string, int>(16, 4);
            Assert.Throws<ArgumentNullException>(() => {
                cache.Add(null, 0);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.Remove(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.ContainsKey(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                var x = cache[null];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetValue(null, out int value);
            });
            Assert.Throws<KeyNotFoundException>(() => {
                int value = cache[""];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetEvictKey(null, out string key);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.CopyTo(null, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                KeyValuePair<string, int>[] dest = new KeyValuePair<string, int>[cache.Capacity];
                cache.CopyTo(dest, -1);
            });
        }

        [Test]
        public void LfuTest2() {
            var cache = new LfuCache<int, int>(4, 4);
            cache.Add(38, 9);
            Assert.IsTrue(cache.Count == 1);
            cache.Add(34, 123);
            Assert.IsTrue(cache.Count == 2);
            Assert.IsTrue(cache.ContainsKey(38));
            Assert.IsTrue(cache.ContainsKey(34));
            Assert.IsTrue(cache.Contains(new KeyValuePair<int, int>(38, 9)));
            Assert.IsFalse(cache.ContainsKey(88));

            Assert.IsTrue(cache.Remove(38));
            Assert.IsTrue(cache.Remove(new KeyValuePair<int,int>(34,123)));
            Assert.IsFalse(cache.Remove(88));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsTrue(cache.Capacity == 16);
        }

        [Test]
        public void LfuTest3() {
            var cache = new LfuCache<string, string>(4, 2);

            cache["key01"] = "value01";
            cache["key02"] = "value02";
            cache["key03"] = "value03";
            cache["key04"] = "value04";
            cache["key05"] = "value05";
            cache["key06"] = "value06";
            cache["key07"] = "value07";
            cache["key08"] = "value08";
            cache["key09"] = "value09";
            cache["key10"] = "value10";
            cache["key11"] = "value11";
            cache["key12"] = "value12";

            KeyValuePair<string, string>[] pairArray = new KeyValuePair<string, string>[cache.Capacity];
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key08");
            Assert.IsTrue(pairArray[2].Key == "key10");
            Assert.IsTrue(pairArray[3].Key == "key03");
            Assert.IsTrue(pairArray[4].Key == "key11");
            Assert.IsTrue(pairArray[5].Key == "key02");
            Assert.IsTrue(pairArray[6].Key == "key09");
            Assert.IsTrue(pairArray[7].Key == "key12");

            Assert.IsTrue(pairArray[0].Value == "value04");
            Assert.IsTrue(pairArray[1].Value == "value08");
            Assert.IsTrue(pairArray[2].Value == "value10");
            Assert.IsTrue(pairArray[3].Value == "value03");
            Assert.IsTrue(pairArray[4].Value == "value11");
            Assert.IsTrue(pairArray[5].Value == "value02");
            Assert.IsTrue(pairArray[6].Value == "value09");
            Assert.IsTrue(pairArray[7].Value == "value12");

            Assert.IsTrue(cache.Remove("key08"));
            Assert.IsTrue(cache.Remove("key10"));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            Assert.IsTrue(cache.Count == 4);

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key03");
            Assert.IsTrue(pairArray[2].Key == "key02");
            Assert.IsTrue(pairArray[3].Key == "key09");

            Assert.IsTrue(pairArray[0].Value == "value04");
            Assert.IsTrue(pairArray[1].Value == "value03");
            Assert.IsTrue(pairArray[2].Value == "value02");
            Assert.IsTrue(pairArray[3].Value == "value09");

            Assert.IsFalse(cache.Remove("key08"));
            Assert.IsFalse(cache.Remove("key10"));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            cache["key13"] = "value13";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key13");
            Assert.IsTrue(pairArray[2].Key == "key03");
            Assert.IsTrue(pairArray[3].Key == "key02");
            Assert.IsTrue(pairArray[4].Key == "key09");

            Assert.IsTrue(pairArray[0].Value == "value04");
            Assert.IsTrue(pairArray[1].Value == "value13");
            Assert.IsTrue(pairArray[2].Value == "value03");
            Assert.IsTrue(pairArray[3].Value == "value02");
            Assert.IsTrue(pairArray[4].Value == "value09");

            Assert.IsTrue(cache.Count == 5);

            string v;
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key13"];
            Assert.IsTrue(v.Equals("value13"));
            v = cache["key13"];
            Assert.IsTrue(v.Equals("value13"));
            v = cache["key13"];
            Assert.IsTrue(v.Equals("value13"));
            v = cache["key03"];
            Assert.IsTrue(v.Equals("value03"));
            v = cache["key03"];
            Assert.IsTrue(v.Equals("value03"));
            v = cache["key03"];
            Assert.IsTrue(v.Equals("value03"));
            v = cache["key02"];
            Assert.IsTrue(v.Equals("value02"));

            cache["key14"] = "value14";
            cache["key15"] = "value15";
            cache["key16"] = "value16";
            cache["key17"] = "value17";
            cache["key18"] = "value18";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key17");
            Assert.IsTrue(pairArray[2].Key == "key03");
            Assert.IsTrue(pairArray[3].Key == "key18");
            Assert.IsTrue(pairArray[4].Key == "key02");
            Assert.IsTrue(pairArray[5].Key == "key15");
            Assert.IsTrue(pairArray[6].Key == "key09");
            Assert.IsTrue(pairArray[7].Key == "key16");
        }

        [Test]
        public void MruTest1() {
            var cache = new MruCache<string, int>(16, 4);
            Assert.Throws<ArgumentNullException>(() => {
                cache.Add(null, 0);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.Remove(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.ContainsKey(null);
            });
            Assert.Throws<ArgumentNullException>(() => {
                var x = cache[null];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetValue(null, out int value);
            });
            Assert.Throws<KeyNotFoundException>(() => {
                int value = cache[""];
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.TryGetEvictKey(null, out string key);
            });
            Assert.Throws<ArgumentNullException>(() => {
                cache.CopyTo(null, 0);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() => {
                KeyValuePair<string, int>[] dest = new KeyValuePair<string, int>[cache.Capacity];
                cache.CopyTo(dest, -1);
            });
        }

        [Test]
        public void MruTest3() {
            var cache = new MruCache<string, string>(4, 2);

            cache["key01"] = "value01";
            cache["key02"] = "value02";
            cache["key03"] = "value03";
            cache["key04"] = "value04";
            cache["key05"] = "value05";
            cache["key06"] = "value06";
            cache["key07"] = "value07";
            cache["key08"] = "value08";
            cache["key09"] = "value09";
            cache["key10"] = "value10";
            cache["key11"] = "value11";
            cache["key12"] = "value12";

            KeyValuePair<string, string>[] pairArray = new KeyValuePair<string, string>[cache.Capacity];
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key08");
            Assert.IsTrue(pairArray[1].Key == "key04");
            Assert.IsTrue(pairArray[2].Key == "key10");
            Assert.IsTrue(pairArray[3].Key == "key03");
            Assert.IsTrue(pairArray[4].Key == "key11");
            Assert.IsTrue(pairArray[5].Key == "key02");
            Assert.IsTrue(pairArray[6].Key == "key12");
            Assert.IsTrue(pairArray[7].Key == "key01");

            Assert.IsTrue(pairArray[0].Value == "value08");
            Assert.IsTrue(pairArray[1].Value == "value04");
            Assert.IsTrue(pairArray[2].Value == "value10");
            Assert.IsTrue(pairArray[3].Value == "value03");
            Assert.IsTrue(pairArray[4].Value == "value11");
            Assert.IsTrue(pairArray[5].Value == "value02");
            Assert.IsTrue(pairArray[6].Value == "value12");
            Assert.IsTrue(pairArray[7].Value == "value01");

            Assert.IsTrue(cache.Remove("key08"));
            Assert.IsTrue(cache.Remove("key10"));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            Assert.IsTrue(cache.Count == 4);

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key04");
            Assert.IsTrue(pairArray[1].Key == "key03");
            Assert.IsTrue(pairArray[2].Key == "key02");
            Assert.IsTrue(pairArray[3].Key == "key01");

            Assert.IsTrue(pairArray[0].Value == "value04");
            Assert.IsTrue(pairArray[1].Value == "value03");
            Assert.IsTrue(pairArray[2].Value == "value02");
            Assert.IsTrue(pairArray[3].Value == "value01");

            Assert.IsFalse(cache.Remove("key08"));
            Assert.IsFalse(cache.Remove("key10"));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key11", "value11")));
            Assert.IsFalse(cache.Remove(new KeyValuePair<string, string>("key12", "value12")));

            cache["key13"] = "value13";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key13");
            Assert.IsTrue(pairArray[1].Key == "key04");
            Assert.IsTrue(pairArray[2].Key == "key03");
            Assert.IsTrue(pairArray[3].Key == "key02");
            Assert.IsTrue(pairArray[4].Key == "key01");

            Assert.IsTrue(pairArray[0].Value == "value13");
            Assert.IsTrue(pairArray[1].Value == "value04");
            Assert.IsTrue(pairArray[2].Value == "value03");
            Assert.IsTrue(pairArray[3].Value == "value02");
            Assert.IsTrue(pairArray[4].Value == "value01");

            Assert.IsTrue(cache.Count == 5);

            string v;
            v = cache["key04"];
            Assert.IsTrue(v.Equals("value04"));
            v = cache["key03"];
            Assert.IsTrue(v.Equals("value03"));
            v = cache["key02"];
            Assert.IsTrue(v.Equals("value02"));
            v = cache["key01"];
            Assert.IsTrue(v.Equals("value01"));

            cache["key14"] = "value14";
            cache["key15"] = "value15";
            cache["key16"] = "value16";
            cache["key17"] = "value17";
            cache["key18"] = "value18";

            Array.Clear(pairArray, 0, pairArray.Length);
            cache.CopyTo(pairArray, 0);

            Assert.IsTrue(pairArray[0].Key == "key17");
            Assert.IsTrue(pairArray[1].Key == "key13");
            Assert.IsTrue(pairArray[2].Key == "key18");
            Assert.IsTrue(pairArray[3].Key == "key03");
            Assert.IsTrue(pairArray[4].Key == "key15");
            Assert.IsTrue(pairArray[5].Key == "key02");
            Assert.IsTrue(pairArray[6].Key == "key16");
            Assert.IsTrue(pairArray[7].Key == "key01");
        }

        [Test]
        public void LfuHamAndEggs() {
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

        [Test]
        public void LruHamAndEggs() {
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

        [Test]
        public void MruHamAndEggs() {
            string value;

            var cache = new MruCache<string, string>(1, 2);

            cache["Eggs"] = "Ham";

            Assert.IsTrue(cache.Count == 1);

            cache["Sam"] = "Iam";

            Assert.IsTrue(cache.Count == 2);
            Assert.IsTrue(cache.TryGetEvictKey("Green", out string evictKey));
            Assert.IsTrue(evictKey.Equals("Sam"));

            cache["Green"] = "EggsAndHam";

            Assert.IsTrue(cache.Count == 2);

            Assert.IsTrue(cache.TryGetValue("Eggs", out value));
            Assert.IsTrue(cache.TryGetValue("Green", out value));

            Assert.IsTrue(cache.ContainsKey("Eggs"));
            Assert.IsTrue(cache.Contains(new KeyValuePair<string, string>("Eggs", "Ham")));
            Assert.IsFalse(cache.ContainsKey("Sam"));
            Assert.IsFalse(cache.Contains(new KeyValuePair<string, string>("Sam", "Iam")));
            Assert.IsTrue(cache.ContainsKey("Green"));

            Assert.IsTrue(cache.Remove("Eggs"));
            Assert.IsFalse(cache.Remove("Sam"));
            Assert.IsTrue(cache.Remove(new KeyValuePair<string, string>("Green", "EggsAndHam")));

            Assert.IsTrue(cache.Count == 0);
            Assert.IsTrue(cache.Capacity == 2);
        }

    }
}
