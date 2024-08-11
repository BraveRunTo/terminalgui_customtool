using YamlDotNet.Serialization;

namespace FastTool_TerminalGUI;

public class DataUtils
{
    public static Deserializer YamlDeserializer = new Deserializer();
    public static Serializer YamlSerializer = new Serializer();
    
    public static T YamlDeserialize<T>(string filePath)
    {
        return YamlDeserializer.Deserialize<T>(new StreamReader(filePath));
    }
    
    public static string YamlSerialize<T>(T obj)
    {
        return YamlSerializer.Serialize(obj);
    }
}