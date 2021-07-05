using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    public abstract class XruArrayCache<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {
        /* Array of indices into ItemArray, split into sets, where each set is sorted from LRU to MRU. */
        protected int[] indexArray_;

        public XruArrayCache(int sets, int ways) : base(sets, ways) {
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
                itemIndex = indexArray_[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray_[offsetIndex] = itemIndex;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (itemArray_[itemIndex].Key.Value == keyHash) {
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

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
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

            var keyHash = key.GetHashCode();
            var set = FindSet(key);
            var setBegin = set * ways_;

            for (int setOffset = 0, offsetIndex = setBegin; setOffset < ways_; ++setOffset, ++offsetIndex) {
                int itemIndex = indexArray_[offsetIndex];

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
                int itemIndex = indexArray_[offsetIndex];

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
                int itemIndex = indexArray_[offsetIndex];

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
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue && itemArray_[itemIndex].Key.Value == keyHash) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray_[offsetIndex] = int.MaxValue;
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
                int itemIndex = indexArray_[offsetIndex];

                if (itemIndex != int.MaxValue &&
                    itemArray_[itemIndex].Key.Value == keyHash &&
                    itemArray_[itemIndex].Value.Equals(item.Value)) {
                    /* Since all access to the cache values goes through the index array first, 
                    I'll try leaving the value in the cache and see how that goes. It's faster, 
                    but for some reason it make me nervous. I suppose I could make value replacement 
                    a feature of the policy class. */
                    indexArray_[offsetIndex] = int.MaxValue;
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

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(itemArray_[itemIndex].Key.Key);
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

                foreach (int itemIndex in indexArray_) {
                    if (itemIndex != int.MaxValue) {
                        value.Add(itemArray_[itemIndex].Value);
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

            foreach (int itemIndex in indexArray_) {
                if (itemIndex != int.MaxValue) {
                    array[arrayIndex] = KeyValuePair.Create(itemArray_[itemIndex].Key.Key, itemArray_[itemIndex].Value);
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
            indexArray_ = new int[Capacity];
            Array.Fill(indexArray_, int.MaxValue);
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
            var hashedKey = KeyValuePair.Create(key, key.GetHashCode());
            itemArray_[itemIndex] = KeyValuePair.Create(hashedKey, value);
            PromoteKey(set, setOffset);
        }


        //
        // Summary:
        //     Move the key in the given set at the given offset to the end of the set. 
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
            int newHeadItem = indexArray_[keyIndex];

            System.Array.Copy(indexArray_, headIndex, indexArray_, headIndex + 1, setOffset);
            indexArray_[headIndex] = newHeadItem;
        }

        //
        // Summary:
        //     Move the key in the given set at the given offset to the front of the set. 
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
            int tailIndex = headIndex + ways_ - 1;
            int count = ways_ - setOffset - 1;
            int newTailItem = indexArray_[keyIndex];

            System.Array.Copy(indexArray_, keyIndex + 1, indexArray_, keyIndex, count);
            indexArray_[tailIndex] = newTailItem;
        }

    }
}
