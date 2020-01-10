#define DEBUG_PATHFINDING

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;

public class FindPath
{
    public const float DiagonalCost = 1.4142135623730950488016887242097f; // sqrt(2)
    public const float LateralCost = 1.0f;

    private Node _startNode;
    private Node _targetNode;

    private GridGraph _grid;

    private MinHeap<Node, float> openSet;
    private HashSet<Node> openSetContainer;
    private HashSet<Node> closedSet;
    private bool _forced;

    public List<Node> GetPath(GridGraph grid, Node startNode, Node targetNode)
    {
        _startNode = startNode;
        _targetNode = targetNode;
        _grid = grid;

        _Initialize();
        Stopwatch sw = new Stopwatch();
        sw.Start();
        if (_CalculateShortestPath())
        {
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            UnityEngine.Debug.Log("Jump Point Path - Path found in : " + ts.Milliseconds + " ms");
            //EventHandler.Instance.Broadcast(new PathTimerEvent(ts.Milliseconds));
            return _RetracePath();
        }
        else
        {
            sw.Stop();
            TimeSpan ts = sw.Elapsed;
            UnityEngine.Debug.Log("Jump Point Path - No path found in : " + ts.Milliseconds + " ms");
            //EventHandler.Instance.Broadcast(new PathTimerEvent(ts.Milliseconds));
            return null;
        }
    }

    private void _Initialize()
    {
        openSet = new MinHeap<Node, float>();
        openSetContainer = new HashSet<Node>();
        closedSet = new HashSet<Node>();
    }

    private bool _CalculateShortestPath()
    {
        Node currentNode;

        openSet.Add(_startNode, 0);
        openSetContainer.Add(_startNode);

        while (openSet.Count > 0)
        {
            currentNode = openSet.Remove();
            if (currentNode == _targetNode)
                return true;
            if (currentNode == null || closedSet.Contains(currentNode))
                continue;

            openSetContainer.Remove(currentNode);
            closedSet.Add(currentNode);
            List<Node> Nodes = _GetSuccessors(currentNode);
            foreach (Node node in Nodes)
            {
                if (closedSet.Contains(node))
                    continue;

#if DEBUG_PATHFINDING
                DebugDrawer.Draw(new Vector2Int(currentNode.x, currentNode.y), new Vector2Int(node.x, node.y), Color.white);
                DebugDrawer.DrawCube(new Vector2Int(node.x, node.y), Vector2Int.one, Color.white);
#endif

                int newGCost = currentNode.gCost + _GetDistance(currentNode, node);
                if (newGCost < node.gCost || !openSetContainer.Contains(node))
                {
                    node.gCost = newGCost;
                    node.hCost = _GetDistance(node, _targetNode);
                    node.parent = currentNode;

                    openSet.Add(node, node.fCost);
                    if (!openSetContainer.Contains(node))
                    {
                        openSetContainer.Add(node);
                    }
                }
            }
        }
        return false;
    }

    private List<Node> _RetracePath()
    {
        List<Node> path = new List<Node>();
        Node currentNode = _targetNode;
        while (currentNode != _startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        path.Reverse();
        return path;
    }

    private List<Node> _GetSuccessors(Node currentNode)
    {
        Node jumpNode;
        List<Node> successors = new List<Node>();
        List<Node> neighbours = _grid.GetNeighbours(currentNode);
        foreach (Node neighbour in neighbours)
        {
            int xDirection = neighbour.x - currentNode.x;
            int yDirection = neighbour.y - currentNode.y;

            jumpNode = _Jump(neighbour.x, neighbour.y, xDirection, yDirection);
            if (jumpNode != null)
                successors.Add(jumpNode);
        }

        return successors;
    }

    private Node _Jump(int posX, int posY, int xDirection, int yDirection)
    {
        if (!_grid.IsWalkable(posX, posY))
            return null;
        if (_targetNode.x == posX && _targetNode.y == posY)
        {
            _forced = true;
            return _grid.GetNodeFromIndexUnchecked(posX, posY);
        }

        _forced = false;
        if (xDirection != 0 && yDirection != 0)
        {
            if ((!_grid.IsWalkable(posX - xDirection, posY) && _grid.IsWalkable(posX - xDirection, posY + yDirection)) ||
                (!_grid.IsWalkable(posX, posY - yDirection) && _grid.IsWalkable(posX + xDirection, posY - yDirection)))
            {
                return _grid.GetNodeFromIndexUnchecked(posX, posY);
            }

            if (_grid.IsWalkable(posX + xDirection, posY + yDirection) && 
                !_grid.IsWalkable(posX + xDirection, posY) &&
                !_grid.IsWalkable(posX, posY + yDirection))
            {
                return null;
            }

            if (_Jump(posX + xDirection, posY, xDirection, 0) != null || _Jump(posX, posY + yDirection, 0, yDirection) != null)
            {
                if (!_forced)
                {
                    throw new NotImplementedException();
                }

                return _grid.GetNodeFromIndexUnchecked(posX, posY);
            }
        }
        else
        {
            if (xDirection != 0)
            {
                if ((_grid.IsWalkable(posX + xDirection, posY + 1) && !_grid.IsWalkable(posX, posY + 1)) ||
                    (_grid.IsWalkable(posX + xDirection, posY - 1) && !_grid.IsWalkable(posX, posY - 1)))
                {
                    _forced = true;
                    return _grid.GetNodeFromIndexUnchecked(posX, posY);
                }
            }
            else
            {
                if ((_grid.IsWalkable(posX + 1, posY + yDirection) && !_grid.IsWalkable(posX + 1, posY)) ||
                    (_grid.IsWalkable(posX - 1, posY + yDirection) && !_grid.IsWalkable(posX - 1, posY)))
                {
                    _forced = true;
                    return _grid.GetNodeFromIndexUnchecked(posX, posY);
                }
            }
        }

       return _Jump(posX + xDirection, posY + yDirection, xDirection, yDirection);
    }

    private int _GetDistance(Node a, Node b)
    {
        var dx = Math.Abs(a.x - b.x);
        var dy = Math.Abs(a.y - b.y);

        return (int)(LateralCost * (dx + dy) + (DiagonalCost - 2 * LateralCost) * Math.Min(dx, dy));
    }
}