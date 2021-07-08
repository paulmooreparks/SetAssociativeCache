using System;
using NUnit.Framework;
using ParksComputing.SetAssociativeCache;
using System.Linq;

namespace SetAssociativeCacheSample {
    class Program {
        static void Main(string[] args) {
            string line;
            int lineCount = 0;
            bool dump = false;
            bool timer = false;

            if (args.Count() >= 1) {
                for (int argIndex = 0; argIndex < args.Count(); ++argIndex) {
                    var arg = args[argIndex].ToLower();

                    if (arg == "-d" || arg == "--dump") {
                        dump = true;
                    }
                    else if (arg == "-t" || arg == "--timer") {
                        timer = true;
                    }
                }
            }

            ISetAssociativeCache<string, string> cache = null; // = new LruArrayCache<string, string>(1, 500);

            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime;

            // Process each line in the input.
            while (!string.IsNullOrEmpty(line = Console.In.ReadLine())) {
                lineCount++;

                try {
                    if (lineCount == 1) {
                        var cacheParams = line.Split(',').Select(i => i.Trim()).ToArray(); ;
                        var setCount = int.Parse(cacheParams[0]);
                        var setSize = int.Parse(cacheParams[1]);

                        string replacementAlgoName = cacheParams[2];
                        
                        switch (replacementAlgoName) {
                        case "LRUReplacementAlgo":
                            cache = new LruCache<string, string>(setCount, setSize);
                            break;

                        case "LFUReplacementAlgo":
                            cache = new LfuCache<string, string>(setCount, setSize);
                            break;

                        case "MRUReplacementAlgo":
                            cache = new MruCache<string, string>(setCount, setSize);
                            break;

                        default:
                            throw new FormatException($"Unknown replacement algo '{replacementAlgoName}'");
                        }
                    }
                    // All remaining lines invoke instance methods on the SetAssociativeCache
                    else {
                        var retValue = InvokeCacheMethod(line, cache);

                        // Write the method's return value (if any) to stdout
                        if (retValue != null) {
                            if (retValue is bool) {
                                retValue = retValue.ToString().ToLowerInvariant();
                            }

                            Console.Out.WriteLine(retValue);
                        }
                    }
                }
                catch (FormatException ex) {
                    throw new InvalidOperationException($"Invalid test case input at line {lineCount}. Cannot parse '{line}'.", ex);
                }
            }

            endTime = DateTime.Now;

            if (dump) {
                Console.WriteLine();
                Console.WriteLine("Cache dump:");

                foreach (var item in cache) {
                    Console.WriteLine($"{item.Key} = {item.Value}");
                }
            }

            if (timer) {
                Console.WriteLine();
                Console.WriteLine("Execution time:");
                TimeSpan duration = endTime - startTime;

                Console.WriteLine($"{duration.ToString("c")}");
            }
        }


        public static object InvokeCacheMethod(string inputLine, ISetAssociativeCache<string, string> cacheInstance) {
            var callArgs = inputLine.Split(',').Select(a => a.Trim()).ToArray();

            var methodName = callArgs[0].ToLowerInvariant();
            var callParams = callArgs.Skip(1).ToArray();

            switch (methodName) {
            case "get":
                string value = "";
                cacheInstance.TryGetValue(callArgs[1], out value);
                return value;
            case "set":
                cacheInstance.Add(callArgs[1], callArgs[2]);
                return null;
            case "containskey":
                return cacheInstance.ContainsKey(callArgs[1]);
            case "getcount":
                return cacheInstance.Count;

            default:
                throw new FormatException($"Unknown method name '{methodName}'");
            }
        }


        static void OldMain(string[] args) {
            /* Create a cache that maps string keys to string values, with 2 sets of 4 elements, or "ways". 
            The policy class name is passed as the last type parameter. In this example, we use LruArrayCache, 
            which removes the least-recently used item (LRU) when a new item is added to a full set. Slots 
            are tracked in an array rather than a linked list, in order to keep it CPU-cache friendly. 
            If we decide later that a least-frequently used cache (LFU) cache is more appropriate, we can 
            change LruArrayCache to LfuArrayCache. We could also add new classes with other implementations. */
            var coupleCache = new LruCache<string, string>(1, 500);

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
        }
    }
}
