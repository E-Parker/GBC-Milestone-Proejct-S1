using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public class IPlayer_Controller : SpriteController{
    /*  This class handles the implementation for the player controller The actual script 
    accessed by Unity instances this class and populates it's fields. */

    // Expected Animation Names:
    public readonly string anim_Pickup =        "Pickup";
    public readonly string anim_AttackSword =   "AttackSword";
    public readonly string anim_AttackFlame =   "AttackFlame";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"AttackSword", "AttackFlame", "Pickup"};
    public GameObject gameObject;

    public Projectile_Handler Projectile;
    private int mana;                   // the maximum mana the player can have.
    private int currentMana = 0;        // the current amount of mana the player has.
    private float manaRate;             // the amount of time it takes for mana to recharge.
    private float manaTimer = 0f;       // timer used to handle changing manaRate.
    private float SwordRange = 0.15f;
    private short SwordDamage = 2;
    private float healthRate;           // the amount of time it takes for health to regenerate.
    private float healthTimer = 0f;     // timer used to handle changing healthRate.
    
    public IPlayer_Controller(
        float friction, float acceleration, float speed, float turnRate, float healthRate, int Mana, float manaRate,
        GameObject gameObject, Rigidbody rigidbody, Collider collider, Sprite_Animation Animation, 
        Sprite_Animator Animator, Health_handler Health, Projectile_Handler Projectile)
        :base(friction, acceleration, speed, turnRate, rigidbody, collider, Health, Animation, Animator){

        this.friction = friction;
        this.acceleration = acceleration;
        this.speed = speed;
        this.Health = Health;
        this.healthRate = healthRate;
        this.mana = Mana;
        this.manaRate = manaRate;

        this.rigidBody = rigidbody;
        this.Animation = Animation;
        this.Animator = Animator;
        this.Projectile = Projectile;
        this.gameObject = gameObject;

        // Make a dictionary of the states by their animation name.
        states = new Dictionary<string, ushort>();                             
        StatesFromAnimations(Animation.GetNames());
    }

    public void FireProjectile(){
        /*  This function casts a fireball in the direction the player is facing. 
        DUPLICATED IN MAGE CONTROLLER! */

        // Check that the character has enough mana:
        if (currentMana > 0){
            Projectile.On_Fire(direction);
            currentMana--;
        }
        else{ // if not, don't play the casting animation.
            unsetState(anim_AttackFlame);
        }
    }

    public void AttackSword(){
        /*  handles ray-casting in the direction the player is facing to check for sword hits. */
        
        RaycastHit hit;
        float angle = (SignedAngleFromVector(direction.x, -direction.z, 0, 1) * Mathf.Rad2Deg + 270f) % 360f;
        Vector3 newDirection;
        
        // Check rays from +- 10 degrees.
        for (int i= -10; i < 10; i+=2){
            
            newDirection = new Vector3(Mathf.Cos((angle + i) * Mathf.Deg2Rad), 0, 
                                       Mathf.Sin((angle + i) * Mathf.Deg2Rad));
            Debug.DrawLine(position, position + newDirection);
            if(Physics.Raycast(position ,newDirection, out hit, SwordRange)){
                GameObject target = hit.collider.gameObject;

                if (target.GetComponent<Health_handler>() != null){
                    target.GetComponent<Health_handler>().SubHealth(SwordDamage);
                }
            }
        }
    }

    public override void UpdateSpecial(){

        // Update Timers
        if(currentMana != mana){
            manaTimer += Time.deltaTime;
            if (manaTimer > manaRate){
                manaTimer %= manaRate;
                currentMana++;
            }
        }

        // Regenerate Health
        if(Health.Current != Health.maxHealth){
            healthTimer += Time.deltaTime;
            if (healthTimer > healthRate){
                healthTimer %= healthRate;
                Health.AddHealth(1);
            }
        }

        if (CurrentStateIs(anim_AttackFlame)){
            FireProjectile();
        }

        if (CurrentStateIs(anim_AttackSword)){
            AttackSword();
        }
        
        // Check that the current animation is not locking input:
        if (StopMovement.Contains(Animation.GetAnimationName())){
            unsetState("Walk");
            return;
        }
    }

    public int GetMaxMana(){
        /* Returns the maximum mana of the player. */
        return mana;
    }

    public int GetCurrentMana(){
        /* Returns the current Mana of the player. */
        return currentMana;
    }

    public void SetMana(int amount){
        mana = amount;
    }
    public void SetCurrentMana(int amount){
        currentMana = amount;
    }
}

