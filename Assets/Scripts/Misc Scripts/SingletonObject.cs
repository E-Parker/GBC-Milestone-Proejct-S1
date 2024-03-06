using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Utility.Utility;

/*  
This class allows for any script to instantly become a singleton object, while still inheriting 
functionality from MonoBehavior.

This is a very slow way to check this sort of thing, but Singletons are only supposed to be 
instanced once, and at scene load anyway. It's not likely that you could ever create enough 
singletons to cause a slowdown anyway.

*/

public abstract class SingletonObject<T> : MonoBehaviour where T : SingletonObject<T>, new(){
    
    // Dictionary of singletons, indexed by their type. I feel so damn smart for this right now.
    public static Dictionary<Type, SingletonObject<T>> Instances = new();

    // These private fields are of type T meaning they are static and local to each singleton Type.
    //private static T IndexerObjectType = new();
    private static Type Indexer = typeof(T);    //IndexerObjectType.GetType();
    
    public static T Instance{
        get { return (T)Instances[Indexer]; }
        //private set{ Instances.Add(value.GetType(), value); }
    }


    public void Awake(){
        /* This function ensures that only one instance of type T exists. */
        try{
            Instances.Add(Indexer, this);   // Try to add this object to the list of singletons. 
            DontDestroyOnLoad(this);        // Ensure the same object persists between Loads.
        }
        catch (ArgumentException) {
            Destroy(gameObject);            // Since It already exits in the scene, destroy it.
        }

        CustomAwake();                      // Call custom overrides for Awake function.
    }


    public virtual void CustomAwake(){ 
        /* Define any additional On Awake calls here. */ 
    }


    public static void MakeInstance(){
        /* This function creates a new instance of type T. */

        // Otherwise, make a new GameObject with the class T. 
        GameObject newObject;
        newObject = Instantiate(EmptyObject, Vector3.zero, quaternion.identity);
        newObject.AddComponent<T>();
        newObject.name = nameof(T);
    }
}