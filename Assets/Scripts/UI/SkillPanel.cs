using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 信号球 UI 面板。
/// 固定 8 个槽位，右侧对齐，左侧填充（新球在左）。
/// 键 8 = 最右侧 = 最早生成，键 1 = 最左侧 = 最新生成（满 8 颗时）。
/// </summary>
public class SkillPanel : MonoBehaviour
{
    /// <summary>信号球预制体（可选，不提供则用代码创建简单 UI）</summary>
    public GameObject OrbPrefab;

    /// <summary>信号球容器，默认挂载在本对象上</summary>
    public Transform OrbContainer;

    /// <summary>单个信号球尺寸</summary>
    public float OrbSize = 48f;

    /// <summary>信号球间距</summary>
    public float OrbSpacing = 4f;

    public PlayerController Owner;

    /// <summary>固定 8 个槽位 UI 元素，索引 0 = 左侧（键 1），7 = 右侧（键 8）</summary>
    private GameObject[] _slotUI = new GameObject[8];
    private bool _slotsInitialized;

    private static readonly Color EmptyColor = new Color(0.2f, 0.2f, 0.2f, 0.3f);

    private void Start()
    {
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        if (_slotsInitialized) return;
        EnsureLayout();

        Transform parent = OrbContainer != null ? OrbContainer : transform;

        for (int i = 0; i < 8; i++)
        {
            GameObject slotGO = OrbPrefab != null
                ? Instantiate(OrbPrefab, parent)
                : CreateSlotUI(parent);

            slotGO.name = $"Slot_{i + 1}";

            // 确保有 Image
            if (slotGO.GetComponent<Image>() == null)
                slotGO.AddComponent<Image>();

            // 添加点击事件
            Button btn = slotGO.GetComponent<Button>();
            if (btn == null)
                btn = slotGO.AddComponent<Button>();

            int capturedIndex = i;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnSlotClicked(capturedIndex));

            _slotUI[i] = slotGO;
        }

        _slotsInitialized = true;
    }

    private void Update()
    {
        if (Owner == null) return;

        var orbs = Owner.GetSignalOrbs();
        int orbCount = orbs.Count;

        // 每个视觉槽位（0=左/键1, 7=右/键8）对应列表索引 MaxSignalOrbs-1-slot
        // 列表索引 0 = 最早生成 = 键 8（右侧），列表末尾 = 最新 = 左侧
        for (int slot = 0; slot < 8; slot++)
        {
            int listIndex = 8 - 1 - slot; // MaxSignalOrbs - 1 - slot
            if (listIndex < orbCount)
            {
                // 该槽位有球 → 显示颜色
                SetSlotColor(slot, PlayerController.OrbColors[(int)orbs[listIndex]], 1f);
            }
            else
            {
                // 该槽位无球 → 透明
                SetSlotColor(slot, EmptyColor, EmptyColor.a);
            }
        }
    }

    private void SetSlotColor(int slotIndex, Color color, float alpha)
    {
        if (slotIndex < 0 || slotIndex >= _slotUI.Length || _slotUI[slotIndex] == null) return;
        Image img = _slotUI[slotIndex].GetComponent<Image>();
        if (img != null)
        {
            color.a = alpha;
            img.color = color;
        }
    }

    /// <summary>用代码创建一个简单的信号球 UI 元素</summary>
    private GameObject CreateSlotUI(Transform parent)
    {
        GameObject go = new GameObject("Slot", typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(OrbSize, OrbSize);

        return go;
    }

    private void EnsureLayout()
    {
        Transform parent = OrbContainer != null ? OrbContainer : transform;

        HorizontalLayoutGroup layout = parent.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
            layout = parent.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.spacing = OrbSpacing;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = parent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = parent.gameObject.AddComponent<ContentSizeFitter>();

        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    /// <summary>信号球点击回调（slotIndex 0~7 = 键 1~8）</summary>
    public void OnSlotClicked(int slotIndex)
    {
        if (Owner != null)
            Owner.TryConsumeSignalOrb(slotIndex);
    }
}
