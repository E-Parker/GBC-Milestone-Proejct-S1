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
    
    public override void CustomAwake(){

        // Attempt to instance all other scripts.
        AudioManager.MakeInstance();
        Enemy_Manager.MakeInstance();
        Ui_Handler.MakeInstance();
        
    }
}