public class Node
{
    public Node parent;
    public int x;
    public int y;

    private int _heapIndex = 0;
    public int HeapIndex
    {
        get
        {
            return _heapIndex;
        }
        set
        {
            _heapIndex = value;
        }
    }

    public int gCost;
    public int hCost;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public Node(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }

    public override string ToString()
    {
        return x.ToString() + " , " + y.ToString();
    }
}