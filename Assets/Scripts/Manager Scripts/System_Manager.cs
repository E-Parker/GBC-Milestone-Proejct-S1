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
        DontDestroyOnLoad(EmptyObject);
        
    }


    void Start(){
        Initialize();
    }


    void Initialize(){
        /* This method instances manager scripts depending on what the current scene is. */

        Debug.Log(SceneManager.GetActiveScene().name);

        switch(SceneManager.GetActiveScene().name){
            
            case "Start":
                // Make Instances:
                AudioManager.MakeInstance();
                AudioManager.IsPersistent = true;
                AudioManager.PlayMusic("Tittle");
                
                // Destroy old / unused objects:
                Enemy_Manager.DestroyInstance();
                Ui_Handler.DestroyInstance();

                break;

            case "Game":
                // Make Instances:
                AudioManager.MakeInstance();
                Enemy_Manager.MakeInstance();
                Ui_Handler.MakeInstance();
                break;
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

        // Load the scene from name:
        SceneManager.LoadSceneAsync(name);
    }


    public static void RestartScene(){
        /*  This could be better but this is the fastest way i can think of to reload the scene. */
        Scene scene = SceneManager.GetActiveScene();
        int sceneIndex = scene.buildIndex;
        SceneManager.UnloadSceneAsync(scene);
        SceneManager.LoadSceneAsync(sceneIndex);
    }
}