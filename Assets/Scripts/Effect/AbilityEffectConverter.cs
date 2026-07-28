using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// AbilityEffect 的多态反序列化转换器：根据 JSON 中的 "Type" 字段
/// 实例化对应的具体效果类型，再填充其余字段。
/// 新增一种效果时，只需在此 switch 中登记即可。
/// </summary>
public class AbilityEffectConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(AbilityEffect);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject jo = JObject.Load(reader);
        string type = jo["Type"]?.Value<string>();

        AbilityEffect effect = type switch
        {
            "SetCombo" => new ComboEffect(),
            "Invincible" => new InvincibleEffect(),
            "Move" => new MoveEffect(),
            "Damage" => new DamageEffect(),
            _ => null
        };

        if (effect == null)
        {
            Debug.LogWarning($"未知的 AbilityEffect 类型: {type}");
            return null;
        }

        // 填充具体类型字段（此时 objectType 为具体子类，CanConvert 返回 false，不会递归）
        serializer.Populate(jo.CreateReader(), effect);
        return effect;
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => throw new NotImplementedException();
}
