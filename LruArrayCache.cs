using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LruArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue>(int sets, int ways) {
            return new LruArrayCacheImpl<TKey, TValue>(sets, ways);
        }
    }

    class LruArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LruArrayCacheImpl(int sets, int ways) : base(sets, ways) {
        }

        override public void Add(TKey key, TValue value) {
            var set = FindSet(key);
            var setBegin = set * Ways;
            int setOffset;
            int offsetIndex;
            int itemIndex;

            for (setOffset = 0, offsetIndex = setBegin; setOffset < Ways; ++setOffset, ++offsetIndex) {
                itemIndex = indexArray[offsetIndex];

                if (itemIndex == int.MaxValue) {
                    itemIndex = offsetIndex;
                    indexArray[offsetIndex] = itemIndex;
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }

                if (itemArray[itemIndex].Key.Equals(key)) {
                    itemIndex = indexArray[offsetIndex];
                    Add(key, value, set, setOffset, itemIndex);
                    return;
                }
            }

            /* If we get here, then the set is full. We'll evict the last item in the set, which is the 
            least-recently used, then rotate it to the front. */
            setOffset = Ways - 1;
            offsetIndex = setBegin + setOffset;
            itemIndex = indexArray[offsetIndex];
            Add(key, value, set, setOffset, itemIndex);
            return;
        }

    }
}
