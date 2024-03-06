using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;


[Serializable] public class Sprite_Animation : MonoBehaviour{
    /*  This script handles storing animations as well as keeping track of the current frame of 
    animation. */
    
    /* 
    This is really bad. I don't like that I have to do it this way else spend the next week
    manually making every single possible animation for each perspective on each character.
    
    It's clear the Unity animator was designed for 3D games, it could work for 2D but I feel like
    a custom code implementation would be better especially for a top-down style of game with
    sprites that aren't symmetrical.
    */
    
    // Constants / Readonly:
    [Serializable] public struct AnimationFrames{
        public string animation_name;   // name of the animation.
        public bool oneShot;            // bool for if the animation should loop / be interrupted.
        public float framerate;         // framerate in FPS of the animation.
        public int[] frame_indecies;    // array corresponding to the frames of an animation.
    }

    [Serializable] public struct AnimationFramesArray{
        public AnimationFrames[] animations; 
    }

    // I used an array here because Unity does not support serialized dictionaries.
    [SerializeField] AnimationFrames[] m_Animations;
    
    // This was the fastest way I could think of to index the array of animationFrames by name.
    private Dictionary<string, AnimationFrames> animationLookup;
    
    private float timeElapsed;          // total time since last frame.
    private int frameCounter;           // index of current frame of animation.
    [HideInInspector] public int frame; // actual frame value.
    
    private Queue<AnimationFrames> playList; // add animations to queue on Play().
    private string previous_anim; // the last animation played when starting a new one
    private string next_anim; // the next looping animation played if overshot is playing.
    
    private string currentAnimationName;        // name of the currently played animation.
    private AnimationFrames currentAnimation;   // holder for current frames.

    void Start(){

        // Initialize Dictionary:
        animationLookup = new Dictionary<string, AnimationFrames>();

        // Add the name and index of each animation in the array of animations.
        for (int i = 0; i < m_Animations.Length; i++){
            
            // Variables:
            string name = m_Animations[i].animation_name;

            // Error checks:
            if(name == ""){ // if unnamed:
                Debug.LogWarning("Animation with no assigned name.");
            }
            else if(animationLookup.Keys.Contains(name)){ // if a duplicate name:
                Debug.LogError("Animations must have unique identifiers.");
            }
            // Animation has valid name, add to the dictionary.   
            animationLookup.Add(name, m_Animations[i]); 
        }

        // Initialize values:
        playList = new Queue<AnimationFrames>();
        currentAnimationName = "";
        next_anim = "";
        frameCounter = 0;
        frame = 0;
        timeElapsed = 0.0f;   
    }

    void LateUpdate(){
        /*  Handles updating the current frame of animation. */

        //Debug.Log(playList.Count);

        if (currentAnimationName == ""){ // No animation is playing, leave update early.
            return;
        }
        
        // Update current frame:
        timeElapsed += Time.deltaTime; // Time in seconds that has elapsed since last call:

        float fractionalFramerate = 1 / currentAnimation.framerate;
       
        if (timeElapsed > fractionalFramerate){  // if a new frame needs to be displayed,
            timeElapsed %= fractionalFramerate;  // keep the remainder.
            frameCounter++;                      // increment frame counter. 
        }
        
        // Handle one-shot animations:
        // I put this here so that the last frame would be held for the correct amount of time.
        if (currentAnimation.oneShot){
            
            // If the animation has ended, call Stop().
            if (frameCounter == currentAnimation.frame_indecies.Length){
                Stop();
                return; // Stop could start another animation so leave early.
            }
        }
        else{ // Current animation is looping, make sure framecounter is within array's range.
            frameCounter %= currentAnimation.frame_indecies.Length;
        }
        
        // Update current frame:
        frame = currentAnimation.frame_indecies[frameCounter];
    }
    
    
    void ReadAnimationsFromJson(TextAsset asset){
        AnimationFramesArray array = JsonUtility.FromJson<AnimationFramesArray>(asset.text);
        m_Animations = array.animations;
    }


    void writeAnimationsToJson(){
        /* This function dumps the animations to the path specified. */
        AnimationFramesArray array;
        array.animations = m_Animations;
        string json = JsonUtility.ToJson(array, true);
        Debug.Log($"{gameObject.name}\n\n{json}");
    }


    public bool Contains(string name){
        return animationLookup.Keys.Contains(name);    
    }

    public string[] GetNames(){
        /*  Returns a list of animation names. */
        List<string> names = new List<string>();
        foreach(string name in animationLookup.Keys){
            names.Add(name);
        }
        return names.ToArray();
    }

    public string GetAnimationName(){
        /* public function for getting the current animation's name without providing access to
        to changing the actual variable. */        
        return currentAnimationName;
    }
    
    public string GetAnimationType(){
        /* This function returns the current type of animation being played */
        return currentAnimation.oneShot? "oneShot" : "Looping";
    }


    public float GetAnimationLength(string name){
        /*  Returns the length of a given animation in seconds. */

        if (!animationLookup.Keys.Contains(name)){
            Debug.LogError("Animation not found, could not provide length. ");
            return 0f;
        }

        // fractional representation of framerate:
        float framerate = 1 / animationLookup[name].framerate;

        // number of frames:
        int framecount = animationLookup[name].frame_indecies.Count();

        // Return the amount of time in seconds.
        return framerate * framecount;
    }
    
    public void Play(string name){
        /*  Handles starting an animation from its name. */
        
        // Check for valid animation name:
        if(!animationLookup.Keys.Contains(name)){ // if name is not in the names of animations:
            Debug.LogWarning("Tried to play missing animation."); 
            return;
        }
        
        // Check that a new animation is being started, otherwise leave early. 
        if (currentAnimationName == name){
            //Debug.Log($"already playing{name}");
            return;
        }
        
        // If the current animation is a Oneshot, add the next animation to the queue.
        if(currentAnimation.oneShot){
            if (animationLookup[name].oneShot) {
                playList.Enqueue(animationLookup[name]);
                //Debug.Log($"added {name} to the queue. ");
                }
            else {
                next_anim = name;
                //Debug.Log($"Added {name} to next anim. ");
                }
            return;
        }

        // Update current animation:
        currentAnimation = animationLookup[name];
        currentAnimationName = name;
        
        // Reset counters:
        frameCounter = 0;
        timeElapsed = 0.0f;
    }

    public void Stop(){
        /*  Stops the current animation. */
        
        // No animations playing, exit early.
        if (currentAnimationName == ""){ 
            //Debug.Log("No animations to play.");
            return;
        }
        
        // no queued animations to play, exit early.
        if ((playList.Count == 0) && (next_anim == "")){
            //Debug.Log("No Queued animations to play.");
            currentAnimationName = "";
            return;
        }

        // if there are oneshots in the playlist:
        if (playList.Count != 0){
            //Debug.Log("Getting next OneShot from queue.");
            // get the next one from the queue.
            currentAnimation = playList.Dequeue();
            
        }
        else{ // otherwise play next_anim
            //Debug.Log("Getting next animation from next_anim");
            currentAnimation = animationLookup[next_anim];
            next_anim = "";
        }

        // Update animation name:
        currentAnimationName = currentAnimation.animation_name;
            
        // reset counters:
        frameCounter = 0;
        timeElapsed = 0.0f;
    }
}

