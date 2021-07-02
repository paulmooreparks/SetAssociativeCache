using NUnit.Framework;

using ParksComputing.SetAssociativeCache;

namespace SetAssociativeCacheTests {
    public class Tests {
        [SetUp]
        public void Setup() {
        }

        [Test]
        public void Test1() {
            var cache1 = new SetAssociativeCache<int, int, LruArrayCache>(16, 4);
            var cache2 = new SetAssociativeCache<char, string, LfuArrayCache>(16, 4);
        }

        [Test]
        public void Test2() {
            var cache1 = new SetAssociativeCache<int, int, LruArrayCache>(4, 4);
            cache1.Add(38, 9);
            cache1.Add(34, 123);
            Assert.IsTrue(cache1.ContainsKey(38));
            Assert.IsTrue(cache1.ContainsKey(34));
            Assert.IsFalse(cache1.ContainsKey(88));
        }
    }
}
