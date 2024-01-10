using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

/*  This file contains various classes that are used by multiple scripts. */

namespace Utility{

public static class Utility{
    
    // Constants:
    public const float inv_PI_Div_4 =  1 / (Mathf.PI / 4);   // Constant for dividing by 45 degrees in radians.
    public const float PI_2 = Mathf.PI * 2;
    public const float MaxEnemyDistance = 1.75f;
    public const float MaxEnemySqrDistance = MaxEnemyDistance * MaxEnemyDistance;

    // Define the expected player object name here. This is for when the scene reloads and every script has to re-find the player.
    public static string PlayerObject = "Player";

    /* This is used as a template when generating objects at runtime. 
    I could have done this with prefabs but using this I can make any combination of scripts I want
    on demand This avoids needing a prefab for an empty object. */
    public static GameObject EmptyObject = new GameObject("Empty");

    // This will affect globally unless a local overwrite is made.
    public const int PixelsPerUnit = 100;   


    public static Vector3 TransformToPixels(Vector3 position, int pixelsPerUnit=PixelsPerUnit){
        /* This function rounds a Vector3 to the pixels per unit. */
        float inv_ppu = 1f / pixelsPerUnit;
        return new Vector3(math.round(position.x * pixelsPerUnit) * inv_ppu, 
                           math.round(position.y * pixelsPerUnit) * inv_ppu, 
                           math.round(position.z * pixelsPerUnit) * inv_ppu);
    }
    
    
    public static float SignedAngleFromVector(float ax, float ay, float bx, float by){
        return Mathf.Atan2( ax*by - ay*bx, ax*bx + ay*by );
    }


    public static void LookAtTransform(GameObject Original, GameObject Target){
        /*  This function points the gameobject, "Original" towards the Target's position. */
        
        /* This is a bit dumb as this will break if RectTransform ends up losing the .LookAt()
        function. I think this is okay for now. I might rewrite this later with a better more
        permanent solution. */

        var ot = (Original.GetComponent<Transform>()!=null)? Original.GetComponent<Transform>():
                                                             Original.GetComponent<RectTransform>();
        
        var tt = (Target.GetComponent<Transform>()!=null)?  Target.GetComponent<Transform>():
                                                            Target.GetComponent<RectTransform>();
        
        ot.rotation = Quaternion.RotateTowards(ot.rotation, tt.rotation,2^32);
        //ot.rotation = Quaternion.Inverse(tt.rotation);
    }


    public static string ushortBitsToString(ushort bits){
        /*  Generate a string from the bits of a ushort. */
        string output = "Data: ";
        ushort mask;                // used to compare each bit, sorta like indexing the ushort.
        for (int i=0; i < 16; i++){
            mask = (ushort)(1 << i);
            output += (i % 4 == 0)? " " : "";   // Add a space very 4 bits for readability.
            output += StateData.compare(bits, mask)? "1" : "0";
        }
        return output;
    } 

    public static T BoomerangLerp<T>(T a, T b, float t, float smooth = 0f){
        /* Blend between the two boomerang lerps for different levels of smoothness. */
        return Lerp(LinearBoomerangLerp(a, b, t), CosBoomerangLerp(a, b, t), smooth);
    }


    public static T LinearBoomerangLerp<T>(T a, T b, float t, float exp = 0f){
        /* Return to sender! */
        return Lerp(a, b, -4f * t * (t - 1f));
    }

    public static T CosBoomerangLerp<T>(T a, T b, float t, float exp = 0f){
        /* Return to sender but smooth! */
        return CosLerp(a, b, t * 2f);
    }

    public static T CosExponentLerp<T>(T a, T b, float t, float exp = 0.8f){
        /*  This function raises t to a power derived from exp. Same thing as ExponentLerp(), 
        just with cosine. https://www.desmos.com/calculator/tebffd9hbu */
        return CosLerp(a, b, Mathf.Pow(t, (exp < 0.5f)?1/(2*exp): 2 - (2*exp)));
    }

    public static T ExponentLerp<T>(T a, T b, float t, float exp = 0.8f){
        /*  This function raises t to a power derived from exp. This warp the interpolation. 
        when exp < 0.5, transform use 1/exp so exp -> inf. when exp > 0.5, change direction, 
        exp -> 0. Here's the graph I made to explain what's going on exactly. 
        https://www.desmos.com/calculator/ez5zhgpylg */
        return Lerp(a, b, Mathf.Pow(t, (exp < 0.5f)?1/(2*exp): 2 - (2*exp)));
    }

    public static T CosLerp<T>(T a, T b, float t, float exp = 0f){
        /* Cosine smooth lerp function. */
        return Lerp(a, b, -0.5f * Mathf.Cos(Mathf.PI * t) + 0.5f);
    }

    public static T smoothLerp<T>(T a, T b, float t, float exp = 0f){
        /* Smoothly interpolates between any values which can be interpolated using quadratics. */
        return Lerp(a, b, Lerp(t * t, 1 - ((t - 1) * (t - 1)), t));
    }

    public static T Lerp<T>(T a, T b, float t, float exp = 0f){
        /*  Static function to interpolate linearly any values which can be interpolated. 
        Interpolation is not performed on types that cannot be compared. */
        
        /* I had to change the Unity .Net API Compatibility level to "Framework" which is actually 
        framework 4.x but incorrectly labeled. I hate this engine. */
        
        dynamic da = a;
        dynamic db = b;

        return a + (da - db) * t;
    }
}

}
