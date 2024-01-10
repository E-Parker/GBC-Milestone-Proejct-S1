using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public class Background_Scroller : MonoBehaviour{
    /*  This script scrolls a tiling background. it's not perfect but it'll do for now. 
    I plan on re-working this later when I do procedural generation. */   

    [SerializeField] string m_Target_Name;      // Name of the game object to track.

    [SerializeField] GameObject[] m_Hide;       // array of game objects to hide with background.
    [SerializeField] GameObject[] m_unHide;     // array of game objects to show with background.
   
    [SerializeField] bool m_OverrideSize;
    [SerializeField] Vector2 size = new Vector2(1.28f, 1.28f);
    [SerializeField] Vector2 bounds = new Vector2(8f, 8f);


    private GameObject target;                  // Game object of the target
    private Vector3 offset;
    private Sprite sprite;
    
    
    void Start(){
        
        // if no target set, override with player.
        if (target == null){
            target = GameObject.Find(m_Target_Name);
        }

        offset = transform.position;                    // Get offset from start position.
        if ( GetComponent<SpriteRenderer>() != null){
            sprite = GetComponent<SpriteRenderer>().sprite; // Get sprite from renderer.
            if (!m_OverrideSize){
                size = new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);
            } 
        }
    }

    
    void Update(){
        /*  Update the target being tracked and set the transform accordingly. */
        
        // try to get the target's transform.   <--- could move this to fixedUpdate to avoid checking this every single frame.
        if (target == null || target.IsDestroyed()){
            target = null;
            target = GameObject.Find(m_Target_Name);
            return; // Return here isn't absolutely necessary as if a target is found but this avoids doing multiple checks for target == null. 
        }
        
        // Get target position.
        Vector3 tp = target.transform.position;   // Short-hand for "Tracked Position".
        
        // Hide or show objects depending on if the target is in bounds: 
        bool onScreen = tp.x > -bounds.x && tp.x < bounds.x && tp.z > -bounds.y && tp.z < bounds.y;
        if (onScreen)showObjects();
        else hideObjects();

        // Move the transform to the nearest repeat point.
        transform.position = new Vector3(((int)(tp.x / size.x)) * size.x, 0,
                                         ((int)(tp.z / size.y)) * size.y);
        
        // Add offset to transform.
        transform.position += offset;
    }


    private void hideObjects(){
        /*  Hide objects marked to be hidden. */
        foreach (GameObject gameObject in m_Hide){
            gameObject.SetActive(false);
        }
        foreach (GameObject gameObject in m_unHide){
            gameObject.SetActive(true);
        }
    }


    private void showObjects(){
        /*  Show objects marked to be Shown. */
        foreach (GameObject gameObject in m_Hide){
            gameObject.SetActive(true);
        }
        foreach (GameObject gameObject in m_unHide){
            gameObject.SetActive(false);
        }
    }
}

