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

    private TMP_Text ScoreText;
    private TMP_Text HPText;
    private TMP_Text MPText;
    private GameObject GameoverButton;
    private GameObject UI_Parent;

    private int health;
    private int healthDiff;
    private int mana;
    private int manaDiff;

    void Start(){
        // This is a dumb hack but it'll work.
        UI_Parent = GameObject.FindGameObjectWithTag("GameUI");
        ScoreText = GameObject.FindGameObjectWithTag("ScoreText").GetComponent<TMP_Text>();
        HPText = GameObject.FindGameObjectWithTag("HPText").GetComponent<TMP_Text>();
        MPText = GameObject.FindGameObjectWithTag("MPText").GetComponent<TMP_Text>();
        GameoverButton = GameObject.FindGameObjectWithTag("GameoverButton");

        DontDestroyOnLoad(UI_Parent);
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
            manaDiff = Player_Controller.Instance.controller.GetCurrentMana() - mana;

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

