using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utility.Utility;
using UnityEngine.SceneManagement;

// cool symbols 🗡

public class Ui_Handler : MonoBehaviour{
    /*  Script for handling UI elements. */
    
    const char HP_Missing = '-';
    const char HP_Has = 'x';

    const char MP_Missing = '-';
    const char MP_Has = 'x';

    [Header("Display Text:")]
    [SerializeField] TMP_Text m_ScoreText;
    [SerializeField] TMP_Text m_HPText;
    [SerializeField] TMP_Text m_MPText;
    [SerializeField] GameObject m_GameoverButton;
    
    private GameObject enemyManager;

    private GameObject player;
    private Health_handler pch;
    private Player_Controller pc;
    
    private int health;
    private int maxHealth;

    private int mana;
    private int maxMana;


    void Start(){
        DontDestroyOnLoad(EmptyObject);
        player = GameObject.Find(PlayerObject);
        enemyManager = GameObject.Find("Enemy_Manger");

        pc = player.GetComponent<Player_Controller>();
        pch = player.GetComponent<Health_handler>();
    }


    public void ChangeSceneTo(int index){
        /*  Swaps the current scene to the one named "name". */

        // Check for valid index:
        if(index < 0 || index > SceneManager.sceneCountInBuildSettings){
            Debug.LogError($"Invalid scene index: {index}");
            return;
        }

        // Load the scene from index:
        SceneManager.LoadScene(index);
    }


    public void RestartScene(){
        /*  This could be better but this is the fastest way i can think of to reload the scene. */
        Scene scene = SceneManager.GetActiveScene();
        string sceneName = scene.name;
        SceneManager.UnloadSceneAsync(scene);
        SceneManager.LoadSceneAsync(sceneName);
    }


    void Update(){
        maxHealth = pch.GetMaxHealth();
        maxMana = pc.controller.GetMaxMana();

        // If player is dead, show the respawn button.
        if (pch.GetHealth() > 0){
            m_GameoverButton.SetActive(false);
        }
        else if (!m_GameoverButton.activeSelf){
            m_GameoverButton.SetActive(true);
        }
        
        if(health != pch.GetHealth()){
            health = pch.GetHealth();

            m_HPText.text = "(Health-";

            if (health > 0){
                m_HPText.text += new string(HP_Has, health);
            }

            if ((maxHealth - health) > 0){
                m_HPText.text += new string(HP_Missing, Mathf.Abs(maxHealth - health));
            }
            m_HPText.text += ")";
        }

        if(mana != pc.controller.GetCurrentMana()){
            mana = pc.controller.GetCurrentMana();
            
            m_MPText.text = "(Mana-";
            
            if (mana != 0){
                m_MPText.text += new string(MP_Has, mana);
            }
            
            if ((maxMana - mana) != 0){
                m_MPText.text += new string(MP_Missing, Mathf.Abs(maxMana - mana));
            } 
            m_MPText.text += ")";
        }
        
        m_ScoreText.text = Enemy_Manager.GetScoreText();
        //m_DebugText.text = Utility.ushortBitsToString(pc.controller.state);
    }
}

