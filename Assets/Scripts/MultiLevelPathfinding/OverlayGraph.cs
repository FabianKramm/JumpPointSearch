using Pathfinding;
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
            public List<OverlayNeighbor>[] Neighbors;
        }

        public struct OverlayNeighbor
        {
            public int neighborID;
            public float cost;
        }

        // Overlay vertices ordered by highest level and cell
        public List<OverlayVertex> overlayVertices;

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
                    CellNumber = graph.VertexCellNumber[graph.Edges[i].From],
                    Neighbors = new List<OverlayNeighbor>[level]
                };

                var to = new OverlayVertex()
                {
                    OriginalVertex = graph.Edges[i].Target,
                    OriginalEdge = i,
                    NeighborOverlayVertex = overlayVertices.Count,
                    CellNumber = graph.VertexCellNumber[graph.Edges[i].Target],
                    Neighbors = new List<OverlayNeighbor>[level]
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
            for (var l = 1; l < cellMapping.Length; l++)
            {
                if (cellMapping[l] == null)
                    continue;
                if (graph.CellsPerLevel[l] == 0)
                    continue;

                constructLevel(l);
            }
        }

        public void constructLevel(int level)
        {
            for (var c = 0; c < cellMapping[level].Length; c++)
            {
                if (level == 1)
                {
                    constructCellBase(c);
                }
                else
                {
                    constructCellOverlay(level, c);
                }
            }
        }

        public void constructCellOverlay(int level, int c)
        {
            var queue = new MinHeap<int, float>();
            var cell = cellMapping[level][c];
            if (cell == null)
                return;
            
            var round = 0;
            var open = new Dictionary<int, int>();
            for (var i = 0; i < cell.Count; i++)
            {
                queue.Clear();

                var start = overlayVertices[cell[i]];
                start.Neighbors[level - 1] = new List<OverlayNeighbor>();
                start.Neighbors[level - 1].Add(new OverlayNeighbor()
                {
                    neighborID = start.NeighborOverlayVertex,
                    cost = graph.Edges[start.OriginalEdge].Cost
                });

                queue.Add(cell[i], 0);
                open[cell[i]] = round;
                while (queue.Count != 0)
                {
                    float currentCost = queue.PeekCost();
                    int currentOverlayVertex = queue.Remove();
                    var neighbors = overlayVertices[currentOverlayVertex].Neighbors[level - 2];
                    if (neighbors == null || neighbors.Count == 0)
                        continue;

                    for (var j = 0; j < neighbors.Count; j++)
                    {
                        var target = neighbors[j].neighborID;
                        if ((int)graph.getCellNumberOnLevel(level, overlayVertices[target].CellNumber) != c)
                        {
                            bool wasFound = false;
                            for (var x = 0; x < start.Neighbors[level - 1].Count; x++)
                            {
                                if (start.Neighbors[level - 1][x].neighborID == currentOverlayVertex)
                                {
                                    wasFound = true;
                                    break;
                                }
                            }

                            if (!wasFound)
                            {
                                start.Neighbors[level - 1].Add(new OverlayNeighbor()
                                {
                                    neighborID = currentOverlayVertex,
                                    cost = currentCost
                                });
                            }

                            continue;
                        }
                        
                        var newCost = currentCost + neighbors[j].cost;
                        var found = open.TryGetValue(target, out int nodeRound);
                        if (!found || nodeRound != round)
                        {
                            queue.Add(target, newCost);
                            open[target] = round;
                        }
                    }
                }

                round++;
            }
        }

        private void constructCellBase(int c)
        {
            var queue = new MinHeap<int, float>();
            var round = 0;
            var open = new Dictionary<int, int>();

            var cell = cellMapping[1][c];
            if (cell == null)
                return;

            for (var i = 0; i < cell.Count; i++)
            {
                queue.Clear();

                var start = overlayVertices[cell[i]];
                start.Neighbors[0] = new List<OverlayNeighbor>();
                start.Neighbors[0].Add(new OverlayNeighbor()
                {
                    neighborID = start.NeighborOverlayVertex,
                    cost = graph.Edges[start.OriginalEdge].Cost
                });

                queue.Add(start.OriginalVertex, 0);
                open[start.OriginalVertex] = round;
                while (queue.Count != 0)
                {
                    var currentCost = queue.PeekCost();
                    var current = queue.Remove();

                    var edgeEnd = (current + 1 == graph.Vertices.Count) ? graph.VertexEdgeMapping.Count : graph.Vertices[current + 1].EdgeOffset;
                    if (edgeEnd == -1)
                        continue;

                    for (var j = graph.Vertices[current].EdgeOffset; j < edgeEnd; j++)
                    {
                        var edge = graph.Edges[graph.VertexEdgeMapping[j]];
                        var target = edge.From == current ? edge.Target : edge.From;
                        if ((int)graph.getCellNumberOnLevel(1, graph.VertexCellNumber[target]) != c)
                        {
                            if (graphVertexOverlayMapping.TryGetValue(((ulong)(uint)current << 32) | (uint)graph.VertexEdgeMapping[j], out int value))
                            {
                                bool wasFound = false;
                                for (var x = 0; x < start.Neighbors[0].Count; x++)
                                {
                                    if (start.Neighbors[0][x].neighborID == value)
                                    {
                                        wasFound = true;
                                        break;
                                    }
                                }

                                if (!wasFound)
                                {
                                    start.Neighbors[0].Add(new OverlayNeighbor()
                                    {
                                        neighborID = value,
                                        cost = currentCost
                                    });
                                }
                            }

                            continue;
                        }

                        var newCost = currentCost + edge.Cost;
                        var found = open.TryGetValue(target, out int nodeRound);
                        if (!found || nodeRound != round)
                        {
                            queue.Add(target, newCost);
                            open[target] = round;
                        }
                    }
                }

                round++;
            }
        }

        private Position getFromGraphVertexID(int vertexID)
        {
            var v = graph.Vertices[vertexID];
            var x = v.GridPosition / graph.sizeY;
            var y = v.GridPosition % graph.sizeY;

            return new Position(x, y);
        }

        private Position getFromOverlayVertexID(int overlayVertexID)
        {
            var v = graph.Vertices[overlayVertices[overlayVertexID].OriginalVertex];
            var x = v.GridPosition / graph.sizeY;
            var y = v.GridPosition % graph.sizeY;

            return new Position(x, y);
        }

        public void DrawGraph()
        {
            for (var i = 0; i < overlayVertices.Count; i++)
            {
                var vertex = graph.Vertices[overlayVertices[i].OriginalVertex];
                var x = vertex.GridPosition / graph.sizeY;
                var y = vertex.GridPosition % graph.sizeY;
                if (overlayVertices[i].Neighbors.Length > 1)
                    DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.red);
                if (overlayVertices[i].Neighbors.Length == 0)
                    continue;

                for (var l = 0; l < overlayVertices[i].Neighbors.Length; l++)
                {
                    if (l < 1)
                        continue;

                    var intensity = 1.0f; // (float)l / (float)overlayVertices[i].Neighbors.Length;
                    for (var n = 0; n < overlayVertices[i].Neighbors[l].Count; n++)
                    {
                        var target = overlayVertices[i].Neighbors[l][n].neighborID;
                        if (target == overlayVertices[i].NeighborOverlayVertex)
                            continue;

                        var tx = graph.Vertices[overlayVertices[target].OriginalVertex].GridPosition / graph.sizeY;
                        var ty = graph.Vertices[overlayVertices[target].OriginalVertex].GridPosition % graph.sizeY;
                        DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), new Color(intensity, intensity, intensity));
                    }
                }
            }
        }
    }
}
