using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static Utility.Utility;


public struct SongData{
    /* Struct for tracks of a song and metadata. */
    
    public AudioClip[] tracks;  // store the audioclips for each track.
    public int numberOfTracks { get { return tracks.Length; } } // the number of tracks in audioclip[] tracks.
    public float duration { get {return tracks[0].length; } }   // length of the music.
    
    public SongData(int tracks){
        this.tracks = new AudioClip[tracks]; 
    }
}


public class AudioManager : SingletonObject<AudioManager>{

    // Constants:
    
    readonly string MusicDirectory = "Sounds/Music";
    readonly string SfxDirectory = "Sounds/Sfx";

    const ushort c_musicTracks = 4;    // number of AudioSources instanced to handle music.
    const float c_dopplerLevel = 0f;   // Amount of doppler for sfx.
    const bool c_spatialize = false;   // bool for if the audio is spatialized

    // Variables:

    static string queuedSong = "";      // This is used if a song is played but the audio manger doesn't exist in the scene yet.

    private AudioSource sfxSource;          // AudioSource for playing one-shot audio.
    private AudioSource[] musicTrack;       // array of AudioSource(s) for playing music.
    private float[] musicTrackVolume;       // array of floats. controls each track's volume.
    private float[] musicTrackVolumeTarget; // array of floats for what the music volume should approach
    private float musicVolumeOffset;        // Adjust each music track volume so the total volume is 1.

    private Dictionary<string, AudioClip> sfxClips;     // Store sfx here.
    private Dictionary<string, SongData> musicClips;    // store music with this.

    public float m_volume = 0.5f;               // Master volume control.
    public float m_fadeRate = 0.05f;            // rate at which the fade sh
    private float musicMasterVolume = 0.8f;
    private float sfxMasterVolume = 0.6f;

    public string currentSong = "";             // stores the current song being played.
    public string nextSong = "";                // stores the next song to be played.

    // Track Switching:
    public bool loopMusic = true;
    public bool loopMusicLatch = true;
    
    public override void CustomAwake(){
        // Initialize dictionaries
        sfxClips = new Dictionary<string, AudioClip>();
        musicClips = new Dictionary<string, SongData>();

        // Assign variables:
        musicVolumeOffset = 1f / (float)c_musicTracks;
        musicTrackVolume =  new float[]{1.0f, 1.0f, 1.0f, 1.0f};
        musicTrackVolumeTarget = new float[]{1.0f, 1.0f, 1.0f, 1.0f};
        loopMusic = true;
        loopMusicLatch = true;
        
        LoadSounds();
        LoadMusic();
        UpdateMusic();

        StartCoroutine(ScheduleNextSong());
        StartCoroutine(FadeVolume());
    }


    IEnumerator FadeVolume(){
        /* This function fades the volume towards a target. */

        while(true){
            for (int i = 0; i < c_musicTracks; i++){
                musicTrackVolume[i] = Lerp<float>(musicTrackVolume[i], musicTrackVolumeTarget[i], 0.05f);
            }
            yield return new WaitForSeconds(0.05f);
        }
    }


    IEnumerator ScheduleNextSong(){
        /* Coroutine for scheduling the next song to be played.  */

        while (true){
            if(currentSong != ""){
                UpdateMusic();
                yield return new WaitForSeconds(musicClips[currentSong].duration);
            }
            yield return null;
        }
    }


    public void UpdateMusic(){
        /* This function plays the next music track. Used in Update function with Invoke. */
        
        // If not looping, set the current song to the next song.
        currentSong = loopMusic? currentSong : nextSong;

        // If not looping, after changing songs, set loop music if it should repeat.
        if (!loopMusic){ loopMusic = loopMusicLatch; }

        // leave early if the current song is blank.
        if (currentSong == "" ){ return; }

        // play the new tracks.
        PlayMusic(currentSong);
        
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
            sfxSource.volume = sfxMasterVolume * m_volume;
            sfxSource.dopplerLevel = c_dopplerLevel;
            sfxSource.spatialize = c_spatialize;
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
            musicTrack = new AudioSource[c_musicTracks];
        }
        
        // For every track, if it already exists stop what is playing and instance a new one.
        for (int i = 0; i < c_musicTracks; i++){
            if (musicTrack[i] != null){
                if (musicTrack[i].isPlaying){
                    musicTrack[i].Stop();
                }
                musicTrack[i].clip = null;
            }
            else{
                musicTrack[i] = gameObject.AddComponent<AudioSource>();
                musicTrack[i].volume = musicVolumeOffset * musicMasterVolume * m_volume;
                musicTrack[i].dopplerLevel = 0f;
                musicTrack[i].loop = false;
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
            if (track > c_musicTracks){
                Debug.LogError($"Could not add track,{'"'}{name}{'"'}. music can only be {c_musicTracks} track(s).");
                continue;
            }
            
            // Check that the music the track is associated with exists, if not add music.
            if (!musicClips.Keys.Contains(name)){
                SongData song = new SongData(c_musicTracks);
                musicClips.Add(name, song);
            }
            // Assign clip to track.
            musicClips[name].tracks[track - 1] = clip; // Assign the current clip to the song with matching name.
        }
    }


    public static void SetmusicMasterVolume(float newVolume){
        /*  Set the music to a specific volume. */

        // Validate input:
        if (newVolume < 0f || newVolume > 1f){
            Debug.LogWarning("Music volume must be in range [0,1].");
            return;
        }

        // Update volume:
        float lastVolume = Instance.musicMasterVolume;
        Instance.musicMasterVolume = newVolume;
        
        for (int i = 0; i < c_musicTracks; i++){
            AudioSource track = Instance.musicTrack[i];
            // Divide out the last volume, then scale to new volume. 
            track.volume = newVolume * Instance.musicVolumeOffset;
  
        }
    }
    

    public static bool CheckValidSongName(string name){
        /* Checks if a song "name" is a valid one that has been loaded. */
        if (!Instance.musicClips.Keys.Contains(name)){
            Debug.LogError($"Could not play music, {'"'}{name}{'"'}.");
            return false;
        }
        return true;
    }


    public static void SetsfxMasterVolume(float newVolume){
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

        if (!CheckValidSongName(name)){ return; }

        Instance.nextSong = name;
        Instance.currentSong = (Instance.currentSong == "") ? name : Instance.currentSong;
        Instance.loopMusic = false;
        Instance.loopMusicLatch = true;
        
        Debug.Log($"Now playing: {'"'}{name}{'"'}.");
    }
    

    public static void PlayMusic(string song){
        /*  This function plays a song by its name. */
        
        SongData clips = Instance.musicClips[song];
        Instance.currentSong = song;

        for (int i = 0; i < c_musicTracks; i++){

            if (clips.tracks[i] == null){ 
                Instance.musicTrack[i].Stop();
                continue; 
            }
            
            Instance.musicTrack[i].clip = clips.tracks[i];
            Instance.musicTrack[i].Play();
        }   

        Instance.loopMusicLatch = true;

    }

    public static void StopMusic(){
        /* This function disables looping. the current song with stop after it finishes the 
        current loop */
        Instance.loopMusicLatch = false;
    }


    public static void PlaySound(string name){
        /*  This function plays a sound effect with name "name". */

        Dictionary<string, AudioClip> sfxClips = Instance.sfxClips;
        
        // If the music cannot be found, leave early.
        if (!sfxClips.Keys.Contains(name)){
            Debug.LogError($"Could not play sound effect, {'"'}{name}{'"'}.");
            return;
        }

        Instance.sfxSource.PlayOneShot(Instance.sfxClips[name]);
    }
}
