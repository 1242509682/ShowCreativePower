using System.ComponentModel;
using Newtonsoft.Json;
using TShockAPI;

namespace Plugin;

internal class Configuration
{
    public static readonly string FilePath = Path.Combine(TShock.SavePath, "显示旅途力量.json");

    [JsonProperty("插件开关", Order = 0)]
    public bool Enabled { get; set; } = true;
    [JsonProperty("允许手游显示力量菜单", Order = 1)]
    public bool PE { get; set; } = true;
    [JsonProperty("解锁全物品研究", Order = 1)]
    public bool Unlock { get; set; } = true;
    [JsonProperty("进服自动显示菜单", Order = 2)]
    public bool AutoOpen { get; set; } = true;
    [JsonProperty("旅途力量名单", Order = 3)]
    public List<string> PlayerNames = new List<string>();

    #region 预设参数方法
    public void SetDefault()
    {
        PlayerNames = new List<string>
        {
            "羽学", "西江",
            "灵乐", "安安",
        };
    }
    #endregion

    #region 读取与创建配置文件方法
    public void Write()
    {
        string json = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(FilePath, json);
    }

    public static Configuration Read()
    {
        if (!File.Exists(FilePath))
        {
            var NewConfig = new Configuration();
            NewConfig.SetDefault();
            NewConfig.Write();
            return NewConfig;
        }
        else
        {
            string jsonContent = File.ReadAllText(FilePath);
            return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
        }
    }
    #endregion
}