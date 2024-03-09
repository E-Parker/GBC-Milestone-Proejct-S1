using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility.Utility;


[Serializable] public class Sprite_Animator : MonoBehaviour{
    /*  This class handles loading and animating spritesheet. */

    // Variables:
    [Header("Animator Target")]
    [SerializeField] GameObject m_TargetSprite;     // Target SpriteRender component.
    
    [Header("SpriteSheet")]
    [SerializeField] Sprite m_SpriteSheet;              // Raw image of sprite sheet.
    [SerializeField] Vector3 m_offset;

    [Header("SpriteSheet Params")]
    [SerializeField] int m_Rows;
    [SerializeField] int m_Collums;
    [SerializeField] Rect m_bounds;                     // Aesprite importer weirdness.               
    [SerializeField] int varient;                       // current row of animation varients.       
    
    private Sprite[] sprites;
    private Sprite_Animation Animation;
    private SpriteRenderer targetSprite;


    void Start(){
        // Split sprites by rows and collums:
        sprites = SplitTexture(m_SpriteSheet, m_Rows, m_Collums, m_bounds);

        // Get Animation component:
        Animation = GetComponent<Sprite_Animation>();

        // Get Sprite renderer componet:
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
        /*  This Method changes the targetsprite position such that it looks like its on the pixel
        grid while the actual transform.position can be any arbatrary number. Use this when using
        RigidBodys. */
        m_TargetSprite.transform.position = m_offset + TransformToPixels(gameObject.transform.position);
    }

    public void ChangeVariant(int index){
        /*  This function updates the varient value for this animator. */
        
        // validate input.
        if ((index < 0) || (index > m_Collums)){
            Debug.LogError("invalid varient index.");
            return;
        }
        
        // update varient.
        varient = index;
        
    }

    private Sprite[] SplitTexture(Sprite SourceSprite, int rows, int collums, Rect bounds){
        /*  This function splits a texture into sprites. I dont know why but Unity can't serialize 
        this, and I'm not reimplementing the TextureAtlas class so this will have to do. */
        
        // Variables:
        Texture2D texture = SourceSprite.texture;       // source sprite raw texture
        int subWidth, subHeight, xOffset, yOffset;    // declairations for other floats
        
        // get subwidth and height:
        subWidth    =   (int)MathF.Ceiling(bounds.size.x / collums);      // cell width
        subHeight   =   (int)MathF.Ceiling(bounds.size.y / rows);         // cell height
        
        // Initialize sprites:
        Sprite[] output_sprites = new Sprite[(rows * collums)];

        for (int y = 0; y < rows; y++){
            for (int x = 0; x < collums; x++){
                
                // Aesprite does some weirdness with croping so I'll just set it manually.
                xOffset = (int)bounds.xMin + (x * subWidth);
                yOffset = (int)bounds.yMin + (y * subHeight);
                
                // generate pivot and rect from curernt possition in the texture.
                Rect rect = new Rect(xOffset, yOffset, subWidth, subHeight);
                Vector2 pivot = new Vector2(0.5f,0.5f);

                // create sprite from texture.
                Sprite sprite = Sprite.Create(texture, rect, pivot);//, Utility.PixelsPerUnit);
                sprite.name = $"x: {x} | y: {y} index: {((rows - 1 - y) * collums) + x}";
                output_sprites[((rows - 1 - y) * collums) + x] = sprite;
            }
        }
        return output_sprites;
    }
}

