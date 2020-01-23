using UnityEngine;
using UnityEngine.Tilemaps;

public class GridController : MonoBehaviour
{
    private static GridController instance;
    public static GridController active
    {
        get
        {
            if (instance == null)
                instance = GameObject.Find("Grid").GetComponent<GridController>();

            return instance;
        }
    }

    private static Tilemap ground;
    public static Tilemap Ground
    {
        get
        {
            if (ground == null)
            {
                ground = GameObject.Find("Ground").GetComponent<Tilemap>();
            }

            return ground;
        }
    }

    private static Tilemap path;
    public static Tilemap Path
    {
        get
        {
            if (path == null)
            {
                path = GameObject.Find("Path").GetComponent<Tilemap>();
            }

            return path;
        }
    }

    public Tile walkable;
    public Tile road;
    public Tile blocked;

    public Tile start;
    public Tile end;

    public Vector2Int size = new Vector2Int(256, 256);

    public void Awake()
    {
        for (int x = 0; x < size.x; x++)
            for (int y = 0; y < size.y; y++)
            {
                Ground.SetTile(new Vector3Int(x, y, 0), walkable);
            }
    }

    public void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var positionInt = new Vector3Int((int)position.x, (int)position.y, 0);

            Debug.Log("Clicked at: " + positionInt.x + " - " + positionInt.y);
            /*var tile = Ground.GetTile(positionInt);

            if (tile == walkable)
            {
                Ground.SetTile(positionInt, blocked);
            }
            else if (tile == blocked)
            {
                Ground.SetTile(positionInt, road);
            }
            else if (tile == road)
            {
                Ground.SetTile(positionInt, walkable);
            }*/
        }
        else if (Input.GetMouseButtonDown(1))
        {
            var position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var positionInt = new Vector3Int((int)position.x, (int)position.y, 0);
            if (has(end) == false && has(start))
            {
                Path.SetTile(positionInt, end);
            }
            else
            {
                DebugDrawer.Clear();
                Path.ClearAllTiles();
                Path.SetTile(positionInt, start);
            }
        }
    }

    private bool has(Tile tile)
    {
        for (var x = 0; x < size.x; x++)
            for (var y = 0; y < size.y; y++)
            {
                var pathTile = Path.GetTile(new Vector3Int(x, y, 0));
                if (pathTile != null)
                {
                    if (pathTile == tile)
                    {
                        return true;
                    }
                }
            }

        return false;
    }
}