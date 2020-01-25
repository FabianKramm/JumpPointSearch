using Pathfinding;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiLevelPathfinding
{
    public class OverlayGraphChunk
    {
        private IGrid grid;
        private OverlayGraph overlayGraph;
        
        public int chunkNumber;
        private int chunkSizeY;
        private int sizeY;

        public List<Vertex> vertices;
        public List<int> vertexEdgeMapping;
        public List<Edge> edges;

        public List<OverlayVertex> overlayVertices;
        public List<int>[][] overlayVerticesCellMapping;

        public Dictionary<ulong, int> edgeToOverlayVertex;
        public Dictionary<int, int> gridPositionToVertex;

        public OverlayGraphChunk(OverlayGraph overlayGraph, IGrid grid, int chunkNumber)
        {
            this.overlayGraph = overlayGraph;
            this.grid = grid;
            this.chunkNumber = chunkNumber;
            this.sizeY = grid.GetSize().y;
            this.chunkSizeY = sizeY / overlayGraph.chunkSize;
        }

        public void Build()
        {
            var (v, vd) = ConstructVertices();

            ConstructEdges(v, vd);
            ConstructOverlayNodes();
            ConstructOverlayEdges();

            // Update vertices with the real cell number
            for (var i = 0; i < vertices.Count; i++)
            {
                var x = vertices[i].GridPosition / sizeY;
                var y = vertices[i].GridPosition % sizeY;

                vertices[i] = new Vertex
                {
                    GridPosition = vertices[i].GridPosition,
                    EdgeOffset = vertices[i].EdgeOffset,
                    CellNumber = OverlayGraphUtilities.GetCellNumber(x, y, sizeY, overlayGraph.offset)
                };
            }
        }

        // ConstructVertices constructs vertices within the specified area and tries to connect them
        public (List<Vertex>, Dictionary<int, int>) ConstructVertices()
        {
            var chunkX = chunkNumber / chunkSizeY;
            var chunkY = chunkNumber % chunkSizeY;

            var startX = Math.Max(0, chunkX * overlayGraph.chunkSize - overlayGraph.chunkSize);
            var startY = Math.Max(0, chunkY * overlayGraph.chunkSize - overlayGraph.chunkSize);
            var endX = Math.Min(grid.GetSize().x, chunkX * overlayGraph.chunkSize + overlayGraph.chunkSize * 2);
            var endY = Math.Min(grid.GetSize().y, chunkY * overlayGraph.chunkSize + overlayGraph.chunkSize * 2);

            var chunkStartX = chunkX * overlayGraph.chunkSize;
            var chunkStartY = chunkY * overlayGraph.chunkSize;
            var chunkEndX = chunkX * overlayGraph.chunkSize + overlayGraph.chunkSize;
            var chunkEndY = chunkY * overlayGraph.chunkSize + overlayGraph.chunkSize;

            var includingNeighborVertices = new List<Vertex>();
            var includingNeighborGridPositionToVertex = new Dictionary<int, int>();

            vertices = new List<Vertex>(); 
            gridPositionToVertex = new Dictionary<int, int>();

            // Construct subgoals
            for (var y = startY; y < endY; y++)
                for (var x = startX; x < endX; x++)
                {
                    if (grid.IsWalkable(x, y) == false)
                        continue;

                    var gridPosition = x * sizeY + y;
                    for (var d = 4; d < 8; d++)
                    {
                        if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y + SubGoalGrid.directions[d][1]) == false)
                        {
                            if (grid.IsWalkable(x + SubGoalGrid.directions[d][0], y) && grid.IsWalkable(x, y + SubGoalGrid.directions[d][1]))
                            {
                                includingNeighborVertices.Add(new Vertex
                                {
                                    GridPosition = gridPosition,
                                    CellNumber = OverlayGraphUtilities.GetCellNumber(x, y, sizeY, overlayGraph.offset),
                                });
                                includingNeighborGridPositionToVertex[gridPosition] = includingNeighborVertices.Count - 1;

                                if (!(x < chunkStartX || x >= chunkEndX || y < chunkStartY || y >= chunkEndY))
                                {
                                    vertices.Add(new Vertex()
                                    {
                                        GridPosition = gridPosition,
                                        CellNumber = OverlayGraphUtilities.GetLocalCellNumber(x, y, sizeY, overlayGraph.offset)
                                    });
                                    gridPositionToVertex[gridPosition] = vertices.Count - 1;
                                }

                                break;
                            }
                        }
                    }
                }

            return (includingNeighborVertices, includingNeighborGridPositionToVertex);
        }

        // ConstructEdges constructs edges for subgoalds within the specified area
        public void ConstructEdges(List<Vertex> includingNeighborVertices, Dictionary<int, int> includingNeighborGridPositionToVertex)
        {
            var chunkX = chunkNumber / chunkSizeY;
            var chunkY = chunkNumber % chunkSizeY;

            var startX = chunkX * overlayGraph.chunkSize;
            var startY = chunkY * overlayGraph.chunkSize;
            var endX = chunkX * overlayGraph.chunkSize + overlayGraph.chunkSize;
            var endY = chunkY * overlayGraph.chunkSize + overlayGraph.chunkSize;

            edges = new List<Edge>();
            vertexEdgeMapping = new List<int>();

            var createdEdges = new Dictionary<ulong, int>();
            for (var i = 0; i < includingNeighborVertices.Count; i++)
            {
                var x = includingNeighborVertices[i].GridPosition / sizeY;
                var y = includingNeighborVertices[i].GridPosition % sizeY;
                var gridPosition = x * sizeY + y;
                if (x < startX || x >= endX || y < startY || y >= endY)
                    continue;

                var otherSubGoals = GetDirectHReachable(x, y, includingNeighborGridPositionToVertex);

                // Get the real vertex id
                if (gridPositionToVertex.TryGetValue(gridPosition, out int vID))
                {
                    vertices[vID] = new Vertex
                    {
                        EdgeOffset = vertexEdgeMapping.Count,
                        GridPosition = gridPosition,
                        CellNumber = vertices[vID].CellNumber
                    };
                }
                else
                {
                    throw new Exception("This should never happen");
                }

                for (int j = 0, count = otherSubGoals.Count; j < count; j++)
                {
                    var otherSubGoal = otherSubGoals[j];
                    if (otherSubGoal == i)
                        continue;

                    var ox = includingNeighborVertices[otherSubGoal].GridPosition / sizeY;
                    var oy = includingNeighborVertices[otherSubGoal].GridPosition % sizeY;
                    var oGridPosition = includingNeighborVertices[otherSubGoal].GridPosition;
                    var isOutsideOfChunk = (ox < startX || ox >= endX || oy < startY || oy >= endY);

                    var edgeID = (ulong)otherSubGoal << 32 | (ulong)(uint)i;
                    if (createdEdges.TryGetValue(edgeID, out int edgeOffset) == false)
                    {
                        var toVertexID = -1;
                        if (isOutsideOfChunk)
                        {
                            toVertexID = -1;
                        }
                        else if (gridPositionToVertex.TryGetValue(oGridPosition, out int oID))
                        {
                            toVertexID = oID;
                        }
                        else
                        {
                            throw new Exception("This should never happen");
                        }

                        edges.Add(new Edge()
                        {
                            FromVertex = vID,
                            ToVertex = toVertexID,
                            ToVertexGridPosition = oGridPosition,
                            Cost = SubGoalGrid.Diagonal(x, y, ox, oy)
                        });

                        var newEdgeID = (ulong)i << 32 | (ulong)(uint)otherSubGoal;
                        createdEdges[newEdgeID] = edges.Count - 1;
                        vertexEdgeMapping.Add(edges.Count - 1);
                    }
                    else
                    {
                        vertexEdgeMapping.Add(edgeOffset);
                    }
                }
            }
        }

        public List<int> GetDirectHReachable(int x, int y, Dictionary<int, int> subGoals)
        {
            var reachable = new List<int>();

            // Get cardinal reachable
            for (int i = 0; i < 8; i++)
            {
                var clearance = Clearance(x, y, SubGoalGrid.directions[i][0], SubGoalGrid.directions[i][1], subGoals);
                var subgoal = new Position(x + clearance * SubGoalGrid.directions[i][0], y + clearance * SubGoalGrid.directions[i][1]);
                if (grid.IsWalkable(subgoal.x, subgoal.y) && subGoals.TryGetValue(subgoal.x * sizeY + subgoal.y, out int vertexRef))
                {
                    reachable.Add(vertexRef);
                }
            }

            // Get diagonal reachable
            for (int d = 4; d < 8; d++)
            {
                for (int c = 0; c <= 1; c++)
                {
                    var cx = c == 0 ? SubGoalGrid.directions[d][0] : 0;
                    var cy = c == 0 ? 0 : SubGoalGrid.directions[d][1];

                    var max = Clearance(x, y, cx, cy, subGoals);
                    var diag = Clearance(x, y, SubGoalGrid.directions[d][0], SubGoalGrid.directions[d][1], subGoals);
                    if (subGoals.ContainsKey((x + max * cx) * sizeY + (y + max * cy)))
                        max--;
                    if (subGoals.ContainsKey((x + diag * SubGoalGrid.directions[d][0]) * sizeY + (y + diag * SubGoalGrid.directions[d][1])))
                        diag--;

                    for (int i = 1; i < diag; i++)
                    {
                        var newPosX = x + i * SubGoalGrid.directions[d][0];
                        var newPosY = y + i * SubGoalGrid.directions[d][1];
                        var j = Clearance(newPosX, newPosY, cx, cy, subGoals);

                        var subGoalPosX = newPosX + j * cx;
                        var subGoalPosY = newPosY + j * cy;
                        if (j <= max && grid.IsWalkable(subGoalPosX, subGoalPosY) && subGoals.TryGetValue(subGoalPosX * sizeY + subGoalPosY, out int vertexRef))
                        {
                            reachable.Add(vertexRef);
                            j--;
                        }
                        if (j < max)
                        {
                            max = j;
                        }
                    }
                }
            }

            return reachable;
        }

        public int Clearance(int x, int y, int dx, int dy, Dictionary<int, int> subGoals)
        {
            int i = 0;
            while (true)
            {
                if (i >= 200 || !grid.IsWalkable(x + i * dx, y + i * dy))
                {
                    return i;
                }

                if (dx != 0 && dy != 0 && (!grid.IsWalkable(x + (i + 1) * dx, y + i * dy) || !grid.IsWalkable(x + i * dx, y + (i + 1) * dy)))
                {
                    return i;
                }

                i = i + 1;
                if (subGoals.ContainsKey((x + i * dx) * sizeY + (y + i * dy)))
                {
                    return i;
                }
            }
        }

        public void ConstructOverlayNodes()
        {
            overlayVertices = new List<OverlayVertex>();
            overlayVerticesCellMapping = new List<int>[overlayGraph.offset.Length][];
            edgeToOverlayVertex = new Dictionary<ulong, int>();

            // First construct all overlay vertices and create the cell map
            for (var i = 0; i < edges.Count; i++)
            {
                var level = OverlayGraph.LevelDimensions.Length;
                if (edges[i].ToVertex != -1)
                {
                    level = OverlayGraphUtilities.GetHighestDifferingLevel(vertices[edges[i].FromVertex].CellNumber, vertices[edges[i].ToVertex].CellNumber, overlayGraph.offset);
                    // var toX = edges[i].ToVertexGridPosition / sizeY;
                    // var toY = edges[i].ToVertexGridPosition % sizeY;
                   // toCellNumber = OverlayGraphUtilities.GetCellNumber(toX, toY, sizeY, overlayGraph.offset);
                }

                if (level == 0)
                    continue;

                var from = new OverlayVertex()
                {
                    OriginalVertex = edges[i].FromVertex,
                    OriginalEdge = i,
                    NeighborOverlayVertex = edges[i].ToVertex == -1 ? -1 : overlayVertices.Count + 1,
                    OverlayEdges = new List<OverlayEdge>[level]
                };

                var fromID = overlayVertices.Count;
                overlayVertices.Add(from);
                edgeToOverlayVertex[((ulong)(uint)vertices[edges[i].FromVertex].GridPosition) << 32 | (uint)edges[i].ToVertexGridPosition] = fromID;

                OverlayVertex to = new OverlayVertex();
                int toID = 0;
                if (edges[i].ToVertex != -1)
                {
                    to = new OverlayVertex()
                    {
                        OriginalVertex = edges[i].ToVertex,
                        OriginalEdge = i,
                        NeighborOverlayVertex = fromID,
                        OverlayEdges = new List<OverlayEdge>[level]
                    };

                    toID = overlayVertices.Count;
                    overlayVertices.Add(to);
                    edgeToOverlayVertex[((ulong)(uint)vertices[to.OriginalVertex].GridPosition) << 32 | (uint)vertices[edges[i].FromVertex].GridPosition] = toID;
                }

                for (var l = level; l > 0; l--)
                {
                    // FROM
                    var cellNumberOnLevel = OverlayGraphUtilities.GetCellNumberOnLevel(l, vertices[from.OriginalVertex].CellNumber, overlayGraph.offset);
                    if (overlayVerticesCellMapping[l] == null)
                    {
                        overlayVerticesCellMapping[l] = new List<int>[overlayGraph.cellsPerLevel[l]];
                    }
                    if (overlayVerticesCellMapping[l][cellNumberOnLevel] == null)
                    {
                        overlayVerticesCellMapping[l][cellNumberOnLevel] = new List<int>();
                    }
                    overlayVerticesCellMapping[l][cellNumberOnLevel].Add(fromID);

                    // TO 
                    if (edges[i].ToVertex != -1)
                    {
                        cellNumberOnLevel = OverlayGraphUtilities.GetCellNumberOnLevel(l, vertices[to.OriginalVertex].CellNumber, overlayGraph.offset);
                        if (overlayVerticesCellMapping[l] == null)
                        {
                            overlayVerticesCellMapping[l] = new List<int>[overlayGraph.cellsPerLevel[l]];
                        }
                        if (overlayVerticesCellMapping[l][cellNumberOnLevel] == null)
                        {
                            overlayVerticesCellMapping[l][cellNumberOnLevel] = new List<int>();
                        }
                        overlayVerticesCellMapping[l][cellNumberOnLevel].Add(toID);
                    }
                }
            }
        }

        public void ConstructOverlayEdges()
        {
            for (var l = 1; l < overlayVerticesCellMapping.Length; l++)
            {
                if (overlayVerticesCellMapping[l] == null)
                    continue;
                if (overlayGraph.cellsPerLevel[l] == 0)
                    continue;

                constructLevel(l);
            }
        }

        public void constructLevel(int level)
        {
            Task[] tasks = new Task[overlayVerticesCellMapping[level].Length];

            for (var c = 0; c < overlayVerticesCellMapping[level].Length; c++)
            {
                if (level == 1)
                {
                    tasks[c] = Task.Factory.StartNew((object cell) =>
                    {
                        constructCellBase((int)cell);
                    }, c);
                }
                else
                {
                    tasks[c] = Task.Factory.StartNew((object cell) =>
                    {
                        constructCellOverlay(level, (int)cell);
                    }, c);
                }
            }

            Task.WaitAll(tasks);
        }

        public void constructCellOverlay(int level, int c)
        {
            var queue = new MinHeap<int, float>();
            var cell = overlayVerticesCellMapping[level][c];
            if (cell == null)
                return;

            var round = 0;
            var open = new Dictionary<int, float>();
            for (var i = 0; i < cell.Count; i++)
            {
                queue.Clear();
                open.Clear();

                var start = overlayVertices[cell[i]];
                start.OverlayEdges[level - 1] = new List<OverlayEdge>();
                start.OverlayEdges[level - 1].Add(new OverlayEdge()
                {
                    NeighborOverlayVertex = start.NeighborOverlayVertex,
                    Cost = edges[start.OriginalEdge].Cost
                });

                queue.Add(cell[i], 0);
                open[cell[i]] = round;
                while (queue.Count != 0)
                {
                    float currentCost = queue.PeekCost();
                    int currentOverlayVertex = queue.Remove();
                    var neighbors = overlayVertices[currentOverlayVertex].OverlayEdges[level - 2];
                    if (neighbors == null || neighbors.Count == 0)
                        continue;

                    for (var j = 0; j < neighbors.Count; j++)
                    {
                        var target = neighbors[j].NeighborOverlayVertex;
                        if (target == -1 || (int)OverlayGraphUtilities.GetCellNumberOnLevel(level, vertices[overlayVertices[target].OriginalVertex].CellNumber, overlayGraph.offset) != c)
                        {
                            bool wasFound = false;
                            for (var x = 0; x < start.OverlayEdges[level - 1].Count; x++)
                            {
                                if (start.OverlayEdges[level - 1][x].NeighborOverlayVertex == currentOverlayVertex)
                                {
                                    if (currentCost < start.OverlayEdges[0][x].Cost)
                                    {
                                        start.OverlayEdges[0][x] = new OverlayEdge
                                        {
                                            NeighborOverlayVertex = currentOverlayVertex,
                                            Cost = currentCost
                                        };
                                    }

                                    wasFound = true;
                                    break;
                                }
                            }

                            if (!wasFound)
                            {
                                start.OverlayEdges[level - 1].Add(new OverlayEdge()
                                {
                                    NeighborOverlayVertex = currentOverlayVertex,
                                    Cost = currentCost
                                });
                            }

                            continue;
                        }

                        var newCost = currentCost + neighbors[j].Cost;
                        var found = open.TryGetValue(target, out float oldCost);
                        if (!found)
                        {
                            queue.Add(target, newCost);
                            open[target] = newCost;
                        }
                        else if (newCost < oldCost)
                        {
                            queue.Update(target, newCost);
                            open[target] = newCost;
                        }
                    }
                }
            }
        }

        private void constructCellBase(int c)
        {
            var queue = new MinHeap<int, float>();
            var open = new Dictionary<int, float>();

            var cell = overlayVerticesCellMapping[1][c];
            if (cell == null)
                return;

            for (var i = 0; i < cell.Count; i++)
            {
                queue.Clear();
                open.Clear();

                var start = overlayVertices[cell[i]];
                start.OverlayEdges[0] = new List<OverlayEdge>();
                start.OverlayEdges[0].Add(new OverlayEdge()
                {
                   NeighborOverlayVertex = start.NeighborOverlayVertex,
                   Cost = edges[start.OriginalEdge].Cost
                });

                queue.Add(start.OriginalVertex, 0);
                open[start.OriginalVertex] = 0;
                while (queue.Count != 0)
                {
                    var currentCost = queue.PeekCost();
                    var current = queue.Remove();
                    var edgeEnd = (current + 1 == vertices.Count) ? vertexEdgeMapping.Count : vertices[current + 1].EdgeOffset;
                    for (var j = vertices[current].EdgeOffset; j < edgeEnd; j++)
                    {
                        var edge = edges[vertexEdgeMapping[j]];
                        var target = edge.FromVertex == current ? edge.ToVertex : edge.FromVertex;
                        if (target == -1 || (int)OverlayGraphUtilities.GetCellNumberOnLevel(1, vertices[target].CellNumber, overlayGraph.offset) != c)
                        {
                            var targetGridPosition = target == -1 ? edge.ToVertexGridPosition : vertices[target].GridPosition;
                            if (edgeToOverlayVertex.TryGetValue(((ulong)(uint)vertices[current].GridPosition << 32) | (uint)targetGridPosition, out int value))
                            {
                                bool wasFound = false;
                                for (var x = 0; x < start.OverlayEdges[0].Count; x++)
                                {
                                    if (start.OverlayEdges[0][x].NeighborOverlayVertex == value)
                                    {
                                        if (currentCost < start.OverlayEdges[0][x].Cost) {
                                            start.OverlayEdges[0][x] = new OverlayEdge
                                            {
                                                NeighborOverlayVertex = value,
                                                Cost = currentCost
                                            };
                                        }

                                        wasFound = true;
                                        break;
                                    }
                                }

                                if (!wasFound)
                                {
                                    start.OverlayEdges[0].Add(new OverlayEdge()
                                    {
                                        NeighborOverlayVertex = value,
                                        Cost = currentCost
                                    });
                                }
                            }

                            continue;
                        }

                        var newCost = currentCost + edge.Cost;
                        var found = open.TryGetValue(target, out float oldCost);
                        if (!found)
                        {
                            queue.Add(target, newCost);
                            open[target] = newCost;
                        }
                        else if (newCost < oldCost)
                        {
                            queue.Update(target, newCost);
                            open[target] = newCost;
                        }
                    }
                }
            }
        }

        public void DrawGraph()
        {
            for (var i = 0; i < vertices.Count; i++)
            {
                var vertex = vertices[i];
                var x = vertex.GridPosition / sizeY;
                var y = vertex.GridPosition % sizeY;

                DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.yellow);
                var edgeEnd = (i + 1 == vertices.Count) ? vertexEdgeMapping.Count : vertices[i + 1].EdgeOffset;
                if (edgeEnd == -1)
                    continue;

                for (var j = vertex.EdgeOffset; j < edgeEnd; j++)
                {
                    var edge = edges[vertexEdgeMapping[j]];
                    var target = edge.FromVertex == i ? edge.ToVertex : edge.FromVertex;
                    if (target == -1)
                        continue;

                    var tx = vertices[target].GridPosition / sizeY;
                    var ty = vertices[target].GridPosition % sizeY;
                    DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), Color.yellow);
                }
            }
        }


        public void DrawOverlayGraph(int level)
        {
            for (var i = 0; i < overlayVerticesCellMapping[level].Length; i++)
            {
                for (var j = 0; j < overlayVerticesCellMapping[level][i].Count; j++)
                {
                    var overlayVertex = overlayVertices[overlayVerticesCellMapping[level][i][j]];
                    var vertex = vertices[overlayVertex.OriginalVertex];
                    var x = vertex.GridPosition / sizeY;
                    var y = vertex.GridPosition % sizeY;
                    DebugDrawer.DrawCube(new UnityEngine.Vector2Int(x, y), Vector2Int.one, Color.red);

                    for (var n = 0; n < overlayVertex.OverlayEdges[level - 1].Count; n++)
                    {
                        var target = overlayVertex.OverlayEdges[level - 1][n].NeighborOverlayVertex;
                        if (target == -1)
                            continue;

                        var tx = vertices[overlayVertices[target].OriginalVertex].GridPosition / sizeY;
                        var ty = vertices[overlayVertices[target].OriginalVertex].GridPosition % sizeY;
                        DebugDrawer.Draw(new Vector2Int(x, y), new Vector2Int(tx, ty), new Color(1, 1, 1));
                    }
                }
            }
        }
    }
}
