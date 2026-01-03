using System.IO.Streams;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Net;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Plugin;

[ApiVersion(2, 1)]
public class ShowCreativePower : TerrariaPlugin
{
    #region 插件信息
    public override string Name => "显示旅途力量";
    public override string Author => "羽学、西江小子";
    public override Version Version => new(1, 0, 0);
    public override string Description => "在非旅途模式下显示旅途力量菜单";
    #endregion

    #region 全局字段
    public static string Scp => "scp";
    public static Color color => new(240, 250, 150);
    public static bool IsAdmin(TSPlayer plr) => plr.HasPermission($"{Scp}.admin");
    #endregion

    #region 注册与释放
    public ShowCreativePower(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        OTAPI.Hooks.NetMessage.SendBytes += OnSendBytes;
        //GetDataHandlers.ReadNetModule.Register(this.OnReadNetModule);
        TShockAPI.Commands.ChatCommands.Add(new Command($"{Scp}.use", Commands.ScpCmd, Scp));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            OTAPI.Hooks.NetMessage.SendBytes -= OnSendBytes;
            //GetDataHandlers.ReadNetModule.UnRegister(this.OnReadNetModule);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.ScpCmd);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args = null!)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[显示旅途力量]重新加载配置完毕。");
    }
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 玩家进入服务器
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr == null || !plr.RealPlayer || !plr.Active ||
            Config is null || !Config.Enabled) return;

        if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr)) return;

        if (Config.AutoOpen) // 只有自动开启时才标记
        {
            plr.SetData(Scp, (byte)GameModeID.Creative);
        }
    }
    #endregion

    #region 玩家更新事件,用于显示旅途菜单
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.RealPlayer ||
            Config is null || !Config.Enabled) return;

        if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr)) return;

        // 检查玩家是否刚加入且自动开启
        if (plr.GetData<byte>(Scp) == (byte)GameModeID.Creative)
        {
            SwitchMenu(plr, true);
        }
    }
    #endregion

    #region 菜单开关方法
    internal static void SwitchMenu(TSPlayer plr, bool flag)
    {
        byte mode = flag ? (byte)GameModeID.Creative : (byte)GameModeID.Normal;
        string open = flag ? "[c/4296D2:开启]" : "[c/E34761:关闭]";

        // 发送世界信息(帮助PE玩家显示力量菜单),OTAPI钩子的OnSendBytes方法会修改WorldInfo包
        if (Config.PE)
            NetMessage.SendData((int)PacketTypes.WorldInfo, plr.Index);

        // 发送玩家信息更新包(帮助PC玩家显示力量菜单)
        plr.TPlayer.difficulty = mode;
        NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, null, plr.Index);

        string mess = "";
        if (Config.Unlock && flag)
        {
            for (int i = 0; i < ItemID.Count; i++)
            {
                var response = NetCreativeUnlocksModule.SerializeItemSacrifice(i, 9999);
                NetManager.Instance.SendToClient(response, plr.Index);
            }

            mess += ",已为你[c/41D396:解锁所有]研究物品";
        }

        plr.SendMessage($"\n旅途力量菜单已{open}" + mess, color);
        plr.SendMessage("如果力量菜单消失，请用指令:[c/F5E44B:/scp on]", color);

        plr.RemoveData(Scp); // 移除标记
    }
    #endregion

    #region SendBytes钩子 - 修改WorldInfo方法
    public void OnSendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs e)
    {
        if (!Config.Enabled || !Config.PE || e.Data == null || e.Data.Length < 2)
        {
            return;
        }

        // 检查是否为WorldInfo数据包
        if (e.Data[2] != (byte)PacketTypes.WorldInfo) return;

        // 尝试从Netplay.Clients查找对应的玩家
        int pIndex = Utils.FindIndexBySocket(e.RemoteClient);
        if (pIndex < 0 || pIndex >= TShock.Players.Length) return;

        try
        {
            var plr = TShock.Players[pIndex];
            if (plr == null || !plr.RealPlayer) return;

            // 检查是否有菜单开关标记
            byte menu = plr.GetData<byte>(Scp);
            if (menu == (byte)GameModeID.Normal) return;

            var packet = new Utils.BytePacket.WorldData(e.Data);
            packet.GameMode = menu;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"修改WorldInfo数据包失败: {ex}");
        }
    }
    #endregion

    #region 监听创意模式网络模块事件
    private void OnReadNetModule(object? sender, GetDataHandlers.ReadNetModuleEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.RealPlayer || Config is null || !Config.Enabled) return;

        switch (e.ModuleType)
        {
            case GetDataHandlers.NetModuleType.CreativePowers:
                plr.SendMessage("您正在使用旅途模式力量菜单！", color);
                ParseCreativePowerData(plr, e.Data);
                break;
            case GetDataHandlers.NetModuleType.CreativeUnlocks:
                plr.SendMessage($"您正在进行研究解锁", color);
                break;
            case GetDataHandlers.NetModuleType.CreativePowerPermissions:
                plr.SendMessage($"您正在修改力量权限", color);
                break;
        }
    }

    private void ParseCreativePowerData(TSPlayer player, MemoryStream data)
    {
        try
        {
            data.Position = 0;
            data.ReadInt16();
            short powerId = data.ReadInt16();
            byte[] powerData = data.ReadBytes((int)(data.Length - data.Position));

            var powerType = (GetDataHandlers.CreativePowerTypes)powerId;
            TShock.Log.ConsoleDebug($"[插件模板] {player.Name} 使用了力量: {powerType}");

            switch (powerType)
            {
                case GetDataHandlers.CreativePowerTypes.FreezeTime:
                    bool freeze = BitConverter.ToBoolean(powerData, 0);
                    player.SendInfoMessage($"您{(freeze ? "冻结" : "解冻")}了时间");
                    break;
                case GetDataHandlers.CreativePowerTypes.Godmode:
                    bool godmode = BitConverter.ToBoolean(powerData, 0);
                    player.SendInfoMessage($"您{(godmode ? "开启" : "关闭")}了上帝模式");
                    break;
                case GetDataHandlers.CreativePowerTypes.SetSpawnRate:
                    float spawnRate = BitConverter.ToSingle(powerData, 0);
                    player.SendInfoMessage($"您设置了刷怪率: {spawnRate}");
                    break;
            }
        }
        catch (Exception ex)
        {
            TShock.Log.Error($"解析创意力量数据失败: {ex}");
        }
    }
    #endregion

}