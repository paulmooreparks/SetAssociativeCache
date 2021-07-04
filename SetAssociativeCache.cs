using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ParksComputing.SetAssociativeCache {
    //
    // Summary:
    //     Shell implementation of a generic set-associative cache of key/value pairs.
    //
    // Type parameters:
    //   TKey:
    //     The type of keys in the cache.
    //
    //   TValue:
    //     The type of values in the cache.
    //
    //   TPolicy:
    //     The class that implements policy used in manipulating the cache, such as LFU, LRU, storage assumptions, etc.
    public class SetAssociativeCache<TKey, TValue, TPolicy> : ISetAssociativeCache<TKey, TValue> where TPolicy : ICachePolicy, new() {
        ICachePolicyImpl<TKey, TValue> impl_;

        //
        // Summary:
        //     Initializes a new SetAssociativeCache instance.
        //
        // Parameters:
        //   key:
        //     The number of sets in the cache.
        //
        // Returns:
        //     The capacity of each set in the cache.
        //
        // Exceptions:
        //   T:System.ArgumentOutOfRangeException:
        //     Either the sets or the ways parameter is not greater than zero.
        //
        public SetAssociativeCache(int sets, int ways) {
            if (sets <= 0) {
                throw new ArgumentOutOfRangeException("sets", "Value of sets must be greater than zero.");
            }

            if (ways <= 0) {
                throw new ArgumentOutOfRangeException("capacity", "Value of ways must be greater than zero.");
            }

            TPolicy policy_ = new();
            impl_ = policy_.MakeInstance<TKey, TValue>(sets, ways);
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
            get => impl_[key];
            set => impl_[key] = value;
        }

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the keys present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the keys of the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public ICollection<TKey> Keys => impl_.Keys;

        //
        // Summary:
        //     Gets an System.Collections.Generic.ICollection`1 containing the values present in the cache.
        //
        // Returns:
        //     An System.Collections.Generic.ICollection`1 containing the values in the object
        //     that implements System.Collections.Generic.ISetAssociativeCache`2.
        public ICollection<TValue> Values => impl_.Values;

        //
        // Summary:
        //     Gets the capacity of the cache
        //
        // Returns:
        //     The number of elements which may be stored in the cache.
        public int Capacity => impl_.Capacity;

        //
        // Summary:
        //     Gets the number of elements contained in the cache.
        //
        // Returns:
        //     The number of elements contained in the cache.
        public int Count => impl_.Count;

        //
        // Summary:
        //     Gets the number of sets in the cache
        //
        // Returns:
        //     The number of sets in the cache
        public int Sets => impl_.Sets;

        //
        // Summary:
        //     Gets the capacity in each set
        //
        // Returns:
        //     The number of elements which may be stored in a set.
        public int Ways => impl_.Ways;

        //
        // Summary:
        //     Gets a value indicating whether the System.Collections.Generic.ICollection`1
        //     is read-only.
        //
        // Returns:
        //     true if the System.Collections.Generic.ICollection`1 is read-only; otherwise,
        //     false.
        public bool IsReadOnly => impl_.IsReadOnly;

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
        public void Add(TKey key, TValue value) {
            impl_.Add(key, value);
        }

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
            impl_.Add(item);
        }

        //
        // Summary:
        //     Removes all items from the System.Collections.Generic.ICollection`1.
        //
        // Exceptions:
        //   T:System.NotSupportedException:
        //     The System.Collections.Generic.ICollection`1 is read-only.
        public void Clear() {
            impl_.Clear();
        }

        /* This function needs to be very fast, as it's likely one of the most important methods 
        on the data-structure class. Users will call this method to see if a given key has been 
        cached, and if so, they will access the value from the cache rather than the original 
        value source. */
        public bool ContainsKey(TKey key) {
            return impl_.ContainsKey(key);
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
        public bool Contains(KeyValuePair<TKey, TValue> item) {
            return impl_.Contains(item);
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
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
            impl_.CopyTo(array, arrayIndex);
        }

        //
        // Summary:
        //     Returns an enumerator that iterates through a collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator object that can be used to iterate through
        //     the collection.
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
            return impl_.GetEnumerator();
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
        public bool Remove(TKey key) {
            return impl_.Remove(key);
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
        public bool Remove(KeyValuePair<TKey, TValue> item) {
            return impl_.Remove(item);
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
        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) {
            return impl_.TryGetValue(key, out value);
        }

        //
        // Summary:
        //     Returns an enumerator that iterates through a collection.
        //
        // Returns:
        //     An System.Collections.IEnumerator object that can be used to iterate through
        //     the collection.
        IEnumerator IEnumerable.GetEnumerator() {
            return impl_.GetEnumerator();
        }
    }
}
