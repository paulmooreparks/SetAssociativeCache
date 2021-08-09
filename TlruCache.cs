using System;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with time-aware least-recently-used (TLRU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TlruCache<TKey, TValue> : XruCache<TKey, TValue> {
        /// <summary>
        /// Create a new <c>TlruCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public TlruCache(int sets, int ways) : base(sets, ways) {
        }

        protected long ttl_ = 300; // in seconds. Just a made-up number for now.

        public long TTL {
            get { return ttl_; }
            set { ttl_ = value; }
        }

        /// <summary>
        /// Gets the index into the pointer array for the item which should be evicted from the set.
        /// </summary>
        protected override int GetEvictionPointerIndex(int set) {
            return ways_ - 1;
        }

        protected virtual bool IsExpired(int set, int pointerIndex) {
            var binTime = pointerArray_[set][pointerIndex].Value;
            var timeStamp = DateTime.FromBinary(binTime);
            var expTime = timeStamp.AddSeconds(ttl_);
            var now = DateTime.UtcNow;

            /* If expiration time is in the past... */
            return (expTime < now);
        }

        protected override bool TryAddAtIndex(TKey key, TValue value, int set, int pointerIndex, int valueIndex, Action<TKey, TValue, int, int, int> OnKeyExists) {
            bool result = base.TryAddAtIndex(key, value, set, pointerIndex, valueIndex, OnKeyExists);

            if (!result && IsExpired(set, pointerIndex)) {
                ReplaceItem(key, value, set, pointerIndex, valueIndex);
                return true;
            }

            return result;
        }

        protected override bool FilterGetValue(TKey key, TValue value, int set, int pointerIndex, int valueIndex) {
            bool result = base.FilterGetValue(key, value, set, pointerIndex, valueIndex);

            if (!result && IsExpired(set, pointerIndex)) {
                Remove(key);
                return true;
            }

            return result;
        }

        protected override bool IsItemEvictable(int set, int pointerIndex) {
            bool result = base.IsItemEvictable(set, pointerIndex);

            if (!result && IsExpired(set, pointerIndex)) {
                return true;
            }

            return result;
        }

        protected override void UpdateSet(int set, int pointerIndex) {
            int newKey = pointerArray_[set][pointerIndex].Key;
            long newValue = DateTime.UtcNow.ToBinary();
            pointerArray_[set][pointerIndex] = new System.Collections.Generic.KeyValuePair<int, long>(newKey, newValue);
            base.UpdateSet(set, pointerIndex);
        }
    }
}
