public class NodePool : ObjectPool<Node>
{
    public static Node NewNode(int x, int y)
    {
        var node = ClaimFromPool();
        node.x = x;
        node.y = y;
        return node;
    }
}