using YamlDotNet.Serialization;

namespace RevampTrader.Functions;

public struct TraderYaml
{
    [YamlMember(Alias = "Prefab", ApplyNamingConventions = false)]
    public string m_prefab { get; set; }
    [YamlMember(Alias = "Stack", ApplyNamingConventions = false)]
    public int m_stack { get; set; }
    [YamlMember(Alias = "Price", ApplyNamingConventions = false)]
    public int m_price { get; set; }
    [YamlMember(Alias = "Required Global Key", ApplyNamingConventions = false)]
    public string m_requiredGlobalKey { get; set; }

    public TraderYaml()
    {
        m_prefab = null;
        m_stack = 0;
        m_price = 0;
        m_requiredGlobalKey = null;
    }
}