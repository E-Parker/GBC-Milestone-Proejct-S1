using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utility.Utility;


public class SpriteController{
    /*Prototype of sprite controller used for moving animated sprites across a scene. */
    
    /* Any animation with these names will be ignored by StatesFromAnimations(). 
    These are still expected to be there since they are played manually. */
    public readonly string anim_Walk =   "Walk";
    public readonly string anim_Idle =   "Idle";

    protected static string[] IgnoreAnimations = new string[]{"Dead", "Dying", "Idle", "Hurt"};

    public Vector3 direction = Vector3.forward; // Unit vector facing direction.
    public Vector3 velocity = Vector3.zero;     // Sprite velocity.

    public float friction = 0.05f;              // amount velocity decays by over time.
    public float acceleration = 0.01f;          // amount velocity changes by over time.
    public float speed = 1;

    public Sprite_Animation Animation;          // Animation Script.
    public Sprite_Animator Animator;            // Animator Script to controller animation.
    public Health_handler Health;               // Health script.
    public Collider collider;
    public Rigidbody rigidbody;                 // Handle movement with ridged body.

    public ushort state;                        // holds the current sprite state. see ActorData.
    protected ushort lastState;                 // holds the last sprite state.
    public Dictionary<string, ushort> states;   // dictionary of named states.
    
    // Initialization:

    public SpriteController(float friction, float acceleration, float speed,
        Rigidbody rigidbody, Collider collider, Health_handler Health, 
        Sprite_Animation Animation, Sprite_Animator Animator){
        
        this.rigidbody = rigidbody;
        this.friction = friction;
        this.acceleration = acceleration;
        this.speed = speed;
        this.Health = Health;
        this.Animation = Animation;
        this.Animator = Animator;
    }
    
    public void StatesFromAnimations(string[] names){
        /*  This function adds states to the look up dictionary. */
        
        // Ignore the Idle animation and add it as the default state. 
        states.Add("Idle", StateData.Idle);

        // Bitshift left by i, start at 8 to ignore first 4 bits reserved for directional movement. 
        for (int i = 0; i < names.Length; i++){
            if (!IgnoreAnimations.Contains(names[i]) && !states.Keys.Contains(names[i])){
                this.states.Add(names[i], (ushort)(16 << i));
            }
        }
    }

    public void SetDirection(Vector3 newDirection){
        /*  This function Changes the direction. */

        if (newDirection == Vector3.zero){
            //Debug.LogError("Cannot set direction to zero vector. ");
            return;
        }

        // Direction is always supposed to be a unit vector, if it isn't normalize the vector.
        if (Vector3.SqrMagnitude(newDirection) != 1f){
            //Debug.LogWarning("newDirection was not of length 1. ");
            newDirection = Vector3.Normalize(newDirection);
        }

        this.direction = newDirection;
        
        // Set the sprite facing direction:
        Animator.ChangeVarient(StateData.directionFromVector(ref state, direction.x, direction.z));
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
        Animator.ChangeVarient(StateData.directionFromVector(ref state, direction.x, direction.z));

    }
    
    // Update functions:
    
    public virtual void UpdateSpecial(){
        /*  Change any special conditions here. For instance, fire a fireball or something. */

    }

    public void Update(){
        /*  Check states and play animations. This is called AFTER getting the player input. */

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

    public void FixedUpdate(bool isMoving){
        // Update position and velocity:
        UpdateVelocity(direction, isMoving);
    }

    public void UpdateVelocity(Vector3 direction, bool isMoving){
        /* This function handles updating velocity, based of the direction. */ 
        
        // apply friction to velocity;
        this.velocity *= (1f - this.friction);

        /*Calculate the amount of speed that can be gained. if velocity is 0, then margin is at it's
        maximum, but as velocity approaches speed the amount of speed that can be gained decreases. */
        float margin = speed - Vector3.Magnitude(velocity);        
        if (isMoving) this.velocity += direction * this.acceleration * margin;
        this.rigidbody.velocity = this.velocity;
    }

    public void OnDeath(){
        /*  This function checks if the sprite is dying, returns the time untill destruct. Add 
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
    
    public void RememberLastState(){
        /*  Does what it says on the tin. this is meant to be called at the start of Update to keep
        track of if the sprite state has changed over the frame. */
        
        if (!Health.IsHit()){   // If not stun-locked, store last state.
            this.lastState = new ushort();
            this.lastState = this.state;
        }
    }
    
    public bool CurrentStateIs(string name){
        /*  Compare the current state to the state "name" in the lookup dictionary. */
        return StateData.compare(this.state, states[name]);
    }

    public void setState(string name){
        /*  This function sets the sprite state from a name. name must be in the dictionary of
        possible states. */

        // If not stunlocked, check for valid state, set state from name.
        if (!Health.IsHit()){
            if (isValidState(name)){        
                StateData.set(ref this.state, states[name]);
            }
        }
    }

    public void unsetState(string name){
        if (isValidState(name)){        
            StateData.unset(ref this.state, states[name]);
        }
    }

    public void unsetAll(){
        /*  This function clears the state. (set state to 0b_0000_0000_0000_0000) */
        this.state = 0;
    }

    public void PlayAnimations(){
        /* This function plays the animation for the current State. */

        // loop over all possible states, if the flag is set, play the corresponding animation.
        foreach(string state in this.states.Keys){
            if(CurrentStateIs(state)){
                Animation.Play(state);
            }    
        }
    }

    public Vector3 GetDirecion(){
        return this.direction;
    }

    public Vector3 GetPosition(){
        return this.rigidbody.position;
    }

    public int GetMaxHealth(){
        /* Returns the maximum health. */
        return this.Health.GetMaxHealth();
    }

    public int GetCurrentHealth(){
        /* Returns the current health. */
        return this.Health.GetHealth();
    }      
}

