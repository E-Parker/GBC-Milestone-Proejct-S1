using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;


public class TileScript : MonoBehaviour{
    
    // Tile Color Variables:
    [SerializeField] private Color original;
    private SpriteRenderer sprite;
    public Color TileColor {
        get{ getSprite(); return sprite.color; } 
        set{ getSprite(); sprite.color = value; }
    }

    // Path finding:
    private TileScript[] neighbourTiles = new TileScript[(int)TileAdjacent.ADJACENT_TILES];
    public PathNode Node { get; set; }              // PathNode used by path finding solver.
    public TileStatus status = TileStatus.UNVISITED;// current state of the tile.
    public float cost = 1f;                         // Cost to visit the tile

    // Other:
    private NavigationObject navigationObject;
    public NavigationObject Navigation { get { getNav(); return navigationObject; } }

    private void getSprite(){
        /* Method set the SpriteRenderer reference from the component on the game object. */
        if(sprite == null){ 
            sprite = gameObject.GetComponent<SpriteRenderer>(); 
        }
    }
    private void getNav(){
        /* Method set the SpriteRenderer reference from the component on the game object. */
        if(navigationObject == null){ 
            navigationObject = gameObject.GetComponent<NavigationObject>();
        }
    }

    public dynamic this[TileAdjacent direction]{
        /* Accessor for either the adjacent tile's TileScript or, the "Node" of that TileScript. 
        Setter takes a GameObject and initializes connections from that GameObject.
        
        {direction}_TILE for the tiles.
        {direction}_NODE for the nodes. 
        */

        get {
            int index = (int)direction % (int)TileAdjacent.ADJACENT_TILES;
            return ((int)direction > (int)TileAdjacent.ADJACENT_TILES)? 
                    neighbourTiles[index] : 
                    neighbourTiles[index].Node;
        }

        set{
            // Leave early if value is null.
            if ((value as GameObject) == null){
                Debug.LogError("When setting a neighbour tile, use the GameObject of the tile.");
                return;
            }

            TileScript newNeighbour = (value as GameObject).GetComponent<TileScript>();

            // Leave early if the object does not have a tileScript, or if the tile is impassable.
            if (newNeighbour == null || newNeighbour.status == TileStatus.IMPASSABLE){
                return;
            }

            // Override the tile at the direction.
            neighbourTiles[(int)direction % (int)TileAdjacent.ADJACENT_TILES] = newNeighbour;   

            // Add the new connection to Node.
            Node.AddConnection( new PathConnection(Node, newNeighbour.Node,
                Vector3.Distance(transform.position, newNeighbour.transform.position)));
                
        }
    }

    public void ResetNeighbourConnections(){
        /* Clears the references neighbour tiles. */
        for (int i = 0; i < (int)TileAdjacent.ADJACENT_TILES; i++){
            neighbourTiles[i] = null;
        }
        Node.connections.Clear();
    }


    internal void SetStatus(TileStatus stat){
        status = stat;
        switch (stat){
            case TileStatus.UNVISITED:
                TileColor = original;
                break;
            case TileStatus.OPEN:
                TileColor = original;
                break;
            case TileStatus.CLOSED:
                TileColor = original;
                break;
            case TileStatus.IMPASSABLE:
                TileColor = new Color(0.5f, 0f, 0f, 0.5f);
                break;
            case TileStatus.GOAL:
                TileColor = new Color(0.5f, 0.5f, 0f, 0.5f);
                break;
            case TileStatus.START:
                TileColor = new Color(0f, 0.5f, 0f, 0.5f);
                break;
            case TileStatus.PATH:
                TileColor = new Color(1f, 1f, 1f, 0.5f);
                break;
        }
    }
}
