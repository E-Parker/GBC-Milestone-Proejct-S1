using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

/*  
    To do:
    rewrite how Songdate class gets updated to to reflect the track deration. ideally, it would 
    just implement IEnumerable, but I cannot be bothered to figure that out right now. also change
    loadSong(), loadSounds().

*/

public struct SongData{
    /*  Struct for holding songdata. Might expand this later to include bpm and multiple transitions. */
    
    public AudioClip[] tracks;  // store the audioclips for each track.
    public int numberOfTracks;  // the number of tracks in audioclip[] tracks.
    public float duration;      // store the total length of the music.
    public float transition;    // store the transition time.
    

    public SongData(int tracks, float transition){
        // This is slightly unsafe, but I wont be making a SongData struct without at least one track.
        this.duration = 0;
        this.numberOfTracks = tracks;
        this.tracks = new AudioClip[tracks]; 
        this.transition = transition % duration;
    }


    public void Update(){
        // VERY very bad way of handing this. I hate this code so much.
        foreach (AudioClip clip in this.tracks){
            if (clip != null){
                this.duration = clip.length;
                return; 
            }
        }
    }
}


public class AudioManager : SingletonObject<AudioManager>{
    // Constants / Readonly:
    
    readonly string MusicDirectory = "Sounds/Music";
    readonly string SfxDirectory ="Sounds/Sfx";

    // Variables:

    private AudioSource sfxSource;      // AudioSoruce for playing oneshot audio.
    private AudioSource[] musicTrack;   // array of AudioSource(s) for playing music.
    private float musicTrackVolume;     // Adjust each musictrack volume so the total volume is 1.

    private Dictionary<string, AudioClip> sfxClips;     // Store sfx here.
    private Dictionary<string, SongData> musicClips;    // store music with this.

    public ushort m_musicTracks = 4;    // number of audiosoruces instanced to handle music.
    public float m_dopplerLevel = 0f;   // Amount of doppler for sfx.
    public float m_volume = 0.5f;       // Master volume control.
    public bool m_spatialize = false;   // bool for if the audio is spatialized

    private float musicVolume = 0.8f;
    private float sfxVolume = 0.6f;

    public string currentSong;
    private bool switching;                 // flag for if changing state.
    

    void Start(){
        // Initialize dictionaries
        sfxClips = new Dictionary<string, AudioClip>();
        musicClips = new Dictionary<string, SongData>();

        // Assign variables:
        musicTrackVolume = 1f / (float)m_musicTracks;

        LoadSounds();
        LoadMusic();
        PlayMusic("Battle_Theme");
    }


    public void LoadSounds(){
        /*  This method loads all sound effects. */

        // Check if sfxSourse still exists. if it does, stop playing audio.
        if (sfxSource != null){
            if (sfxSource.isPlaying){
                sfxSource.Stop();    
            }
            sfxSource.clip = null;
        }
        else{
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.volume = sfxVolume * m_volume;
            sfxSource.dopplerLevel = m_dopplerLevel;
            sfxSource.spatialize = m_spatialize;
            sfxSource.loop = false;
        }

        // Load all sound effects:
        AudioClip[] clips = Resources.LoadAll<AudioClip>(SfxDirectory);
        foreach(AudioClip clip in clips){
            sfxClips.Add(clip.name, clip);
        }
    }
    

