using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapFixer : MonoBehaviour{   

    /*  This is quite literally the dumbest thing I think I have ever writen in code. For whatever
    reason, Tilemaps have a hidden property that allows them to recive shadows...
    
    I don't know why it's like this.
    I don't want to know why it's like this.
    
    I hate this stupid engine with every fiber of my being. */
    
    void Start(){
        TilemapRenderer tilemap = GetComponent<TilemapRenderer>();
        tilemap.receiveShadows = true; // The ONLY perpose of this script is to change this value.
    }
}

