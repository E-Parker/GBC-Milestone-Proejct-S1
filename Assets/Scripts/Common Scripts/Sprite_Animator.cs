using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility.Utility;


public class Sprite_Animator : MonoBehaviour{
    /*  This class handles loading and animating sprite sheets from a single image. */

    // Variables:
    [Header("Animator Target")]
    [SerializeField] GameObject m_TargetSprite;     // Target SpriteRender component.
    
    [Header("SpriteSheet")]
    [SerializeField] Sprite m_SpriteSheet;  // Raw image of sprite sheet.
    [SerializeField] Vector3 m_offset;

    [Header("SpriteSheet Params")]
    [SerializeField] int m_Rows;
    [SerializeField] int m_Collums;         // yes, this is misspelled. I would fix it, but this clears literally every single sprite so no.
    [SerializeField] Rect m_bounds;         // Aesprite importer weirdness.               
    [SerializeField] int varient;           // current row of animation variants.       
    
    private Sprite[] sprites;
    private Sprite_Animation Animation;
    private SpriteRenderer targetSprite;


    void Start(){
        // Split sprites by rows and columns:
        sprites = SpriteManager.SplitTexture(m_SpriteSheet, m_Rows, m_Collums, m_bounds);

        Animation = GetComponent<Sprite_Animation>();
        targetSprite = m_TargetSprite.GetComponent<SpriteRenderer>();
    }

    void Update(){
        /* Update target sprite to the current frame from the animation handler */
        // set current sprite:
        targetSprite.sprite = sprites[(varient * m_Collums) + Animation.frame];
        
        // point sprite towards camera:
        LookAtTransform(m_TargetSprite, Camera.main.gameObject);                                    
    }

    public void SnapToPixels(){
        /*  This Method changes the target sprite position such that it looks like its on the pixel
        grid while the actual transform.position can be any arbitrary number. Use this when using
        RigidBodies. */
        m_TargetSprite.transform.position = m_offset + TransformToPixels(transform.position);
    }

    public void ChangeVariant(int index){
        /*  This function updates the variant value for this animator. */
        
        varient = index;
        
    }
}

