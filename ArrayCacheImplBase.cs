using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

using Microsoft.VisualBasic.CompilerServices;

namespace ParksComputing.SetAssociativeCache {
    public abstract class ArrayCacheImplBase<TKey, TValue> : ISetAssociativeCache<TKey, TValue> {

        /* I'm being a very naughty object-oriented developer. By using fields instead of 
        properties, I've found that I can eliminate function calls from the release version 
        of the JITted assembly code, at least on Intel/AMD x64. That actually surprises me. 
        The properties still exist, though, in case external clients need to know the values 
        for some reason. */
        protected int sets_; // Number of sets in the cache
        protected int ways_; // Capacity of each set in the cache
        protected int version_;
        protected KeyValuePair<TKey, TValue>[] itemArray_; // Key/value pairs stored in the cache

        public ArrayCacheImplBase(int sets, int ways) {
            sets_ = sets;
            ways_ = ways;
            itemArray_ = new KeyValuePair<TKey, TValue>[Capacity];
        }

        //
        // Summary:
        //     Gets or sets the element with the specified key.
        //
        // Parameters:
        //   key:
        //     The key of the element to get or set.
        //
        // Returns:
        //     The element with the specified key.
        //
        // Exceptions:
        //   T:System.ArgumentNullException:
        //     key is null.
        //
        //   T:System.Collections.Generic.KeyNotFoundException:
        //     The property is retrieved and key is not found.
        //
        //   T:System.NotSupportedException:
        //     The property is set and the System.Collections.Generic.ISetAssociativeCache`2 is read-only.
        public TValue this[TKey key] {
            get {
                if (TryGetValue(key, out TValue value)) {
                    return value;
                }

                throw new KeyNotFoundException(string.Format("Key '{0}' not found in cache", key));
            }

            set {
                Add(key, value);
            }
        }

        //
        // Summary:
        //     Gets the number of sets in the cache
        //
        // Returns:
        //     The number of sets in the cache
        public int Sets {
            get => sets_;
        }

        //
        // Summary:
        //     Gets the capacity in each set
        //
        // Returns:
        //     The number of elements which may be stored in a set.
        public int Ways {
            get => ways_;
        }

        //
        // Summary:
        //     Gets the capacity of the cache
        //
        // Returns:
        //     The number of elements which may be stored in the cache.
        public int Capacity { get => sets_ * ways_; }

        //
        // Summary:
        //     Gets the number of elements contained in the System.Collections.Generic.ICollection`1.
        //
        // Returns:
        //     The number of elements contained in the System.Collections.Generic.ICollection`1.
        public abstract int Count { get; }

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
        public abstract void Add(TKey key, TValue value);

        //
        // Summary:
        //     Adds an item to the cache.
        //
        // Parameters:
        //   item:
        //     The key/value pair to add to the cache.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ICollection`1 is read-only.
        public void Add(KeyValuePair<TKey, TValue> item) {
            Add(item.Key, item.Value);
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
        public abstract bool ContainsKey(TKey key);

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
        public abstract bool Contains(KeyValuePair<TKey, TValue> item);

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
        public abstract bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value);

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
        public abstract bool Remove(TKey key);

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
        public abstract bool Remove(KeyValuePair<TKey, TValue> item);

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the keys present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the keys of the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public abstract ICollection<TKey> Keys { get; }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the values present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the values in the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public abstract ICollection<TValue> Values { get; }

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
        public abstract void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex);

        //
        // Summary:
        //     Returns an enumerator that iterates through a collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator object that can be used to iterate through
        //     the collection.
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return new CacheEnumerator(this);
        }

        [Serializable]
        private sealed class CacheEnumerator : IEnumerator<KeyValuePair<TKey, TValue>> {
            ArrayCacheImplBase<TKey, TValue> cache_;
            int version_;
            int count_;
            int index_;
            KeyValuePair<TKey, TValue> current_;

            public CacheEnumerator(ArrayCacheImplBase<TKey,TValue> cache) {
                this.cache_ = cache;
                this.version_ = cache.version_;
                this.count_ = cache.Count;
                Reset();
            }

            public KeyValuePair<TKey, TValue> Current { get => current_; }
            
            object IEnumerator.Current { 
                get {
                    if (index_ == 0 || index_ == count_ + 1) {
                        throw new InvalidOperationException();
                    }

                    return new KeyValuePair<TKey, TValue>(current_.Key, current_.Value);
                }
            }

            public bool MoveNext() {
                if (version_ != cache_.version_) {
                    throw new InvalidOperationException("Emumerator is invalid due to cache update");
                }

                while (index_ < count_) {
                    current_ = new KeyValuePair<TKey, TValue>(cache_.itemArray_[index_].Key, cache_.itemArray_[index_].Value);
                    ++index_;
                    return true;
                }

                index_ = count_ + 1;
                current_ = new KeyValuePair<TKey, TValue>();
                return false;
            }

            public void Reset() {
                if (version_ != cache_.version_) {
                    throw new InvalidOperationException("Emumerator is invalid due to cache update");
                }

                this.index_ = 0;
                this.current_ = new KeyValuePair<TKey, TValue>();
            }

            public void Dispose() {
            }
        }

        //
        // Summary:
        //     Returns an enumerator that iterates through a collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator object that can be used to iterate through
        //     the collection.
        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        //
        // Summary:
        //     Gets a value indicating whether the System.Collections.Generic.ICollection`1
        //     is read-only.
        //
        // Returns:
        //     true if the System.Collections.Generic.ICollection`1 is read-only; otherwise,
        //     false.
        public bool IsReadOnly { get => false; }

        //
        // Summary:
        //     Removes all items from the System.Collections.Generic.ICollection`1.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ICollection`1 is read-only.
        public abstract void Clear();

        //
        // Summary:
        //     Given a key, find the set into which the key should be placed.
        //     Since this is an internal function, the key parameter is assumed 
        //     to be not null.
        protected int FindSet(TKey key) {
            /* For integer types, GetHashCode() returns the integer, so what we end up with here is 
            a simple MOD operation. A better hashing algorithm is probably a good idea. */
            /* The bitwise OR removes the high bit so that we only get a positive number */
            int hashCode = key.GetHashCode() & 0x7FFFFFFF; 
            return hashCode % sets_;
        }
    }
}