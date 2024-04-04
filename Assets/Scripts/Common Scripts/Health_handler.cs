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
        return isAlive;
    }

    public bool IsDying(){
        return isDying;
    } 

    public void SetDying(){
        AudioManager.PlaySound("Hit_Hurt_Big");
        isDying = true;
    }

    public void SetHealth(int amount){
        health = amount;    // I intentionally let this be any value to alow for overheal.
    }

    public void SubHealth(int amount){

        if (amount < 0){
            health -= amount;
            health = Mathf.Clamp(health, 0, maxHealth);
            return;
        }

        if (!IsHit()){
            AudioManager.PlaySound("Hit_Hurt");
            health -= amount;
            invulnTimer = 0f;
        }
    }

    public void AddHealth(int amount){
        health += amount;
        health = health > maxHealth? maxHealth: health; 
    }

    public bool IsHit(){
        /* Returns true if character is in InvulnTime. */
        return invulnTimer < invulnTime;
    }
}

