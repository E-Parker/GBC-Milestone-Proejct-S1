
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


/*  This class handles the implementation for the mage enemy. I REALLY dont want too but I might
have to do multiple layers of inheritance here if I want to avoid duplicate code. */


public class EnemyMage_Controller_Interface : SpriteController{

    /*  This class handles the implementation for the player controller The actual script 
    accessed by Unity instances this class and populates it's fields. */

    // Expected Animation Names:
    public readonly string anim_Attack = "Attack";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"Attack"};

    // holder for target to shoot at:
    public GameObject target;

    private Projectile_Handler Projectile;
    private float turnrate;             // amount direction can change over time.
    private int mana;                   // the maximum mana the player can have.
    private int currentMana = 0;        // the current amount of mana the player has.
    private float manaRate;             // the amount of time it takes for mana to recharge.
    private float manaTimer = 0f;       // timer used to handle changing manaRate.
    private float MaxDistance;          // Distance the player can be seen from.
    private float MinDistance;          // Distance the mage will stay away from.
    private float ActionOpertunity;     // Amount of time(seconds) before the enemy choeses to move.
    private float ActionTimer;          // Tracks the amount of time since last action taken.
    public Vector3 wanderTarget;        // Random location min distance from player to wander too.

    private float aggression;            // Likelyhood that the AI will chose to shoot at the player.
    private float overshoot = 10f;      // aim ahead by this amount.

     // This is for Predicting where the target will be.
    private Vector3 TargetPosition;     // Stores the target's transform.position.
    private Vector3 LastTargetPosition; // stores the last position the target was at.
    private Vector3 toTarget;           // Stores the predicted position of the target.
    private Vector3 LastPosition;       // stores last position. for checking if stuck on something.

    public EnemyMage_Controller_Interface(
        float friction, float acceleration, float speed, float turnrate, int Mana, float manaRate,
        float ActionOpertunity, float MinDistance, float MaxDistance, 
        float overshoot, float aggression,
        Rigidbody rigidbody, Collider collider, Health_handler Health, Sprite_Animation Animation, 
        Sprite_Animator Animator, Projectile_Handler Projectile)
        :base(friction, acceleration, speed, rigidbody, collider, Health, Animation, Animator){
        
        this.turnrate = turnrate;
        this.MinDistance = MinDistance;
        this.MaxDistance = MaxDistance;
        this.overshoot = overshoot;
        this.aggression = aggression;

        mana = Mana;
        this.manaRate = manaRate;
        this.ActionOpertunity = ActionOpertunity;
        
        this.Projectile = Projectile;

        // Make a dictionary of the states by their animation name.
        states = new Dictionary<string, ushort>();                             
        StatesFromAnimations(Animation.GetNames());

        // set a wander target and get the actual Target
        ActionTimer = 0f;

        SetTarget();
    }

    public void SetTarget(){
        /*  This function sets the target to the player. */ 
        if (target == null){
            target = Player_Controller.Instance.gameObject;
        
            TargetPosition = target.transform.position;
            LastTargetPosition = target.transform.position;
            LastPosition = rigidbody.position;
            toTarget = target.transform.position;
        }
    }

    public void FireProjectile(){
        /*  This function casts a fireball towards the player. */
        
        // Check that the character has enough mana:
        if (currentMana > 0){
            Projectile.On_Fire(direction);
            currentMana--;
        }
        else{ // if not, don't play the casting animation.
            unsetState(anim_Attack);
        }
    }

    public override void ResetController(){
        base.ResetController();

        // Clear timers / counters:
        ActionTimer = 0f;
        manaTimer = 0f;
        currentMana = 0;

        // Clear target variables:
        SetTarget();
    }

    public override void UpdateSpecial(){
        // Check for missing target:
        SetTarget();
        
        // Update Timers
        if(currentMana != mana){
            manaTimer += Time.deltaTime;
            if (manaTimer > manaRate){
                manaTimer %= manaRate;
                currentMana++;
            }
        }
        
        // Check that the current animation is not locking input:
        if (StopMovement.Contains(Animation.GetAnimationName())){
            unsetState("Walk");
            return;
        }

        // Check for attack:
        if (CurrentStateIs(anim_Attack))
            FireProjectile();
    }
    
    public void UpdateAi(){

        // Check if enough time as passed to take an action:
        if (!ActionOpertunityCheck()){
            return;
        }
        
        LastTargetPosition = TargetPosition;        // Store position for next frame predictions.
        TargetPosition = target.transform.position; // Update current Target Position:
        
        Vector3 RelativeTargetPosition = GetPosition() - TargetPosition;
        toTarget = RelativeTargetPosition + ((LastTargetPosition - TargetPosition) * overshoot);
        
        float relativeSqrDistance = Vector3.SqrMagnitude(RelativeTargetPosition); 


        // Reset state, Process AI will set new state values.
        unsetAll();     

        // Process AI:

        // if the enemy is too far away, teleport in front of the player.
        if(relativeSqrDistance > MaxEnemySqrDistance){
            direction = Vector3.Normalize(RelativeTargetPosition);
            rigidbody.gameObject.transform.position = TargetPosition - direction; // this is nasty.
            setState(anim_Walk);
        }
        // If the enemy is to far away walk towards the player.
        else if (relativeSqrDistance > SqrMaxDistance()){
            // if the enemy has not moved very much since the last frame, try to get un-stuck.
            if(Vector3.SqrMagnitude(rigidbody.position - LastPosition) < 0.025f){
                MovementOpertunity();
            }
            // otherwise, walk towards the player.
            else{
                direction = Vector3.Normalize(-RelativeTargetPosition);
                setState(anim_Walk);
            }
        }
        // If player is under the minimum distance, try to run away. Don't check for stuck so the enemy seems more panicked.
        else if(relativeSqrDistance < SqrMinDistance()){
            direction = Vector3.Normalize(RelativeTargetPosition);
            setState(anim_Walk);
        }
        // Aggression determines how likely the character is to attack.
        else if(Random.Range(0f,1f) > aggression){
            MovementOpertunity();
        }
        else{
            AttackOpertunity(toTarget);
        }
    }

    public bool ActionOpertunityCheck(){
        /*  Check for a movement opertunities. Called in Update function. */

        /*  This works by taking the amount of time that has passed as a fraction of time specified 
        by ActionOpertunity, which determines the probability that something will happen.
        Think of it like this, if its been the maximum time, there is a 100% chance to do something,
        if an action was just performed the chance is 0%, if half the time has passed its 50% etc.*/

        ActionTimer += Time.deltaTime;
        float fractionalTime = ActionTimer / ActionOpertunity;
        if (Random.Range(0f,1f) < fractionalTime){
            ActionTimer = 0f;   // Reset timer.
            return true;   
        }
        return false;
    }

    private void AttackOpertunity(Vector3 ToTarget){
        /* this function attacks in the predicted direction of the player. */
        
        // Check that the character has enough mana:
        if (currentMana > 0){
            direction = Vector3.Normalize(-ToTarget);
            setState(anim_Attack);
        }
    }

    private void MovementOpertunity(){   
        /*  Randomly walk in any direction. */
        Vector3 wander = new Vector3(Random.Range(-MaxDistance, MaxDistance), 0, 
                                     Random.Range(-MaxDistance, MaxDistance));
        
        // Get the current direction.
        Vector3 newDirection = direction * (1f - turnrate);
        
        // If wander is not a zero vector, change direction.
        if (wander != Vector3.zero){
            newDirection -= Vector3.Normalize(wander) * turnrate * 1.25f;    
        }
        Vector3.Normalize(newDirection);
        SetDirection(newDirection);
        setState(anim_Walk);
    }

    public float SqrMaxDistance(){
        return MaxDistance * MaxDistance;
    }

    public float GetMaxDistance(){
        return MaxDistance;
    }

    public float SqrMinDistance(){
        return MinDistance * MinDistance;
    }

    public float GetMinDistance(){
        return MinDistance;
    }

    public int GetMaxMana(){
        /* Returns the maximum mana of the player. */
        return mana;
    }

    public int GetCurrentMana(){
        /* Returns the current Mana of the player. */
        return currentMana;
    }
}


