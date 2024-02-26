using System;
using System.Collections.Generic;
using UnityEngine;

/*  
This class allows for any script to instantly become a singleton object, while still inheriting 
functionality from MonoBehavior.

This is a very slow way to check this sort of thing, but Singletons are only supposed to be 
instanced once, and at scene load anyway. It's not likely that you could ever create enough 
singletons to cause a slowdown anyway.

*/

public abstract class SingletonObject<T> : MonoBehaviour where T : SingletonObject<T>, new(){
    
    public static List<SingletonObject<T>> Instances;  // List of all singleton instances.
    private static T instanceType;
    private static Type type;
    
    public static dynamic Instance{
        get {
            // Check that the list exists.
            if (Instances == null){
                Instances = new List<SingletonObject<T>>();
                return null;
            }

            // TODO: replace this with a hashset for faster lookups.
            // Iterate through all singletons and check for any matching objects.
            foreach(var singleton in Instances){
                // Check if the list of Instances contains an object of the specified type.
                if (singleton is T){
                    return singleton;
                }
            }
            
            // no instance was found, return null.
            return null; 
        }

        private set{
            // Check that the incoming singleton does not already exist in the list.
            if (!Instances.Contains(value)){
                Instances.Add(value);
            }
        }
    }

    virtual public void Awake(){
        // Check that there isn't already a singleton of this type in the scene.
        if (Instance == null){
            Instance = this;
            DontDestroyOnLoad(this);
        }

        // Since one already exits in the scene, destroy it.
        else{
            Destroy(gameObject);
        }
    }
}