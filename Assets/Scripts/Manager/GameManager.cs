using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        //Time.timeScale = 0.5f;
    }
}
