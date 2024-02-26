using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using System.IO;
using static Utility.Utility;


public class Enemy_Manager : SingletonObject<Enemy_Manager>{   

    public static GameObject[] c_Entities;

    // enemy spawn handling structs:

    [Serializable] public struct IEnemySpawn{
        public int ID;  // Stores the index of the entity in the list of possible entities.
        public int Max; // Maximum of this type.
    }

    [Serializable] public struct IEnemyWave{
        public IEnemySpawn[] SpawnID;   // array of IEnemySpawn indices.
        public int level;       // level required to progress to the next wave.
    }

    [Serializable] public struct IEnemyWaveArray{
        public IEnemyWave[] Waves;
    }

    public struct EnemySpawn{
        /*  Struct for the enemy prefab and number of enemies to spawn. */
        public EnemySpawn(int ID, int Max){
            this.maxNumber = Max;
            this.Entity = ID < c_Entities.Length? c_Entities[ID] : null;
            Spawns.Add(this);
        }

        public EnemySpawn(IEnemySpawn Spawn){
            this.maxNumber = Spawn.Max;
            this.Entity = Spawn.ID < c_Entities.Length? c_Entities[Spawn.ID] : null;
            Spawns.Add(this);
        }
        
        public static List<EnemySpawn> Spawns;
        public GameObject Entity { get; }
        public int maxNumber { get; }
    }


    public struct EnemyWave{
        /* Struct for storing waves of enemies with the level requirement and requirement for next wave. */
        public EnemyWave(EnemySpawn[] enemySpawns, int levelReq, int nextLevelReq){
            Spawns = enemySpawns;
            Level = levelReq;       // must be positive integer in terms of the number of points.
            Next = nextLevelReq;    // value of -1 means final wave.
        }
        
        public EnemyWave(IEnemyWave wave, int next = -1){
            Spawns = new EnemySpawn[wave.SpawnID.Length];
            //foreach(int i in wave.SpawnID){ Spawns[i] = EnemySpawn.Spawns[i]; }
            Level = wave.level;    
            Next = next;
        }

        public EnemySpawn[] Spawns;
        public int Level;
        public int Next; 
    }

    // Settings:
    public  float m_SpawnRadius = 2f;      // Radius around camera that enemies spawn.
    public  float m_SpawnRate = 1f;        // Time(Seconds) between spawns.
    public  float m_GracePeriod = 5f;      // Time(Seconds) between waves.
    public  GameObject m_TargetOverride;   // If this value is set, override the default.
    private GameObject target;             // GameObject for where enemies should spawn.
    
    // Score Counters:
    private int currentWaveCounter = 0;    // Number of waves the player has finished.
    private int waveScore = 0;             // Score in the current wave.
    private int score = 0;                 // player's total score.
    
    // Timers:
    private float enemyTimer = 0f;         // timer for spawning enemies.
    private float waveTimer = 0f;          // timer for starting waves.

    // Arrays:
    private IEnemyWave[] Waves;
    private Queue<EnemyWave> WaveQueue;

    private EnemyWave currentWave;
    private EnemySpawn currentType;
    private bool spawning = false;
    private int totalAmount = 0;
    private int totalSpawns = 0;
    private int currentTypeIndex = 0;
    private int currentTypeAmount = 0;

    private List<GameObject> alive = new List<GameObject>();    // list of alive enemies
    private List<GameObject> dead = new List<GameObject>();     // list of dead (marked inactive) enemies.

    void Start(){
        // Initialize enemies:
        
        IEnemySpawn test1;
        test1.Max = 1;
        test1.ID = 0;
        IEnemySpawn test2;
        test2.Max = 1;
        test2.ID = 0;
        IEnemySpawn test3;
        test3.Max = 1;
        test3.ID = 0;
        IEnemySpawn test4;
        test4.Max = 1;
        test4.ID = 0;

        IEnemyWave wave1;
        wave1.level = 5;
        wave1.SpawnID = new IEnemySpawn[4]{test1, test2, test3, test4};
        IEnemyWave wave2;

        wave2.level = 10;
        wave2.SpawnID = new IEnemySpawn[4]{test1, test2, test3, test4};

        IEnemyWaveArray waves;
        waves.Waves = new IEnemyWave[2]{wave1,wave2};

        string test = JsonUtility.ToJson(test1, true);
        Debug.Log(test);

        test = JsonUtility.ToJson(wave1, true);
        Debug.Log(test);
        
        test = JsonUtility.ToJson(waves, true);
        Debug.Log(test);
        

        string json = File.ReadAllText($"{Application.dataPath}/Resources/Data/EnemySpawns.json");
        IEnemyWaveArray newWave = JsonUtility.FromJson<IEnemyWaveArray>(json);
        
        test = JsonUtility.ToJson(newWave, true);
        Debug.Log(test);

        //Waves = LoadArrayFromJson<IEnemyWave>("EnemySpawns", out test);
        //Debug.Log(test);
        InitializeWaves();
    }

    public static void InitializeWaves(){
        /*  This function sorts the waves by level. */

        int iterations = Instance.Waves.Length;
        List<IEnemyWave> SortedWaves = new List<IEnemyWave>();
        Instance.WaveQueue = new Queue<EnemyWave>();
        
        Debug.Log(Instance.Waves);

        // sort waves by level requirement using insertion sort:
        for (int i = 0; i < iterations; i++){

            IEnemyWave smallest = Instance.Waves[i];
            
            foreach(IEnemyWave wave in Instance.Waves){
                Debug.Log(wave);
                if ((!SortedWaves.Contains(wave)) && (wave.level < smallest.level)){
                    smallest = wave;
                }
            }
            // add to sorted list
            SortedWaves.Add(smallest);
        }

        // Generate list of waves:
        for (int i = 0; i < iterations; i++){ 
            IEnemyWave IWave = SortedWaves[i];
            Debug.Log($"wave: {i}, level: {IWave.level}");
            int nextLevel = (iterations-1 != i)?SortedWaves[i+1].level : -1;
            //Debug.Log(nextLevel);
            EnemyWave wave = new EnemyWave(IWave, nextLevel);
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
                    if(currentTypeIndex < currentWave.Spawns.Length){
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
        return $"(Total-Score-{s}-Wave-{Instance.currentWaveCounter}-Score-{ws})\\n{rm}"; //
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

