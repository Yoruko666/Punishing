using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class BattleManager : SingletonMonoBehaviour<BattleManager>
{
    public int CharacterId;

    private void Start()
    {
        Addressables.LoadAssetAsync<GameObject>(CharacterPath.GetPath(CharacterId)).Completed += (obj) =>
        {
            if (obj.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject go = Instantiate(obj.Result);
                EventCenter.Invoke(EventType.OnCharacterSwitch, go.transform);
            }
        };
    }
}