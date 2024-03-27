using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Utility;
using static Utility.Utility;

public enum Event{
    // Achievement Events:

    PlayerMoved,
    Survived_1m,
    Survived_5m,
    Survived_10m,
    Survived_1h,
    MageKilled,
    MagesKilled_10,
    MagesKilled_100,
    MagesKilled_1000,
    
    // Other Events:

}

public interface IObserver{
    /* Class interface for observable events. */
    
    public void OnNotify(Event GameEvent);

}

public interface IReceiver{
    /* Class interface that handles Observing game objects. */

    public List<IObserver> Observers { get; }    
}

public static class ReceiverExtensions{

    public static void AddObserver(this IReceiver receiver, IObserver observer){
        receiver.Observers.Add(observer);
    }

    public static void RemoveObserver(this IReceiver receiver, IObserver observer){
        receiver.Observers.Remove(observer);
    }

    public static void NotifyObservers(this IReceiver receiver, Event gameEvent){
        
        // This function notifies all observers with the gameEvent.
        foreach (var observer in receiver.Observers){
            observer.OnNotify(gameEvent);
        }
    }

}