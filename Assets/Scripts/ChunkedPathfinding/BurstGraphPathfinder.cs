using Pathfinding;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace ChunkedPathfinding
{
    [BurstCompile]
    public struct BurstGraphPathfinder : IJob
    {
        public struct GraphChunk
        {
            public int chunkNumber;
            public int isLoaded; // 0 = false, 1 = true

            [NativeDisableContainerSafetyRestriction]
            public NativeNestedArray<Vertex> vertices;
            [NativeDisableContainerSafetyRestriction]
            public NativeNestedArray<Edge> edges;
            [NativeDisableContainerSafetyRestriction]
            public NativeHashMap<int, int> gridPositionToVertex;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct GraphNode
        {
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

        public int expandedNodes;

        private float shortestPath;
        private GraphNode middleNode;
        
        [NativeDisableContainerSafetyRestriction]
        private NativeNestedArray<GraphChunk> graphChunks;

        private NativeHashMap<int, GraphNode> parentsA;
        private NativeHashMap<int, GraphNode> parentsB;
        private NativeHashMap<int, GraphNode> nodeLookup;

        private NativeMinHeap<GraphNode, float> forwardQueueGraph;
        private NativeMinHeap<GraphNode, float> backwardQueueGraph;

        private int sizeY;
        private int chunkSize;
        private int chunkSizeY;

        private NativeList<GraphNode> result;

        public BurstGraphPathfinder(NativeNestedArray<GraphChunk> graphChunks, NativeList<GraphNode> result, GraphNode[] startNodes, GraphNode[] endNodes, int chunkSize, int chunkSizeY, int sizeY)
        {
            forwardQueueGraph = new NativeMinHeap<GraphNode, float>(128, Allocator.TempJob);
            backwardQueueGraph = new NativeMinHeap<GraphNode, float>(128, Allocator.TempJob);

            nodeLookup = new NativeHashMap<int, GraphNode>(128, Allocator.TempJob);
            parentsA = new NativeHashMap<int, GraphNode>(128, Allocator.TempJob);
            parentsB = new NativeHashMap<int, GraphNode>(128, Allocator.TempJob);

            this.graphChunks = graphChunks;
            this.result = result;
            this.chunkSize = chunkSize;
            this.chunkSizeY = chunkSizeY;
            this.sizeY = sizeY;

            expandedNodes = 0;
            shortestPath = float.PositiveInfinity;
            middleNode = new GraphNode { VertexID = -1 };

            for (var i = 0; i < startNodes.Length; i++)
            {
                var startNode = startNodes[i];
                startNode.setOpenA();
                var gridPos = graphChunks[startNode.ChunkID].vertices[startNode.VertexID].GridPosition;

                forwardQueueGraph.Add(startNode, startNode.CostA);
                nodeLookup[gridPos] = startNode;
            }

            for (var i = 0; i < endNodes.Length; i++)
            {
                var endNode = endNodes[i];
                endNode.setOpenB();
                var gridPos = graphChunks[endNode.ChunkID].vertices[endNode.VertexID].GridPosition;

                forwardQueueGraph.Add(endNode, endNode.CostB);
                nodeLookup[gridPos] = endNode;
            }
        }

        public void Search(int maxTicks = 25000)
        {
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

            if (middleNode.VertexID == -1)
                return;

            tracebackPath(ref middleNode);
        }

        public void tracebackPath(ref GraphNode touch)
        {
            GraphNode current = touch;
            while (current.VertexID != -1)
            {
                result.Add(current);
                if (parentsA.TryGetValue(graphChunks[current.ChunkID].vertices[current.VertexID].GridPosition, out GraphNode parent))
                {
                    current = parent;
                }
                else
                {
                    current = new GraphNode
                    {
                        VertexID = -1
                    };
                }
            }

            // Reverse list
            for (var i = 0; i < result.Length / 2; i++)
            {
                var t = result[i];
                result[i] = result[result.Length - 1 - i];
                result[result.Length - 1 - i] = t;
            }
            
            if (parentsB.ContainsKey(graphChunks[current.ChunkID].vertices[current.VertexID].GridPosition))
            {
                current = parentsB[graphChunks[current.ChunkID].vertices[current.VertexID].GridPosition];
                while (current.VertexID != -1)
                {
                    result.Add(current);
                    if (parentsB.TryGetValue(graphChunks[current.ChunkID].vertices[current.VertexID].GridPosition, out GraphNode parent))
                    {
                        current = parent;
                    }
                    else
                    {
                        current = new GraphNode
                        {
                            VertexID = -1
                        };
                    }
                }
            }
        }

        private void searchForwardBasicGraph()
        {
            var min = forwardQueueGraph.Remove();
            var chunk = graphChunks[min.ChunkID];
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Length) ? chunk.edges.Length : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[i];
                var edgeTarget = edge.ToVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graphChunks[edgeTargetChunkID];
                if (edgeTargetChunk.isLoaded == 0)
                    continue;

                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

                // Get the graph node
                var node = getNodeGraph(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition, edgeTargetVertexID, edgeTargetChunkID);

                // The new distance of this edge
                var newDist = min.CostA + edge.Cost;
                if (node.isOpenA() == false || newDist < node.CostA)
                {
                    parentsA[edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition] = min;
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

                    nodeLookup[edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition] = node;

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
            var chunk = graphChunks[min.ChunkID];
            var edgeEnd = (min.VertexID + 1 == chunk.vertices.Length) ? chunk.edges.Length : chunk.vertices[min.VertexID + 1].EdgeOffset;
            for (var i = chunk.vertices[min.VertexID].EdgeOffset; i < edgeEnd; i++)
            {
                var edge = chunk.edges[i];
                var edgeTarget = edge.ToVertex;
                var edgeTargetChunkID = edgeTarget == -1 ? GetChunkID(edge.ToVertexGridPosition) : chunk.chunkNumber;
                var edgeTargetChunk = graphChunks[edgeTargetChunkID];
                if (edgeTargetChunk.isLoaded == 0)
                    continue;

                var edgeTargetVertexID = edgeTarget == -1 ? edgeTargetChunk.gridPositionToVertex[edge.ToVertexGridPosition] : edgeTarget;

                // Get the graph node
                var node = getNodeGraph(edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition, edgeTargetVertexID, edgeTargetChunkID);

                // The new distance of this edge
                var newDist = min.CostB + edge.Cost;
                if (node.isOpenB() == false || newDist < node.CostB)
                {
                    parentsB[edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition] = min;
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

                    nodeLookup[edgeTargetChunk.vertices[edgeTargetVertexID].GridPosition] = node;

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

        public int GetChunkID(int gridPosition)
        {
            return ((gridPosition / sizeY) / chunkSize) * chunkSizeY + ((gridPosition % sizeY) / chunkSize);
        }

        public void Execute()
        {
            Search();
        }
    }
}
