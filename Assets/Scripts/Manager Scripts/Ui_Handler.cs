using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utility.Utility;
using UnityEngine.SceneManagement;

// cool symbols 🗡

public class Ui_Handler : SingletonObject<Ui_Handler>{
    /*  Script for handling UI elements. */
    
    const char HP_Missing = '-';
    const char HP_Has = 'x';

    const char MP_Missing = '-';
    const char MP_Has = 'x';

    public TMP_Text m_ScoreText;
    public TMP_Text m_HPText;
    public TMP_Text m_MPText;
    public GameObject m_GameoverButton;

    private Health_handler pch;
    private Player_Controller pc;
    
    private int health;
    private int maxHealth;

    private int mana;
    private int maxMana;


    void Start(){
        pc = Player.GetComponent<Player_Controller>();
        pch = Player.GetComponent<Health_handler>();
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

