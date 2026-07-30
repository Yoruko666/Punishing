using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

/// <summary>
/// 检测形状抽象基类。定义伤害检测的范围和方式，
/// 每种子类实现不同的几何体检测（球体/立方体等）。
/// 通过 DetectionShapeConverter 支持 JSON 多态反序列化。
/// </summary>
[JsonConverter(typeof(DetectionShapeConverter))]
public abstract class DetectionShape
{
    /// <summary>检测中心相对攻击者的本地坐标偏移</summary>
    public Vector3 Offset;

    /// <summary>执行检测，返回命中的碰撞体列表。</summary>
    public abstract Collider[] Detect(Transform origin, int layerMask);
}

// ==================== 具体形状 ====================

/// <summary>球体范围检测（Physics.OverlapSphere）</summary>
public class SphereDetection : DetectionShape
{
    public float Radius;

    public override Collider[] Detect(Transform origin, int layerMask)
    {
        Vector3 worldPos = origin.TransformPoint(Offset);
        return Physics.OverlapSphere(worldPos, Radius, layerMask);
    }
}

/// <summary>立方体范围检测（Physics.OverlapBox）</summary>
public class BoxDetection : DetectionShape
{
    public Vector3 HalfExtents;

    public override Collider[] Detect(Transform origin, int layerMask)
    {
        Vector3 worldPos = origin.TransformPoint(Offset);
        return Physics.OverlapBox(worldPos, HalfExtents, origin.rotation, layerMask);
    }
}

// ==================== JSON 反序列化转换器 ====================

/// <summary>DetectionShape 多态反序列化：根据 JSON 中的 "ShapeType" 创建对应子类。</summary>
public class DetectionShapeConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) => objectType == typeof(DetectionShape);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;

        JObject jo = JObject.Load(reader);
        string shapeType = jo["ShapeType"]?.Value<string>();

        DetectionShape shape = shapeType switch
        {
            "Sphere" => new SphereDetection(),
            "Box" => new BoxDetection(),
            _ => null
        };

        if (shape == null)
        {
            Debug.LogWarning($"未知的 DetectionShape 类型: {shapeType}");
            return null;
        }

        serializer.Populate(jo.CreateReader(), shape);
        return shape;
    }

    public override bool CanWrite => false;

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        => throw new NotImplementedException();
}
