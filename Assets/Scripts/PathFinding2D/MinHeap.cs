using System;

/// <summary>
/// Implements a basic Binary Min Heap that can be used for A* pathfinding. Items are removed from 
/// the list in order from lowest to highest cost
/// </summary>
public class MinHeap<TData, TCost> where TCost : IComparable<TCost>
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

    private HeapItem[] m_items = null;
    private int m_count = 0;
    private int m_capacity = DEFAULT_SIZE;

    #endregion

    #region Constructors

    /// <summary>
    /// Creates a new binary heap.
    /// </summary>
    public MinHeap()
    {
        m_items = new HeapItem[DEFAULT_SIZE];
        m_capacity = DEFAULT_SIZE;
    }

    /// <summary>
    /// Initializes a new instance of the BinaryHeap class with a the indicated capacity
    /// </summary>
    public MinHeap(int capacity)
    {
        if (capacity < 1)
            throw new ArgumentException("Capacity must be greater than zero");

        m_capacity = capacity;
        m_items = new HeapItem[capacity];
    }

    #endregion

    #region Public methods

    /// <summary>
    /// Removes all items from the heap.
    /// </summary>
    public void Clear()
    {
        this.m_count = 0;
        Array.Clear(m_items, 0, m_items.Length);
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

        m_items[m_count].Set(item, cost);

        bubbleUp();

        m_count++;
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

        return m_items[0].Data;
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

        var v = m_items[0].Data;

        // Decrease heap size by 1
        m_count -= 1;

        m_items[0] = m_items[m_count];
        m_items[m_count].Data = default(TData); // Clears the Last Node

        bubbleDown();

        return v;
    }

    #endregion

    #region Private utility methods

    private void setCapacity(int newCapacity)
    {
        newCapacity = Math.Max(newCapacity, m_count);
        if (m_capacity != newCapacity)
        {
            m_capacity = newCapacity;
            Array.Resize(ref m_items, m_capacity);
        }
    }

    private void bubbleUp()
    {
        var index = m_count;
        var item = m_items[index];
        var parent = (index - 1) >> 1;

        while (parent > -1 && item.Cost.CompareTo(m_items[parent].Cost) < 0)
        {
            m_items[index] = m_items[parent]; // Swap nodes
            index = parent;
            parent = (index - 1) >> 1;
        }

        m_items[index] = item;
    }

    private void bubbleDown()
    {
        var index = 0;
        var parent = 0;
        var item = m_items[parent];

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
                index = (m_items[ch1].Cost.CompareTo(m_items[ch2].Cost) < 0) ? ch1 : ch2;
            }

            if (item.Cost.CompareTo(m_items[index].Cost) < 0)
                break;

            m_items[parent] = m_items[index]; // Swap nodes
            parent = index;
        }

        m_items[parent] = item;
    }

    #endregion

    #region Nested types

    private struct HeapItem
    {
        public TData Data;
        public TCost Cost;

        public void Set(TData data, TCost cost)
        {
            this.Data = data;
            this.Cost = cost;
        }
    }

    #endregion
}