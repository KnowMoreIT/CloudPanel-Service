using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace CPService
{
    //
    // This class collects a sequence of items of type T (value or reference type)
    // and keeps them in the order they get added.
    // A list-of-lists technique is used to keep everything in the regular heap,
    // i.e. this class should not challenge the large object heap unless
    // the number of items collected gets really big (above 100 million).
    // To control what is inside the collection:
    //	- use Add to add an item at the end;
    //	- use Clear to clear the collection.
    // To obtain information about its content:
    //	- use the Count property;
    //	- use the IEnumerable<T> interface (or the old-fashioned IEnumerable if you must)
    //	  (both foreach and LINQ rely on the IEnumerable<T> interface when present)
    //
    // Note: value types such as structs should be small (max 8B) otherwise
    // the level1 lists may not fit in the regular heap.
    public class ListOfLists<T> : IEnumerable, IEnumerable<T>
    {
        public const int MAX_ITEMS_IN_LEVEL1 = 10000;
        //public static ILog Logger;
        private List<List<T>> level0;
        private List<T> currentLevel1;

        // constructor
        public ListOfLists()
        {
            Clear();
        }

        // logging utility
        private void log(string msg)
        {
            //if(Logger!=null) Logger.log(msg);
        }

        // empty the collection
        public void Clear()
        {
            level0 = new List<List<T>>();
            currentLevel1 = new List<T>();
            level0.Add(currentLevel1);
        }

        // add an item at the end of the collection
        public void Add(T item)
        {
            if (currentLevel1.Count >= MAX_ITEMS_IN_LEVEL1)
            {
                currentLevel1 = new List<T>();
                level0.Add(currentLevel1);
            }
            currentLevel1.Add(item);
        }

        // get the number of items in the collection
        public int Count
        {
            get
            {
                int totalCount = 0;
                foreach (List<T> level1 in level0) totalCount += level1.Count;
                return totalCount;
            }
        }

        // log the internals
        public void DumpCounts()
        {
            int totalCount = 0;
            log("level0.Count=" + level0.Count);
            int i = 0;
            foreach (List<T> level1 in level0)
            {
                int count1 = level1.Count;
                log("level0[" + i + "].Count=" + count1);
                totalCount += count1;
                i++;
            }
            log("total count=" + totalCount);
        }

        // get an old-style enumerator (typeless)
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        // get an enumerator (this one gets used by foreach and LINQ)
        public IEnumerator<T> GetEnumerator()
        {
            foreach (List<T> level1 in level0)
            {
                foreach (T item in level1)
                {
                    yield return item;
                }
            }
        }
    }
}