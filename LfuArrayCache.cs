using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParksComputing.SetAssociativeCache {
    public class LfuArrayCache : ICachePolicy {
        ICachePolicyImpl<TKey, TValue> ICachePolicy.MakeInstance<TKey, TValue> (int capacity, int sets) {
            return new LfuArrayCacheImpl<TKey, TValue>(capacity, sets);
        }
    }

    class LfuArrayCacheImpl<TKey, TValue> : ArrayCacheImplBase<TKey, TValue> {

        public LfuArrayCacheImpl(int sets, int ways) : base(sets, ways) {
        }

        override public void Add(TKey key, TValue value) {
            /* This is still the LRU algorithm. I need to replace this with the LFU version. */
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
