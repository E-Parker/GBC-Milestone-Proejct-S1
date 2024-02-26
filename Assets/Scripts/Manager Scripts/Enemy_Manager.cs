using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using static Utility.Utility;


[Serializable] public class Enemy_Manager : SingletonObject<Enemy_Manager>{   

    [Serializable] public struct EnemySpawn{
        /*  Struct for the enemy prefab and number of enemies to spawn. */
        public GameObject Entity;
        public int maxNumber;
    }

    [Serializable] public struct EnemyWaveUI{
        /*  Struct for interfacing with Unity's inspector. */
        public List<EnemySpawn> enemySpawns;
        public int level;
    }

    public struct EnemyWave{
        /* Struct for storing waves of enemies with the level requirement and requirement for next wave. */
        public EnemyWave(List<EnemySpawn> enemySpawns, int levelReq, int nextLevelReq){
            Spawns = enemySpawns;
            Level = levelReq;       // must be positive integer in terms of the number of points.
            Next = nextLevelReq;    // value of -1 means final wave.
        }

        public List<EnemySpawn> Spawns;
        public int Level;
        public int Next; 
    }

    [Header("Attack Waves")]
    [SerializeField] List<EnemyWaveUI> m_waves;
    
    [Header("Settings")]
    [SerializeField] GameObject m_targetPrefab;     // player prefab:   
    [SerializeField] float m_SpawnRadius = 1.5f;    // Radius around camera that enemies spawn.
    [SerializeField] float m_SpawnRate = 1f;        // Time(Seconds) between spawns.
    [SerializeField] float m_GracePeriod = 5f;      // Time(Seconds) between waves.
    
    public GameObject target;                       // player gameObject

    private int currentWaveCounter = 0;             // Number of waves the player has finished.
    private int score = 0;                          // player's total score.
    private int waveScore = 0;                      // Score in the current wave.
    private float enemyTimer = 0f;                  // timer for spawning enemies.
    private float waveTimer = 0f;                   // timer for starting waves.

    private Queue<EnemyWave> WaveQueue;
    private EnemyWave currentWave;

    private EnemySpawn currentType;
    private bool spawning = false;
    private int currentTypeIndex = 0;
    private int totalAmount = 0;
    private int totalSpawns = 0;
    private int currentTypeAmount = 0;

    private List<GameObject> alive = new List<GameObject>();    // list of alive enemies
    private List<GameObject> dead = new List<GameObject>();     // list of dead (marked inactive) enemies.

    void Start(){
        // Initialize enemies:
        InitializeWaves();
    }

    public static void InitializeWaves(){
        /*  This function sorts the waves by level. */

        int iterations = Instance.m_waves.Count;
        List<EnemyWaveUI> SortedWaves = new List<EnemyWaveUI>();
        Instance.WaveQueue = new Queue<EnemyWave>();

        // sort waves by level requirement using insertion sort:
        for (int i = 0; i < iterations; i++){

            EnemyWaveUI smallest = Instance.m_waves[i];
            
            foreach(EnemyWaveUI wave in Instance.m_waves){
                if ((!SortedWaves.Contains(wave)) && (wave.level < smallest.level)){
                    smallest = wave;
                }
            }
            // add to sorted list
            SortedWaves.Add(smallest);
        }

        // Generate list of waves:
        for (int i = 0; i < iterations; i++){ 
            EnemyWaveUI waveUI = SortedWaves[i];
            Debug.Log($"wave: {i}, level: {waveUI.level}");
            int nextLevel = (iterations-1 != i)?SortedWaves[i+1].level : -1;
            Debug.Log(nextLevel);
            EnemyWave wave = new EnemyWave(waveUI.enemySpawns, waveUI.level,nextLevel);
            Debug.Log(JsonUtility.ToJson(wave));
            Instance.WaveQueue.Enqueue(wave);
        }

        Instance.currentWave = Instance.WaveQueue.Dequeue();
    }


    private void GenerateEnemies(){
        /*  This function fills out the list of enemies based off the total amount of each type. */
        
        // Clear lists of alive and dead enemies:
        foreach(GameObject enemy in alive){
            Destroy(enemy);
        }
        alive.Clear();

        foreach(GameObject enemy in dead){
            Destroy(enemy);
        }
        dead.Clear();

        // For each type of enemy in the wave instanciate a copy, set active to false.
        foreach(EnemySpawn enemyType in currentWave.Spawns){
            
            // Generate the max number enemys of each type, set inactive and add to the dead list.
            for (int i = 0; i < enemyType.maxNumber; i++){
                // Create new enemy:
                GameObject enemy = Instantiate(enemyType.Entity,transform.position,quaternion.identity);
                
                // Disable enemy and at to list of dead enemies:
                enemy.SetActive(false);
                dead.Add(enemy);
            }   
        }
    }

    private void GenerateEnemiesAsync(){
        /*  This function Generates new enemies over time to even out the load and prevent lag
        spikes. */

        // Initialize variables:
        if (!spawning){
            // Clear lists of alive and dead enemies:
            foreach(GameObject enemy in alive){
                Destroy(enemy);
            }
            alive.Clear();

            foreach(GameObject enemy in dead){
                Destroy(enemy);
            }
            dead.Clear();

            spawning = true;
            currentTypeIndex = 0;
            currentTypeAmount = 0;
            totalAmount = 0;
            totalSpawns = 0;
            foreach (EnemySpawn enemySpawn in currentWave.Spawns){
                totalAmount += enemySpawn.maxNumber;
            }
            currentType = currentWave.Spawns[currentTypeIndex];
        }

        // get the amount of spawns that should have happened, minus the amount that already happened.
        int spawns = (int)(waveTimer / m_GracePeriod * (float)totalAmount) - totalSpawns;
        totalSpawns += spawns;
        Debug.Log($"spawns: {spawns}, totalAmount: {totalAmount}, TotalSpawns: {totalSpawns}");
        if (totalSpawns <= totalAmount){
            for (int i = 0; i < spawns; i++){
                // update the type being spawned:
                if (currentTypeAmount == currentType.maxNumber){
                    currentTypeAmount = 0;
                    currentTypeIndex++;
                    if(currentTypeIndex < currentWave.Spawns.Count){
                        currentType = currentWave.Spawns[currentTypeIndex];
                    }
                    else{
                        spawning = false;
                        break;
                    }    
                }

                // Create new enemy:
                GameObject enemy = Instantiate(currentType.Entity, transform.position, Quaternion.identity);
                enemy.SetActive(false);
                dead.Add(enemy);  
                currentTypeAmount++;
            }
        }
        else{
            spawning = false;
        }
    }


    void Update(){
        /*  Handle spawning new enemies here */

        if (alive == null && dead == null){
            GenerateEnemies();
        }
        
        //  HANDLE SPAWNING NORMALY, STILL FIGHTING CURRENT WAVE:
        if(waveScore < currentWave.Next || currentWave.Next == -1){
            // Update spawn timer:
            enemyTimer += Time.deltaTime;

            // Spawn New Enemies:
            if (enemyTimer > m_SpawnRate){
                enemyTimer = 0f;
                SpawnEnemy();        
            }
        }
        // LEVEL QUOTA REACHED, WAIT FOR PLAYER TO KILL REMAINING ENEMIES, LOAD NEXT WAVE.
        else{ 
            waveScore = currentWave.Next;
            // if no enemies are alive, handle loading next wave. 
            if (alive.Count == 0){
                currentWave = WaveQueue.Dequeue();
                currentWaveCounter++;
                waveTimer = 0;
                waveScore = 0;
            }
        }
        
        // Check if still in grace period.
        if (waveTimer < m_GracePeriod){
            waveTimer += Time.deltaTime;
            GenerateEnemiesAsync();
            return;
        }
        // Otherwise, there are still enemies alive but the wave has ended.
        UpdateLists(); // Update Lists to reflect which enemies are dead
    }


    private Vector3 GetSpawnPosition(){
        /*  Returns a random vector m_SpawnRadius distance from the target. */

        // Get random position on radius:
        Vector3 position = new Vector3(UnityEngine.Random.Range(-1f,1f),
                                    target.transform.position.y,
                                    UnityEngine.Random.Range(-1f,1f));
        
        // Varify position is not zero vector before normalization.
        if (position == Vector3.zero){position = Vector3.down;}
        return Vector3.Normalize(position) * m_SpawnRadius;
    }


    private void UpdateLists(){
        /*  This function checks for any dead enemies, assigning them to the correct list. */
        
        int index = 0;
        
        while (index < alive.Count){

            // Get the current enemy.
            GameObject enemy = alive[index];

            // If not active:
            if (!enemy.activeSelf){
                
                // Change score by the max health the dead enemy had.
                score += enemy.GetComponent<Health_handler>().GetMaxHealth(); 
                waveScore += enemy.GetComponent<Health_handler>().GetMaxHealth();
                
                // Update Lists:
                dead.Add(enemy);
                alive.Remove(enemy);
            }
            else{
                index++;
            }
        }
    }


    private void SpawnEnemy(){
        /*  This function handles spawning a new enemy and adding it to the list of enemies. */

        // Leave early if there are no enemies left to spawn.
        if (dead.Count == 0){
            return;
        }
        
        // Pull a random enemy from the list of dead enemies:
        GameObject enemy = dead[UnityEngine.Random.Range(0,dead.Count)];

        // Get spawn position:
        Vector3 position = GetSpawnPosition();

        // Set enemy position:
        enemy.transform.position = position;
        enemy.GetComponent<Rigidbody>().position = position;
        enemy.SetActive(true);

        // Add to list:
        alive.Add(enemy);
        dead.Remove(enemy);
    }

    // TODO: Stinky formatted text in the enemy manger!! move it to UI.
    public static string GetScoreText(){
        /*  Generates a string that neatly displays the current wave and score for that wave. */
        string ws = (Instance.waveScore != Instance.currentWave.Next)? Instance.waveScore.ToString() : "MAX";
        string rm = (Instance.waveScore != Instance.currentWave.Next)?"" : $"(Foes-Remaining-{Instance.alive.Count})";
        string s = ((Instance.score < 10)? "00" : (Instance.score < 100)? "0" : "") + (Instance.score % 999).ToString();
        return $"(Total-Score-{s}-Wave-{Instance.currentWaveCounter}-Score-{ws})\n{rm}"; //
    }

    public static int TotalEnemies(){
        return Instance.alive.Count + Instance.dead.Count;
    }

    public static int AliveEnemies(){
        return Instance.alive.Count;
    }

    public static int GetScore(){
        return Instance.score;
    }
}

