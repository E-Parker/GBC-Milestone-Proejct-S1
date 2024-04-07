using System;
using System.Collections;
using Unity.Collections;
using Unity.VisualScripting;
using UnityEngine;


public class Grid : SingletonObject<Grid> {

    const float nodeRadius = 0.05f;                 // radius around each node checked for unwalkable surfaces.
    const float nodeDiameter = nodeRadius * 2.0f;
    
    public static LayerMask unWalkableMask = 3;                     // Layer mask to define colliders that are walkable.
    public static Vector2 gridSize = new Vector2(6f, 6f);       // size of the grid.
    public Node[,] grid {get; private set; }                    // Array of grid nodes.

    private static int GridSizeX, GridSizeY;


    public override void CustomAwake(){
        GridSizeX = Mathf.RoundToInt(gridSize.x / nodeDiameter);
        GridSizeY = Mathf.RoundToInt(gridSize.y / nodeDiameter);
        IsPersistent = true;
    }

    void Start(){
        BuildGrid();
        StartCoroutine(UpdateGrid());
    }

    IEnumerator UpdateGrid(){
        /* This function runs collision checks to update the walkable and unwalkable grid tiles. */
        int x, y;
        bool walkable;

        while(true){
            for (x = 0; x < GridSizeX; x++){
                for (y = 0; y < GridSizeY; y++){
                    walkable = !Physics.CheckSphere(grid[x,y].worldPosition, nodeRadius, unWalkableMask);
                    grid[x,y].walkable = walkable;
                    yield return new WaitForSeconds(0.005f);    
                }
            }  
        }
    }

    void BuildGrid(){
        /* This function initializes a grid of nodes specified by grideSize. */
        
        grid = new Node[GridSizeX,GridSizeY];

        Vector3 position = transform.position;
        Vector3 WorldBottomLeft = position - (Vector3.right * gridSize.x * 0.5f) - (Vector3.forward * gridSize.y * 0.5f);
        Vector3 nodePosition;

        int x, y;
        bool walkable;

        // Create nodes for each position in the grid.
        for (x = 0; x < GridSizeX; x++){
            for (y = 0; y < GridSizeY; y++){
                nodePosition = WorldBottomLeft + (Vector3.right * (x * nodeDiameter + nodeRadius)) + Vector3.forward * (y * nodeDiameter + nodeRadius);
                walkable = !Physics.CheckSphere(nodePosition, nodeRadius, unWalkableMask);
                grid[x,y] = new Node(walkable, nodePosition);
            }
        }  
    }

    void OnDrawGizmos(){
        // Debug visual for the grid.
        Gizmos.DrawWireCube(transform.position, new Vector3(gridSize.x, 1.0f, gridSize.y));

        if (grid != null){
            foreach (Node node in grid){
                Gizmos.color = node.walkable? Color.green : Color.red;
                Gizmos.DrawCube(node.worldPosition, Vector3.one * (nodeDiameter - 0.01f));
            }
        }
    }

}