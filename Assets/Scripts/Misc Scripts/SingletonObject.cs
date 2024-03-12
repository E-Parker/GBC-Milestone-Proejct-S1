using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using Unity.Mathematics;
using static Utility.Utility;


public abstract class SingletonObject<T> : MonoBehaviour where T : SingletonObject<T>, new(){
    /* This class allows for any script to instantly become a singleton object, while still 
    inheriting functionality from MonoBehavior. */

    // Dictionary of singletons, indexed by their type.
    public static Dictionary<Type, SingletonObject<T>> Instances = new();
    public static Dictionary<Type, bool> Persistent = new();

    // These private fields are static and local to each type of singleton.
    private static Type Indexer = typeof(T);
    
    public static T Instance{
        get {     
            try{ return (T)Instances[Indexer]; }
            catch{ return null; } 
        }
    }

    public static bool IsPersistent{
        get{ return Persistent[Indexer]; }
        
        set{ 

            Persistent[Indexer] = value;

            // mark the singleton in question accordingly.
            if(value){ DontDestroyOnLoad(Instances[Indexer].gameObject); }
            else{ DestroyOnLoad(Instances[Indexer].gameObject); }
        }
    }


    protected void Awake(){
        /* This function ensures that only one instance of type T exists. */
        try{
            Instances.Add(Indexer, this);   // Try to add this object to the list of singletons. 
            //DontDestroyOnLoad(this);      // Ensure the same object persists between Loads.
        }
        catch(ArgumentException){
            Debug.Log("Instance Already In scene.");
            Destroy(gameObject);            // Since It already exits in the scene, destroy it.
        }
        CustomAwake();                      // Call custom overrides for Awake function.
    }


    public virtual void CustomAwake(){ 
        /* Define any additional On Awake calls here. */ 
    }


    public static void MakeInstance(){
        /* This function creates a new instance of type T. */

        if (Instances.ContainsKey(Indexer)){
            Debug.Log("Instance already exists! Use DestroyInstance() before trying to make another.");
            return;
        }

        // Otherwise, make a new GameObject with the class T. 
        GameObject newObject;
        newObject = Instantiate(EmptyObject, Vector3.zero, quaternion.identity);
        Instances[Indexer] = newObject.AddComponent<T>();
        newObject.name = Indexer.FullName;
    }
    
    
    public static void DestroyInstance(){
        /* This function destroys the instance of a singleton. This will break many things if used 
        improperly. This is only intended to be called once */

            if (!Instances.ContainsKey(Indexer)){
                Debug.Log("Cannot destroy a singleton which has not been initialized! ");
                return;
            }

            // Find and destroy the instance.
            SingletonObject<T> singleton = Instances[Indexer];
            
            // Check if the gameObject is already destroyed.
            if (!singleton.IsDestroyed()){
                Destroy(singleton.gameObject);
            }

            // Remove the instance from the dictionary. 
            Instances.Remove(Indexer);

    }
}

