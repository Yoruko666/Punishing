using System;

[Serializable]
public class CharacterData
{
    public int Id;
    public int Level;
    public int Exp;
    public int EquipedWeaponUid;
    public MemoryData[] MemorySlots = new MemoryData[6];

    public CharacterData()
    {

    }
}
