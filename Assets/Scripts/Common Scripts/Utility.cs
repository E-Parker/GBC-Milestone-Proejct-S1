using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Mathematics;

/*  This file contains various classes that are used by multiple scripts. */

namespace Utility{

public static class Utility{
    
    // Constants:
    public const float inv_PI_Div_4 =  1 / (Mathf.PI / 4);   // Divide by 45 degrees in radians.
    public const float PI_2 = Mathf.PI * 2;
    public const float MaxEnemyDistance = 1.75f;
    public const float MaxEnemySqrDistance = MaxEnemyDistance * MaxEnemyDistance;

    public static Scene CurrentScene; 

    /* This is used as a template when generating objects at runtime. 
    I could have done this with prefabs but using this I can make any combination of scripts I want
    on demand This avoids needing a prefab for an empty object. */
    public static GameObject EmptyObject = new GameObject("Empty");

    /*========================================================================================*\
    |                                  USHORT STATE HANDLER CLASS                              |
    \*========================================================================================*/

    public static class StateData{
        /* This class handles bitwise comparisons for actor states. States are stored as a ushort giving
        16 bits for actions. Note that the first 4 bits are reserved for directions leaving 12 bits 
        free for any additional states. */

        // Constants:

        //                                   Decimal        Binary 
        public const ushort Idle        = 0;            // 0000 0000
        public const ushort South       = 1;            // 0000 0001
        public const ushort East        = 2;            // 0000 0010
        public const ushort North       = 4;            // 0000 0100
        public const ushort West        = 8;            // 0000 1000
        public const ushort SouthEast   = South | East; // 0000 0011
        public const ushort NorthEast   = North | East; // 0000 0110
        public const ushort SouthWest   = South | West; // 0000 1001
        public const ushort NorthWest   = North | West; // 0000 1100

        static ushort[] lookup = new ushort[8]{South, SouthEast, East, NorthEast, 
                                            North, NorthWest, West, SouthWest};

        public static bool compare(ushort a, ushort b){
            return (a & b) == b;
        }
        
        public static void set(ref ushort a, ushort b){
            a |= b;
        }
        public static void unset(ref ushort a, ushort b){
            a &= (ushort)~b; // cast to ushort because bitwise not converts to int.. this sucks.
        }

        public static int directionFromVector(ref ushort a, float x, float y, int offset=2){
            /*  This function sets a to the nearest cardinal direction given a directional vector. */
            
            // Get angle as float in radians:
            float angle = Mathf.Atan2(y, x);

            // Normalize angle to be positive:
            if (angle < 0) angle += PI_2;
            
            // Convert angle to index 0-7: inv_PI_Div_4 is equivalent to angle(as degrees) / 45 degrees.
            int index = (Mathf.RoundToInt(angle * inv_PI_Div_4) + offset) % 8;

            // Set a to the corresponding direction.
            set(ref a, directionLookup(index));

            // return the index here because i cant figure out a better way to do this.
            return index;
        }

        public static ushort directionLookup(int direction){
            /*  This function returns the direction from the lookup table. index must be int 0-7 */
            return lookup[direction];
        }
    }


    /*========================================================================================*\
    |                               3D TRANSFORMATION & ALIGNMENT                              |
    \*========================================================================================*/

    public const int PixelsPerUnit = 100;   
    public const float inv_ppu = 1f / (float)PixelsPerUnit;

    public static Vector3 TransformToPixels(Vector3 position){
        /* This function rounds a Vector3 to the pixels per unit. */
        return new Vector3(math.round(position.x * PixelsPerUnit) * inv_ppu, 
                           math.round(position.y * PixelsPerUnit) * inv_ppu, 
                           math.round(position.z * PixelsPerUnit) * inv_ppu);
    }
    
    public static float SignedAngleFromVector(float ax, float ay, float bx, float by){
        return Mathf.Atan2( ax*by - ay*bx, ax*bx + ay*by );
    }

    public static void LookAtTransform(GameObject Original, GameObject Target){
        /*  This function points the GameObject, "Original" towards the Target's position. */
        
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

    /*========================================================================================*\
    |                                       MISCELLANEOUS                                      |
    \*========================================================================================*/

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

    public static void DestroyOnLoad(GameObject gameObject){
        /* This is literally the dumbest thing I have ever written besides maybe the sprite fixer. 
        All this does is move an object marked as don't destroy on load from the scene to the 
        current one. */
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetActiveScene());
    }

    /*========================================================================================*\
    |                                       INTERPOLATION                                      |
    \*========================================================================================*/
    
    public struct GradientNode<T>{

        public float position;  // Position in the gradient.
        public T value;         // Value at the position.

        public GradientNode(float position, T value)
        {
            this.position = Mathf.Clamp01(position);
            this.value = value;
        }
    }

    public struct ValueGradient<T>{
        /* Struct for sampling a gradient defined by a list of position-value pairs. */
        
        private List<GradientNode<T>> nodes;

        public ValueGradient(float[] points, T[] values){

            this.nodes = new();

            // if the arrays are valid, add a node for each pair.
            if (points.Length == values.Length){
                for(int i = 0; i < points.Length; i++){
                    this.nodes.Add(new GradientNode<T>(points[i], values[i]));
                }
                SortNodes();
            }
        }

        public ValueGradient(List<GradientNode<T>> nodes){
            this.nodes = nodes;
            SortNodes();
        }

        public void Add(GradientNode<T> node){
            nodes.Add(node);
            SortNodes();
        }

        private void SortNodes(){
            nodes.Sort((a, b) => a.position.CompareTo(b.position));
        }

        private void SearchNodes(int approxIndex, float t, out GradientNode<T> left,  out GradientNode<T> right){
            /* Check the nodes left and right of the approximate index. if a better value is found 
            recessively search the new best indices. */

            int leftIndex = Mathf.Clamp(approxIndex - 1, 0, nodes.Count);
            int rightIndex = Mathf.Clamp(approxIndex + 1, 0, nodes.Count);

            float leftDist = t - nodes[leftIndex].position;
            float rightDist = t - nodes[rightIndex].position;

            // The left and right pivots have opposing signs. the approximate index is the nearest.
            if(leftDist < 0.0f && rightDist > 0.0f ){
                left = nodes[leftIndex];
                right = nodes[rightIndex];
                return;
            }

            if (leftDist > 0.0f && rightDist > 0.0f ){
                SearchNodes(leftIndex, t, out left, out right);
                return;
            }

            SearchNodes(rightIndex, t, out left, out right);
            return;
        }

        public T Sample(float input, Func<T, T, float, float, T> interpolation = null){
            /* This function returns the gradient at "t" from the list of nodes. */
            
            // Check for interpolation type.
            if (interpolation == null){
                interpolation = Lerp<T>;
            }

            // No nodes. return the default for this type.
            if (nodes.Count == 0)
                return default;
            
            // Only one value, interpolation cannot be done. Return the value from the only node.
            if (nodes.Count == 1){
                return nodes[0].value;
            }

            // Find the nearest nodes on the left and right of the input.
            GradientNode<T> left, right;

            /* Since the nodes are sorted, truncate to the nearest index into the list of nodes. 
            This would only work first try if the nodes happen to be evenly spaced, although
            this does massively cut down on the number of checks needed to find the actual nearest 
            left and right. */

            SearchNodes((int)(input * nodes.Count), Mathf.Clamp01(input), out left, out right);
            float t = Mathf.InverseLerp(left.position, right.position, input);  // Calculate the interpolation factor:
            return interpolation(left.value, left.value, t, 0.0f);              // Interpolate the values:
        }
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
