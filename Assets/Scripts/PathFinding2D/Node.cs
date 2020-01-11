public class Node
{
    public Node parent;
    public int x;
    public int y;
    public uint _flags;

    public void setOpen() { _flags |= 1; }
    public void setClosed() { _flags |= 2; }
    public bool isOpen() { return (_flags & 1) == 1; }
    public bool isClosed() { return (_flags & 2) == 2; }

    public int gCost;
    public int hCost;
    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public Node() { }

    public Node(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public void Reset()
    {
        gCost = 0;
        hCost = 0;
        parent = null;
    }

    public override string ToString()
    {
        return x.ToString() + " , " + y.ToString();
    }
}