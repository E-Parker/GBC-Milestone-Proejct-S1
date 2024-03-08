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


    public override void CustomAwake(){
        /* This function gets called with Awake defined in SingletonObject. */

        // housekeeping stuff:
        IsPersistent = true;
        CurrentScene = SceneManager.GetActiveScene();
        DontDestroyOnLoad(EmptyObject);
        
    }


    void Start(){
        Initialize();
        AudioManager.PlayMusic("Tittle");
    }


    void Initialize(){
        /* This method instances manager scripts depending on what the current scene is. */
        
        if (CurrentScene.name == "Start"){
            
            // Destroy old / unused objects:
            Enemy_Manager.DestroyInstance();
            Ui_Handler.DestroyInstance();
            
            // Make Instances:
            AudioManager.MakeInstance();
            AudioManager.IsPersistent = true;

        }

        if(CurrentScene.name == "Game"){

            Debug.Log("Loaded Game Scene.");

            // Destroy old / unused objects:
            Enemy_Manager.DestroyInstance();
            Ui_Handler.DestroyInstance();

            // Make Instances:
            Enemy_Manager.MakeInstance();
            Ui_Handler.MakeInstance();
            AudioManager.MakeInstance();

        }
    }

    public static void ExitGame(){
        //TODO: Maybe add some functionality to save the game state.
        // Alternatively, this might not be the best idea because some creatures out there just Alt+f4 every program.
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
}