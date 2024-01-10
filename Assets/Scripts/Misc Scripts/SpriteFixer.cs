using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteFixer : MonoBehaviour{   

    /*  Same sentements as in the tilemapFixer */
    
    void Start(){
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.receiveShadows = true; // The ONLY perpose of this script is to change this value.
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }
}

