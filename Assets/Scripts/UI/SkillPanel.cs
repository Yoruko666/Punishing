using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillPanel : MonoBehaviour
{
    public List<GameObject> SkillSlots;
    public PlayerController Owner;

    void Start()
    {
    }

    void Update()
    {
        for(int i = 0; i < SkillSlots.Count; i++)
        {
            float alpha = Owner.GetSkillCoolDown(i) / Owner.PlayerConfig.SkillList[i].CoolDown;
            SkillSlots[i].transform.Find("Mask").GetComponent<Image>().fillAmount = alpha;
        }
    }
}
