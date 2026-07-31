using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public abstract class CharacterBase : MonoBehaviour
{
    public int Level;
    private Animator Animator;
    private AudioSource AudioSource;

    protected virtual void Start()
    {
        Animator = GetComponent<Animator>();
        AudioSource = GetComponent<AudioSource>();
    }

    public void PlayAnim(string animName, float crossTime = 0.14f)
    {
        Animator.CrossFadeInFixedTime(animName, crossTime);
    }

    public void PlaySound(string soundName)
    {
        Addressables.LoadAssetAsync<AudioClip>(soundName).Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                AudioSource.PlayOneShot(handle.Result);
            }
        };
    }

    public float GetAnimNormalizedTime() =>  Animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
}
