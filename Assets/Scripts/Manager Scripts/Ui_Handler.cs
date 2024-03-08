using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utility.Utility;

// cool symbols 🗡

public class Ui_Handler : SingletonObject<Ui_Handler>{
    /*  Script for handling UI elements. */
    
    const char HP_Missing = '-';
    const char HP_Has = 'x';

    const char MP_Missing = '-';
    const char MP_Has = 'x';

    private TMP_Text ScoreText = null;
    private TMP_Text HPText = null;
    private TMP_Text MPText = null;
    private GameObject GameoverButton = null;

    private int health;
    private int healthDiff;
    private int mana;
    private int manaDiff;


    void Start(){
        gameObject.layer = 5;
        ScoreText = GameObject.FindWithTag("GameUI_Score").GetComponent<TMP_Text>();
        HPText = GameObject.FindWithTag("GameUI_Health").GetComponent<TMP_Text>();
        MPText = GameObject.FindWithTag("GameUI_Mana").GetComponent<TMP_Text>();
        GameoverButton = GameObject.FindWithTag("GameUI_GameOverButton");
    }
    

    void Update(){
        // If player is dead, show the respawn button.
        if (Player_Controller.Instance.GetHealth() > 0){
            GameoverButton.SetActive(false);
        }
        else if (!GameoverButton.activeSelf){
            GameoverButton.SetActive(true);
        }
        
        if(health != Player_Controller.Instance.GetHealth()){
            health = Player_Controller.Instance.GetHealth();
            healthDiff = Player_Controller.Instance.controller.GetMaxHealth() - health;

            HPText.text = "(Health-";

            if (health > 0){
                HPText.text += new string(HP_Has, health);
            }
            
            if (healthDiff > 0){
                HPText.text += new string(HP_Missing, healthDiff);
            }
            HPText.text += ")";
        }

        if(mana != Player_Controller.Instance.controller.GetCurrentMana()){
            mana = Player_Controller.Instance.controller.GetCurrentMana();
            manaDiff = Player_Controller.Instance.controller.GetMaxMana() - mana;

            MPText.text = "(Mana-";
            
            if (mana > 0){
                MPText.text += new string(MP_Has, mana);
            }
            
            if (manaDiff > 0){
                MPText.text += new string(MP_Missing, manaDiff);
            } 
            MPText.text += ")";
        }

        ScoreText.text = Enemy_Manager.GetScoreText();
        //DebugText.text = Utility.ushortBitsToString(Player_Controller.Instance.controller.state);
    }
}

