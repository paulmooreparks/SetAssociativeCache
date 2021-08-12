using System;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Cache with time-aware least-recently-used (TLRU) eviction policy.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class TlruCache<TKey, TValue> : XruCache<TKey, TValue, DateTime> {
        /// <summary>
        /// Create a new <c>TlruCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public TlruCache(int sets, int ways) : base(sets, ways) {
        }

        protected long defaultTtl_ = 300; // in seconds. Just a made-up number for now.

        public long DefaultTTL {
            get { return defaultTtl_; }
            set { defaultTtl_ = value; }
        }

        /// <summary>
        /// Gets the index into the pointer array for the item which should be evicted from the set.
        /// </summary>
        protected override int GetEvictionPointerIndex(int set) {
            return ways_ - 1;
        }

        protected override bool TryAddAtIndex(TKey key, TValue value, DateTime tag, int set, int pointerIndex, int valueIndex, Action<TKey, TValue, DateTime, int, int, int> OnKeyExists) {
            bool result = base.TryAddAtIndex(key, value, tag, set, pointerIndex, valueIndex, OnKeyExists);

            if (!result && IsExpired(set, pointerIndex)) {
                ReplaceItem(key, value, tag, set, pointerIndex, valueIndex);
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

        protected override void AddItem(TKey key, TValue value, DateTime tag, int set, int pointerIndex, int valueIndex) {
            base.AddItem(key, value, tag, set, pointerIndex, valueIndex);
            int newKey = pointerArray_[set][pointerIndex].Key;
            DateTime expTime = DateTime.UtcNow.AddSeconds(defaultTtl_);
            pointerArray_[set][pointerIndex] = new System.Collections.Generic.KeyValuePair<int, DateTime>(newKey, expTime);
        }

        protected virtual bool IsExpired(int set, int pointerIndex) {
            var expTime = pointerArray_[set][pointerIndex].Value;

            /* If expiration time is in the past... */
            return DateTime.Compare(expTime, DateTime.UtcNow) < 0;
        }

        public bool SetTimeout(TKey key, long ttlSeconds) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            int set = FindSet(key);

            for (int pointerIndex = 0; pointerIndex < ways_; ++pointerIndex) {
                int valueIndex = pointerArray_[set][pointerIndex].Key;

                /* If the key is found in the value array... */
                if (valueIndex != EMPTY_MARKER && valueArray_[set][valueIndex].Value.Key.Equals(key)) {
                    int newKey = pointerArray_[set][pointerIndex].Key;
                    DateTime expTime = DateTime.UtcNow.AddSeconds(ttlSeconds);
                    pointerArray_[set][pointerIndex] = new System.Collections.Generic.KeyValuePair<int, DateTime>(newKey, expTime);
                    return true;
                }
            };

            return false;
        }
    }
}
