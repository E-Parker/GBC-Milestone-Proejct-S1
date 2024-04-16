using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Pathfinder : MonoBehaviour {
    /* Class for handling pathfinding. */

    List<Node> openSet = new();         // List of nodes to be checked.
    HashSet<Node> closedSet = new();    // List of locked in nodes.

    void Awake(){
        ResetPathValues();
    }

    private void ResetPathValues(){
        /* Clear any non-persistent changes made to the nodes. Prevents some issues with multiple 
        agents accessing the same nodes. */

        foreach (Node node in openSet){
            node.hCost = 0.0f;
            node.gCost = 0.0f;
            node.parent = null;
        }

        foreach (Node node in closedSet){
            node.hCost = 0.0f;
            node.gCost = 0.0f;
            node.parent = null;
        }

        openSet = new();
        closedSet = new();
    }

	public Stack<Node> FindPath(Vector3 startPos, Vector3 targetPos){
		
        Node startNode = Grid.nodeFromPosition(startPos);
		Node targetNode = Grid.nodeFromPosition(targetPos);
        
		openSet.Add(startNode);

		while (openSet.Count > 0){
            // Find the best node in the open list to evaluate first.
			Node node = openSet[0];
			for (int i = 1; i < openSet.Count; i ++){
				if ((openSet[i].fCost < node.fCost) && (openSet[i].hCost < node.hCost)){
                    node = openSet[i];
				}
			}

			openSet.Remove(node);
			closedSet.Add(node);

            // If the target has been found, return the list of nodes that leads to the target.
			if (node == targetNode){
                Stack<Node> path = RetracePath(startNode, targetNode);
                ResetPathValues();
				return path;
			}

			for (AdjacentNode i = 0; (int)i < Node.MaxConnections; i++) {
                Node neighbour = node[i];
                // if the node doesn't exist, isn't walkable, or is in the closed set, continue.
				if (neighbour == null || !neighbour.Walkable || closedSet.Contains(neighbour)) {
					continue;
				}

				float newCostToNeighbour = node.gCost + Grid.GetDistance(node, neighbour);
				if (newCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
					neighbour.gCost = newCostToNeighbour;
					neighbour.hCost = Grid.GetDistance(neighbour, targetNode);
					neighbour.parent = node;

					if (!openSet.Contains(neighbour))
						openSet.Add(neighbour);
				}
			}
		}
        ResetPathValues();
        return null;    // If the program ends up here, something has gone very wrong.
	}

	Stack<Node> RetracePath(Node start, Node end){
        /* This method generates a queue of nodes leading from start to end. */
        
        Stack<Node> path = new();   // I used a queue here so the agent can just pull the next value off the queue.
		Node currentNode = end;

		while (currentNode != start) {
			path.Push(currentNode);
			currentNode = currentNode.parent;
		}
        return path;
	}

    public static Node FindBestNode(Vector3 agent, Vector3 target, Func<Node,Node,Node,bool> check, float radius = 0.5f){

        Node targetNode = Grid.nodeFromPosition(target);
        Node startNode = Grid.nodeFromPosition(agent);
        Node bestTarget = startNode;
        Node current;
        
        float x, y;
        
        for(x = -radius; x < radius; x += Node.nodeRadius){
            for(y = -radius; y < radius; y += Node.nodeRadius){
                // Get the current node under the active position.
                current = Grid.nodeFromPosition(new Vector3(agent.x + x, 0.0f, agent.z + y));
                
                if (current == null || !current.Walkable){
                    continue;
                }

                if(check(current, bestTarget, targetNode)){
                    bestTarget = current;
                }
            }
        }
        return bestTarget;
    }

    public static bool LowestCost(Node current, Node best, Node target){
        return current.Desirability + (1.0f / Grid.GetDistance(current, target)) < best.Desirability + (1.0f / Grid.GetDistance(best, target));

    }
}