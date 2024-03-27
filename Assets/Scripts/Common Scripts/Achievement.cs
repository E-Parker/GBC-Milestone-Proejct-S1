using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utility;
using static Utility.Utility;

public class Achievement{
    // Define Properties:
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsUnlocked {get; private set; }
    public float PercentProgress { get { return (float)progress / (float)maxProgress; } }

    // Define Variables:
    private ushort progress;    // Current completion amount.
    private ushort maxProgress; // completion amount.
    

    public Achievement(string name, string description, ushort maxProg = 1){
        Name = name;
        Description = description;
        IsUnlocked = false;
        progress = 0;
        maxProgress = maxProg;
    }
    
    public void Progress(){
        /* This method advances the progress of an achievement by one. */
        if (++progress == maxProgress){
            IsUnlocked = true;
            Debug.Log($"\"{Name}\" Unlocked!");
        }
    }
}

public class AchievementObserver : IObserver{

    private Dictionary<Event, Achievement> achievements;

    public AchievementObserver(){
        achievements = new();
        achievements[Event.PlayerMoved] = new Achievement("Baby's first steps.", "Walk in any direction.", 1);
    }

    public void OnNotify(Event gameEvent){

        // if the event is an achievement, and the achievement isn't unlocked:
        if(achievements.ContainsKey(gameEvent) && !achievements[gameEvent].IsUnlocked){
            achievements[gameEvent].Progress();   
        }
    }
}