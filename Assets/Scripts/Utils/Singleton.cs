using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> where T: Singleton<T>
{
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Activator.CreateInstance(typeof(T), true) as T;
            }
            return _instance;
        }
    }
    private static T _instance;
}
