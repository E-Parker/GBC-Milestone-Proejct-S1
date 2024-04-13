using System;
using System.Collections;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;

public class Grid : SingletonObject<Grid> {

    public static Vector2 gridSize = new Vector2(6.5f, 6.5f);   // World-space size of the grid.
    public static Vector3 WorldBottomLeft { get; private set; }
    public static int GridSizeX { get; private set; }
    public static int GridSizeY { get; private set; }
    public Node[,] grid { get; private set; }

    public override void CustomAwake(){
        IsPersistent = true;
        GridSizeX = Mathf.RoundToInt(gridSize.x / Node.nodeDiameter);
        GridSizeY = Mathf.RoundToInt(gridSize.y / Node.nodeDiameter);
        WorldBottomLeft = transform.position - (Vector3.right * gridSize.x * 0.5f) - (Vector3.forward * gridSize.y * 0.5f);
    }

    void Start(){
        BuildGrid();
        StartCoroutine(UpdateGrid());
    }

    IEnumerator UpdateGrid(){

        while(true){

            // Update each node in the grid.
            foreach(Node n in grid){
                n.Update();
            }
            
            // Update the nodes under each character in the scene with it's effect and range.
            Node node;
            foreach(var character in SpriteController.Characters){
                node = nodeFromPosition(character.position);
                node.Propagate(character.nodeEffect, character.nodeRange);
            }
            yield return null;
        }
    }

    public static Node nodeFromPosition(Vector3 position){

        // Get the nearest x, y index from the world position.
        int x = (int)((GridSizeX - 1) * (position.x + (gridSize.x * 0.5f)) / gridSize.x);
        int y = (int)((GridSizeY - 1) * (position.z + (gridSize.y * 0.5f)) / gridSize.y);

        return Instance.grid[x, y];
    }
    
    Vector3 offsetFromIndex(int x, int y){
        /* calculate the world-space position of a node from the x, y indices of it. */
        Vector3 result = Vector3.right * (x * Node.nodeDiameter + Node.nodeRadius);
        result += Vector3.forward * (y * Node.nodeDiameter +  Node.nodeRadius);
        result += WorldBottomLeft;
        return result;
    }

    void BuildGrid(){
        /* This function initializes a grid of nodes specified by grideSize. */
        
        grid = new Node[GridSizeX,GridSizeY];
        Vector3 nodePosition;
        int x, y;
        bool walkable;

        // Create nodes for each position in the grid.
        for (x = 0; x < GridSizeX; x++){
            for (y = 0; y < GridSizeY; y++){
                nodePosition = offsetFromIndex(x, y);
                walkable = !Physics.CheckSphere(nodePosition, Node.nodeRadius, unWalkableMask);
                grid[x,y] = new Node(walkable, nodePosition);
            }
        }

        // populate connections.
        for (x = 1; x < GridSizeX - 1; x++){
            for (y = 1; y < GridSizeY - 1; y++){
                //TODO: replace this with something better.
                grid[x,y][AdjacentNode.North] = grid[x,y + 1];
                grid[x,y][AdjacentNode.NorthWest] = grid[x - 1,y + 1];
                grid[x,y][AdjacentNode.NorthEast] = grid[x + 1,y + 1];
                grid[x,y][AdjacentNode.South] = grid[x,y - 1];
                grid[x,y][AdjacentNode.SouthWest] = grid[x - 1,y - 1];
                grid[x,y][AdjacentNode.SouthEast] = grid[x + 1,y - 1];
                grid[x,y][AdjacentNode.West] = grid[x - 1,y];
                grid[x,y][AdjacentNode.East] = grid[x + 1,y];
            }
        }
    }

    void OnDrawGizmos(){
        // Debug visual for the grid.
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, 1.0f, gridSize.y));

        if (grid != null){
            foreach (Node node in grid){
                float val = (node.Desirability + 1.0f) * 0.5f;
                Gizmos.color =  Color.Lerp( Color.red, Color.green, val); //node.Walkable ? Color.Lerp( Color.red, Color.green, val) : Color.blue;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (Node.nodeDiameter - 0.01f));
            }
        }
    }
}