/*================================================================================================*\
|                                   INTERFACING WITH UNITY ENGINE                                  |
\*================================================================================================*/


public class EnemyMage_Controller: MonoBehaviour{

    // Variables:

    [Header("Movement")]
    [SerializeField] float m_Acceleration = 0.6f;
    [SerializeField] float m_Friction = 0.2f;
    [SerializeField] float m_Speed = 0.005f;
    [SerializeField] float m_Turnrate = 0.25f; //determines how much direction can change per frame.

    [Header("Behavior")]
    [SerializeField] float m_MaxDistance = 3f;
    [SerializeField] float m_MinDistance = 0.5f;
    [SerializeField] float m_Agression = 0.5f;
    [SerializeField] float m_Overshoot = 10f;

    [Header("Statistics")]
    [SerializeField] int m_Mana = 3;
    [SerializeField] float m_ManaRate = 0.5f;
    [SerializeField] float m_ActionOpertunity = 1.5f;
    [SerializeField] int m_Health = 3;

    //private EnemyMage_Controller_Interface controller;
    protected SpriteController controller;
    private bool initialized;

    void Awake(){
        initialized = false;
    }

    void Start(){
        // Initialize health.
        GetComponent<Health_handler>().Initialize(m_Health, m_Health);

        // Initialize the interface script. 
        controller = new EnemyMage_Controller_Interface(
            m_Friction, m_Acceleration, m_Speed, m_Turnrate, m_Mana, m_ManaRate, m_ActionOpertunity,
            m_MinDistance, m_MaxDistance, m_Overshoot, m_Agression,
            GetComponent<Rigidbody>(),
            GetComponent<Collider>(),
            GetComponent<Health_handler>(),
            GetComponent<Sprite_Animation>(), 
            GetComponent<Sprite_Animator>(),
            GetComponent<Projectile_Handler>());
        controller.rigidbody.position = transform.position;
        initialized = true;
    }

