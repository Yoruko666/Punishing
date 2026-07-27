using UnityEngine;

public class SingletonMonoBehaviour<T> : MonoBehaviour where T: SingletonMonoBehaviour<T>
{
    public static T Instance => _instance;

    private static T _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
        }
        else Destroy(gameObject);

        OnAwake();
    }

    protected virtual void OnAwake() { }
}
