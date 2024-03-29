using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public class EnemyMage_Controller: SpriteController{

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
    
    [Header("Other")]
    [SerializeField] GameObject DropOnDeath;
    [SerializeField] float DropRate = 0.25f;

    // Expected Animation Names:
    public readonly string anim_Attack = "Attack";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"Attack"};

    // holder for target to shoot at:
    public GameObject target;
    private Projectile_Handler Projectile;

    private int mana;                   // the maximum mana the player can have.
    private int currentMana = 0;        // the current amount of mana the player has.
    private float manaTimer = 0f;       // timer used to handle changing manaRate.
    private float ActionTimer;          // Tracks the amount of time since last action taken.
    public Vector3 wanderTarget;        // Random location min distance from player to wander too.
    private Vector3 TargetPosition;     // Stores the target's transform.position.
    private Vector3 LastTargetPosition; // stores the last position the target was at.
    private Vector3 toTarget;           // Stores the predicted position of the target.
    private Vector3 LastPosition;       // stores last position. for checking if stuck on something.

    public override void CustomStart(){
   
        Projectile = GetComponent<Projectile_Handler>();

        // Make a dictionary of the states by their animation name.
        states = new Dictionary<string, ushort>();                             
        StatesFromAnimations(Animation.GetNames());

        ActionTimer = 0f;
        mana = m_Mana;
        friction = m_Friction;
        acceleration = m_Acceleration;
        speed = m_Speed;
        SetTarget();
    }
    
    public override void ResetController(){
        base.ResetController();

        // Clear timers / counters:
        ActionTimer = 0f;
        manaTimer = 0f;
        currentMana = 0;

        // Clear target variables:
        SetTarget();
        Health.Initialize(m_Health, m_Health);
    }

    void LateUpdate(){
        // Check for dead state at the last possibility.
        if(!Health.Alive() && Health.IsDying() && Animation.GetAnimationName() != "Dying"){
            
            if(DropOnDeath != null && UnityEngine.Random.Range(0f,1f) < DropRate){
                Instantiate(DropOnDeath, position, quaternion.identity).GetComponent<Projectile_Controller>().Setvalues(Vector3.zero, this.gameObject);
            }
            
            gameObject.SetActive(false);
        }
    }

    public void SetTarget(){
        /*  This function sets the target to the player. */ 
        if (target == null){
            target = Player_Controller.Instance.gameObject;
        
            TargetPosition = target.transform.position;
            LastTargetPosition = target.transform.position;
            LastPosition = position;
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

    public override void UpdateSpecial(){

        // Stupid workaround to keep enemies on the ground.
        transform.position = new Vector3(transform.position.x, 0.06f, transform.position.z);  
    
        SetTarget();            // Check for missing target:
        RememberLastState();    // Remember the previous state
        unsetState(anim_Attack);
        UpdateAi();             // Update AI code with new predicted target location.
        
        // Update Timers
        if(currentMana != mana){
            manaTimer += Time.deltaTime;
            if (manaTimer > m_ManaRate){
                manaTimer %= m_ManaRate;
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
        
        unsetAll();             // Clear all states

        LastTargetPosition = TargetPosition;        // Store position for next frame predictions.
        TargetPosition = target.transform.position; // Update current Target Position:

        Vector3 RelativeTargetPosition = position - TargetPosition;
        toTarget = RelativeTargetPosition + ((LastTargetPosition - TargetPosition) * m_Overshoot);  
        
        float relativeSqrDistance = Vector3.SqrMagnitude(RelativeTargetPosition); 

        // Process AI:

        // if the enemy is too far away, teleport in front of the player.
        if(relativeSqrDistance > MaxEnemySqrDistance){
            direction = Vector3.Normalize(RelativeTargetPosition);
            rigidbody.gameObject.transform.position = TargetPosition - direction; // this is nasty.
            setState(anim_Walk);
        }

        // If the enemy is to far away walk towards the player.
        else if (relativeSqrDistance > m_MaxDistance * m_MaxDistance){
            
            // if the enemy has not moved very much since the last frame, try to get un-stuck.
            if(Vector3.SqrMagnitude(rigidbody.position - LastPosition) < 0.025f){
                MovementOpertunity();
            }
            
            // otherwise, walk towards the player.
            else{
                direction = -RelativeTargetPosition.normalized;
                setState(anim_Walk);
            }
        }

        // If player is under the minimum distance, try to run away. Don't check for stuck so the enemy seems more panicked.
        else if(relativeSqrDistance < m_MinDistance * m_MinDistance){
            direction = RelativeTargetPosition.normalized;
            setState(anim_Walk);
        }

        // Aggression determines how likely the character is to attack.
        else if(UnityEngine.Random.Range(0f,1f) > m_Agression){
            MovementOpertunity();
        }
        else{
            AttackOpertunity(toTarget);
        }
    }

    public bool ActionOpertunityCheck(){
        /*  Check for a movement opertunities. Called in Update function. */

        ActionTimer += Time.deltaTime;
        float fractionalTime = ActionTimer / m_ActionOpertunity;
        if (UnityEngine.Random.Range(0f,1f) < fractionalTime){
            ActionTimer = 0f;   // Reset timer.
            return true;   
        }
        return false;
    }

    private void AttackOpertunity(Vector3 ToTarget){
        /* this function attacks in the predicted direction of the player. */
        
        // Check that the character has enough mana:
        if (currentMana > 0){
            direction = -ToTarget.normalized;
            setState(anim_Attack);
        }
    }

    private void MovementOpertunity(){   
        /*  Randomly walk in any direction. */
        Vector3 wander = new Vector3(UnityEngine.Random.Range(-m_MaxDistance, m_MaxDistance), 0, 
                                     UnityEngine.Random.Range(-m_MaxDistance, m_MaxDistance));
        
        direction = wander.normalized;
        setState(anim_Walk);
    
    }
}

