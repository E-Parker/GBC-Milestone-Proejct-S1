using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public class Player_Controller : SpriteController, IReceiver{

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
    [SerializeField] int m_Mana = 5;        // the maximum mana the player can have.
    [SerializeField] float m_ManaRate = 1f; // the amount of time it takes for mana to recharge.
    [SerializeField] int m_Health = 5;      // maximum health the player can have.
    
    // Expected Animation Names:
    public readonly string anim_Pickup = "Pickup";
    public readonly string anim_AttackSword = "AttackSword";
    public readonly string anim_AttackFlame = "AttackFlame";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"AttackSword", "AttackFlame", "Pickup"};

    // Properties:
    static public Player_Controller Instance{ get; private set; }
    public List<IObserver> Observers { get; private set; }
    public Projectile_Handler Projectile {get; private set;}
    
    private int currentMana = 0;        // the current amount of mana the player has.
    private int mana;
    private float manaTimer = 0f;       // timer used to handle changing m_ManaRate.
    private float SwordRange = 0.15f;
    private int SwordDamage = 2;

    // Initialization:

    void Awake(){
        Instance = this;
    }

    public override void CustomStart(){
        Projectile = GetComponent<Projectile_Handler>();
        Health.Initialize(m_Health, m_Health);
        SetCurrentMana(m_Mana);

        states = new Dictionary<string, ushort>();                             
        StatesFromAnimations(Animation.GetNames());

        Observers = new();
        this.AddObserver(new AchievementObserver());

        friction = m_Friction;
        acceleration = m_Acceleration;
        speed = m_Speed;

    }

    public override void UpdateSpecial(){

        if (!Health.Alive()){
            OnDeath();
            return;
        }   

        RememberLastState();    // Remember the previous state
        unsetAll();             // Clear all states

        // Handle Input:

        // Get the current direction.
        Vector3 newDirection = direction * (1f - m_Turnrate);

        // Change direction by keypress * turnrate.
        if (Input.GetKey(m_Up)){
            newDirection += Vector3.forward * m_Turnrate * 1.25f;
            setState(anim_Walk);
        }

        if (Input.GetKey(m_Left)){
            newDirection += Vector3.left * m_Turnrate * 1.25f;
            setState(anim_Walk);
        }

        if (Input.GetKey(m_Right)){
            newDirection += Vector3.right * m_Turnrate * 1.25f;
            setState(anim_Walk);
        }
        
        if (Input.GetKey(m_Down)){
            newDirection += Vector3.back * m_Turnrate * 1.25f;
            setState(anim_Walk);
        }

        direction = newDirection.normalized;

        // Set states by keypress.
        if(Input.GetKeyDown(m_Attack))          {setState(anim_AttackSword);}
        if(Input.GetKeyDown(m_Attack_special))  {setState(anim_AttackFlame);}
        if(Input.GetKeyDown(m_Interact))        {setState(anim_Pickup);}

        // Update Observers:
        if(CurrentStateIs(anim_Walk)){
            this.NotifyObservers(Event.PlayerMoved);
        }

        if (CurrentStateIs(anim_AttackFlame)){
            FireProjectile();
        }

        if (CurrentStateIs(anim_AttackSword)){
            AttackSword();
        }

        // Update Timers
        if(currentMana < m_Mana){
            manaTimer += Time.deltaTime;
            if (manaTimer > m_ManaRate){
                manaTimer %= m_ManaRate;
                currentMana++;
            }
        }
        
        // Check that the current animation is not locking input:
        if (StopMovement.Contains(Animation.GetAnimationName())){
            unsetState("Walk");
        }
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
        float angle = ((AngleFromVector(direction.x, -direction.z, 0, 1) * Mathf.Rad2Deg) - 90f) % 360f;
        Vector3 newDirection;
        Health_handler targetHealth;
        
        // Check rays from +- 10 degrees.
        for (int i= -10; i < 10; i+=2){
            
            newDirection = new Vector3(Mathf.Cos((angle + i) * Mathf.Deg2Rad), EyeLevel, 
                                       Mathf.Sin((angle + i) * Mathf.Deg2Rad));
            
            if(Physics.Raycast(position ,newDirection, out hit, SwordRange)){
                targetHealth = hit.collider.gameObject.GetComponent<Health_handler>();
                if (targetHealth != null){
                    targetHealth.SubHealth(SwordDamage);
                }
            }
        }
    }

    public int GetMaxMana(){
        /* Returns the maximum mana of the player. */
        return m_Mana;
    }

    public int GetCurrentMana(){
        /* Returns the current Mana of the player. */
        return currentMana;
    }


    public void SetCurrentMana(int amount){
        currentMana = amount;
    }
    
    public float GetPercentHealth(){
        return (float)Health.GetHealth() / (float)Health.GetMaxHealth();
    }
}

