using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Utility.Utility;

/* This script contains the main system manager which handles instancing for all over manager 
scripts. */

public class System_Manger : SingletonObject<System_Manger>{
    
    [Header("Audio Settings:")]
    [SerializeField] ushort m_musicTracks = 4;  // number of audio sources instanced to handle music.
    [SerializeField] float m_dopplerLevel = 0f; // Amount of doppler for sfx.
    [SerializeField] float m_volume = 0.5f;     // Master volume control.
    [SerializeField] bool m_spatialize = false; // bool for if the audio is spatialized
    
    [Header("UI Settings:")]
    [SerializeField] TMP_Text m_ScoreText;
    [SerializeField] TMP_Text m_HPText;
    [SerializeField] TMP_Text m_MPText;
    [SerializeField] GameObject m_GameoverButton;

    public override void CustomAwake(){

        // Attempt to instance all other scripts.
        AudioManager.MakeInstance();
        Enemy_Manager.MakeInstance();
        Ui_Handler.MakeInstance();
        
        DontDestroyOnLoad(EmptyObject);
        DontDestroyOnLoad(Player);
    }

    void Start(){
        // Set up Audio:
        AudioManager.Instance.m_musicTracks = m_musicTracks;
        AudioManager.Instance.m_dopplerLevel = m_dopplerLevel;
        AudioManager.Instance.m_volume = m_volume;
        AudioManager.Instance.m_spatialize = m_spatialize;
        
        // Set up UI: TODO: replace this with something better.
        Ui_Handler.Instance.m_ScoreText = m_ScoreText;
        Ui_Handler.Instance.m_HPText = m_HPText;
        Ui_Handler.Instance.m_MPText = m_MPText;
        Ui_Handler.Instance.m_GameoverButton = m_GameoverButton;
    }
}