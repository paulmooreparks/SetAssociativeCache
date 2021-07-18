using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public static class Extensions {
        const ulong offsetBasis = 14695981039346656037;
        const ulong prime = 1099511628211;

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this String key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ulong hash = offsetBasis;
            char[] chars = key.ToCharArray();

            foreach (char c in chars) {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this ulong key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this long key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this uint key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this int key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this char key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue(this byte key) {
            ulong hash = offsetBasis;
            hash ^= (ulong)key;
            hash *= prime;
            return hash;
        }

        /// <summary>
        /// Convert a given key to a hash value.
        /// </summary>
        /// <param name="key">The key to hash.</param>
        /// <returns>The hash value for the given key.</returns>
        /// <exception cref="System.ArgumentNullException">key is null.</exception>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function for 
        /// more details about the FNV hash algorithm.
        /// </remarks>
        public static ulong GetHashValue<T>(this T key) {
            if (key == null) {
                throw new ArgumentNullException(nameof(key));
            }

            ulong hash = offsetBasis;
            char[] chars = key.ToString().ToCharArray();

            foreach (char c in chars) {
                hash ^= c;
                hash *= prime;
            }

            return hash;
        }
    }
}
