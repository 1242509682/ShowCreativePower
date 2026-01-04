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
    public override Version Version => new(1, 0, 1);
    public override string Description => "在非旅途模式下显示旅途力量菜单";
    #endregion

    #region 全局字段
    public static string Scp => "scp";
    public static HashSet<int> Join = new HashSet<int>();
    public static string NeedFix => "fix";
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
        ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        OTAPI.Hooks.NetMessage.SendBytes += OnSendBytes;
        TShockAPI.Commands.ChatCommands.Add(new Command($"{Scp}.use", Commands.ScpCmd, Scp));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            OTAPI.Hooks.NetMessage.SendBytes -= OnSendBytes;
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

    #region 玩家进入与离开服务器
    private void OnGreetPlayer(GreetPlayerEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr == null || !plr.RealPlayer || !plr.Active ||
            Config is null || !Config.Enabled) return;

        // 检查玩家是否在名单中或为管理员 不是则返回
        if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr)) return;

        if (Config.Join && !Join.Contains(plr.Index))
        {
            Join.Add(plr.Index);
        }
    }

    private void OnLeave(LeaveEventArgs args)
    {
        var plr = TShock.Players[args.Who];
        if (plr != null)
        {
            // 移除标记
            plr.RemoveData(NeedFix);
        }
    }
    #endregion

    #region 玩家更新事件,用于显示旅途菜单
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (plr == null || !plr.RealPlayer ||
            Config is null || !Config.Enabled) return;

        // 检查玩家是否在名单中或为管理员 不是则返回
        if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr)) return;

        // 检查玩家是否刚加入且自动开启
        if (Join.Contains(plr.Index))
        {
            SwitchMenu(plr, true);
        }
    }
    #endregion

    #region 菜单开关方法
    internal static void SwitchMenu(TSPlayer plr, bool flag)
    {
        // 设置自动修改标记
        plr.SetData(NeedFix, flag);

        // 发送玩家信息更新包(帮助PC玩家显示力量菜单)
        plr.TPlayer.difficulty = flag ? (byte)GameModeID.Creative : (byte)GameModeID.Normal;
        NetMessage.SendData((int)PacketTypes.PlayerInfo, -1, -1, null, plr.Index);

        // 发送世界信息(帮助PE玩家显示力量菜单),OTAPI钩子的OnSendBytes方法会修改WorldInfo包
        if (Config.PE)
            NetMessage.SendData((int)PacketTypes.WorldInfo, plr.Index);

        // 解锁全物品研究
        string mess = "";
        if (Config.Unlock && flag)
        {
            var item = new Item();
            for (int i = 0; i < ItemID.Count; i++)
            {
                if (Config.ItemList.Contains(i)) continue;

                item.SetDefaults(i);
                var response = NetCreativeUnlocksModule.SerializeItemSacrifice(i, item.maxStack);
                NetManager.Instance.SendToClient(response, plr.Index);
            }

            mess += ",已为你[c/41D396:解锁所有]研究物品";
        }

        string open = flag ? "[c/4296D2:开启]" : "[c/E34761:关闭]";
        plr.SendMessage($"\n旅途力量菜单已{open}" + mess, color);

        // 移除加入标记
        if (Join.Contains(plr.Index))
        {
            Join.Remove(plr.Index);
        }
    }
    #endregion

    #region SendBytes钩子 - 修改WorldInfo方法
    public void OnSendBytes(object? sender, OTAPI.Hooks.NetMessage.SendBytesEventArgs e)
    {
        if (!Config.Enabled || !Config.PE || e.Data == null || e.Data.Length < 30)
        {
            return;
        }

        // 检查是否为WorldInfo数据包
        if (e.Data[2] != (byte)PacketTypes.WorldInfo) return;

        // 尝试从Netplay.Clients查找对应的玩家
        var client = Netplay.Clients[e.RemoteClient];
        if (client == null || !client.IsActive ||
            client.Id < 0 || client.Id >= TShock.Players.Length) return;

        // 判断玩家有效性
        var plr = TShock.Players[client.Id];
        if (plr == null || !plr.RealPlayer) return;

        try
        {
            // 检查玩家是否应该显示菜单
            if (!plr.GetData<bool>(NeedFix)) return;

            var packet = new Utils.BytePacket.WorldData(e.Data);
            packet.GameMode = (byte)GameModeID.Creative;
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"修改WorldInfo数据包失败: {ex}");
        }
    }
    #endregion

}