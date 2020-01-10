using System.Collections.Generic;
using UnityEngine;

public class DebugDrawer : MonoBehaviour
{
    private static DebugDrawer instance;
    public static DebugDrawer active
    {
        get
        {
            if (instance == null)
                instance = GameObject.FindObjectOfType<DebugDrawer>();

            return instance;
        }
    }

    public struct Cube
    {
        public Vector3 center;
        public Vector3 size;
        public Color color;

        public Cube(Vector3 center, Vector3 size, Color color)
        {
            this.center = center;
            this.size = size;
            this.color = color;
        }
    }

    public struct Line
    {
        public Vector3 from;
        public Vector3 to;
        public Color color;

        public Line(Vector3 from, Vector3 to, Color color)
        {
            this.from = from;
            this.to = to;
            this.color = color;
        }
    }

    public static void Draw(Vector2Int from, Vector2Int to, Color color)
    {
        active.lines.Add(new Line(new Vector3(from.x + 0.5f, from.y + 0.5f, 0), new Vector3(to.x + 0.5f, to.y + 0.5f, 0), color));
    }

    public static void DrawCube(Vector2Int center, Vector2Int size, Color color)
    {
        active.cubes.Add(new Cube(new Vector3(center.x + 0.5f, center.y + 0.5f, 0), new Vector3(size.x, size.y, 0), color));
    }

    public static void Clear()
    {
        active.lines.Clear();
        active.cubes.Clear();
    }

    public List<Line> lines = new List<Line>();
    public List<Cube> cubes = new List<Cube>();

    public void OnDrawGizmos()
    {
        foreach(var line in lines)
        {
            Gizmos.color = line.color;
            Gizmos.DrawLine(line.from, line.to);
        }

        foreach(var cube in cubes)
        {
            Gizmos.color = cube.color;
            Gizmos.DrawCube(cube.center, cube.size);
        }
    }
}