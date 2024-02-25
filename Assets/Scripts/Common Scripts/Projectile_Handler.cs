using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Projectile_Handler : MonoBehaviour{

    // Variables:
    [SerializeField] GameObject projectile_prefab;  // prefab object of projectile. must have an animator.
    [SerializeField] float velocity = 0.5f;         // amount of velocity added to the projectile when fired.
    [SerializeField] float m_Randomness = 1f;       // Random speed at direction amount.


    public void On_Fire(Vector3 direction){
        /* Create a new projectile and fire it a direction. */
        GameObject projectile = Instantiate(projectile_prefab, transform.position,
                                                               transform.rotation); 
        // get the handler script from the projectile,
        Projectile_Controller controller = projectile.GetComponent<Projectile_Controller>();
        
        // create random offset to the velocity:
        Vector3 random = new Vector3(Random.Range(-m_Randomness,m_Randomness),
                                     0,//Random.Range(-m_Randomness,m_Randomness),
                                     Random.Range(-m_Randomness,m_Randomness));

        // Change set up direction by random offset:
        direction = -(direction + (random * 0.1f)) * velocity;
        
        // Initialize controller:
        controller.Setvalues(direction, this.transform.gameObject);
    }
}

