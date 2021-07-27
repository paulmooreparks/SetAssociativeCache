# SetAssociativeCache
This is a C# implementation of a set-associative cache with multiple policies (LRU, LFU, etc.). There is a basic 
generic cache interface, ISetAssociativeCache, which is parameterized by the key type and value type of the items 
stored in the cache, much like the IDictionary interface. Implementations of the interface may use different 
policies for when items are evicted from the cache, such as least-frequently used or least-recently used.

    /* Create a cache that maps string keys to string values, with 2 sets of 4 elements, or "ways". 
    In this example, we use the LruArrayCache implementation, which removes the least-recently used 
    item (LRU) when a new item is added to a full set. Slots are tracked in an array rather than a 
    linked list, in order to keep it CPU-cache friendly. 
    
    If we decide later that a least-frequently used cache (LFU) cache is more appropriate, we can 
    change LruArrayCache to LfuArrayCache. We could also add new classes with other implementations. */
    

    var coupleCache = new LruArrayCache<string, string>(sets: 2, ways: 4);

    Console.WriteLine($"There is room for {coupleCache.Capacity} couples. Let the games begin....");

    coupleCache["Brad"] = "Angelina";
    coupleCache["Ben"] = "Jennifer";
    coupleCache["Kanye"] = "Kim";
    coupleCache["Sonny"] = "Cher";
    coupleCache["Desi"] = "Lucy";
    coupleCache["Donald"] = "Ivana";
    coupleCache["Burt"] = "Loni";
    coupleCache["Johnny"] = "June";
    coupleCache["John"] = "Yoko";
    coupleCache["Kurt"] = "Goldie";

    Console.WriteLine($"Out of 10 couples added, {coupleCache.Count} couples remain in the cache");
    Console.WriteLine();

    foreach (var couple in coupleCache) {
        Console.WriteLine($"{couple.Key} loves {couple.Value}");
    }

    /* Note that Brangelina and Kimye have been evicted from their respective sets. */
