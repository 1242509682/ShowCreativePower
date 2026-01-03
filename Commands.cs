using System.Text;
using Terraria.ID;
using TShockAPI;
using static Plugin.ShowCreativePower;

namespace Plugin;

internal class Commands
{
    #region 主指令方法
    internal static void ScpCmd(CommandArgs args)
    {
        if (!Config.Enabled) return;

        var plr = args.Player;

        if (args.Parameters.Count == 0)
        {
            HelpCmd(plr);
            return;
        }

        string other;
        TSPlayer? plr2;
        switch (args.Parameters[0].ToLower())
        {
            case "on":
                if (!plr.RealPlayer) 
                { 
                    plr.SendInfoMessage("控制台无法使用此命令！");
                    return;
                }

                if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr))
                {
                    plr.SendInfoMessage("你不在旅途力量名单中，无法使用此命令！");
                    return;
                }
                   
                plr.SetData(Scp, (byte)GameModeID.Creative);
                SwitchMenu(plr, true);
                break;

            case "off":
                if (!plr.RealPlayer)
                {
                    plr.SendInfoMessage("控制台无法使用此命令！");
                    return;
                }

                if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr))
                {
                    plr.SendInfoMessage("你不在旅途力量名单中，无法使用此命令！");
                    return;
                }

                plr.SetData(Scp, (byte)GameModeID.Normal);
                SwitchMenu(plr, false);
                break;

            case "ls":
            case "list":
                var lines = Utils.WarpLines(Config.PlayerNames);
                if (lines.Count == 0)
                {
                    plr.SendInfoMessage("名单为空！");
                    return;
                }
                lines.Insert(0, "旅途力量名单:");
                plr.SendMessage(string.Join("\n", lines), color);
                break;

            case "pe":
                if (!IsAdmin(plr)) return;
                Config.PE = !Config.PE;
                Config.Write();
                plr.SendMessage($"允许手游显示旅途力量设为: {(Config.PE ? "[c/4296D2:开启]" : "[c/E34761:关闭]")}", color);
                break;

            case "at":
            case "auto":
                if (!IsAdmin(plr)) return;
                Config.AutoOpen = !Config.AutoOpen;
                Config.Write();
                plr.SendMessage($"进服显示旅途力量设为: {(Config.AutoOpen ? "[c/4296D2:开启]" : "[c/E34761:关闭]")}", color);
                break;

            case "add":
                if (!IsAdmin(plr)) return;
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendInfoMessage("请输入玩家名，用法：/spm add <玩家名>");
                    return;
                }

                other = args.Parameters[1];
                if (Config.PlayerNames.Contains(other))
                {
                    args.Player.SendInfoMessage($"玩家 {other} 已在旅途力量名单中！");
                    return;
                }

                Config.PlayerNames.Add(other);
                Config.Write();

                plr2 = Array.Find(TShock.Players, p => p != null && p.Active && p.Name == other);
                if (plr2 != null)
                {
                    plr2.SetData(Scp, (byte)GameModeID.Creative);
                    SwitchMenu(plr2, true);
                    plr2.SendMessage($"你已被管理员 [c/4296D2:{plr.Name}] 添加旅途力量名单", color);
                }
                args.Player.SendSuccessMessage($"已将玩家 [c/4296D2:{other}] 添加到旅途力量名单！");

                break;

            case "del":
            case "remove":
                if (!IsAdmin(plr)) return;
                if (args.Parameters.Count < 2)
                {
                    args.Player.SendInfoMessage("请输入玩家名，用法：/spm del <玩家名>");
                    return;
                }

                other = args.Parameters[1];
                if (!Config.PlayerNames.Contains(other))
                {
                    plr.SendMessage($"玩家 {other} 不在旅途力量名单！", color);
                    return;
                }

                Config.PlayerNames.Remove(other);
                Config.Write();

                plr2 = Array.Find(TShock.Players, p => p != null && p.Active && p.Name == other);
                if (plr2 != null)
                {
                    plr2.SetData(Scp, (byte)GameModeID.Normal);
                    SwitchMenu(plr2, false);
                    plr2.SendMessage($"你已被管理 [c/4296D2:{plr.Name}] 移出旅途力量名单", color);
                }

                plr.SendMessage($"已将玩家 [c/4296D2:{other}] 移出旅途力量名单", color);
                break;

            default:
                HelpCmd(plr);
                break;
        }
    }
    #endregion

    #region 菜单方法
    private static void HelpCmd(TSPlayer plr)
    {
        if (plr.RealPlayer)
        {
            var mess = new StringBuilder();

            plr.SendMessage("[i:3455][c/AD89D5:显示][c/D68ACA:旅途][c/DF909A:力][c/E5A894:量][i:3454] " +
            "[i:3456][C/F2F2C7:开发] [C/BFDFEA:by] [c/00FFFF:羽学|西江] [i:3459]", color);

            mess.Append($"/{Scp} on - 开启你的力量菜单\n" +
                        $"/{Scp} off - 关闭你的力量菜单\n" +
                        $"/{Scp} list - 查看名单\n");


            if (IsAdmin(plr))
            {
                mess.Append($"/{Scp} at - 切换进服自动开启\n" +
                            $"/{Scp} pe - 切换手游是否显示\n" +
                            $"/{Scp} list - 查看名单\n" +
                            $"/{Scp} add <玩家名> - 添加玩家到名单\n" +
                            $"/{Scp} del <玩家名> - 移除玩家从名单\n");
            }

            Utils.GradMess(plr, mess.ToString());
        }
        else
        {
            plr.SendMessage("《显示旅途力量》\n" +
                            $"/{Scp} at - 切换进服自动开启\n" +
                            $"/{Scp} pe - 切换手游是否显示\n" +
                            $"/{Scp} list - 查看名单\n" +
                            $"/{Scp} add <玩家名> - 添加玩家到名单\n" +
                            $"/{Scp} del <玩家名> - 移除玩家从名单\n",color);
        }
    }
    #endregion

}