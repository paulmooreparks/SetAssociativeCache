using System;
using System.Collections.Generic;

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

        protected long defaultTtu_ = 300; // in seconds. Just a made-up number for now.

        public long DefaultTTU {
            get { return defaultTtu_; }
            set { defaultTtu_ = value; }
        }

        /// <summary>
        /// Gets the expiration time for element with the specified key.
        /// </summary>
        /// <param name="key">The key of the element to get.</param>
        /// <returns>The expiration date and time for the requested element.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">The <paramref name="key"/> is not found.</exception>
        public DateTime GetExpirationTime(TKey key) {
            return GetMetaData(key);
        }

        public void SetExpirationTime(TKey key, DateTime expTime) {
            SetMetaData(key, expTime);
        }

        public void SetTimeout(TKey key, long ttuSeconds) {
            DateTime expTime = DateTime.UtcNow.AddSeconds(ttuSeconds);
            SetExpirationTime(key, expTime);
        }

        /// <summary>
        /// Gets the index into the pointer array for the item which should be evicted from the set.
        /// </summary>
        protected override int GetEvictionPointerIndex(int set) {
            return ways_ - 1;
        }

        protected override bool TryAddAtIndex(TKey key, TValue value, DateTime meta, int set, int pointerIndex, int valueIndex, Action<TKey, TValue, DateTime, int, int, int> OnKeyExists) {
            bool result = base.TryAddAtIndex(key, value, meta, set, pointerIndex, valueIndex, OnKeyExists);

            if (!result && IsExpired(set, pointerIndex)) {
                ReplaceItem(key, value, meta, set, pointerIndex, valueIndex);
                PromoteKey(set, pointerIndex);
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

        public override TValue this[TKey key] { 
            get => base[key];
            set {
                DateTime expTime = DateTime.UtcNow.AddSeconds(defaultTtu_);
                AddOrUpdate(key, value, expTime, (key, value, meta, set, pointerIndex, valueIndex) => {
                    ReplaceItem(key, value, expTime, set, pointerIndex, valueIndex);
                    PromoteKey(set, pointerIndex);
                });
            }
        }

        public override void Add(TKey key, TValue value) {
            DateTime expTime = DateTime.UtcNow.AddSeconds(defaultTtu_);
            base.Add(key, value, expTime);
        }

        protected virtual bool IsExpired(int set, int pointerIndex) {
            var expTime = pointerArray_[set][pointerIndex].Value;

            /* If expiration time is in the past... */
            return DateTime.Compare(expTime, DateTime.UtcNow) < 0;
        }
    }
}