/*================================================================================================*\
|                                   INTERFACING WITH UNITY ENGINE                                  |
\*================================================================================================*/

public class Player_Controller : MonoBehaviour{

    // Variables:
    
    // Define what buttons do what:
    [Header("Input Buttons")]
    [SerializeField] KeyCode m_Up;
    [SerializeField] KeyCode m_Down;
    [SerializeField] KeyCode m_Left;
    [SerializeField] KeyCode m_Right;
    [SerializeField] KeyCode m_Attack;
    [SerializeField] KeyCode m_Attack_special;
    [SerializeField] KeyCode m_Interact;

    [Header("Movement")]
    [SerializeField] float m_Acceleration = 0.5f;
    [SerializeField] float m_Friction = 0.1f;
    [SerializeField] float m_Speed = 5f;
    [SerializeField] float m_TurnRate = 1f; // determines how much direction can change per frame.

    [Header("Statistics")]
    [SerializeField] int m_Mana = 5;
    [SerializeField] float m_ManaRate = 1f;
    [SerializeField] int m_Health = 5;
    [SerializeField] float m_HealthRate = 2f;

    public IPlayer_Controller controller;
    public Health_handler Health;
    private bool initialized = false;

    void OnEnable(){
        if (initialized){
            controller.ResetController();
            StartCoroutine(controller.ApplyTurnRate());
        }
    }

    void Start(){

        // Initialize health.
        Health = gameObject.GetComponent<Health_handler>();
        Health.Initialize(m_Health, m_Health);

        // Initialize the interface script. 
        controller = new IPlayer_Controller(
            m_Friction, m_Acceleration, m_Speed, m_TurnRate, m_HealthRate, m_Mana, m_ManaRate, gameObject,
            gameObject.GetComponent<Rigidbody>(),
            gameObject.GetComponent<Collider>(),
            gameObject.GetComponent<Sprite_Animation>(), 
            gameObject.GetComponent<Sprite_Animator>(),
            gameObject.GetComponent<Health_handler>(),
            gameObject.GetComponent<Projectile_Handler>());
        
        StartCoroutine(controller.ApplyTurnRate());
        controller.SetCurrentMana(m_Mana);
        Player = controller;
        initialized = true;
    }

    void Update(){
        /* Get user input and update character controller. */

        // Fix projectile handler if resetting scene:
        if (controller.Projectile == null){
            controller.Projectile = gameObject.GetComponent<Projectile_Handler>();
        }

        if (!controller.Health.Alive){
            controller.OnDeath();
            return;
        }   

        controller.RememberLastState();    // Remember the previous state
        controller.unsetAll();             // Clear all states

        // Handle Input:

        // Get the current direction.
        Vector3 newDirection = Vector3.zero;

        // Change direction by keypress * turnRate.
        if (Input.GetKey(m_Up)){
            newDirection += Vector3.forward;
            controller.setState(controller.anim_Walk);
        }

        if (Input.GetKey(m_Left)){
            newDirection += Vector3.left;
            controller.setState(controller.anim_Walk);
        }

        if (Input.GetKey(m_Right)){
            newDirection += Vector3.right;
            controller.setState(controller.anim_Walk);
        }
        
        if (Input.GetKey(m_Down)){
            newDirection += Vector3.back;
            controller.setState(controller.anim_Walk);
        }
        
        // Normalize new direction.
        if (newDirection != Vector3.zero){
            Vector3.Normalize(newDirection);
            controller.direction = newDirection;
        }

        // Set states by keypress.
        if(Input.GetKeyDown(m_Attack))          {controller.setState(controller.anim_AttackSword);}
        if(Input.GetKeyDown(m_Attack_special))  {controller.setState(controller.anim_AttackFlame);}
        if(Input.GetKeyDown(m_Interact))        {controller.setState(controller.anim_Pickup);}

        // Update player controller:
        controller.Update();
    }
    
    void FixedUpdate(){
        /* Handle Collisions / movement here. */
        controller.FixedUpdate(controller.CurrentStateIs(controller.anim_Walk));
    }
}

