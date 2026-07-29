using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillPanel : MonoBehaviour
{
    public List<GameObject> SkillSlots;

    /// <summary>终极技槽位（可选，不拖入则不更新）</summary>
    public GameObject UltimateSlot;

    public PlayerController Owner;

    void Update()
    {
        // 技能槽 1~N（根据 SkillAbilityIds 长度）
        for (int i = 0; i < SkillSlots.Count; i++)
        {
            float alpha = Owner.GetSkillAbilityCoolDownRatio(i);
            SkillSlots[i].transform.Find("Mask").GetComponent<Image>().fillAmount = alpha;
        }

        // 终极技槽位
        if (UltimateSlot != null)
        {
            float alpha = Owner.GetUltimateCoolDownRatio();
            UltimateSlot.transform.Find("Mask").GetComponent<Image>().fillAmount = alpha;
        }
    }
}
