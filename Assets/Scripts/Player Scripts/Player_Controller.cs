using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public class Player_Controller_Interface : SpriteController{
    /*  This class handles the implementation for the player controller The actual script 
    accessed by Unity instances this class and populates it's fields. */

    // Expected Animation Names:
    public readonly string anim_Pickup =        "Pickup";
    public readonly string anim_AttackSword =   "AttackSword";
    public readonly string anim_AttackFlame =   "AttackFlame";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"AttackSword", "AttackFlame", "Pickup"};

    public Projectile_Handler Projectile;
    private int mana;                   // the maximum mana the player can have.
    private int currentMana = 0;        // the current amount of mana the player has.
    private float manaRate;             // the amount of time it takes for mana to recharge.
    private float manaTimer = 0f;       // timer used to handle changing manaRate.
    private float SwordRange = 0.15f;
    private int SwordDamage = 2;
    private float healthRate;           // the amount of time it takes for health to regenerate.
    private float healthTimer = 0f;     // timer used to handle changing healthRate.
    
    public Player_Controller_Interface(
        float friction, float acceleration, float speed, float healthRate, int Mana, float manaRate,
        Rigidbody rigidbody, Collider collider, Sprite_Animation Animation,  Sprite_Animator Animator, 
        Health_handler Health, Projectile_Handler Projectile)
        :base(friction, acceleration, speed, rigidbody, collider, Health, Animation, Animator){

        this.friction = friction;
        this.acceleration = acceleration;
        this.speed = speed;
        this.Health = Health;
        this.healthRate = healthRate;
        this.mana = Mana;
        this.manaRate = manaRate;

        this.rigidbody = rigidbody;
        this.Animation = Animation;
        this.Animator = Animator;
        this.Projectile = Projectile;

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
        /*  handles raycasting in the direction the player is facing to check for sword hits. */
        
        RaycastHit hit;
        float angle = (SignedAngleFromVector(direction.x, -direction.z, 0, 1) * Mathf.Rad2Deg + 270f) % 360f;
        Vector3 newDirection;
        
        // Check rays from +- 10 degrees.
        for (int i= -10; i < 10; i+=2){
            
            newDirection = new Vector3(Mathf.Cos((angle + i) * Mathf.Deg2Rad), 0, 
                                       Mathf.Sin((angle + i) * Mathf.Deg2Rad));
            Debug.DrawLine(rigidbody.position, rigidbody.position + newDirection);
            if(Physics.Raycast(rigidbody.position ,newDirection, out hit, SwordRange)){
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

        if(Health.GetHealth() != Health.GetMaxHealth()){
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
    [SerializeField] float m_Turnrate = 1f; // determines how much direction can change per frame.

    [Header("Statistics")]
    [SerializeField] int m_Mana = 5;
    [SerializeField] float m_ManaRate = 1f;
    [SerializeField] int m_Health = 5;
    [SerializeField] float m_HealthRate = 2f;

    public Player_Controller_Interface controller; 

    void Start(){

        // Initialize health.
        this.gameObject.GetComponent<Health_handler>().Initialize(m_Health,m_Health);

        // Initialize the interface script. 
        controller = new Player_Controller_Interface(
            m_Friction, m_Acceleration, m_Speed, m_HealthRate, m_Mana, m_ManaRate,
            gameObject.GetComponent<Rigidbody>(),
            gameObject.GetComponent<Collider>(),
            gameObject.GetComponent<Sprite_Animation>(), 
            gameObject.GetComponent<Sprite_Animator>(),
            gameObject.GetComponent<Health_handler>(),
            gameObject.GetComponent<Projectile_Handler>());
        
        controller.SetCurrentMana(m_Mana); // For debuging. 
    }

    void Update(){
        /* Get user input and update character controller. */

        // Fix projectile handler if resetting scene:
        if (controller.Projectile == null){
            controller.Projectile = gameObject.GetComponent<Projectile_Handler>();
        }

        if (!controller.Health.Alive()){
            controller.OnDeath();
            return;
        }   

        controller.RememberLastState();    // Remember the previous state
        controller.unsetAll();             // Clear all states

        // Handle Input:

        // Get the current direction.
        Vector3 newDirection = controller.GetDirecion() * (1f - m_Turnrate);

        // Change direction by keypress * turnrate.
        if (Input.GetKey(m_Up)){
            newDirection += Vector3.forward * m_Turnrate * 1.25f;
            controller.setState(controller.anim_Walk);
        }

        if (Input.GetKey(m_Left)){
            newDirection += Vector3.left * m_Turnrate * 1.25f;
            controller.setState(controller.anim_Walk);
        }

        if (Input.GetKey(m_Right)){
            newDirection += Vector3.right * m_Turnrate * 1.25f;
            controller.setState(controller.anim_Walk);
        }
        
        if (Input.GetKey(m_Down)){
            newDirection += Vector3.back * m_Turnrate * 1.25f;
            controller.setState(controller.anim_Walk);
        }
        // Normalize new direction.
        Vector3.Normalize(newDirection);
        controller.SetDirection(newDirection);

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