    public void LoadMusic(){
        /*  This method handles loading music files. */

        // Clear the music tracks:
        if (musicTrack == null){
            musicTrack = new AudioSource[m_musicTracks];
        }
        
        // For every track, if it already exists stop what is playing and instance a new one.
        for (int i = 0; i < m_musicTracks; i++){
            if (musicTrack[i] != null){
                if (musicTrack[i].isPlaying){
                    musicTrack[i].Stop();
                }
                musicTrack[i].clip = null;
            }
            else{
                musicTrack[i] = gameObject.AddComponent<AudioSource>();
                musicTrack[i].volume = musicTrackVolume * musicVolume * m_volume;
                musicTrack[i].dopplerLevel = 0f;
                musicTrack[i].loop = true;
            }
        }
        
        // load music:
        AudioClip[] clips = Resources.LoadAll<AudioClip>(MusicDirectory);
        
        foreach(AudioClip clip in clips){
            // Parse the file name for the track number and song name.
            string[] data = clip.name.Split('-');
            string name = data[0];
            int track = int.Parse(data[1]); 
            
            // If there are to many tracks, don't add the track. 
            if (track > m_musicTracks){
                Debug.LogError($"Could not add track,{'"'}{name}{'"'}. music can only be {m_musicTracks} track(s).");
                continue;
            }
            
            // Check that the music the track is associated with exists, if not add music.
            if (!musicClips.Keys.Contains(name)){
                SongData song = new SongData(m_musicTracks,0);
                musicClips.Add(name, song);
            }
            // Assign clip to track.
            musicClips[name].tracks[track - 1] = clip; // Assign the current clip to the song with matching name.
            musicClips[name].Update();                 // Update the track duration.
            // This is not efficient in the slightest but I just need this to work for now.
        }
    }


    public static void SetMusicTrackVolume(int trackNumber, float newVolume){
        /*  set the volume of a specific track. */

        // Validate input:
        if (newVolume < 0f || newVolume > 1f){
            Debug.LogWarning("Music volume must be in range [0,1].");
            return;
        }

        if (trackNumber < 0 || trackNumber > Instance.m_musicTracks){
            Debug.LogWarning("Track out of range. Ignoring request to change volume. ");
            return;
        }

        // Set track volume.
        SetTrackVolume(trackNumber, newVolume);

    }


    private static void SetTrackVolume(int trackNumber, float newVolume){
        /* Hidden version for internal functions that already handel error checking. */

        AudioSource track = Instance.musicTrack[trackNumber];
        track.volume = newVolume * Instance.musicTrackVolume * Instance.musicVolume * Instance.m_volume;
    }


    public static void SetMusicVolume(float newVolume){
        /*  Set the music to a specific volume. */

        // Validate input:
        if (newVolume < 0f || newVolume > 1f){
            Debug.LogWarning("Music volume must be in range [0,1].");
            return;
        }

        // Update volume:
        float lastVolume = Instance.musicVolume;
        Instance.musicVolume = newVolume;
        
        for (int i = 0; i < Instance.m_musicTracks; i++){
            AudioSource track = Instance.musicTrack[i];
            // Divide out the last volume, then scale to new volume. 
            track.volume = newVolume * Instance.musicTrackVolume;
  
        }
    }


    public static void SetSfxVolume(float newVolume){
        /*set the sfx to a specific volume. */

        // Validate input:
        if (newVolume < 0f || newVolume > 1f){
            Debug.LogWarning("Music volume must be in range [0,1].");
            return;
        }

        // Update the sfxSoruce volume:
        Instance.sfxSource.volume = newVolume;
    }
    

    public static void switchMusic(string name){
        /* Change music after the current song ends. */ 

        SongData nextSong = Instance.musicClips[name];

        for (int i = 0; i < Instance.m_musicTracks; i++){
            AudioSource track = Instance.musicTrack[i];
            track.clip = nextSong.tracks[i];
            if (nextSong.tracks[i] != null){
                track.PlayDelayed(track.time);
            }
        }
    }
    

    public static void PlayMusic(string name){
        /*  This function plays a song by its name. */
        
        // If the music cannot be found, leave early.
        if (!Instance.musicClips.Keys.Contains(name)){
            Debug.LogError($"Could not play music, {'"'}{name}{'"'}.");
            return;
        }

        Instance.currentSong = name;
  
        SongData clips = Instance.musicClips[name];

        for (int i = 0; i < Instance.m_musicTracks; i++){
            Instance.musicTrack[i].Stop();
            Instance.musicTrack[i].clip = clips.tracks[i];
            Instance.musicTrack[i].Play();
        }
    }


    public static void PlaySound(string name){
        /*  This function plays a sound effect with name "name". */
        
        // If the music cannot be found, leave early.
        if (!Instance.sfxClips.Keys.Contains(name)){
            Debug.LogError($"Could not play sound effect, {'"'}{name}{'"'}.");
            return;
        }
        Instance.sfxSource.PlayOneShot(Instance.sfxClips[name]);     
    }
}
