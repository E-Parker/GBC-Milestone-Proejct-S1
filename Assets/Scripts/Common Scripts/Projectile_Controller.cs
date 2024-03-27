using System;
using UnityEngine;
using static Utility.Utility;


public class Projectile_Controller : MonoBehaviour{  
    // Constants:

    // This enum is for fall off options. using interpolation curves.
    public enum FalloffOptions{
        [InspectorName("Linear Falloff")]               Linear,
        [InspectorName("Smooth Falloff (Quadratic)")]   Quadratic,
        [InspectorName("Smooth Falloff (Cosine)")]      Cosine,
        [InspectorName("Exponential Falloff")]          Exponent,
        [InspectorName("Smooth Exponential Falloff")]   ExponentCosine,
        [InspectorName("Boomerang Falloff")]            Boomerang,      //'Stralia mate!
    }
    
    public enum InterpDirection{
        [InspectorName("From Start to End")]to_from,
        [InspectorName("From End to Start")]from_to
    }
    
    public enum Lighting{
        [InspectorName("No lighting")]no_light,
        [InspectorName("Point light on projectile")]lighting1,
        [InspectorName("Point light on caster")]lighting2,
        [InspectorName("Point lights on both")]lighting3,
    }

    // Expected animation names:
    private const string OnSpawn = "Spawn";
    private const string Idle = "Idle";
    private const string OnDestroy = "Destruct";

    // Variables:
    
    [Header("Falloff")]
    [SerializeField] FalloffOptions falloff;            // Serialize the enum field
    
    [Header("Falloff Settings")]
    [SerializeField] InterpDirection falloffDirection;  // Direction to interpolate to / from.

    // Specific to CosExponentLerp
    [SerializeField] float m_CosineExponent = 0.8f;

    // Specific to ExponentLerp
    [SerializeField] float m_Exponent = 0.8f;           // Exponent specific to exponent cosine.

    // Specific to BoomerangLerp
    [SerializeField] float m_Smoothness = 0.2f;         // Amount of easing. 

    [Header("Lighting")]
    [SerializeField] Lighting lighting;

    // Lighting on projectile specific:
    [SerializeField] Color m_Projectile_Color;
    [SerializeField] float m_Projectile_Intensity = 2.5f;
    [SerializeField] float m_Projectile_Range = 0.25f;

    //Lighting on caster specific:
    [SerializeField] Color m_Caster_Color;
    [SerializeField] float m_Caster_Intensity = 1.5f;
    [SerializeField] float m_Caster_Range = 0.25f;
    
    [Header("Settings")]
    [SerializeField] float m_Lifetime = 10;             // Time in seconds before self destruct.
    [SerializeField] public int m_Damage = 1;           // Amount of damage dealt on collision.
    [SerializeField] string m_Sfx_cast = "Cast_Magic";
    [SerializeField] string m_Sfx_Hit = "Metallic_Hit";

    private Func<Vector3, Vector3, float, float, Vector3> interpolationType;
    private bool interp;
    
    private Vector3 direction;                  // Combined initial velocity and direction.
    private Vector3 position;                       
    private Vector3 startpos;                   // position to travel from
    private Vector3 endpos;                     // position to end up at from
    private Sprite_Animation Animation;
    
    private Light p_light;                      // Projectile light holder
    private Light caster_light;                 // Caster light holder
    
    private GameObject caster_gameObject;
    private GameObject Caster;
    private float currentTime;
    private float currentFTime;                 // Ratio of currentTime to totalTime.
    private float timeToDestruct;               // Time needed for destruction animation to play.
    private float timeToCast;                   // Time needed for spawn animation to play.
    private float totalTime;
    private bool isDestructing = false;


    void Start(){
        // Initialize Values:
        interp = falloffDirection == InterpDirection.to_from;

        // Set interpolation function:
        switch(falloff){
            case FalloffOptions.Linear:         interpolationType = Lerp; break;
            case FalloffOptions.Quadratic:      interpolationType = smoothLerp; break;
            case FalloffOptions.Cosine:         interpolationType = CosLerp; break;
            case FalloffOptions.Exponent:       interpolationType = ExponentLerp; break;
            case FalloffOptions.ExponentCosine: interpolationType = CosExponentLerp; break;
            case FalloffOptions.Boomerang:      interpolationType = BoomerangLerp; break;
        }

        // Set position / start and end points.
        position = transform.position;
        startpos = transform.position;
        endpos = startpos + (direction * m_Lifetime);

        // Get Animation component:
        Animation = GetComponent<Sprite_Animation>();
        
        // Get Animation components:
        timeToDestruct = Animation.GetAnimationLength(OnDestroy);
        timeToCast = Animation.GetAnimationLength(OnSpawn);
        totalTime = timeToCast + m_Lifetime + timeToDestruct;
        
        // Queue up any animations:
        Animation.Play(OnSpawn); // Start spawn in animation.
        Animation.Play(Idle);    // Queue the idle animation to play after spawn animation.
        AudioManager.PlaySound(m_Sfx_cast);
    }

