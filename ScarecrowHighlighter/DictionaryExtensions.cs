namespace ScarecrowHighlighter;

public static class DictionaryExtensions
{
    public static void AddOrAppend<TKey, TValue>(this Dictionary<TKey, HashSet<TValue>> dictionary, TKey key, TValue value) where TKey : notnull
    {
        if (dictionary.ContainsKey(key))
        {
            dictionary[key].Add(value);
        }
        else
        {
            dictionary.Add(key, new HashSet<TValue>{ value });
        }
    }
}