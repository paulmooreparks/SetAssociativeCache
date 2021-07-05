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
        public void LruTest1() {
            var cache = new LruArrayCache<int, int>(16, 4);
            Assert.IsTrue(cache.Capacity == (16 * 4));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue(123, out int value));
        }

        [Test]
        public void LruTest2() {
            var cache = new LruArrayCache<string, string>(6, 6);
            Assert.IsTrue(cache.Capacity == (6 * 6));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue("abc", out string value));
        }

        [Test]
        public void LruTest3() {
            var cache = new LruArrayCache<int, int>(4, 4);
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
            var cache = new LfuArrayCache<int, int>(16, 4);
            Assert.IsTrue(cache.Capacity == (16 * 4));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue(123, out int value));
        }

        [Test]
        public void LfuTest2() {
            var cache = new LfuArrayCache<string, string>(6, 6);
            Assert.IsTrue(cache.Capacity == (6 * 6));
            Assert.IsTrue(cache.Count == 0);
            Assert.IsFalse(cache.TryGetValue("abc", out string value));
        }

        [Test]
        public void LfuTest3() {
            var cache = new LfuArrayCache<int, int>(4, 4);
            cache.Add(38, 9);
            Assert.IsTrue(cache.Count == 1);
            cache.Add(34, 123);
            Assert.IsTrue(cache.Count == 2);
            Assert.IsTrue(cache.ContainsKey(38));
            Assert.IsTrue(cache.ContainsKey(34));
            Assert.IsFalse(cache.ContainsKey(88));
        }

    }
}
