using System;
using System.Collections.Generic;

[Serializable]
public class PlayerInventory
{
    public List<CharacterData> CharacterList;
    public List<WeaponData> WeaponList;
    private Dictionary<int, CharacterData> _characterDict;
    private Dictionary<int, WeaponData> _weaponDict;

    public CharacterData GetCharacter(int id)
    {
        return _characterDict.TryGetValue(id, out CharacterData character) ? character : null;
    }

    public bool AddCharacter(CharacterData character)
    {
        CharacterList.Add(character);
        _characterDict.Add(character.Id, character);
        return true;
    }

    public WeaponData GetWeapon(int uid)
    {
        return _weaponDict.TryGetValue(uid, out WeaponData weapon) ? weapon : null;
    }

    public bool AddWeapon(WeaponData weapon)
    {
        if (_weaponDict.ContainsKey(weapon.Uid))
            return false;
        WeaponList.Add(weapon);
        _weaponDict.Add(weapon.Uid, weapon);
        return true;
    }

    public bool DeleteWeapon(int uid)
    {
        if (_weaponDict.TryGetValue(uid, out WeaponData weapon))
        {
            WeaponList.Remove(weapon);
            _weaponDict.Remove(uid);
            return true;
        }
        return false;
    }
}