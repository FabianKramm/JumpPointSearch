﻿using Pathfinding;
using System.Collections.Generic;
using UnityEngine;

namespace ChunkedPathFinding
{
    public class GraphPathfinder
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

            public void setOpenA() { _flags |= 1; }
            public void setOpenB() { _flags |= 2; }

            public bool isOpenA() { return (_flags & 1) == 1; }
            public bool isOpenB() { return (_flags & 2) == 2; }
        }

        public Graph graph;
        public int expandedNodes = 0;

        private ulong startCellNumber;
        private ulong targetCellNumber;

        private float shortestPath;
        private GraphNode middleNode;
        
        private Dictionary<int, GraphNode> nodeLookup;

        private MinHeap<GraphNode, float> forwardQueueGraph;
        private MinHeap<GraphNode, float> backwardQueueGraph;


        public GraphPathfinder(Graph graph)
        {
            this.graph = graph;
        }

        public List<GraphNode> BidirectionalDijkstra(int startX, int startY, int targetX, int targetY)
        {
            nodeLookup = new Dictionary<int, GraphNode>();

            forwardQueueGraph = new MinHeap<GraphNode, float>();
            backwardQueueGraph = new MinHeap<GraphNode, float>();

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
            while (forwardQueueGraph.Count > 0 && backwardQueueGraph.Count > 0)
            {
                ticks++;

                var minForwardGraph = forwardQueueGraph.Count > 0 ? forwardQueueGraph.PeekCost() : float.PositiveInfinity;
                var minBackwardGraph = backwardQueueGraph.Count > 0 ? backwardQueueGraph.PeekCost() : float.PositiveInfinity;

                // Are we done?
                if (shortestPath < minForwardGraph + minBackwardGraph)
                    break;

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
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Length) ? chunk.vertexEdgeMapping.Length : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[chunk.vertexEdgeMapping[i]];
                var edgeTarget = edge.FromVertex == min.VertexID ? edge.ToVertex : edge.FromVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? graph.GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graph.GetChunk(edgeTargetChunkID);
                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

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
        }

        private void searchBackwardBasicGraph()
        {
            var min = backwardQueueGraph.Remove();
            var chunk = graph.GetChunk(min.ChunkID);
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Length) ? chunk.vertexEdgeMapping.Length : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[chunk.vertexEdgeMapping[i]];
                var edgeTarget = edge.FromVertex == min.VertexID ? edge.ToVertex : edge.FromVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? graph.GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graph.GetChunk(edgeTargetChunkID);
                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

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

        private GraphNode getNodeGraph(int gridPosition, int vertexID, int chunkID)
        {
            if (nodeLookup.TryGetValue(gridPosition, out GraphNode targetNeighborNode) == false)
            {
                targetNeighborNode = new GraphNode
                {
                    VertexID = vertexID,
                    ChunkID = chunkID,
                };

                expandedNodes++;
                nodeLookup[gridPosition] = targetNeighborNode;
            }

            return targetNeighborNode;
        }
    }
}