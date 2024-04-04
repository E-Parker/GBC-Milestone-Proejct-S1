using System.Collections.Generic;
using UnityEngine;


public enum TileStatus{
    UNVISITED,
    OPEN,
    CLOSED,
    IMPASSABLE,
    GOAL,
    START,
    PATH
};

public enum TileAdjacent{
    TOP_TILE,       // Tile above this tile.
    RIGHT_TILE,     // Tile to the right of this tile.
    BOTTOM_TILE,    // Tile bellow this tile.
    LEFT_TILE,      // TIle to the left of this tile.

    ADJACENT_TILES, // The number of adjacent tiles. 

    TOP_NODE,       // Node of the tile above this tile.
    RIGHT_NODE,     // Node of the tile to the right of this tile.
    BOTTOM_NODE,    // Node of the tile bellow this tile.
    LEFT_NODE       // Node of the tile to the left of this tile.
};


public class GridManager : SingletonObject<GridManager>{

    public const float TileScale = 0.16f;

    public static GameObject tilePrefab;
    public static float baseTileCost = 1f;
    public static bool useManhattanHeuristic = true;

    public TileScript[,] Grid;

    private int rows = 12;
    private int columns = 16;
    private List<GameObject> mines = new List<GameObject>();
    
    public override void CustomAwake()
    {
        tilePrefab = (GameObject)Resources.Load("Prefabs/Tile");
        Debug.Log("Building grid:");
        BuildGrid();
        
    }
    

    private void BuildGrid(){
        
        // Clear private arrays:
        Grid = new TileScript[rows, columns];

        //int count = 0;
        float rowPos = 5.5f;
        for (int row = 0; row < rows; row++, rowPos--){
            float colPos = -7.5f;
            for (int col = 0; col < columns; col++, colPos++){
                GameObject tileInst = Instantiate(tilePrefab, new Vector3(colPos, 0f, rowPos), Quaternion.identity);
                TileScript tileScript = tileInst.GetComponent<TileScript>();
                Grid[row,col] = tileScript;

                // TODO: Commented out for Lab 6a.
                //tileScript.TileColor = (colors[System.Convert.ToInt32((count++ % 2 == 0))]);
                //tileInst.transform.parent = transform;
                
                // Instantiate a new TilePanel and link it to the Tile instance.
                //GameObject panelInst = Instantiate(tilePanelPrefab, tilePanelPrefab.transform.position, Quaternion.identity);
                //panelInst.transform.SetParent(panelParent.transform);
                //RectTransform panelTransform = panelInst.GetComponent<RectTransform>();
                //panelTransform.localScale = Vector3.one;
                //panelTransform.anchoredPosition = new Vector3(64f * col, -64f * row);
                //tileScript.tilePanel = panelInst.GetComponent<TilePanelScript>();
                
                // Create a new PathNode for the new tile.
                //tileScript.Node = new PathNode(tileInst);
            }
            // TODO: Commented out for Lab 6a.
            //count--;
        }
        // TODO: Commented out for Lab 6a.
        // Set the tile under the ship to Start.
        //GameObject ship = GameObject.FindGameObjectWithTag("Ship");
        //Vector2 shipIndices = ship.GetComponent<NavigationObject>().GetGridIndex();
        //grid[(int)shipIndices.y, (int)shipIndices.x].GetComponent<TileScript>().SetStatus(TileStatus.START);
        // Set the tile under the player to Goal and set tile costs.
        //GameObject planet = GameObject.FindGameObjectWithTag("Planet");
        //Vector2 planetIndices = planet.GetComponent<NavigationObject>().GetGridIndex();
        //grid[(int)planetIndices.y, (int)planetIndices.x].GetComponent<TileScript>().SetStatus(TileStatus.GOAL);
        //SetTileCosts(planetIndices);
    }
    
