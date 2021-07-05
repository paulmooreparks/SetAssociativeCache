using NUnit.Framework;

using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheTests {
    public class Tests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void Test1() {
            var cache1 = new LruArrayCache<int, int>(16, 4);
            Assert.IsTrue(cache1.Capacity == (16 * 4));

            var cache2 = new LfuArrayCache<char, string>(6, 6);
            Assert.IsTrue(cache2.Capacity == (6 * 6));
        }

        [Test]
        public void Test2() {
            var cache1 = new LruArrayCache<int, int>(4, 4);
            cache1.Add(38, 9);
            Assert.IsTrue(cache1.Count == 1);
            cache1.Add(34, 123);
            Assert.IsTrue(cache1.Count == 2);
            Assert.IsTrue(cache1.ContainsKey(38));
            Assert.IsTrue(cache1.ContainsKey(34));
            Assert.IsFalse(cache1.ContainsKey(88));
        }
    }
}
