using UnityEngine;

public class SpriteFixer : MonoBehaviour{   

    /*  Same sentements as in the tilemapFixer */
    
    void Awake(){
        SpriteRenderer sprite = GetComponent<SpriteRenderer>();
        sprite.receiveShadows = true; // The ONLY purpose of this script is to change this value.
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
    }
}

