using System;
using System.Collections.Generic;
using UnityEngine;


class SpriteManager : SingletonObject<SpriteManager>{
    /* Class to store and manage sprites to avoid reloading them where possible. */
     
    public static Dictionary<string, Sprite[]> SplitSprites;

    public override void CustomAwake(){
        SplitSprites = new();
    }

    public static Sprite[] SplitTexture(Sprite SourceSprite, int rows, int columns, Rect bounds){
        /* This method splits an incoming sprite into an array of sprites from the number of rows 
        and columns. */

        // If the incoming sprite already exists, return that instead.
        if (SplitSprites.ContainsKey(SourceSprite.name)){
            return SplitSprites[SourceSprite.name];
        }

        // Variables:
        Texture2D texture = SourceSprite.texture;   // Source sprite raw texture.
        int subWidth, subHeight, xOffset, yOffset;
        
        // get sub-width and height:
        subWidth = (int)MathF.Ceiling(bounds.size.x / columns); // Cell width.
        subHeight = (int)MathF.Ceiling(bounds.size.y / rows);   // Cell height.
        
        // Initialize sprites:
        SplitSprites[SourceSprite.name] = new Sprite[(rows * columns)];

        for (int y = 0; y < rows; y++){
            for (int x = 0; x < columns; x++){
                
                // Aesprite does some weirdness with cropping so I'll just set it manually.
                xOffset = (int)bounds.xMin + (x * subWidth);
                yOffset = (int)bounds.yMin + (y * subHeight);
                
                // Generate pivot and rect from current position in the texture.
                Rect rect = new Rect(xOffset, yOffset, subWidth, subHeight);
                Vector2 pivot = new Vector2(0.5f,0.5f);

                // Create sprite from texture.
                Sprite sprite = Sprite.Create(texture, rect, pivot);//, Utility.PixelsPerUnit);
                sprite.name = $"x: {x} | y: {y} index: {((rows - 1 - y) * columns) + x}";
                SplitSprites[SourceSprite.name][((rows - 1 - y) * columns) + x] = sprite;
            }
        }
        return SplitSprites[SourceSprite.name];
    }

    


}