    void Update(){
        /* update character controller. */

        // Parse controller to get local variables:
        EnemyMage_Controller_Interface parsed = (EnemyMage_Controller_Interface)(controller);
        
        // only process AI if target exists.
        if (parsed.target == null){
            return;
        }
        
        controller.RememberLastState();             // Remember the previous state
        controller.unsetState(parsed.anim_Attack);  // Clear all states
        parsed.UpdateAi();                  // Update AI code with new predicted target location.
        controller.Update();                // Call standard update function from SpriteController.
        
        if (controller.Health.Alive()){
            // Stupid workaround to keep enemies on the ground.
            transform.position = new Vector3(transform.position.x, 0.06f, transform.position.z);  
        }
    }
    
    void FixedUpdate(){
        /*Handle Collisions / movement here. */
        controller.FixedUpdate(controller.CurrentStateIs(controller.anim_Walk));
    }
    
    void LateUpdate(){
        // Check for dead state at the last possibility.
        if(!controller.Health.Alive() && controller.Health.IsDying() && controller.Animation.GetAnimationName() != "Dying"){
            this.gameObject.SetActive(false);
        }
    }
    
    void OnEnable(){
        if (initialized){
            ResetController();
        }
    }

    public void ResetController(){
        /*  Reset the enemy here, avoids reinstanciating copies every time an enemy is killed. */

        // Initialize health.
        GetComponent<Health_handler>().Initialize(m_Health, m_Health);
        
        // Clear variables.
        controller.ResetController();

    }

}

