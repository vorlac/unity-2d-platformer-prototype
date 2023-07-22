using System;
using System.Collections.Generic;
 
public enum GameEvent 
{ 
    RebuildSpatialIndex, 
    RebuildGraph, 
    UpdatePaths 
};

public static class EventManager
{
    // Stores the delegates that get called when an event is fired
    private static Dictionary<GameEvent, Action> eventTable = new Dictionary<GameEvent, Action>();

    // Adds a delegate to get called for a specific event
    public static void AddHandler(GameEvent evnt, Action action)
    {
        if (!eventTable.ContainsKey(evnt)) 
            eventTable[evnt] = action;
        else 
            eventTable[evnt] += action;
    }

    public static void RemoveHandler(GameEvent evnt, Action action)
    {
        if (eventTable[evnt] != null)
            eventTable[evnt] -= action;
        if (eventTable[evnt] == null)
            eventTable.Remove(evnt);
    }

    // Fires the event
    public static void Broadcast(GameEvent evnt)
    {
        if (eventTable[evnt] != null) 
            eventTable[evnt]();
    }
}
