using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static Utility.Utility;

// cool symbols 🗡

public class Ui_Handler : SingletonObject<Ui_Handler>{
    /*  Script for handling UI elements. */
    
    const float UpdateRate = 8.0f;
    const float UpdateInterval = 1.0f / UpdateRate;

    const char HP_Missing = '-';
    const char HP_Has = 'x';

    const char MP_Missing = '-';
    const char MP_Has = 'x';

    private TMP_Text ScoreText = null;
    private TMP_Text HPText = null;
    private TMP_Text MPText = null;
    private GameObject GameOverButton = null;

    private int health;
    private int healthDiff;
    private int mana;
    private int manaDiff;
    
    private IEnumerator UpdateUI(){
        while(true){
            // If player is dead, show the respawn button.
            GameOverButton.SetActive(!Player.Health.Alive);
            UpdateHealthUI();
            UpdateManaUI();
            ScoreText.text = Enemy_Manager.GetScoreText();
            yield return new WaitForSeconds(UpdateInterval);
        }
    }

    void Start(){
        gameObject.layer = 5;
        ScoreText = GameObject.FindWithTag("GameUI_Score").GetComponent<TMP_Text>();
        HPText = GameObject.FindWithTag("GameUI_Health").GetComponent<TMP_Text>();
        MPText = GameObject.FindWithTag("GameUI_Mana").GetComponent<TMP_Text>();
        GameOverButton = GameObject.FindWithTag("GameUI_GameOverButton");
        GameOverButton.SetActive(false);
        StartCoroutine(UpdateUI());
    }
    
    private void UpdateHealthUI(){
        
        if(health == Player.Health.Current){ return; }

        health = Player.Health.Current;
        healthDiff = Player.Health.maxHealth - health;
        HPText.text = "(Health-";
        if (health > 0){ HPText.text += new string(HP_Has, health); }
        if (healthDiff > 0){ HPText.text += new string(HP_Missing, healthDiff); }
        HPText.text += ")";
    }

    private void UpdateManaUI(){
        
        if(mana == Player.GetCurrentMana()){ return; }

        mana = Player.GetCurrentMana();
        manaDiff = Player.GetMaxMana() - mana;
        MPText.text = "(Mana-";
        if (mana > 0){ MPText.text += new string(MP_Has, mana); }
        if (manaDiff > 0){ MPText.text += new string(MP_Missing, manaDiff); } 
        MPText.text += ")";
    }
}

