using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    /// <summary>
    /// Base class for caches with an eviction policy based on recency of item usage.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the cache.</typeparam>
    /// <typeparam name="TValue">The type of values in the cache.</typeparam>
    /// <remarks>
    /// There is still a lot of copied code between this class and <c>XfuArrayCache</c>. I need to factor that 
    /// out into <c>ArrayCacheImplBase</c> or some other appropriate intermediate class.
    /// </remarks>
    public abstract class XruArrayCache<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {
        /* Array of indices into ItemArray, split into sets, where each set is sorted from LRU to MRU. */
        protected int[] indexArray_;

        /// <summary>
        /// Create a new <c>XfuArrayCache</c> instance.
        /// </summary>
        /// <param name="sets">The number of sets into which the cache is divided.</param>
        /// <param name="ways">The number of storage slots in each set.</param>
        public XruArrayCache(int sets, int ways) : base(sets, ways) {
            Clear();
        }

        /// <summary>
        /// Adds an element with the provided <paramref name="key"/> and <paramref name="value"/> 
        /// to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="key"/> is null.</exception>
        override public void Add(TKey key, TValue value) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray_[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray_[offsetIndex] = itemIndex;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (itemArray_[itemIndex].Key.Equals(key)) {
                    itemIndex = indexArray_[offsetIndex];
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */
            setOffset = ReplacementOffset;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray_[offsetIndex];
            Add(key, value, set, setOffset, itemIndex);
            return;
        }

        /// <summary>
        /// The offset into the set for the item which should be evicted from the cache.
        /// </summary>
        protected abstract int ReplacementOffset { get; }

        /// <summary>
        /// Gets the number of elements contained in the System.Collections.Generic.ICollection.
        /// </summary>
        /// <value>
        /// The number of elements contained in the System.Collections.Generic.ICollection.
        /// </value>
        public override int Count {
            get {
                int value = 0;

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
                        ++value;
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// Determines whether the ParksComputing.ISetAssociativeCache contains an element with the specified key.
        /// </summary>
        /// <param name="key">The key to locate in the ParksComputing.ISetAssociativeCache.</param>
        /// <returns>
        /// true if the ParksComputing.ISetAssociativeCache contains an element with the key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        public override bool ContainsKey(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(key)) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines whether the System.Collections.Generic.ICollection contains a specific value.
        /// </summary>
        /// <param name="item">The object to locate in the System.Collections.Generic.ICollection.</param>
        /// <returns>true if item is found in the System.Collections.Generic.ICollection; otherwise, false.</returns>
        public override bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(item.Key) &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// This is the other function(alongside ContainsKey) that needs to be very fast. Users will call 
        /// this method to retrieve values that have been cached. If it's not  significantly faster to 
        /// retrieve the value from the cache than from the original value source, there isn't much point 
        /// in having a cache.
        /// </summary>
        /// <param name="key">The key whose value to get.</param>
        /// <param name="value">
        /// When this method returns, the value associated with the specified key, if the key is found; 
        /// otherwise, the default value for the type of the value parameter. This parameter is passed 
        /// uninitialized.
        /// </param>
        /// <returns>
        /// true if the object that implements ParksComputing.ISetAssociativeCache contains 
        /// an element with the specified key; otherwise, false.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Equals(key)) {
                    PromoteKey(set, setOffset);
                    value = itemArray_[itemIndex].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if key 
        /// was not found in the original ParksComputing.ISetAssociativeCache.
        /// </returns>
        public override bool Remove(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Equals(key)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = int.MaxValue;
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Removes the element with the specified key from the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The key of the element to remove.</param>
        /// <returns>
        /// true if the element is successfully removed; otherwise, false. This method also returns false if key 
        /// was not found in the original ParksComputing.ISetAssociativeCache.
        /// </returns>
        public override bool Remove(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Equals(item.Key) &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = int.MaxValue;
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets an System.Collections.Generic.ICollection containing the keys of the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// An System.Collections.Generic.ICollection containing the keys of the object that implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public override ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(itemArray_[itemIndex].Key);
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// Gets an System.Collections.Generic.ICollection containing the values in the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <value>
        /// An System.Collections.Generic.ICollection containing the values in the object that implements ParksComputing.ISetAssociativeCache.
        /// </value>
        public override ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(itemArray_[itemIndex].Value);
                    }
                }

                return value;
            }
        }

        /// <summary>
        /// Copies the elements of the System.Collections.Generic.ICollection to an 
        /// System.Array, starting at a particular System.Array index.
        /// </summary>
        /// <param name="array">
        /// The one-dimensional System.Array that is the destination of the elements copied from 
        /// System.Collections.Generic.ICollection. The System.Array must have zero-based indexing.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in array at which copying begins.
        /// </param>
        /// <exception cref="System.ArgumentNullException"><paramref name="array"/> is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
        /// <exception cref="System.ArgumentException">
        /// The number of elements in the source System.Collections.Generic.ICollection is greater than the 
        /// available space from arrayIndex to the end of the destination array.
        /// </exception>
        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            foreach (int itemIndex in indexArray_) {
                if (itemIndex != int.MaxValue) {
                    array[arrayIndex] = KeyValuePair.Create(itemArray_[itemIndex].Key, itemArray_[itemIndex].Value);
                    ++arrayIndex;
                }
            }
        }

        /// <summary>
        /// Removes all items from the System.Collections.Generic.ICollection.
        /// </summary>
        public override void Clear() {
            /* Keep in mind that the data aren't cleared. We are clearing the indices which point 
            to the data. With no indices, the data aren't accessible. */
            ++version_;
            indexArray_ = new int[Capacity];
            Array.Fill(indexArray_, int.MaxValue);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the ParksComputing.ISetAssociativeCache.
        /// </summary>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <param name="set">The set in which to add the element.</param>
        /// <param name="setOffset">The offset into the set at which to add the element.</param>
        /// <param name="itemIndex">The index into the item array at which the element is stored.</param>
        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            ++version_;
            itemArray_[itemIndex] = KeyValuePair.Create(key, value);
            PromoteKey(set, setOffset);
        }


        /// <summary>
        /// Move the key in the given set at the given offset to the front of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected void PromoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int newHeadItem = indexArray_[keyIndex];

            System.Array.Copy(indexArray_, headIndex, indexArray_, headIndex + 1, setOffset);
            indexArray_[headIndex] = newHeadItem;
        }

        /// <summary>
        /// Move the key in the given set at the given offset to the end of the set. 
        /// </summary>
        /// <param name="set">The set in which the key is stored.</param>
        /// <param name="setOffset">The offset into the set at which the key is stored.</param>
        protected void DemoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            int tailIndex = headIndex + ways_ - 1;
            int count = ways_ - setOffset - 1;
            int newTailItem = indexArray_[keyIndex];

            System.Array.Copy(indexArray_, keyIndex + 1, indexArray_, keyIndex, count);
            indexArray_[tailIndex] = newTailItem;
        }
    }
}
