using System;
using System.Collections.Generic;
using UnityEngine;

public class EventsHandler : MonoBehaviour
{
    public delegate void EventCallback();
    public delegate void EventCallback<T>(T data);

    private static EventsHandler _instance;
    public static bool isQuit = false;

    public static EventsHandler Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventsHandler>();
                if (_instance == null)
                {
                    if (isQuit) return null;

                    GameObject obj = new GameObject("EventsHandler");
                    _instance = obj.AddComponent<EventsHandler>();
                    DontDestroyOnLoad(obj);
                }
            }

            return _instance;
        }
    }

    private readonly Dictionary<string, Delegate> eventDictionary = new();

    private static string GetKey<T>(string name)
    {
        return $"{name}_{typeof(T).FullName}";
    }

    public static void RegisterEvent(string name, EventCallback callback)
    {
        var instance = Instance;
        if (instance == null || callback == null) return;

        if (instance.eventDictionary.TryGetValue(name, out var existing))
        {
            instance.eventDictionary[name] = Delegate.Combine(existing, callback);
        }
        else
        {
            instance.eventDictionary.Add(name, callback);
        }
    }

    public static void RegisterEvent<T>(string name, EventCallback<T> callback)
    {
        var instance = Instance;
        if (instance == null || callback == null) return;

        string key = GetKey<T>(name);

        if (instance.eventDictionary.TryGetValue(key, out var existing))
        {
            instance.eventDictionary[key] = Delegate.Combine(existing, callback);
        }
        else
        {
            instance.eventDictionary.Add(key, callback);
        }
    }

    public static void UnregisterEvent(string name, EventCallback callback)
    {
        var instance = Instance;
        if (instance == null || callback == null) return;

        if (!instance.eventDictionary.TryGetValue(name, out var existing)) return;

        var current = Delegate.Remove(existing, callback);

        if (current == null)
            instance.eventDictionary.Remove(name);
        else
            instance.eventDictionary[name] = current;
    }

    public static void UnregisterEvent<T>(string name, EventCallback<T> callback)
    {
        var instance = Instance;
        if (instance == null || callback == null) return;

        string key = GetKey<T>(name);

        if (!instance.eventDictionary.TryGetValue(key, out var existing)) return;

        var current = Delegate.Remove(existing, callback);

        if (current == null)
            instance.eventDictionary.Remove(key);
        else
            instance.eventDictionary[key] = current;
    }

    public static void TriggerEvent(string name)
    {
        var instance = Instance;
        if (instance == null) return;

        if (instance.eventDictionary.TryGetValue(name, out var callback))
        {
            ((EventCallback)callback)?.Invoke();
        }
        else
        {
            Debug.LogWarning($"event {name} not registered");
        }
    }

    public static void TriggerEvent<T>(string name, T eventData)
    {
        var instance = Instance;
        if (instance == null) return;

        string key = GetKey<T>(name);

        if (instance.eventDictionary.TryGetValue(key, out var callback))
        {
            ((EventCallback<T>)callback)?.Invoke(eventData);
        }
        else
        {
            Debug.LogWarning($"event {key} not registered");
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            isQuit = true;
            ClearAllEvents();
            _instance = null;
        }
    }

    private void ClearAllEvents()
    {
        eventDictionary.Clear();
    }
}