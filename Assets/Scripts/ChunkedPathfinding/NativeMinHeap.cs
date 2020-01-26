using System;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace ChunkedPathfinding
{
    /// <summary>
    /// Implements a basic Binary Min Heap that can be used for A* pathfinding. Items are removed from 
    /// the list in order from lowest to highest cost
    /// </summary>
    [NativeContainer]
    public unsafe struct NativeMinHeap<TData, TCost> where TCost : IComparable<TCost>
    {
        #region Public properties

        /// <summary>
        /// Gets the number of values in the heap. 
        /// </summary>
        public int Count
        {
            get { return m_count; }
        }

        /// <summary>
        /// Gets or sets the capacity of the heap.
        /// </summary>
        public int Capacity
        {
            get { return m_capacity; }
            set
            {
                setCapacity(value);
            }
        }

        #endregion

        #region Private data

        private const int DEFAULT_SIZE = 32;
        private const int GROWTH_FACTOR = 2;

        [NativeDisableUnsafePtrRestriction]
        internal UnsafeList* m_items;
        private int m_count;
        private int m_capacity;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the BinaryHeap class with a the indicated capacity
        /// </summary>
        public NativeMinHeap(int capacity, Allocator allocator)
        {
            if (capacity < 1)
                throw new ArgumentException("Capacity must be greater than zero");

            m_capacity = capacity;
            m_items = UnsafeList.Create(UnsafeUtility.SizeOf<HeapItem>(), UnsafeUtility.AlignOf<HeapItem>(), capacity, allocator);
            m_count = 0;
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Removes all items from the heap.
        /// </summary>
        public void Clear()
        {
            this.m_count = 0;
            m_items->Clear();
        }

        /// <summary>
        /// Adds a new item to the heap
        /// </summary>
        /// <param name="item">The item to add to the heap.</param>
        public void Add(TData item, TCost cost)
        {
            if (m_count == m_capacity)
            {
                setCapacity((int)(m_capacity * GROWTH_FACTOR));
            }

            set(m_count, new HeapItem { Data = item, Cost = cost });
            bubbleUp(m_count);

            m_count++;
        }

        public void Update(TData item, TCost cost)
        {
            for (int i = 0; i < m_count; i++)
            {
                if (get(i).Data.Equals(item))
                {
                    set(i, new HeapItem
                    {
                        Data = item,
                        Cost = cost
                    });
                    bubbleDown(i);
                    bubbleUp(i);
                    return;
                }
            }
        }

        public TCost PeekCost()
        {
            return get(0).Cost;
        }

        /// <summary>
        /// Returns the item with the lowest cost without removing it from the heap
        /// </summary>
        /// <returns></returns>
        public TData Peek()
        {
            if (this.m_count == 0)
            {
                throw new InvalidOperationException("Cannot peek at first item, heap is empty.");
            }

            return get(0).Data;
        }

        /// <summary>
        /// Removes and returns the item with the lowest cost
        /// </summary>
        /// <returns>The first value in the heap.</returns>
        public TData Remove()
        {
            if (this.m_count == 0)
            {
                throw new InvalidOperationException("Cannot remove item, heap is empty.");
            }

            var v = get(0).Data;

            // Decrease heap size by 1
            m_count -= 1;

            m_items[0] = m_items[m_count];
            set(m_count, new HeapItem
            {
                Cost = get(m_count).Cost,
                Data = default(TData) // Clears the Last Node
            });
            bubbleDown(0);

            return v;
        }

        private HeapItem get(int index)
        {
            return UnsafeUtility.ReadArrayElement<HeapItem>(m_items->Ptr, index);
        }

        private void set(int index, HeapItem item)
        {
            UnsafeUtility.WriteArrayElement(m_items->Ptr, index, item);
        }

        #endregion

        #region Private utility methods

        private void setCapacity(int newCapacity)
        {
            newCapacity = Math.Max(newCapacity, m_count);
            if (m_capacity != newCapacity)
            {
                m_capacity = newCapacity;
                m_items->Resize(UnsafeUtility.SizeOf<HeapItem>(), UnsafeUtility.AlignOf<HeapItem>(), m_capacity, NativeArrayOptions.ClearMemory);
            }
        }

        private void bubbleUp(int index)
        {
            if (index == 0)
                return;

            var item = get(index);
            var parent = (index - 1) >> 1;

            while (parent > -1 && item.Cost.CompareTo(get(parent).Cost) < 0)
            {
                m_items[index] = m_items[parent]; // Swap nodes
                index = parent;
                parent = (index - 1) >> 1;
            }

            set(index, item);
        }

        private void bubbleDown(int index)
        {
            var parent = index == 0 ? 0 : (index - 1) >> 1;
            var item = get(parent);

            while (true)
            {
                int ch1 = (parent << 1) + 1;
                if (ch1 >= m_count)
                    break;

                int ch2 = (parent << 1) + 2;
                if (ch2 >= m_count)
                {
                    index = ch1;
                }
                else
                {
                    index = (get(ch1).Cost.CompareTo(get(ch2).Cost) < 0) ? ch1 : ch2;
                }

                if (item.Cost.CompareTo(get(index).Cost) < 0)
                    break;

                m_items[parent] = m_items[index]; // Swap nodes
                parent = index;
            }

            set(parent, item);
        }

        #endregion

        #region Nested types

        [StructLayout(LayoutKind.Sequential)]
        private struct HeapItem
        {
            public TData Data;
            public TCost Cost;
        }

        #endregion
    }
}