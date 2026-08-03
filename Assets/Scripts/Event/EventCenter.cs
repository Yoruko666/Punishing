using System;
using System.Collections.Generic;

public static class EventCenter
{
    private static readonly Dictionary<EventType, Delegate> eventDic = new();

    public static void AddListener(EventType eventType, Action callback)
    {
        eventDic[eventType] = (Delegate)Delegate.Combine(eventDic.GetValueOrDefault(eventType), callback);
    }

    public static void RemoveListener(EventType eventType, Action callback)
    {
        var handler = eventDic.GetValueOrDefault(eventType);
        var result = Delegate.Remove(handler, callback);
        if (result == null) eventDic.Remove(eventType);
        else eventDic[eventType] = result;
    }

    public static void Invoke(EventType eventType)
    {
        (eventDic.GetValueOrDefault(eventType) as Action)?.Invoke();
    }

    public static void AddListener<T>(EventType eventType, Action<T> callback)
    {
        eventDic[eventType] = (Delegate)Delegate.Combine(eventDic.GetValueOrDefault(eventType), callback);
    }

    public static void RemoveListener<T>(EventType eventType, Action<T> callback)
    {
        var handler = eventDic.GetValueOrDefault(eventType);
        var result = Delegate.Remove(handler, callback);
        if (result == null) eventDic.Remove(eventType);
        else eventDic[eventType] = result;
    }

    public static void Invoke<T>(EventType eventType, T arg)
    {
        (eventDic.GetValueOrDefault(eventType) as Action<T>)?.Invoke(arg);
    }

    public static void Clear()
    {
        eventDic.Clear();
    }
}
