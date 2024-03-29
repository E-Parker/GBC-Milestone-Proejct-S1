using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utility.Utility;

/* This script contains the main system manager which handles instancing for all over manager 
scripts. */

// GetPercentHealth()

public class System_Manger : SingletonObject<System_Manger>{

    const float DefaultSfxVolume = 0.25f;
    const float DefaultMusVolume = 0.1f;
    static string path;
   
    [Serializable] private struct SoundVolumeData{
        public float SfxVolume;
        public float MusVolume;

        public SoundVolumeData(float MusVolume, float SfxVolume){
            this.MusVolume = MusVolume;
            this.SfxVolume = SfxVolume;
        }
    }


    public override void CustomAwake(){
        /* This function gets called with Awake defined in SingletonObject. */

        // housekeeping stuff:
        IsPersistent = true;
        DontDestroyOnLoad(EmptyObject);
        CurrentScene = SceneManager.GetActiveScene();
        path = $"{Application.persistentDataPath}\\Settings.json";
        
    }


    void Start(){
        Initialize();
        LoadSoundVolume();
    }


    void Initialize(){
        /* This method instances manager scripts depending on what the current scene is. */
        
        if (CurrentScene.name == "Start"){

            // Clean up the DontDestroyOnLoad Scene:
            Enemy_Manager.DestroyInstance();
            Ui_Handler.DestroyInstance();
            
            // Make Instances:
            SpriteManager.MakeInstance();
            AudioManager.MakeInstance();
            AudioManager.switchMusic("BattleCalm");

            Debug.Log("Loaded Start Scene.");
            
        }

        if(CurrentScene.name == "Game"){
            
            // Clean up the DontDestroyOnLoad Scene:
            Enemy_Manager.DestroyInstance();
            Ui_Handler.DestroyInstance();

            // Make Instances:
            SpriteManager.MakeInstance();
            AudioManager.MakeInstance();
            Enemy_Manager.MakeInstance();
            Ui_Handler.MakeInstance();

            Debug.Log("Loaded Game Scene.");

        }
    }

    public static void ExitGame(){
        //TODO: Maybe add some functionality to save the game state.
        // Alternatively, this might not be the best idea because some creatures out there just Alt+f4 every program.
        
        SaveSoundVolume();
        Application.Quit();
    }

    public static void ChangeSceneTo(string name){
        /*  Swaps the current scene to the one named "name". */
        Debug.Log($"Now loading {'"'}{name}{'"'} scene.");
        CurrentScene = SceneManager.GetSceneByName(name);
        // Load the scene from name:
        SceneManager.LoadScene(name, LoadSceneMode.Single);
        Instance.Invoke("Initialize", 0.1f);    // This is awful. but i literally cant get this to work any other way.
    }


    public static void RestartScene(){
        /*  This could be better but this is the fastest way i can think of to reload the scene. */
        CurrentScene = SceneManager.GetActiveScene();
        int sceneIndex = CurrentScene.buildIndex;
        SceneManager.UnloadSceneAsync(CurrentScene);
        SceneManager.LoadSceneAsync(sceneIndex);
        Instance.Invoke("Initialize", 0.1f);
    }


    public static void LoadSoundVolume(){
        
        if (File.Exists(path)){
            string json = File.ReadAllText(path);
            SoundVolumeData data = JsonUtility.FromJson<SoundVolumeData>(json);
            AudioManager.SetsfxMasterVolume(data.SfxVolume);
            AudioManager.SetmusicMasterVolume(data.MusVolume);
        }
        else{
            AudioManager.SetsfxMasterVolume(DefaultSfxVolume);
            AudioManager.SetmusicMasterVolume(DefaultMusVolume);
        }
    }

    public static void SaveSoundVolume(){

        SoundVolumeData soundVolumeData = new SoundVolumeData(AudioManager.getmusicMasterVolume(), AudioManager.getsfxMasterVolume());
        string json = JsonUtility.ToJson(soundVolumeData);
        File.WriteAllText(path, json);
    }
}