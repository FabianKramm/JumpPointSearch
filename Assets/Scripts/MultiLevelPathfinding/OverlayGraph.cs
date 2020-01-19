using System.Collections.Generic;
using UnityEngine;

namespace MultiLevelPathfinding
{
    public class OverlayGraph
    {
        public class OverlayVertex
        {
            // The border overlay vertex this overlay vertex points to
            public int OriginalVertex;
            public int NeighborOverlayVertex;
            public int OriginalEdge;

            public ulong CellNumber;

            // This is a list that stores for each level the offset to itself in the overlay id mapping
            public List<OverlayNeighbor>[] neighbors;
        }

        public struct OverlayNeighbor
        {
            public int neighborID;
            public float cost;
        }

        // Overlay vertices ordered by highest level and cell
        public List<OverlayVertex> overlayVertices;
        public List<int> overlayCellMapping;

        // Holds for each level a list with the vertices that are in the cell
        public List<int>[][] cellMapping;

        // Graph vertex id << 32 | Graph edge -> overlay vertex
        public Dictionary<ulong, int> graphVertexOverlayMapping;

        private Graph graph;

        public OverlayGraph(Graph graph)
        {
            this.graph = graph;
        }

        public void ConstructOverlayNodes()
        {
            overlayVertices = new List<OverlayVertex>();
            overlayCellMapping = new List<int>();
            graphVertexOverlayMapping = new Dictionary<ulong, int>();
            cellMapping = new List<int>[graph.offset.Length][];

            // First construct all overlay vertices and create the cell map
            for (var i = 0; i < graph.Edges.Count; i++)
            {
                var level = graph.getHighestDifferingLevel(graph.VertexCellNumber[graph.Edges[i].From], graph.VertexCellNumber[graph.Edges[i].Target]);
                if (level == 0)
                    continue;

                var from = new OverlayVertex()
                {
                    OriginalVertex = graph.Edges[i].From,
                    OriginalEdge = i,
                    NeighborOverlayVertex = overlayVertices.Count + 1,
                    CellNumber = graph.VertexCellNumber[graph.Edges[i].From]
                };

                var to = new OverlayVertex()
                {
                    OriginalVertex = graph.Edges[i].Target,
                    OriginalEdge = i,
                    NeighborOverlayVertex = overlayVertices.Count,
                    CellNumber = graph.VertexCellNumber[graph.Edges[i].From]
                };

                var fromID = overlayVertices.Count;
                overlayVertices.Add(from);
                graphVertexOverlayMapping[((ulong)(uint)from.OriginalVertex) << 32 | (uint)from.OriginalEdge] = fromID;

                var toID = overlayVertices.Count;
                overlayVertices.Add(to);
                graphVertexOverlayMapping[((ulong)(uint)to.OriginalVertex) << 32 | (uint)to.OriginalEdge] = toID;

                for (var l = level; l > 0; l--)
                {
                    // FROM
                    var cellNumberOnLevel = graph.getCellNumberOnLevel(l, from.CellNumber);
                    if (cellMapping[l] == null)
                    {
                        cellMapping[l] = new List<int>[graph.CellsPerLevel[l]];
                    }
                    if (cellMapping[l][cellNumberOnLevel] == null)
                    {
                        cellMapping[l][cellNumberOnLevel] = new List<int>();
                    }
                    cellMapping[l][cellNumberOnLevel].Add(fromID);

                    // TO 
                    cellNumberOnLevel = graph.getCellNumberOnLevel(l, to.CellNumber);
                    if (cellMapping[l] == null)
                    {
                        cellMapping[l] = new List<int>[graph.CellsPerLevel[l]];
                    }
                    if (cellMapping[l][cellNumberOnLevel] == null)
                    {
                        cellMapping[l][cellNumberOnLevel] = new List<int>();
                    }
                    cellMapping[l][cellNumberOnLevel].Add(toID);
                }
            }
        }

        public void ConstructOverlayEdges()
        {

        }

        public void DrawGraph()
        {
            for (var i = 0; i < overlayVertices.Count; i++)
            {
                var vertex = graph.Vertices[overlayVertices[i].OriginalVertex];
                var x = vertex.GridPosition / graph.sizeY;
                var y = vertex.GridPosition % graph.sizeY;
                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.red);

                var target = overlayVertices[overlayVertices[i].NeighborOverlayVertex].OriginalVertex;
                var tx = graph.Vertices[target].GridPosition / graph.sizeY;
                var ty = graph.Vertices[target].GridPosition % graph.sizeY;
                DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), Color.red);
            }
        }
    }
}
