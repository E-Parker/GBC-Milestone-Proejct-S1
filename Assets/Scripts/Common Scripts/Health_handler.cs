using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Health_handler : MonoBehaviour{

    // Variables:
    [Header("Health Settings")]
    [SerializeField] float Invulnerability = 0.25f;     // Time before character can be hit again:
    public short Current {get; private set; } = 5;     // Current health of the object.
    public ushort maxHealth { get; private set; } = 5;  // Maximum Health the object can have.

    private bool isDying = false;   // private flag for if the object is currently dying.
    
    [HideInInspector]
    public float Percent { get {return (float)Current / (float)maxHealth; } } // Health as a percent of the maximum.
    
    public bool Hit { get; private set; } = false;  // flag for if currently being hit.

    public bool Dying { // public accessor for "isDying" flag.
        get{ return isDying; }
        set{
            if (value){
                AudioManager.PlaySound("Hit_Hurt_Big");
            }
            isDying = value; 
        }
    }

    public bool Alive{  // public accessor for "Alive" flag.
        get{ return Current > 0; }
    }

    private IEnumerator MarkAsInvulnerable(){
        /* Coroutine for marking the object invulnerable for a set time. */
        Hit = true;
        yield return new WaitForSeconds(Invulnerability);
        Hit = false;
    }

    public void Initialize(int newCurrent, int newMax){
        maxHealth = (ushort)newMax;
        Current = (short)newCurrent;
    }

    public void SubHealth(short amount){
        // Check that the object isn't invulnerable.
        if (!Hit){
            AudioManager.PlaySound("Hit_Hurt");     // Play hurt sound.
            Current -= amount;                      // Subtract amount from health.
            StartCoroutine(MarkAsInvulnerable());   // mark as invulnerable until a set time.
        }
    }

    public void AddHealth(short amount){
        Current += amount;
    }
}

