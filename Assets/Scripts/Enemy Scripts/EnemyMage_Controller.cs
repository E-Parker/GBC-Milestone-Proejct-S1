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
    
    [Header("Item Drops")]
    [SerializeField] GameObject DropOnDeath;
    [SerializeField] float DropRate = 0.25f;

    // Expected Animation Names:
    public readonly string anim_Attack = "Attack";

    // Animations that halt movement:
    private readonly string[] StopMovement = new string[]{"Attack"};

    public Player_Controller Target;
    private Projectile_Handler Projectile;
    private Pathfinder pathfinder;
    private float timeSincePathChange;
    private Stack<Node> path;
    private Stack<Node> Path{
        get{ 
            return path; 
        }
        
        set{
            if(timeSincePathChange > 0.25f){
                timeSincePathChange = 0.0f;
                path = value;
                currentNode = null;
            }
        }
    }
    private Node currentNode;
    Vector3 directionToNode;
    //private Color debugColor = Color.black;

    private int mana;                   // the maximum mana the player can have.
    private int currentMana = 0;        // the current amount of mana the player has.
    private float manaTimer = 0f;       // timer used to handle changing manaRate.
    private float ActionTimer;          // Tracks the amount of time since last action taken.

    private Vector3 LastTargetPosition;         // stores the last position the target was at.
    private Vector3 PredictedTargetPosition;    // Stores the predicted position of the target.

    public override void CustomStart(){
   
        Projectile = GetComponent<Projectile_Handler>();
        pathfinder = GetComponent<Pathfinder>();

        // Make a dictionary of the states by their animation name.
        states = new Dictionary<string, ushort>();                             
        StatesFromAnimations(Animation.GetNames());

        ActionTimer = 0f;
        mana = m_Mana;
        friction = m_Friction;
        acceleration = m_Acceleration;
        speed = m_Speed * 1.5f;
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
        if (Target == null){
            Target = Player_Controller.Instance;
            LastTargetPosition = Target.position;
            PredictedTargetPosition = Target.position;
        }
    }

    public void FireProjectile(){
        /*  This function casts a fireball towards the player. */
        
        // Check that the character has enough mana:
        if (currentMana > 0){
            Debug.DrawRay(position + Vector3.up, direction, Color.green,0.25f);
            Projectile.On_Fire(direction);
            currentMana--;
        }
        else{ // if not, don't play the casting animation.
            unsetState(anim_Attack);
        }
    }

    public override void UpdateSpecial(){
        
        //Debug.DrawRay(position, Vector3.up, debugColor);

        SetTarget();            // Check for missing target:
        RememberLastState();    // Remember the previous state
        UpdateAi();             // Update AI code with new predicted target location.
        updatePathfinding();

        timeSincePathChange += Time.deltaTime; //TODO: replace this hack with something better.
        
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
        }

        // Check for attack:
        if (CurrentStateIs(anim_Attack))
            FireProjectile();

        // Stupid workaround to keep enemies on the ground.
        transform.position = new Vector3(transform.position.x, 0.06f, transform.position.z);  
    
    }

    public void updatePathfinding(){

        if(Path == null){
            return;
        }

        if (Path.Count != 0){

            if (currentNode == null || Vector3.SqrMagnitude(directionToNode) < (Node.nodeRadius * Node.nodeRadius)){
                currentNode = path.Pop();
            }

            directionToNode = position - currentNode.worldPosition;
            direction = -directionToNode.normalized;
            setState(anim_Walk);
            return;
        }

        path = null;
    }
    
    public void UpdateAi(){
        
        // Check if enough time as passed to take an action:
        if (!ActionOpertunityCheck()){
            return;
        }

        unsetAll();                             // Clear all states
        Vector3 RelativeTargetPosition = position - Target.position;
        PredictedTargetPosition = Target.position + (Target.direction * m_Overshoot * 0.05f);
        LastTargetPosition = Target.position;   // Store position for next update predictions.
             
        Debug.DrawLine(Target.position, PredictedTargetPosition, Color.black, 0.25f);
        Debug.DrawLine(Target.position, position, Color.red, 0.25f);

        float relativeSqrDistance = Vector3.SqrMagnitude(RelativeTargetPosition); 

        // Process AI:

        // If the enemy is really far away, target a random position.
        if (relativeSqrDistance > MaxEnemySqrDistance){
            //debugColor = Color.blue;
            Wander();
            return;
        }

        // If the enemy is too far away walk towards the target.
        if (relativeSqrDistance > m_MaxDistance * m_MaxDistance){
            //debugColor = Color.green;
            SeekTarget();
            return;
        }

        // If the target is too close, blindly run in the opposite direction. 
        if(relativeSqrDistance < (m_MinDistance * m_MinDistance)){
            //debugColor = Color.yellow;
            path = null;
            direction = RelativeTargetPosition.normalized;
            setState(anim_Walk);
            return;
        }

        // Randomly chose to try to find a better position, or attack.
        if(UnityEngine.Random.Range(0.0f, 1.0f) > m_Agression){
            //debugColor = Color.gray;
            Reposition();
        }
        else{
            Attack(PredictedTargetPosition);
            //debugColor = Color.cyan;
        }
    }

    private void Reposition(){
        // Search for a better position.
        SeekTarget();   // Was going to add LOS, doesn't work don't have time.
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

    private void Attack(Vector3 PredictedTargetPosition){
        /* this function attacks in the predicted direction of the player. */

        if (currentMana > 0){
            direction = -PredictedTargetPosition.normalized;
            setState(anim_Attack);
        }
    }

    private void PathToTarget(Vector3 target){
        /* this function starts pathfinding towards a target. */
        Path = pathfinder.FindPath(position, target);        
    }
    
    private void EscapeTarget(Vector3 target, float radius = 0.5f){
        /* This function paths to a safe location away from the target. */
        Node best = Pathfinder.FindBestNode(position, target, Pathfinder.LowestCost, radius);
        PathToTarget(best.worldPosition);
    }

    private void SeekTarget(){
        /* Walk towards the best node around the target. */
        //Debug.Log(Target.position);

        Vector3 random = new Vector3(
            UnityEngine.Random.Range(-1.0f, 1.0f), 0.0f, 
            UnityEngine.Random.Range(-1.0f, 1.0f)).normalized * m_MinDistance;

        PathToTarget(random + Target.position);
    }

    private void Wander(){   
        /* Randomly walk to a target position. */
        Vector3 wander = new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0.0f, 
                                     UnityEngine.Random.Range(-1.0f, 1.0f));
        
        PathToTarget(wander.normalized);
    }
}