    // TODO: Comment out for Lab 6a. We don't need to connect grid for Lab 6.
    public void ConnectGrid(){
        TileScript tileScript;
        // Iterate through every row and column, connect the cells accordingly.
        for (int row = 0; row < rows; row++){
            for (int col = 0; col < columns; col++){
                // Get and reset the current tile:
                tileScript = Grid[row, col];
                tileScript.ResetNeighbourConnections();
                if (tileScript.status == TileStatus.IMPASSABLE) continue;

                // Add the left, right, top and bottom tiles if they exist.
                if (row > 0){               // Set top neighbour if tile is not in top row.
                    tileScript[TileAdjacent.TOP_TILE] = Grid[row - 1, col]; 
                } 
                
                if (col < columns - 1) {    // Set right neighbour if tile is not in rightmost row.
                    tileScript[TileAdjacent.RIGHT_TILE] = Grid[row, col + 1]; 
                }   
                
                if (row < rows - 1) {       // Set bottom neighbour if tile is not in bottom row.
                    tileScript[TileAdjacent.BOTTOM_TILE] = Grid[row + 1, col]; 
                }  
                
                if (col > 0) {              // Set left neighbour if tile is not in leftmost row.
                    tileScript[TileAdjacent.LEFT_TILE] = Grid[row, col - 1]; 
                }    
            }
        }
    }

    public Vector2 GetGridPosition(Vector2 worldPosition){
        float xPos = Mathf.Floor(worldPosition.x) + 0.5f;
        float yPos = Mathf.Floor(worldPosition.y) + 0.5f;
        return new Vector2(xPos, yPos);
    }

    public void SetTileCosts(Vector2 targetIndices){
        float distance = 0f;
        float dx = 0f;
        float dy = 0f;

        for (int row = 0; row < rows; row++){
            for (int col = 0; col < columns; col++){
                TileScript tileScript = Grid[row, col];
                if (useManhattanHeuristic){
                    dx = Mathf.Abs(col - targetIndices.x);
                    dy = Mathf.Abs(row - targetIndices.y);
                    distance = dx + dy;
                }
                else{   // Euclidean.
                    dx = targetIndices.x - col;
                    dy = targetIndices.y - row;
                    distance = Mathf.Sqrt(dx * dx + dy * dy);
                }

                float adjustedCost = distance * baseTileCost;
                tileScript.cost = adjustedCost;
            }
        }
    }

    public void SetTileStatuses(){
        
        foreach (TileScript tile in Grid){
            tile.SetStatus(TileStatus.UNVISITED);
        }

        //TODO: Replace this with a static call that sets it up for a pathfind call.
        // Set the tile under the ship to Start.
        //Vector2 shipIndices = ship.GetComponent<NavigationObject>().GetGridIndex();
        // Grid[(int)shipIndices.y, (int)shipIndices.x].SetStatus(TileStatus.START);
        
        // Set the tile under the player to Goal.
        //GameObject planet = GameObject.FindGameObjectWithTag("Planet");
        //Vector2 planetIndices = planet.GetComponent<NavigationObject>().GetGridIndex();
        //Grid[(int)planetIndices.y, (int)planetIndices.x].SetStatus(TileStatus.GOAL);
    }

    public static void CheckLineOfSight(Vector3 position){
        
        Vector3 tilePos;

        foreach(TileScript tile in Instance.Grid){
            // Ignore null tiles.
            if (tile == null) { 
                continue; 
            }
            
            tilePos = tile.transform.position;

            bool hasShipLineOfSight = false;

            Vector3 direction = Player_Controller.Instance.position - tilePos;
            float magnitude = Vector3.Magnitude(direction);
            direction /= magnitude;

            hasShipLineOfSight = tile.Navigation.HasLOS(tile.gameObject, "Player", direction, magnitude);

            if(hasShipLineOfSight){
                tile.TileColor = Color.green;
            }
            else{
                tile.TileColor = Color.red;
            }
            
        }
    }
}
