using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Health_handler : MonoBehaviour{
    // This function handles health.

    // Variables
    int health;
    int maxHealth;
    [SerializeField] float invulnTime = 0.25f;  // Time before character can be hit again:
    float invulnTimer;
    bool isAlive;
    bool isDying;

    public void Initialize(int health, int maxHealth){
        // Initialize Values.
        isDying = false;
        isAlive = true;
        invulnTimer = invulnTime;
        this.health = health;
        this.maxHealth = maxHealth;
    }

    void Update(){
        // Check that health has is zero:
        isAlive = health > 0;
        
        // Update timer.
        if (invulnTimer < invulnTime){
            invulnTimer += Time.deltaTime;
        }
    }

    public int GetHealth(){
        return health;
    }

    public int GetMaxHealth(){
        return maxHealth;
    }

    public bool Alive(){
        return this.isAlive;
    }

    public bool IsDying(){
        return this.isDying;
    } 

    public void SetDying(){
        AudioManager.PlaySound("Hit_Hurt_Big");
        this.isDying = true;
    }

    public void SetHealth(int amount){
        this.health = amount;    // I intentionally let this be any value to alow for overheal.
    }

    public void SubHealth(int amount){

        if (amount < 0){
            this.health -= amount;
            this.health = Mathf.Clamp(health, 0, maxHealth);
            return;
        }

        if (!IsHit()){
            AudioManager.PlaySound("Hit_Hurt");
            this.health -= amount;
            invulnTimer = 0f;
        }
    }

    public void AddHealth(int amount){
        this.health += amount;
    }

    public bool IsHit(){
        /* Returns true if character is in InvulnTime. */
        return invulnTimer < invulnTime;
    }
}

