using System.Collections.Generic;

/// <summary>
/// 通用属性集合，以字符串名为键。
/// 支持任意角色定义专属属性（如 "LuciaSwordEnergy"），无需修改本类。
/// </summary>
public class AttributeSet
{
    private readonly Dictionary<string, Attribute> _attributes = new();

    /// <summary>确保属性存在（如不存在则以默认值创建）</summary>
    public void EnsureAttribute(string name, float defaultValue = 0)
    {
        if (!_attributes.ContainsKey(name))
            _attributes[name] = new Attribute { BaseValue = defaultValue };
    }

    /// <summary>获取属性最终值（不存在则返回 0）</summary>
    public float GetAttribute(string name)
    {
        return _attributes.TryGetValue(name, out var attr) ? attr.FinalValue : 0;
    }

    /// <summary>直接设置 BaseValue</summary>
    public void SetBaseAttribute(string name, float value)
    {
        EnsureAttribute(name);
        _attributes[name].BaseValue = value;
    }

    /// <summary>设置加法修正值</summary>
    public void SetAddModifier(string name, float value)
    {
        EnsureAttribute(name);
        _attributes[name].AddModifier = value;
    }

    /// <summary>设置乘法修正值</summary>
    public void SetMultModifier(string name, float value)
    {
        EnsureAttribute(name);
        _attributes[name].MultModifier = value;
    }
}
