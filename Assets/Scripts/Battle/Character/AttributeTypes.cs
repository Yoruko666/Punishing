/// <summary>
/// 预定义的属性名称常量。
/// 以字符串替代 enum，任何角色可自由定义专属属性而不修改共享代码。
///
/// 扩展方式：直接在配置 JSON 中用你的属性名即可，
/// 也可以在此类中添加新常量便于 IDE 提示。
/// 不需要修改任何 C# 核心类型（enum / Effect / 转换器）。
/// </summary>
public static class AttributeTypes
{
    public const string Health = "Health";
    public const string MaxHealth = "MaxHealth";
    public const string Attack = "Attack";
    public const string Defence = "Defence";

    public const string LuciaSwordEnergy = "LuciaSwordEnergy";
}
