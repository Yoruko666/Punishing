using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class SignalOrbList : SingletonMonoBehaviour<SignalOrbList>
{
    public GameObject OrbPrefab;
    private TextMeshProUGUI OrbNum;

    private const float orbSpacing = 28f;
    private const float orbWidth = 140f;
    private const float moveSpeed = 6000f;
    private const float spawnOffset = 400f;
    private const int PoolSize = 8;

    public PlayerController Owner;

    private class OrbView
    {
        public int Id;
        public PlayerController.SignalOrbType Type;
        public GameObject Go;
        public RectTransform Rect;
        public float TargetX;
        public int Key;
        public TextMeshProUGUI KeyText;
    }

    private readonly List<OrbView> _views = new(PoolSize);

    private readonly Stack<OrbView> _pool = new(PoolSize);

    private readonly Dictionary<string, Sprite> _spriteCache = new();

    private int _visibleStart;
    private PlayerController _subscribed;

    private void Awake()
    {
        OrbNum = transform.Find("OrbNum").GetComponent<TextMeshProUGUI>();

        for (int i = 0; i < PoolSize; i++)
        {
            OrbView view = CreatePooledView();
            view.Go.SetActive(false);
            _pool.Push(view);
        }

        BattleManager.Instance.OnCharacterSwitch += (player) => Owner = player.GetComponent<PlayerController>();
    }

    /// <summary>创建一颗池中球的 GameObject（一次性设置，之后复用）。</summary>
    private OrbView CreatePooledView()
    {
        GameObject go = Instantiate(OrbPrefab, transform);
        go.name = $"Orb_Pooled_{_pool.Count}";

        var view = new OrbView
        {
            Go = go,
            Rect = go.GetComponent<RectTransform>(),
            KeyText = go.transform.Find("Key/Text").GetComponent<TextMeshProUGUI>(),
        };

        Button btn = go.GetComponent<Button>();
        if (btn != null)
        {
            OrbView captured = view;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnOrbClicked(captured));
        }

        return view;
    }

    /// <summary>从池中取出一个 OrbView，按 orb 数据配置并放置到初始滑入位置。</summary>
    private OrbView AcquireFromPool(PlayerController.SignalOrb orb, int index)
    {
        OrbView view = _pool.Pop();
        view.Id = orb.Id;
        view.Type = orb.Type;
        view.Go.SetActive(true);

        ApplyIcon(view);

        float step = orbWidth + orbSpacing;
        float spawnX = -step * index - spawnOffset;
        view.TargetX = spawnX;
        SetX(view.Rect, spawnX);

        return view;
    }

    /// <summary>将 OrbView 归还池中（停用 GameObject）。</summary>
    private void ReleaseToPool(OrbView view)
    {
        view.Go.SetActive(false);
        _pool.Push(view);
    }

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

        // 销毁池中所有 GameObject
        foreach (var view in _pool)
            if (view.Go != null) Destroy(view.Go);

        // 销毁当前视图中可能遗留的（虽然理论上 Rebuild 已归还）
        foreach (var view in _views)
            if (view.Go != null) Destroy(view.Go);

        _pool.Clear();
        _views.Clear();
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
            Owner.OnOrbsReset += OnOwnerOrbsReset;
            RebuildFromData();   // 订阅前可能已有球，按当前数据全量重建一次
        }
    }

    private void Unsubscribe()
    {
        if (_subscribed != null)
        {
            _subscribed.OnOrbAdded -= OnOwnerOrbAdded;
            _subscribed.OnOrbsRemoved -= OnOwnerOrbsRemoved;
            _subscribed.OnOrbsReset -= OnOwnerOrbsReset;
            _subscribed = null;
        }
    }

    /// <summary>全量刷新（Blade Will 转球/退出时调用），重置可见窗口。</summary>
    private void OnOwnerOrbsReset()
    {
        _visibleStart = 0;
        RebuildFromData();
    }

    /// <summary>按当前数据重建视图，只显示最多 8 颗球。始终显示最旧的球，超过 8 颗的新球在数据列表中等候（不挤掉可见球）。</summary>
    private void RebuildFromData()
    {
        var orbs = Owner.GetSignalOrbs();

        // ---- 维护可见窗口 ----
        // 始终从最旧的球开始显示（_visibleStart = 0），超过 PoolSize 的球在数据列表中等候
        _visibleStart = 0;

        int start = _visibleStart;
        int end = Mathf.Min(start + PoolSize, orbs.Count);

        // 收集新可见数据的 Id 集合
        HashSet<int> newIds = new HashSet<int>();
        for (int i = start; i < end; i++)
            newIds.Add(orbs[i].Id);

        // 1) 归还 Id 不再可见的视图
        for (int i = _views.Count - 1; i >= 0; i--)
        {
            if (!newIds.Contains(_views[i].Id))
            {
                ReleaseToPool(_views[i]);
                _views.RemoveAt(i);
            }
        }

        // 2) 存活视图按 Id 建立映射
        Dictionary<int, OrbView> idToView = new Dictionary<int, OrbView>();
        foreach (var view in _views)
            idToView[view.Id] = view;
        _views.Clear();

        // 3) 按新数据顺序构建视图列表
        for (int i = start; i < end; i++)
        {
            var orb = orbs[i];
            if (idToView.TryGetValue(orb.Id, out var existingView))
            {
                existingView.Type = orb.Type;
                ApplyIcon(existingView);
                _views.Add(existingView);
                idToView.Remove(orb.Id);
            }
            else
            {
                int vi = _views.Count;
                _views.Add(AcquireFromPool(orb, vi));
            }
        }

        // 4) 归还剩余不匹配的视图
        foreach (var view in idToView.Values)
            ReleaseToPool(view);

        RefreshSignalNum();
    }

    /// <summary>更新信号球计数文本为 "当前数量/16"。</summary>
    private void RefreshSignalNum()
    {
        if (OrbNum == null || Owner == null) return;
        OrbNum.text = $"{Owner.GetSignalOrbs().Count}/{Owner.GetMaxDataSignalOrbs()}";
    }

    // ---------------- 数据层事件回调 ----------------

    /// <summary>获得一颗球：全量重建（数据与视图不再一一对应，简化处理）。</summary>
    private void OnOwnerOrbAdded(PlayerController.SignalOrb orb)
    {
        RebuildFromData();
    }

    /// <summary>消掉一批球：全量重建。</summary>
    private void OnOwnerOrbsRemoved(int startListIndex, int count)
    {
        RebuildFromData();
    }

    // ---------------- 视图布局与动画 ----------------

    private void RefreshTargets()
    {
        for (int i = 0; i < _views.Count; i++)
        {
            _views[i].TargetX = -(orbWidth + orbSpacing) * i;
            _views[i].Key = PoolSize - i; // 左=槽1, 右=槽8, 从右往左生成（最右=8，向左递减到1）
            if (_views[i].KeyText != null)
                _views[i].KeyText.text = _views[i].Key.ToString();
        }
    }

    /// <summary>每帧把每个球朝目标位置平滑移动。</summary>
    private void AnimateViews()
    {
        float maxDelta = moveSpeed * Time.deltaTime;
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

    /// <summary>点击某个球时，按数据索引直接消除（绕过 slot 换算，兼容等候区偏移）。</summary>
    private void OnOrbClicked(OrbView view)
    {
        if (Owner == null) return;

        int viewIndex = _views.IndexOf(view);
        if (viewIndex < 0) return;

        // viewIndex: 0=最右(最旧), Count-1=最左(最新)
        int dataIndex = _visibleStart + viewIndex;
        Owner.TryConsumeSignalOrbByDataIndex(dataIndex);
    }
}