    public void Setvalues(Vector3 newDirection, GameObject caster){
        /*  This function is called right after instantiating the projectile, to set parameters 
        of the projectile. */
        
        // Set up point lights:
        switch(lighting){
            case Lighting.lighting1: 
                InstanceProjectileLight(); 
                break;
            
            case Lighting.lighting2: 
                InstanceCasterLight();
                Caster = caster;
                caster_gameObject.transform.parent = Caster.transform;
                break;
            
            case Lighting.lighting3: 
                InstanceProjectileLight();                  
                InstanceCasterLight();
                Caster = caster;
                caster_gameObject.transform.parent = Caster.transform;
                break;
        }

        // Set Direction:
        this.direction = newDirection;
        this.endpos = startpos+(direction * m_Lifetime);
    }


    private void InstanceProjectileLight(){
        /*  This function instances a point light with no shadows on the projectile. */
        p_light = gameObject.AddComponent<Light>();
        p_light.shadows =   LightShadows.None;
        p_light.color =     m_Projectile_Color;
        p_light.range =     m_Projectile_Range;
        p_light.intensity = m_Projectile_Intensity;
    }


    private void InstanceCasterLight(){
        /*  This function instances a point light with no shadows on the caster of the projectile. */     

        // Instance the object:
        caster_gameObject = Instantiate(EmptyObject, transform.position, Quaternion.identity);
        caster_gameObject.name = "Caster_Light";
        
        // Set lighting:
        caster_light = caster_gameObject.AddComponent<Light>();
        caster_light.shadows = LightShadows.None;
        caster_light.color = m_Caster_Color;
        caster_light.range = m_Caster_Range;
        caster_light.intensity = m_Caster_Intensity;
    }


    void Update(){
        // Update current time:
        currentTime += Time.deltaTime;

        // Self-destruct after total lifetime has elapsed. leave early.
        if (currentTime > totalTime){    
            Destroy(transform.gameObject);
            return;
        }

        // If still casting:
        if (caster_gameObject != null){
            if (currentTime < timeToCast + 1f){
                float castFTime = 1f - currentTime / (timeToCast + 1f);
                caster_light.intensity =  ExponentLerp(0f,m_Caster_Intensity, castFTime, 0.75f);
                caster_light.intensity =  ExponentLerp(0f,m_Caster_Range,     castFTime, 0.75f);
            }
            // If the caster light still exists, remove it.
            else{
                Destroy(caster_gameObject);
            }
        }

        // If not casting but still alive:
        if (currentTime < timeToCast + m_Lifetime){
            // Get the ratio of currentTime to totalTime.
            currentFTime = currentTime / totalTime;
        }
        // Otherwise not alive, approximate slowing by updating currentFTime independently of the actual time.
        else{
            currentFTime += Time.deltaTime / totalTime * 0.25f;
            // Nothing *should* break if currentFTime is greater than 1, but do this just in case.
            currentFTime = Mathf.Min(currentFTime, 1f);     
        }
        
        // Set light intensity:
        if (p_light != null){
            p_light.intensity = CosLerp(m_Projectile_Intensity, 0f, 1f-currentFTime);
            p_light.range = CosLerp(m_Projectile_Range, 0f, 1f-currentFTime);
        }

        // Play de-spawn animation and remove self from scene:
        if (!isDestructing && (currentTime > timeToCast + m_Lifetime)){
            Animation.Stop();           // Stop idle because we don't want to return to it after.
            Animation.Play(OnDestroy);  // Play animation.
            isDestructing = true;       // Set isDestructing flag.
        }

        // Change position by using the interpolation function.
        position = interpolationType(startpos, endpos, interp?currentFTime:1f-currentFTime, m_Exponent);

        // Update transform to rounded version of position. 
        transform.position = TransformToPixels(position);
    }


    void OnTriggerEnter(Collider col){   
        /*  Handle projectiles colliding with things. */
        
        // Variables:
        GameObject collision = col.gameObject;
        Health_handler health = collision.GetComponent<Health_handler>();

        // If the projectile collides with something marked to destroy it, self destruct.
        if (collision.tag == "ProjectileDestroy"){  // Change current time to destruct early.
            currentTime = timeToCast + m_Lifetime;
            AudioManager.PlaySound(m_Sfx_Hit);
            return;
        }

        // Check collision is with another projectile, ignore collision.
        if (collision.GetComponent<Projectile_Controller>() != null){
            return;
        }

        // If the collision does not have a health component, ignore collision.
        if (health == null){
            return;
        }
        
        // If it's dead, ignore the collision.
        if (!health.Alive()){
            return;
        }

        // If colliding with something that is not the same tag as itself:
        if (collision.tag != this.gameObject.tag){
            currentTime = timeToCast + m_Lifetime;   // Change current time to des struct early.
            AudioManager.PlaySound(m_Sfx_Hit);
            health.SubHealth(m_Damage);              // Apply damage.   
        }
    }
}
