﻿using Pathfinding;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MultiLevelPathfinding
{
    public class OverlayGraphPathfinder
    {
        public class GraphNode
        {
            public GraphNode ParentA;
            public GraphNode ParentB;

            public int VertexID;
            public int ChunkID;
            public float CostA;
            public float CostB;
            public uint _flags;
            public int QueryLevel;

            public void setOpenA() { _flags |= 1; }
            public void setOpenB() { _flags |= 2; }

            public bool isOpenA() { return (_flags & 1) == 1; }
            public bool isOpenB() { return (_flags & 2) == 2; }
        }

        public OverlayGraph graph;


        public int expandedNodes = 0;

        private ulong startCellNumber;
        private ulong targetCellNumber;

        private float shortestPath;
        private GraphNode middleNode;

        private Dictionary<ulong, GraphNode> overlayNodeLookup;
        private Dictionary<int, GraphNode> nodeLookup;

        private MinHeap<GraphNode, float> forwardQueueGraph;
        private MinHeap<GraphNode, float> forwardQueueOverlay;

        private MinHeap<GraphNode, float> backwardQueueGraph;
        private MinHeap<GraphNode, float> backwardQueueOverlay;


        public OverlayGraphPathfinder(OverlayGraph graph)
        {
            this.graph = graph;
        }

        public List<GraphNode> BidirectionalDijkstra(int startX, int startY, int targetX, int targetY)
        {
            startCellNumber = OverlayGraphUtilities.GetCellNumber(startX, startY, graph.sizeY, graph.offset);
            targetCellNumber = OverlayGraphUtilities.GetCellNumber(targetX, targetY, graph.sizeY, graph.offset);

            overlayNodeLookup = new Dictionary<ulong, GraphNode>();
            nodeLookup = new Dictionary<int, GraphNode>();

            forwardQueueGraph = new MinHeap<GraphNode, float>();
            forwardQueueOverlay = new MinHeap<GraphNode, float>();

            backwardQueueGraph = new MinHeap<GraphNode, float>();
            backwardQueueOverlay = new MinHeap<GraphNode, float>();

            var startNodeChunkID = graph.GetChunkID(startX, startY);
            var startNode = new GraphNode()
            {
                VertexID = graph.GetVertexID(startX, startY, startNodeChunkID),
                ChunkID = startNodeChunkID,
            };

            forwardQueueGraph.Add(startNode, 0);

            var targetNodeChunkID = graph.GetChunkID(targetX, targetY);
            var targetNode = new GraphNode()
            {
                VertexID = graph.GetVertexID(targetX, targetY, targetNodeChunkID),
                ChunkID = targetNodeChunkID,
            };

            backwardQueueGraph.Add(targetNode, 0);

            startNode.setOpenA();
            targetNode.setOpenB();

            middleNode = null; 
            shortestPath = float.PositiveInfinity;

            var ticks = 0;
            while (forwardQueueGraph.Count + forwardQueueOverlay.Count > 0 && backwardQueueGraph.Count + backwardQueueOverlay.Count > 0)
            {
                ticks++;

                var minForwardGraph = forwardQueueGraph.Count > 0 ? forwardQueueGraph.PeekCost() : float.PositiveInfinity;
                var minForwardOverlay = forwardQueueOverlay.Count > 0 ? forwardQueueOverlay.PeekCost() : float.PositiveInfinity;

                var minBackwardGraph = backwardQueueGraph.Count > 0 ? backwardQueueGraph.PeekCost() : float.PositiveInfinity;
                var minBackwardOverlay = backwardQueueOverlay.Count > 0 ? backwardQueueOverlay.PeekCost() : float.PositiveInfinity;

                // Are we done?
                if (shortestPath < Math.Min(minForwardGraph, minForwardOverlay) + Math.Min(minBackwardGraph, minBackwardOverlay))
                    break;

                // Search on Graph
                if (Math.Min(minForwardGraph, minBackwardGraph) < Math.Min(minForwardOverlay, minBackwardOverlay))
                {
                    // Search Forward
                    if (minForwardGraph < minBackwardGraph)
                    {
                        searchForwardBasicGraph();
                    }
                    // Search Backward
                    else
                    {
                        searchBackwardBasicGraph();
                    }
                }
                // Search on Overlay Graph
                else
                {
                    // Search Forward
                    if (minForwardOverlay < minBackwardOverlay)
                    {
                        searchForwardOverlayGraph();
                    }
                    // Search Backward
                    else
                    {
                        searchBackwardOverlayGraph();
                    }
                }
            }

            Debug.Log("Found path in " + ticks + " ticks and " + expandedNodes + " expanded nodes");

            if (middleNode == null)
                return null;

            return tracebackPath(middleNode);
        }

        public List<GraphNode> tracebackPath(GraphNode touch)
        {
            GraphNode current = touch;
            List<GraphNode> path = new List<GraphNode>();
            while (current != null)
            {
                path.Add(current);
                current = current.ParentA;
            }

            path.Reverse();
            if (touch.ParentB != null)
            {
                current = touch.ParentB;
                while (current != null)
                {
                    path.Add(current);
                    current = current.ParentB;
                }
            }

            return path;
        }

        private void searchForwardBasicGraph()
        {
            var min = forwardQueueGraph.Remove();
            var chunk = graph.GetChunk(min.ChunkID);
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Count) ? chunk.vertexEdgeMapping.Count : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[chunk.vertexEdgeMapping[i]];
                var edgeTarget = edge.FromVertex == min.VertexID ? edge.ToVertex : edge.FromVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? graph.GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graph.GetChunk(edgeTargetChunkID);
                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

                var queryLevel = OverlayGraphUtilities.GetQueryLevel(startCellNumber, targetCellNumber, edgeTargetChunk.vertices[edgeTargetVertexID].CellNumber, graph.offset);
                // We stay on the graph
                if (queryLevel == 0)
                {
                    // Get the graph node
                    var node = getNodeGraph(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition, edgeTargetVertexID, edgeTargetChunkID);

                    // The new distance of this edge
                    var newDist = min.CostA + edge.Cost;
                    if (node.isOpenA() == false || newDist < node.CostA)
                    {
                        node.ParentA = min;
                        node.CostA = newDist;
                        if (node.isOpenA() == false)
                        {
                            node.setOpenA();
                            forwardQueueGraph.Add(node, newDist);
                        }
                        else
                        {
                            forwardQueueGraph.Update(node, newDist);
                        }

                        if (node.isOpenB())
                        {
                            var newPathLength = node.CostA + node.CostB;
                            if (newPathLength < shortestPath)
                            {
                                shortestPath = newPathLength;
                                middleNode = node;
                            }
                        }
                    }
                }
                // We go to the overlay graph
                else
                {
                    var overlayID = ((ulong)(uint)(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition) << 32) | (uint)chunk.vertices[min.VertexID].GridPosition;
                    if (edgeTargetChunk.edgeToOverlayVertex.TryGetValue(overlayID, out int overlayVertex) == false)
                        continue;

                    // Get the overlay node
                    var node = getNodeOverlay(overlayVertex, edgeTargetChunkID, queryLevel);

                    // The new distance of this edge
                    var newDist = min.CostA + edge.Cost;
                    if (node.isOpenA() == false || newDist < node.CostA)
                    {
                        node.ParentA = min;
                        node.CostA = newDist;
                        if (node.isOpenA() == false)
                        {
                            node.setOpenA();
                            forwardQueueOverlay.Add(node, newDist);
                        }
                        else
                        {
                            forwardQueueOverlay.Update(node, newDist);
                        }

                        if (node.isOpenB())
                        {
                            var newPathLength = node.CostA + node.CostB;
                            if (newPathLength < shortestPath)
                            {
                                shortestPath = newPathLength;
                                middleNode = node;
                            }
                        }
                    }
                }
            }
        }

        private void searchBackwardBasicGraph()
        {
            var min = backwardQueueGraph.Remove();
            var chunk = graph.GetChunk(min.ChunkID);
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Count) ? chunk.vertexEdgeMapping.Count : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[chunk.vertexEdgeMapping[i]];
                var edgeTarget = edge.FromVertex == min.VertexID ? edge.ToVertex : edge.FromVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? graph.GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graph.GetChunk(edgeTargetChunkID);
                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

                var queryLevel = OverlayGraphUtilities.GetQueryLevel(startCellNumber, targetCellNumber, edgeTargetChunk.vertices[edgeTargetVertexID].CellNumber, graph.offset);
                // We stay on the graph
                if (queryLevel == 0)
                {
                    // Get the graph node
                    var node = getNodeGraph(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition, edgeTargetVertexID, edgeTargetChunkID);

                    // The new distance of this edge
                    var newDist = min.CostB + edge.Cost;
                    if (node.isOpenB() == false || newDist < node.CostB)
                    {
                        node.ParentB = min;
                        node.CostB = newDist;
                        if (node.isOpenB() == false)
                        {
                            node.setOpenB();
                            backwardQueueGraph.Add(node, newDist);
                        }
                        else
                        {
                            backwardQueueGraph.Update(node, newDist);
                        }

                        if (node.isOpenA())
                        {
                            var newPathLength = node.CostA + node.CostB;
                            if (newPathLength < shortestPath)
                            {
                                shortestPath = newPathLength;
                                middleNode = node;
                            }
                        }
                    }
                }
                // We go to the overlay graph
                else
                {
                    var overlayID = ((ulong)(uint)(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition) << 32) | (uint)chunk.vertices[min.VertexID].GridPosition;
                    if (edgeTargetChunk.edgeToOverlayVertex.TryGetValue(overlayID, out int overlayVertex) == false)
                        continue;

                    // Get the overlay node
                    var node = getNodeOverlay(overlayVertex, edgeTargetChunkID, queryLevel);

                    // The new distance of this edge
                    var newDist = min.CostB + edge.Cost;
                    if (node.isOpenB() == false || newDist < node.CostB)
                    {
                        node.ParentB = min;
                        node.CostB = newDist;
                        if (node.isOpenB() == false)
                        {
                            node.setOpenB();
                            backwardQueueOverlay.Add(node, newDist);
                        }
                        else
                        {
                            backwardQueueOverlay.Update(node, newDist);
                        }

                        if (node.isOpenA())
                        {
                            var newPathLength = node.CostA + node.CostB;
                            if (newPathLength < shortestPath)
                            {
                                shortestPath = newPathLength;
                                middleNode = node;
                            }
                        }
                    }
                }
            }
        }

        private void searchForwardOverlayGraph()
        {
            var min = forwardQueueOverlay.Remove();
            var chunk = graph.GetChunk(min.ChunkID);
            var overlayVertex = chunk.overlayVertices[min.VertexID];

            for (var i = 0; i < overlayVertex.OverlayEdges[min.QueryLevel - 1].Count; i++)
            {
                var overlayEdge = overlayVertex.OverlayEdges[min.QueryLevel - 1][i];
                int neighborOverlayVertexID;
                OverlayGraphChunk neighborChunk;
                if (overlayEdge.NeighborOverlayVertex == -1)
                {
                    neighborChunk = graph.GetChunk(graph.GetChunkID(chunk.edges[overlayVertex.OriginalEdge].ToVertexGridPosition));
                    var overlayNodeID = (ulong)(uint)chunk.edges[overlayVertex.OriginalEdge].ToVertexGridPosition << 32 | (uint)chunk.vertices[overlayVertex.OriginalVertex].GridPosition;
                    if (neighborChunk.edgeToOverlayVertex.TryGetValue(overlayNodeID, out neighborOverlayVertexID) == false)
                    {
                        continue;
                    }
                }
                else
                {
                    neighborOverlayVertexID = overlayEdge.NeighborOverlayVertex;
                    neighborChunk = chunk;
                }

                // Get the overlay node
                var node = getNodeOverlay(neighborOverlayVertexID, neighborChunk.chunkNumber, min.QueryLevel);
                var newDist = min.CostA + overlayEdge.Cost;
                if (node.isOpenA() == false || newDist < node.CostA)
                {
                    node.CostA = newDist;
                    node.ParentA = min;
                    node.setOpenA();

                    // Did we find the same node as the backward search?
                    if (node.isOpenB() && node.CostB + node.CostA < shortestPath)
                    {
                        shortestPath = node.CostB + node.CostA;
                        middleNode = node;
                    }

                    // Traverse the overlay node to the next cell
                    var neighborOverlayVertex = neighborChunk.overlayVertices[neighborOverlayVertexID];
                    int targetNeighborOverlayVertexID;
                    OverlayGraphChunk targetNeighborChunk;
                    if (neighborOverlayVertex.NeighborOverlayVertex == -1)
                    {
                        targetNeighborChunk = graph.GetChunk(graph.GetChunkID(neighborChunk.edges[neighborOverlayVertex.OriginalEdge].ToVertexGridPosition));
                        var targetNeighborNodeID = ((ulong)(uint)neighborChunk.edges[neighborOverlayVertex.OriginalEdge].ToVertexGridPosition) << 32 | (uint)neighborChunk.vertices[neighborOverlayVertex.OriginalVertex].GridPosition;
                        if (targetNeighborChunk.edgeToOverlayVertex.TryGetValue(targetNeighborNodeID, out targetNeighborOverlayVertexID) == false)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        targetNeighborOverlayVertexID = neighborOverlayVertex.NeighborOverlayVertex;
                        targetNeighborChunk = neighborChunk;
                    }

                    // Get the overlay node
                    var targetNeighborNode = getNodeOverlay(targetNeighborOverlayVertexID, targetNeighborChunk.chunkNumber, min.QueryLevel);
                    newDist = node.CostA + neighborChunk.edges[neighborOverlayVertex.OriginalEdge].Cost;
                    var queryLevel = OverlayGraphUtilities.GetQueryLevel(startCellNumber, targetCellNumber, targetNeighborChunk.vertices[targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex].CellNumber, graph.offset);
                    // We are back on the base graph
                    if (queryLevel == 0)
                    {
                        var originalVertex = targetNeighborChunk.vertices[targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex];
                        var originalNode = getNodeGraph(originalVertex.GridPosition, targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex, targetNeighborChunk.chunkNumber);

                        if (originalNode.isOpenA() == false || newDist < originalNode.CostA)
                        {
                            originalNode.ParentA = node;
                            originalNode.CostA = newDist;
                            if (originalNode.isOpenA() == false)
                            {
                                originalNode.setOpenA();
                                forwardQueueGraph.Add(originalNode, newDist);
                            }
                            else
                            {
                                forwardQueueGraph.Update(originalNode, newDist);
                            }

                            if (originalNode.isOpenB())
                            {
                                var newPathLength = originalNode.CostA + originalNode.CostB;
                                if (newPathLength < shortestPath)
                                {
                                    shortestPath = newPathLength;
                                    middleNode = originalNode;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (targetNeighborNode.isOpenA() == false || newDist < targetNeighborNode.CostA)
                        {
                            targetNeighborNode.CostA = newDist;
                            targetNeighborNode.ParentA = node;
                            targetNeighborNode.QueryLevel = queryLevel;
                            if (targetNeighborNode.isOpenA() == false)
                            {
                                targetNeighborNode.setOpenA();
                                forwardQueueOverlay.Add(targetNeighborNode, newDist);
                            }
                            else
                            {
                                forwardQueueOverlay.Update(targetNeighborNode, newDist);
                            }

                            if (targetNeighborNode.isOpenB() && targetNeighborNode.CostA + targetNeighborNode.CostB < shortestPath)
                            {
                                shortestPath = targetNeighborNode.CostA + targetNeighborNode.CostB;
                                middleNode = targetNeighborNode;
                            }
                        }
                    }
                }
            }
        }


        private void searchBackwardOverlayGraph()
        {
            var min = backwardQueueOverlay.Remove();
            var chunk = graph.GetChunk(min.ChunkID);
            var overlayVertex = chunk.overlayVertices[min.VertexID];
            for (var i = 0; i < overlayVertex.OverlayEdges[min.QueryLevel - 1].Count; i++)
            {

                var overlayEdge = overlayVertex.OverlayEdges[min.QueryLevel - 1][i];
                int neighborOverlayVertexID;
                OverlayGraphChunk neighborChunk;
                if (overlayEdge.NeighborOverlayVertex == -1)
                {
                    neighborChunk = graph.GetChunk(graph.GetChunkID(chunk.edges[overlayVertex.OriginalEdge].ToVertexGridPosition));
                    var overlayNodeID = (ulong)(uint)chunk.edges[overlayVertex.OriginalEdge].ToVertexGridPosition << 32 | (uint)chunk.vertices[overlayVertex.OriginalVertex].GridPosition;
                    if (neighborChunk.edgeToOverlayVertex.TryGetValue(overlayNodeID, out neighborOverlayVertexID) == false)
                    {
                        continue;
                    }
                }
                else
                {
                    neighborOverlayVertexID = overlayEdge.NeighborOverlayVertex;
                    neighborChunk = chunk;
                }

                // Get the overlay node
                var node = getNodeOverlay(neighborOverlayVertexID, neighborChunk.chunkNumber, min.QueryLevel);
                var newDist = min.CostB + overlayEdge.Cost;
                if (node.isOpenB() == false || newDist < node.CostB)
                {
                    node.CostB = newDist;
                    node.ParentB = min;
                    node.setOpenB();

                    // Did we find the same node as the backward search?
                    if (node.isOpenA() && node.CostB + node.CostA < shortestPath)
                    {
                        shortestPath = node.CostB + node.CostA;
                        middleNode = node;
                    }

                    // Traverse the overlay node to the next cell
                    var neighborOverlayVertex = neighborChunk.overlayVertices[neighborOverlayVertexID];
                    ulong targetNeighborNodeID;
                    int targetNeighborOverlayVertexID;
                    OverlayGraphChunk targetNeighborChunk;
                    if (neighborOverlayVertex.NeighborOverlayVertex == -1)
                    {
                        targetNeighborChunk = graph.GetChunk(graph.GetChunkID(neighborChunk.edges[neighborOverlayVertex.OriginalEdge].ToVertexGridPosition));
                        targetNeighborNodeID = (ulong)(uint)neighborChunk.edges[neighborOverlayVertex.OriginalEdge].ToVertexGridPosition << 32 | (uint)neighborChunk.vertices[neighborOverlayVertex.OriginalVertex].GridPosition;
                        if (targetNeighborChunk.edgeToOverlayVertex.TryGetValue(targetNeighborNodeID, out targetNeighborOverlayVertexID) == false)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        targetNeighborOverlayVertexID = neighborOverlayVertex.NeighborOverlayVertex;
                        targetNeighborChunk = neighborChunk;
                        targetNeighborNodeID = (ulong)(uint)neighborChunk.vertices[neighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex].GridPosition << 32 | (uint)neighborChunk.vertices[neighborOverlayVertex.OriginalVertex].GridPosition;
                    }

                    // Get the overlay node
                    var targetNeighborNode = getNodeOverlay(targetNeighborOverlayVertexID, targetNeighborChunk.chunkNumber, min.QueryLevel);
                    newDist = node.CostB + neighborChunk.edges[neighborOverlayVertex.OriginalEdge].Cost;
                    var queryLevel = OverlayGraphUtilities.GetQueryLevel(startCellNumber, targetCellNumber, targetNeighborChunk.vertices[targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex].CellNumber, graph.offset);
                    // We are back on the base graph
                    if (queryLevel == 0)
                    {
                        var originalVertex = targetNeighborChunk.vertices[targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex];
                        var originalNode = getNodeGraph(originalVertex.GridPosition, targetNeighborChunk.overlayVertices[targetNeighborOverlayVertexID].OriginalVertex, targetNeighborChunk.chunkNumber);

                        if (originalNode.isOpenB() == false || newDist < originalNode.CostB)
                        {
                            originalNode.ParentB = node;
                            originalNode.CostB = newDist;
                            if (originalNode.isOpenB() == false)
                            {
                                originalNode.setOpenB();
                                backwardQueueGraph.Add(originalNode, newDist);
                            }
                            else
                            {
                                backwardQueueGraph.Update(originalNode, newDist);
                            }

                            if (originalNode.isOpenA())
                            {
                                var newPathLength = originalNode.CostA + originalNode.CostB;
                                if (newPathLength < shortestPath)
                                {
                                    shortestPath = newPathLength;
                                    middleNode = originalNode;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (targetNeighborNode.isOpenB() == false || newDist < targetNeighborNode.CostB)
                        {
                            targetNeighborNode.CostB = newDist;
                            targetNeighborNode.ParentB = node;
                            targetNeighborNode.QueryLevel = queryLevel;
                            if (targetNeighborNode.isOpenB() == false)
                            {
                                targetNeighborNode.setOpenB();
                                backwardQueueOverlay.Add(targetNeighborNode, newDist);
                            }
                            else
                            {
                                backwardQueueOverlay.Update(targetNeighborNode, newDist);
                            }

                            if (targetNeighborNode.isOpenA() && targetNeighborNode.CostA + targetNeighborNode.CostB < shortestPath)
                            {
                                shortestPath = targetNeighborNode.CostA + targetNeighborNode.CostB;
                                middleNode = targetNeighborNode;
                            }
                        }
                    }
                }
            }
        }

        private void drawEdgesVertex(int vertexID, int chunkID)
        {
            var targetNeighborChunk = graph.GetChunk(chunkID);
            var targetVertex = targetNeighborChunk.vertices[vertexID];
            var (tx, ty) = fromGridPosition(targetVertex.GridPosition);
            for (var k = targetVertex.EdgeOffset; k < targetNeighborChunk.vertices[vertexID + 1].EdgeOffset; k++)
            {
                var edge = targetNeighborChunk.edges[targetNeighborChunk.vertexEdgeMapping[k]];
                var edgeTarget = edge.FromVertex == vertexID ? edge.ToVertex : edge.FromVertex;
                var (hx, hy) = fromGridPosition(edgeTarget == -1 ? edge.ToVertexGridPosition : targetNeighborChunk.vertices[edgeTarget].GridPosition);
                DebugDrawer.Draw(new Vector2Int(tx, ty), new Vector2Int(hx, hy), Color.white);
            }
        }

        private (int, int) fromGridPosition(int gridPosition)
        {
            return (gridPosition / graph.sizeY, gridPosition % graph.sizeY);
        }

        private GraphNode getNodeOverlay(int vertexID, int chunkID, int queryLevel)
        {
            var dictID = ((ulong)(uint)vertexID << 32) | (uint)chunkID;
            if (overlayNodeLookup.TryGetValue(dictID, out GraphNode targetNeighborNode) == false)
            {
                targetNeighborNode = new GraphNode
                {
                    VertexID = vertexID,
                    ChunkID = chunkID,
                    CostA = float.PositiveInfinity,
                    CostB = float.PositiveInfinity,
                    QueryLevel = queryLevel,
                };

                expandedNodes++;
                overlayNodeLookup[dictID] = targetNeighborNode;
            }

            return targetNeighborNode;
        }

        private GraphNode getNodeGraph(int gridPosition, int vertexID, int chunkID)
        {
            if (nodeLookup.TryGetValue(gridPosition, out GraphNode targetNeighborNode) == false)
            {
                targetNeighborNode = new GraphNode
                {
                    VertexID = vertexID,
                    ChunkID = chunkID,
                    CostA = float.PositiveInfinity,
                    CostB = float.PositiveInfinity,
                    QueryLevel = 0,
                };

                expandedNodes++;
                nodeLookup[gridPosition] = targetNeighborNode;
            }

            return targetNeighborNode;
        }
    }
}
