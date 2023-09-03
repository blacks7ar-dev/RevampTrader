using YamlDotNet.Serialization;

namespace RevampTrader.Functions;

public struct SellableItem
{
    [YamlMember(Alias = "Prefab", ApplyNamingConventions = false)]
    public string m_prefab { get; set; }
    [YamlMember(Alias = "Value", ApplyNamingConventions = false)]
    public int m_value { get; set; }

    public SellableItem()
    {
        m_prefab = null;
        m_value = 0;
    }
}