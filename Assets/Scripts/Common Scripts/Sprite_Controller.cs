using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Utility.Utility;
using Unity.VisualScripting;


/*================================================================================================*\
|                                      USHORT STATE HANDLER CLASS                                  |
\*================================================================================================*/

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

/*================================================================================================*\
|                                 SPRITE CONTROLLER TEMPLATE CLASS                                 |
\*================================================================================================*/

public class SpriteController{
    /*Prototype of sprite controller used for moving animated sprites across a scene. */
    
    /* Any animation with these names will be ignored by StatesFromAnimations(). 
    These are still expected to be there since they are played manually. */
    public readonly string anim_Walk =   "Walk";
    public readonly string anim_Idle =   "Idle";

    protected static string[] IgnoreAnimations = new string[]{"Dead", "Dying", "Idle", "Hurt"};

    private Vector3 _direction = Vector3.forward;
    private Vector3 _targetDirection = Vector3.forward;

    public Vector3 direction {  // Unit vector facing direction.
        get {return _direction; }
        set { _targetDirection = (value != Vector3.zero)? value: _targetDirection; }   
    }

    public Vector3 position {       // Proxy for rigidBody's position.
        get{ return rigidBody.position; }
        set {rigidBody.position = value; } 
    }
    
    public Vector3 velocity {       // Proxy for rigidBody's velocity.
        get{ return rigidBody.velocity; }    
        set{ rigidBody.velocity = value; }
    }
    
    public float friction = 0.05f;              // amount velocity decays by over time.
    public float acceleration = 0.01f;          // amount velocity changes by over time.
    public float speed = 1;
    public float turnRate = 0.25f;

    public Sprite_Animation Animation;          // Animation Script.
    public Sprite_Animator Animator;            // Animator Script to controller animation.
    public Health_handler Health;               // Health script.
    public Collider collider;
    public Rigidbody rigidBody;                 // Handle movement with ridged body.

    public ushort state;                        // holds the current sprite state. see ActorData.
    protected ushort lastState;                 // holds the last sprite state.
    public Dictionary<string, ushort> states;   // dictionary of named states.
    private Vector3 lastDirection = Vector3.forward;

    // Initialization:

    public SpriteController(float friction, float acceleration, float speed, float turnRate,
        Rigidbody rigidBody, Collider collider, Health_handler Health, 
        Sprite_Animation Animation, Sprite_Animator Animator){
        
        this.rigidBody = rigidBody;
        this.friction = friction;
        this.acceleration = acceleration;
        this.speed = speed;
        this.turnRate = turnRate;
        this.Health = Health;
        this.Animation = Animation;
        this.Animator = Animator;
    }
    
    public void StatesFromAnimations(string[] names){
        /*  This function adds states to the look up dictionary. */
        
        // Ignore the Idle animation and add it as the default state. 
        states.Add("Idle", StateData.Idle);

        // Bit-shift left by i, start at 8 to ignore first 4 bits reserved for directional movement. 
        for (int i = 0; i < names.Length; i++){
            if (!IgnoreAnimations.Contains(names[i]) && !states.Keys.Contains(names[i])){
                this.states.Add(names[i], (ushort)(16 << i));
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
        lastDirection = Vector3.forward;

        // Set state from direction and change to correct animation variant:
        int variant = StateData.directionFromVector(ref state, direction.x, direction.z);
        Animator.ChangeVariant(variant);
    }
    
    // Update functions:
    
    public virtual void UpdateSpecial(){
        /*  Change any special conditions here. For instance, fire a fireball or something. */

    }

    public void Update(){
        /*  Check states and play animations. This is called AFTER getting the player input. */

        // If the character is hit, play the hurt animation. Skip playing any other animations.
        if (Health.Hit){
            Animation.Play("Hurt");
            return;
        }

        // Check if dying. Check to make sure there isn't an action happening either.
        if(!Health.Alive){
            OnDeath();
            return;
        }

        // Update special conditions:
        UpdateSpecial();

        // Update the facing direction.
        UpdateDirection();

        // Check that state has changes:
        if(lastState != state)
            PlayAnimations();

        // Snap position to pixels:
        Animator.SnapToPixels();
    }

    public void FixedUpdate(bool isMoving){
        // Update position and velocity:
        UpdateVelocity(isMoving);
    }

    public IEnumerator ApplyTurnRate(){
        while(true){
            _direction = _direction * (1f - turnRate) + _targetDirection * turnRate;
            yield return new WaitForSeconds(0.05f);
        }
    }

    public void OverrideTurnRate(){
        _direction = _targetDirection;
    }

    private void UpdateDirection(){
        /*  This function Changes the direction. */

        // If the direction hasn't changed, leave early.
        if (direction == lastDirection){ 
            return; 
        }

        if (direction == Vector3.zero){
            direction = lastDirection;
            return;
        }

        // Direction is always supposed to be a unit vector, if it isn't normalize the vector.
        if (Vector3.SqrMagnitude(direction) != 1f){ direction = Vector3.Normalize(direction); }
        
        lastDirection = direction;
        
        // Set the sprite facing direction:
        int variant = StateData.directionFromVector(ref state, direction.x, direction.z);
        Animator.ChangeVariant( variant );
    }

    public void UpdateVelocity( bool isMoving ){
        /* This function handles updating velocity, based of the direction. */ 
        
        // apply friction to velocity;
        velocity *= 1f - friction;

        /*Calculate the amount of speed that can be gained. if velocity is 0, then margin is at it's
        maximum, but as velocity approaches speed the amount of speed that can be gained decreases. */
        float margin = speed - Vector3.Magnitude(velocity);        
        if (isMoving) { velocity += direction * acceleration * margin; }
    }

    public void OnDeath(){
        /*  This function checks if the sprite is dying, returns the time until destruct. Add 
        implementation for this on Unity script. */

        // if "not alive" but still "not dying", Handle dying animation then die.
        if(!Health.Alive && !Health.Dying){

            // Clear state so character doesn't keep walking while dead.
            unsetAll();

            // Stop idle because we don't want to return to it after playing "Dying"
            Animation.Stop();

            // Set dying state. prevents OnDeath() from being called more than once.
            Health.Dying = true;

            // Play animations last. If one or both are missing the object will still be destroyed.
            Animation.Play("Dying");
            Animation.Play("Dead");
        }
    }

    // State changing functions:

    private bool isValidState(string name){
        if (!states.ContainsKey(name)){
            Debug.LogError($"State, {'"'}{name}{'"'} is not defined as a possible state. ");
            return false;
        }
        return true;
    }
    
    public void RememberLastState(){
        /*  Does what it says on the tin. this is meant to be called at the start of Update to keep
        track of if the sprite state has changed over the frame. */
        
        // If not stun-locked, store last state.
        if (!Health.Hit){   
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

        // If not stun-locked, check for valid state, set state from name.
        if (!Health.Hit){
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
}

