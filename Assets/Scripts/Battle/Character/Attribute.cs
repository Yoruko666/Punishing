public class Attribute
{
    public float BaseValue;

    public float AddModifier;

    public float MultModifier = 1f;

    public float FinalValue => BaseValue * MultModifier + AddModifier;
}