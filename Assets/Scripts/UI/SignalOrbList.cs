using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;

public class SignalOrbList : MonoBehaviour
{
    public GameObject OrbPrefab;

    private float OrbSpacing = 20f;

    private float MoveSpeed = 6000f;

    private float SpawnOffset = 400f;

    public PlayerController Owner;

    private class OrbView
    {
        public int Id;     // 绑定的球唯一 Id（同色球也彼此不同）
        public PlayerController.SignalOrbType Type;
        public GameObject Go;
        public RectTransform Rect;
        public float TargetX;
    }

    private readonly List<OrbView> _views = new(8);

    private readonly Dictionary<string, Sprite> _spriteCache = new();

    private int _createCount;
    private float _orbWidth;
    private PlayerController _subscribed;

    private void Update()
    {
        if (Owner == null) return;

        EnsureSubscribed();
        RefreshTargets(); 
        AnimateViews();  
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
    }

    // ---------------- 事件订阅 ----------------

    /// <summary>确保已订阅当前 Owner 的增删事件；Owner 变化时自动重订阅并重建视图。</summary>
    private void EnsureSubscribed()
    {
        if (_subscribed == Owner) return;

        Unsubscribe();

        _subscribed = Owner;
        if (Owner != null)
        {
            Owner.OnOrbAdded += OnOwnerOrbAdded;
            Owner.OnOrbsRemoved += OnOwnerOrbsRemoved;
            RebuildFromData();   // 订阅前可能已有球，按当前数据全量重建一次
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed != null)
        {
            _subscribed.OnOrbAdded -= OnOwnerOrbAdded;
            _subscribed.OnOrbsRemoved -= OnOwnerOrbsRemoved;
            _subscribed = null;
        }
    }

    /// <summary>按当前数据全量重建视图（用于首次订阅时对齐已有球）。</summary>
    private void RebuildFromData()
    {
        for (int i = _views.Count - 1; i >= 0; i--)
        {
            if (_views[i].Go != null) Destroy(_views[i].Go);
        }
        _views.Clear();

        var orbs = Owner.GetSignalOrbs();
        for (int i = 0; i < orbs.Count; i++)
            _views.Add(CreateView(orbs[i]));
    }

    // ---------------- 数据层事件回调 ----------------

    /// <summary>获得一颗球：追加一个视图。新球从左侧滑入。</summary>
    private void OnOwnerOrbAdded(PlayerController.SignalOrb orb)
    {
        _views.Add(CreateView(orb));
    }

    /// <summary>消掉一批球：删除数据下标区间 [startListIndex, startListIndex + count) 对应的视图。</summary>
    private void OnOwnerOrbsRemoved(int startListIndex, int count)
    {
        int end = Mathf.Min(startListIndex + count, _views.Count);
        for (int i = end - 1; i >= startListIndex && i >= 0; i--)
        {
            if (_views[i].Go != null) Destroy(_views[i].Go);
            _views.RemoveAt(i);
        }
    }

    // ---------------- 视图创建与布局 ----------------

    /// <summary>创建一个信号球视图，初始置于容器最左侧之外，等待滑入。</summary>
    private OrbView CreateView(PlayerController.SignalOrb orb)
    {
        GameObject go = Instantiate(OrbPrefab, transform);
        go.name = $"Orb_{_createCount++}";

        var view = new OrbView
        {
            Id = orb.Id,
            Type = orb.Type,
            Go = go,
            Rect = go.GetComponent<RectTransform>(),
        };

        // 记录布局基准球宽（首个球创建时）。锚点/pivot 由预制体设为右侧中间 (1, 0.5)，
        // 因此 anchoredPosition.x = 0 表示球贴容器右边缘，向左为负。
        if (view.Rect != null && _orbWidth <= 0f)
            _orbWidth = view.Rect.sizeDelta.x;

        // 绑定点击消除
        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            OrbView captured = view;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnOrbClicked(captured));
        }

        ApplyIcon(view);

        // 新球出现在最左侧之外，随后由 AnimateViews 向目标滑入。
        // 此处只需一个足够靠左的初始位置，真正的目标 X 由 RefreshTargets 计算。
        float step = _orbWidth + OrbSpacing;
        float spawnX = -step * _views.Count - SpawnOffset;
        view.TargetX = spawnX;
        SetX(view.Rect, spawnX);

        return view;
    }

    private void RefreshTargets()
    {
        for (int i = 0; i < _views.Count; i++)
            _views[i].TargetX = -(_orbWidth + OrbSpacing) * i;
    }

    /// <summary>每帧把每个球朝目标位置平滑移动。</summary>
    private void AnimateViews()
    {
        float maxDelta = MoveSpeed * Time.deltaTime;
        foreach (var view in _views)
        {
            RectTransform rect = view.Rect;
            if (rect == null) continue;

            float x = rect.anchoredPosition.x;
            float newX = Mathf.MoveTowards(x, view.TargetX, maxDelta);
            if (!Mathf.Approximately(newX, x))
                rect.anchoredPosition = new Vector2(newX, rect.anchoredPosition.y);
        }
    }

    private static void SetX(RectTransform rect, float x)
    {
        if (rect != null)
            rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    // ---------------- 贴图 ----------------

    /// <summary>为一个球设置贴图；未加载则触发异步加载并暂时透明。</summary>
    private void ApplyIcon(OrbView view)
    {
        Image img = GetIcon(view);
        if (img == null) return;

        string spriteKey = Owner.GetOrbSprite(view.Type);
        if (string.IsNullOrEmpty(spriteKey))
        {
            img.sprite = null;
            img.color = Color.clear;
            return;
        }

        if (_spriteCache.TryGetValue(spriteKey, out var sprite) && sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            img.sprite = null;
            img.color = Color.clear;
            if (!_spriteCache.ContainsKey(spriteKey))
                LoadSprite(spriteKey, view.Type);
        }
    }

    /// <summary>获取球预制体上 Icon 子物体的 Image。</summary>
    private Image GetIcon(OrbView view)
    {
        if (view?.Go == null) return null;
        Transform icon = view.Go.transform.Find("Icon");
        return icon != null ? icon.GetComponent<Image>() : null;
    }

    /// <summary>异步加载贴图并写入缓存（每个 key 只加载一次），完成后回填所有同类型球。</summary>
    private void LoadSprite(string key, PlayerController.SignalOrbType type)
    {
        _spriteCache[key] = null;   // 占位，避免重复发起加载
        Addressables.LoadAssetAsync<Sprite>(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                _spriteCache[key] = handle.Result;
                foreach (var view in _views)
                    if (view.Type == type)
                        ApplyIcon(view);
            }
            else
            {
                Debug.LogWarning($"[信号球] 贴图加载失败: {key}");
            }
        };
    }

    // ---------------- 点击消除 ----------------

    /// <summary>点击某个球时，换算其当前可见位置对应的槽位索引再交给数据层消除。</summary>
    private void OnOrbClicked(OrbView view)
    {
        if (Owner == null) return;

        int index = _views.IndexOf(view);
        if (index < 0) return;

        // 数据 index -> 槽位（键 1~8），与 PlayerController.ListIndexToSlot 一致
        int slot = Owner.ListIndexToSlot(index);
        Owner.TryConsumeSignalOrb(slot);
    }
}
