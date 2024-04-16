using System;
using System.Collections.Generic;
using UnityEngine;
using static Utility.Utility;

public enum AdjacentNode {
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
}

public class Node {
    /* class for storing a node used for pathfinding. */

    public const int MaxConnections = (int)AdjacentNode.NorthWest + 1;
    public const float nodeRadius = 0.075f;                  // Determines the size of each node.
    public const float nodeDiameter = nodeRadius * 2.0f;
    public const float decayRate = 0.7f;
    
    private const int OppositeOffset = MaxConnections / 2;

    public static Func<AdjacentNode,AdjacentNode> OppositePaths = path => (AdjacentNode)(((int)path + OppositeOffset) % MaxConnections);

    static readonly public List<AdjacentNode> Paths = new(){ // List of path options for propagation
        AdjacentNode.North,
        AdjacentNode.East, 
        AdjacentNode.South,
        AdjacentNode.West
    };  
    
    // Variables:

    public Vector3 worldPosition { get; private set; }                      // The world-space position of the node.
    public Dictionary<AdjacentNode,Node> connections { get; private set; }  // list containing adjacent nodes.
    private float desirability;     // Agent bias towards pathfinding to this node.
    public bool Walkable = false;   // Bool for if the current tile is walkable or not.
    public bool LineOfSight = true; // Bool for if the tile in line of sight for the player.

    // Non-Persistent pathfinding variables:
    public float fCost { get { return hCost + gCost; } } 
    public float hCost = 0.0f;
    public float gCost = 0.0f;
  
    public Node parent;             // Stores the node used to traverse to this node.

    // Attributes:

    public float Desirability {
        get { return desirability; }
        set { desirability = Mathf.Clamp(value, -1.0f, 1.0f); }
    }
    

    public Node(bool walkable, Vector3 worldPosition){
        this.connections = new();
        this.Walkable = walkable;
        this.worldPosition = worldPosition;
        this.desirability = 0.0f;
    }

    public dynamic this[AdjacentNode direction]{
        get{
            // If the node exists and is an active connection, return that node.
            if (connections.ContainsKey(direction) && connections[direction].Walkable){
                return connections[direction];
            }
            // Otherwise return null.
            return null; 
        }
        
        set{
            // Ensure no null values get written to the dictionary.
            if (value != null){
                connections[direction] = value;  
            }
        }
    }

    public void Update(){
        /* This method runs a physics check to see if the node is intersecting a collider. */
        
        Walkable = !Physics.CheckSphere(worldPosition, nodeRadius, unWalkableMask);
        
        // if walkable, decay desirability towards zero.
        if (Walkable){
            Desirability *= decayRate;
            return;
        }
        
        // Propagate the -1.0f value to the adjacent walkable nodes.
        Propagate(-1.0f, 3);
    }

    public void Propagate(float val = -1.0f, int range = 1){
        /* This function propagates a value to the adjacent nodes. */

        // call the internal version of the function with more exposed variables.
        Propagate(Paths, val, range);
    }

    public void Propagate(float val = -1.0f, float range = 0.5f){
        /* This function propagates a value to the adjacent nodes. */

        // call the internal version of the function with more exposed variables.
        Propagate(Paths, val, Paths.Count - (int)((float)Paths.Count * Mathf.Clamp01(range)));
    }
    
    private void Propagate(List<AdjacentNode> paths, float val = -1.0f, int end = 0){
        /* This function propagates a value to the adjacent nodes, specified by path. */
        
        Desirability += val * (1.0f - decayRate);

        if(paths.Count == end){
            return;
        }

        List<AdjacentNode> nextPaths;

        // for each direction in the list:
        foreach(var path in paths){
            
            // exit if the node is not set.
            if (this[path] == null){
                continue;
            }

            // otherwise, run the normal propagate.
            nextPaths = new List<AdjacentNode>(paths);      // copy the starting list of paths.
            if(nextPaths.Contains(OppositePaths(path))){    // if the new list contains the opposite direction, remove it.
                nextPaths.Remove(OppositePaths(path));
            }
            else{                                           // Otherwise, remove the current direction.
                nextPaths.Remove(path);
            }

            // Otherwise, propagate for that node.
            this[path].Propagate(nextPaths, val * 0.15f);  // call the next node's propagate with the shortened list.
        
        }
    }

    public bool HasLOS(Vector3 source, Vector3 direction, string tag, float distance = 1.0f){
        /* Check if a source object has line of sight to this node. */

        RaycastHit hit;
        Physics.Raycast(source, direction, out hit, distance, unWalkableMask);

        if (hit.collider != null && hit.collider.CompareTag(tag)){
            LineOfSight = true;
        }
        else{
            LineOfSight = false;
        }

        return LineOfSight;
    }
}