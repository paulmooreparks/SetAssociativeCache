using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public abstract class XfuArrayCache<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {
        /* TKey is index into ItemArray; TValue is usage count. */
        protected KeyValuePair<int, int>[] indexArray_;

        /* Comparer object used to sort items in indexArray in LFU order. */
        IComparer<KeyValuePair<int, int>> lfuComparer_ = new XfuComparer();

        public XfuArrayCache(int sets, int ways) : base(sets, ways) {
            Clear();
        }

        //
        // Summary:
        //     Adds an element with the provided key and value to the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Parameters:
        //   key:
        //     The object to use as the key of the element to add.
        //
        //   value:
        //     The object to use as the value of the element to add.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        override public void Add(TKey key, TValue value) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            var keyHash = key.GetHashCode();
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray_[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray_[offsetIndex].Value);
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (itemArray_[itemIndex].Key.Value == keyHash) {
                    itemIndex = indexArray_[offsetIndex].Key;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. Evict the appropriate element depending on 
            policy, then add the new value at that offset. */
            setOffset = ReplacementOffset;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray_[offsetIndex].Key;
            Add(key, value, set, setOffset, itemIndex);
            return;
        }

        protected abstract int ReplacementOffset { get; }

        //
        // Summary:
        //     Gets the number of elements contained in the System.Collections.Generic.ICollection`1.
        //
        // Returns:
        //     The number of elements contained in the System.Collections.Generic.ICollection`1.
        public override int Count {
            get {
                int value = 0;

                foreach (KeyValuePair<int, int> itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        ++value;
                    }
                }

                return value;
            }
        }

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.ISetAssociativeCache`2 contains an element
        //     with the specified key.
        //
        // Parameters:
        //   key:
        //     The key to locate in the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Returns:
        //     true if the System.Collections.Generic.ISetAssociativeCache`2 contains an element with
        //     the key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        public override bool ContainsKey(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            var keyHash = key.GetHashCode();

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Value == keyHash) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        //
        // Summary:
        //     Determines whether the System.Collections.Generic.ICollection`1 contains a specific
        //     value.
        //
        // Parameters:
        //   item:
        //     The object to locate in the System.Collections.Generic.ICollection`1.
        //
        // Returns:
        //     true if item is found in the System.Collections.Generic.ICollection`1; otherwise,
        //     false.
        public override bool Contains(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * ways_;
            var keyHash = item.Key.GetHashCode();

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Value == keyHash &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    PromoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        //
        // Summary:
        //     Gets the value associated with the specified key.
        //     This is the other function(alongside ContainsKey) that needs to be very fast.
        //     Users will call this method to retrieve values that have been cached. If it's not 
        //     significantly faster to retrieve the value from the cache than from the original value
        //     source, there isn't much point in having a cache.
        //
        // Parameters:
        //   key:
        //     The key whose value to get.
        //
        //   value:
        //     When this method returns, the value associated with the specified key, if the
        //     key is found; otherwise, the default value for the type of the value parameter.
        //     This parameter is passed uninitialized.
        //
        // Returns:
        //     true if the object that implements System.Collections.Generic.ISetAssociativeCache`2 contains
        //     an element with the specified key; otherwise, false.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            var keyHash = key.GetHashCode();

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Value == keyHash) {
                    PromoteKey(set, setOffset);
                    value = itemArray_[itemIndex].Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        //
        // Summary:
        //     Removes the element with the specified key from the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Parameters:
        //   key:
        //     The key of the element to remove.
        //
        // Returns:
        //     true if the element is successfully removed; otherwise, false. This method also
        //     returns false if key was not found in the original System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        public override bool Remove(TKey key) {
            if (key == null) {
                throw new ArgumentNullException("key");
            }

            var set = FindSet(key);
            var setBegin = set * ways_;
            var keyHash = key.GetHashCode();

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Value == keyHash) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray_[offsetIndex].Value);
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        //
        // Summary:
        //     Removes the first occurrence of a specific object from the System.Collections.Generic.ICollection`1.
        //
        // Parameters:
        //   item:
        //     The object to remove from the System.Collections.Generic.ICollection`1.
        //
        // Returns:
        //     true if item was successfully removed from the System.Collections.Generic.ICollection`1;
        //     otherwise, false. This method also returns false if item is not found in the
        //     original System.Collections.Generic.ICollection`1.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ICollection`1 is read-only.
        public override bool Remove(KeyValuePair<TKey, TValue> item) {
            var set = FindSet(item.Key);
            var setBegin = set * ways_;
            var keyHash = item.Key.GetHashCode();

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex].Key;

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Value == keyHash &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    ++version_;
                    indexArray_[offsetIndex] = KeyValuePair.Create(itemIndex, indexArray_[offsetIndex].Value);
                    DemoteKey(set, setOffset);
                    return true;
                }
            }

            return false;
        }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the keys present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the keys of the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public override ICollection<TKey> Keys {
            get {
                List<TKey> value = new();

                foreach (KeyValuePair<int, int> itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(itemArray_[itemIndex.Key].Key.Key);
                    }
                }

                return value;
            }
        }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the values present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the values in the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public override ICollection<TValue> Values {
            get {
                List<TValue> value = new();

                foreach (KeyValuePair<int, int> itemIndex in indexArray_) {
                    if (itemIndex.Key != int.MaxValue) {
                        value.Add(itemArray_[itemIndex.Key].Value);
                    }
                }

                return value;
            }
        }

        //
        // Summary:
        //     Copies the elements of the System.Collections.Generic.ICollection`1 to an System.Array,
        //     starting at a particular System.Array index.
        //
        // Parameters:
        //   array:
        //     The one-dimensional System.Array that is the destination of the elements copied
        //     from System.Collections.Generic.ICollection`1. The System.Array must have zero-based
        //     indexing.
        //
        //   arrayIndex:
        //     The zero-based index in array at which copying begins.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     array is null.
        //
        //   T:System.ArgumentOutOfRangeException:
        //     arrayIndex is less than 0.
        //
        //   T:System.ArgumentException:
        //     The number of elements in the source System.Collections.Generic.ICollection`1
        //     is greater than the available space from arrayIndex to the end of the destination
        //     array.
        public override void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            if (array == null) {
                throw new ArgumentNullException("array");
            }

            if (arrayIndex < 0) {
                throw new ArgumentOutOfRangeException("arrayIndex");
            }

            foreach (KeyValuePair<int, int> itemIndex in indexArray_) {
                if (itemIndex.Key != int.MaxValue) {
                    array[arrayIndex] = KeyValuePair.Create(itemArray_[itemIndex.Key].Key.Key, itemArray_[itemIndex.Key].Value);
                    ++arrayIndex;
                }
            }
        }

        //
        // Summary:
        //     Removes all items from the System.Collections.Generic.ICollection`1.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ICollection`1 is read-only.
        public override void Clear() {
            /* Keep in mind that the data aren't cleared. We are clearing the indices which point 
            to the data. With no indices, the data aren't accessible. */
            ++version_;
            indexArray_ = new KeyValuePair<int, int>[Capacity];
            Array.Fill(indexArray_, KeyValuePair.Create(int.MaxValue, 0));
        }

        //
        // Summary:
        //     Adds an element with the provided key and value to the System.Collections.Generic.ISetAssociativeCache`2.
        //
        // Parameters:
        //   key:
        //     The object to use as the key of the element to add.
        //
        //   value:
        //     The object to use as the value of the element to add.
        //
        //   set:
        //     The set in which to add the element.
        //
        //   setOffset:
        //     The offset into the set at which to add the element.
        //
        //   itemIndex:
        //     The index into the item array at which the element is stored.
        protected void Add(TKey key, TValue value, int set, int setOffset, int itemIndex) {
            ++version_;
            var hashedKey = KeyValuePair.Create(key, key.GetHashCode());
            itemArray_[itemIndex] = KeyValuePair.Create(hashedKey, value);
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            var newHeadItem = KeyValuePair.Create(indexArray_[keyIndex].Key, 1);

            /* The new index gets sorted to the front, but with a count of 1. A newly-cached item 
            should not be immediately evicted, so it's safe until pushed down by other new items. */
            System.Array.Copy(indexArray_, headIndex, indexArray_, headIndex + 1, setOffset);
            indexArray_[headIndex] = newHeadItem;
        }

        //
        // Summary:
        //     Increment the count for the last cache item accessed, then sort the set based on all counts.
        //
        // Parameters:
        //   set:
        //     The set in which the key is stored.
        //
        //   setOffset:
        //     The offset into the set at which the key is stored.
        protected void PromoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            indexArray_[keyIndex] = KeyValuePair.Create(indexArray_[keyIndex].Key, indexArray_[keyIndex].Value + 1);

            Array.Sort(indexArray_, headIndex, ways_, lfuComparer_);
        }

        //
        // Summary:
        //     Set an item's count to zero (removal from cache, for example), then sort the set based on all counts.
        //
        // Parameters:
        //   set:
        //     The set in which the key is stored.
        //
        //   setOffset:
        //     The offset into the set at which the key is stored.
        protected void DemoteKey(int set, int setOffset) {
            int headIndex = set * ways_;
            int keyIndex = headIndex + setOffset;
            indexArray_[keyIndex] = KeyValuePair.Create(indexArray_[keyIndex].Key, 0);

            Array.Sort(indexArray_, headIndex, ways_, lfuComparer_);
        }

        /* Custom comparer used to sort the items in indexArray in LFU order. */
        internal class XfuComparer : Comparer<KeyValuePair<int, int>> {
            // Compares by Length, Height, and Width.
            public override int Compare(KeyValuePair<int, int> x, KeyValuePair<int, int> y) {
                /* I reversed the sign of < and > because I want a reverse sort */
                if (x.Value < y.Value) {
                    return 1;
                }
                else if (x.Value > y.Value) {
                    return -1;
                }
                else {
                    return 0;
                }
            }
        }

    }
}
