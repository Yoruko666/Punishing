using System.Collections.Generic;

public static class CharacterPath
{
    private static readonly Dictionary<int, string> _characterPathDict;

    static CharacterPath()
    {
        _characterPathDict = new Dictionary<int, string>();
        _characterPathDict[1001] = "Lucia";
    } 

    public static string GetPath(int id)
    {
        return _characterPathDict[id];
    }
}