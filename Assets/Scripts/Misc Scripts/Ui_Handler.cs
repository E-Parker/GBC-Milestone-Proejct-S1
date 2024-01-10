using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utility.Utility;
using UnityEngine.SceneManagement;

// cool symbols 🗡

public class Ui_Handler : MonoBehaviour{
    /*  Script for handling UI elements. */
    
    const char HP_Missing = 'x';
    const char HP_Has = '-';

    const char MP_Missing = 'x';
    const char MP_Has = '-';

    [Header("Display Text:")]
    [SerializeField] TMP_Text m_ScoreText;
    [SerializeField] TMP_Text m_HPText;
    [SerializeField] TMP_Text m_MPText;
    [SerializeField] GameObject m_GameoverButton;

    // I sadly couldn't get this to work in time... Really disapointing.

    //[Header("Music Track Cuves")]
    //[SerializeField] AnimationCurve m_Track_1;
    //[SerializeField] AnimationCurve m_Track_2;
    //[SerializeField] AnimationCurve m_Track_3;
    //[SerializeField] AnimationCurve m_Track_4;

    private GameObject player;
    private GameObject enemyManager;

    private Health_handler pch;
    private Player_Controller pc;
    private Enemy_Manager em;

    private int health;
    private int maxHealth;

    private int mana;
    private int maxMana;

    private float action;
    private float lastAction;

    void Start(){
        
        player = GameObject.Find(PlayerObject);
        enemyManager = GameObject.Find("Enemy_Manger");

        pc = player.GetComponent<Player_Controller>();
        pch = player.GetComponent<Health_handler>();
        em = enemyManager.GetComponent<Enemy_Manager>();

    }

    void UpdateMusic(){
        /*  The UI manager already has access to most of the parameters so handle dynamic music here. */

        action = 0.25f - (pch.GetHealth() / pch.GetMaxHealth() * 0.25f) + (0.75f * (em.AliveEnemies() / em.TotalEnemies()));

        if (action == lastAction){
            return;
        }
        Debug.Log(action);
        lastAction = action;

        //AudioManager.SetMusicTrackVolume(0,m_Track_1.Evaluate(action));
        //AudioManager.SetMusicTrackVolume(1,m_Track_2.Evaluate(action));
        //AudioManager.SetMusicTrackVolume(2,m_Track_3.Evaluate(action));
        //AudioManager.SetMusicTrackVolume(3,m_Track_4.Evaluate(action));
    }

    public void RestartScene(){
        /*  This could be better but this is the fastest way i can think of to reload the scene. */
        Scene scene = SceneManager.GetActiveScene();
        string sceneName = scene.name;
        SceneManager.UnloadSceneAsync(scene);
        SceneManager.LoadSceneAsync(sceneName);
    }


    void Update(){
        //UpdateMusic();
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
        m_ScoreText.text = em.GetScoreText();
        //m_DebugText.text = Utility.ushortBitsToString(pc.controller.state);
    }
}

