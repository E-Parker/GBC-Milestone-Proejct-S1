using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utility.Utility;

public abstract class SpriteController : MonoBehaviour{
    /* Prototype of sprite controller used for moving animated sprites across a scene. */
    
    /* Any animation with these names will be ignored by StatesFromAnimations(). 
    These are still expected to be there since they are played manually. */

    public readonly string anim_Walk =   "Walk";
    public readonly string anim_Idle =   "Idle";

    protected static string[] IgnoreAnimations = new string[]{"Dead", "Dying", "Idle", "Hurt"};
    
    public static List<SpriteController> Characters = new();

    public Vector3 position {
        get { return rigidbody.position; }
        set { rigidbody.position = value; }
    }

    public Vector3 velocity {
        get { return rigidbody.velocity; } 
        set { rigidbody.velocity = value; }
    }

    public Vector3 direction{
        get { return true_direction; }
        set { 
            if(value == Vector3.zero){
                return;
            }
            true_direction = value.normalized;
            Animator.ChangeVariant(StateData.directionFromVector(ref state, value.x, value.z)); 
        }
    } 

    public float EyeLevel {
        get { return collider.bounds.size.y * 0.75f; }
    }
    
    private Vector3 true_direction;             // store the actual direction here since a validation step is needed.
    
    [HideInInspector] public float friction = 0.05f;              // amount velocity decays by over time.
    [HideInInspector] public float acceleration = 0.01f;          // amount velocity changes by over time.
    [HideInInspector] public float speed = 1;

    [HideInInspector] public Sprite_Animation Animation;          // Animation Script.
    [HideInInspector] public Sprite_Animator Animator;            // Animator Script to controller animation.
    [HideInInspector] public Health_handler Health;               // Health script.

    [HideInInspector] public new Collider collider;
    [HideInInspector] public new Rigidbody rigidbody;

    [HideInInspector] public ushort state;                        // holds the current sprite state. see ActorData.
    [HideInInspector] protected ushort lastState;                 // holds the last sprite state.
    [HideInInspector] public Dictionary<string, ushort> states;   // dictionary of named states.
    [HideInInspector] public bool initialized { get; private set; } = false;

    [Header("Pathfinding")]
    public float nodeEffect = -0.8f;    // Float value propagated to nodes under the sprite. 
    public float nodeRange = 1.0f;      // Range of effect the sprite has.

    // Initialization:

    public virtual void CustomStart(){
        // Override this to run any additional initialization
    }

    protected void Start(){
        // Default initialization:
        collider = GetComponent<Collider>();
        rigidbody = GetComponent<Rigidbody>();
        Health = GetComponent<Health_handler>();
        Animation = GetComponent<Sprite_Animation>();
        Animator = GetComponent<Sprite_Animator>();
        
        gameObject.layer = unWalkableMask;

        if(!Characters.Contains(this)){
            Characters.Add(this);
        }
        else{
            Debug.Log("Already added to the list!!");
        }

        // Run custom start parameters.
        CustomStart();
        initialized = true;
    } 
    
    protected void OnEnable(){
        // Leave early if the controller is not initialized. avoids error when this gets called on start.
        if (!initialized){
            return;
        }

        // Otherwise reset the controller.
        ResetController();
    }

    protected void OnDestroy(){
        Characters.Remove(this);
    }

    protected void StatesFromAnimations(string[] names){
        /* This function adds states to the look up dictionary. */
        
        // Ignore the Idle animation and add it as the default state. 
        states.Add("Idle", StateData.Idle);

        // Bit-shift left by i, start at 8 to ignore first 4 bits reserved for directional movement. 
        for (int i = 0; i < names.Length; i++){
            if (!IgnoreAnimations.Contains(names[i]) && !states.Keys.Contains(names[i])){
                states.Add(names[i], (ushort)(16 << i));
            }
        }
    }

    public virtual void ResetController(){
        /* Clear reset controller here */

        // Clear states:
        unsetAll();
        
        // Stop any animations:
        Animation.Stop();
        
        // Clear vectors:
        velocity = Vector3.zero;
        direction = Vector3.forward;

        // Set state from direction and change to correct animation variant:
        Animator.ChangeVariant(StateData.directionFromVector(ref state, direction.x, direction.z));
        Characters.Add(this);
    }
    
    // Update functions:
    
    public virtual void UpdateSpecial(){
        /* Change any special conditions here. For instance, fire a fireball or something. */
    }

    protected void Update(){
        /* Check states and play animations. This is called AFTER getting the player input. */
        
        // If the character is hit, play the hurt animation. Skip playing any other animations.
        if (Health.IsHit()){
            Animation.Play("Hurt");
            return;
        }

        // Check if dying. Check to make sure there isn't an action happening either.
        if(!Health.Alive()){
            OnDeath();
            return;
        }

        // Update special conditions:
        UpdateSpecial();

        // Check that state has changes:
        if(lastState != state)
            PlayAnimations();

        // Snap position to pixels:
        Animator.SnapToPixels();
    }

    protected void FixedUpdate(){
        /* handles updating velocity, based of the direction. */ 
        
        // apply friction to velocity;
        velocity *= 1f - friction;  
        
         // change velocity by walk speed.
        if (CurrentStateIs(anim_Walk)){
            float margin = speed - velocity.magnitude;
            velocity += direction * acceleration * margin;
        } 
    }

    public void OnDeath(){
        /*  This function checks if the sprite is dying, returns the time until destruct. Add 
        implementation for this on Unity script. */

        // if "not alive" but still "not dying", Handle dying animation then die.
        if(!Health.Alive() && !Health.IsDying()){

            // Clear state so character doesn't keep walking while dead.
            unsetAll();

            // Stop idle because we don't want to return to it after playing "Dying"
            Animation.Stop();

            // Set dying state. prevents OnDeath() from being called more than once.
            Health.SetDying();

            // Play animations last. If one or both are missing the object will still be destroyed.
            Animation.Play("Dying");
            Animation.Play("Dead");
            Characters.Remove(this);
        }
    }

    // State changing functions:

    private bool isValidState(string name){
        if (!states.ContainsKey(name)){
            Debug.LogError($"State, \"{name}\" is not defined as a possible state. ");
            return false;
        }
        return true;
    }
    
    protected void RememberLastState(){
        /*  Does what it says on the tin. this is meant to be called at the start of Update to keep
        track of if the sprite state has changed over the frame. */
        
        if (!Health.IsHit()){   // If not stun-locked, store last state.
            lastState = new ushort();
            lastState = state;
        }
    }
    
    protected bool CurrentStateIs(string name){
        /*  Compare the current state to the state "name" in the lookup dictionary. */
        return StateData.compare(state, states[name]);
    }

    protected void setState(string name){
        /*  This function sets the sprite state from a name. name must be in the dictionary of
        possible states. */

        // If not stun-locked, check for valid state, set state from name.
        if (!Health.IsHit()){
            if (isValidState(name)){        
                StateData.set(ref this.state, states[name]);
            }
        }
    }

    protected void unsetState(string name){
        if (isValidState(name)){        
            StateData.unset(ref this.state, states[name]);
        }
    }

    protected void unsetAll(){
        /*  This function clears the state. (set state to 0b_0000_0000_0000_0000) */
        this.state = 0;
    }

    protected void PlayAnimations(){
        /* This function plays the animation for the current State. */

        // loop over all possible states, if the flag is set, play the corresponding animation.
        foreach(string state in this.states.Keys){
            if(CurrentStateIs(state)){
                Animation.Play(state);
            }    
        }
    }   
}

