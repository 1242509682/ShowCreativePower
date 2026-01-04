using System.Text;
using TShockAPI;
using Terraria;
using Terraria.GameContent.NetModules;
using Terraria.ID;
using Terraria.Net;
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

                SwitchMenu(plr, false);
                break;

            case "ls":
            case "list":
                if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr))
                {
                    plr.SendInfoMessage("你不在旅途力量名单中，无法使用此命令！");
                    return;
                }

                var lines = Utils.WarpLines(Config.PlayerNames, Config.Lines);
                if (lines.Count == 0)
                {
                    plr.SendInfoMessage("旅途力量名单为空！");
                    return;
                }
                lines.Insert(0, "旅途力量名单:");
                plr.SendMessage(string.Join("\n", lines), color);
                break;

            case "i":
            case "item":
                ShowItemList(plr);
                break;

            case "is":
            case "items":
                if (!IsAdmin(plr)) return;
                HandleItems(plr, args.Parameters);
                break;

            case "pe":
                if (!IsAdmin(plr)) return;
                Config.PE = !Config.PE;
                Config.Write();
                TSPlayer.All.SendMessage($"允许手游显示旅途力量设为: {(Config.PE ? "[c/4296D2:开启]" : "[c/E34761:关闭]")}", color);
                break;

            case "j":
            case "join":
                if (!IsAdmin(plr)) return;
                Config.Join = !Config.Join;
                Config.Write();
                plr.SendMessage($"进服显示旅途力量设为: {(Config.Join ? "[c/4296D2:开启]" : "[c/E34761:关闭]")}", color);
                break;

            case "md":
                if (!IsAdmin(plr)) return;
                HandlePlayers(args, plr);
                break;

            default:
                HelpCmd(plr);
                break;
        }
    }
    #endregion

    #region 修改玩家名单方法
    private static void HandlePlayers(CommandArgs args, TSPlayer plr)
    {
        if (args.Parameters.Count < 2)
        {
            plr.SendInfoMessage($"请输入玩家名，用法：/{Scp} md <玩家名>");
            plr.SendInfoMessage($"存在则移除,不在则添加");
            return;
        }

        TSPlayer? plr2;
        var other = args.Parameters[1];

        if (Config.PlayerNames.Contains(other))
        {
            // 从名单中移除
            Config.PlayerNames.Remove(other);
            Config.Write();

            plr2 = Array.Find(TShock.Players, p => p != null && p.Active && p.Name == other);
            if (plr2 != null)
            {
                SwitchMenu(plr2, false);
                plr2.SendMessage($"你已被管理 [c/4296D2:{plr.Name}] 移出旅途力量名单", color);
            }

            plr.SendMessage($"已将玩家 [c/4296D2:{other}] 移出旅途力量名单", color);
        }
        else
        {
            // 添加到名单
            Config.PlayerNames.Add(other);
            Config.Write();

            plr2 = Array.Find(TShock.Players, p => p != null && p.Active && p.Name == other);
            if (plr2 != null)
            {
                SwitchMenu(plr2, true);
                plr2.SendMessage($"你已被管理员 [c/4296D2:{plr.Name}] 添加旅途力量名单", color);
            }

            plr.SendSuccessMessage($"已将玩家 [c/4296D2:{other}] 添加到旅途力量名单！");
        }
    } 
    #endregion

    #region 排除物品查询方法
    private static void ShowItemList(TSPlayer plr)
    {
        if (Config.ItemList.Count == 0)
        {
            plr.SendMessage("排除研究物品表为空", color);
            return;
        }

        if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr))
        {
            plr.SendInfoMessage("你不在旅途力量名单中，无法使用此命令！");
            return;
        }

        // 使用WarpLines方法将物品列表格式化
        var itemNames = Config.ItemList.Select(itemId =>
            plr.RealPlayer ?
            Utils.ItemIconStack(itemId, 1) :
            Lang.GetItemNameValue(itemId)
        ).ToList();

        var lines = Utils.WarpLines(itemNames, Config.Lines);
        lines.Insert(0, "排除研究物品表:");

        plr.SendMessage(string.Join("\n", lines), color);
    }
    #endregion

    #region 修改补偿物品方法
    private static void HandleItems(TSPlayer plr, List<string> parm)
    {
        // 检查参数
        if (parm.Count < 2)
        {
            plr.SendMessage($"用法: /{Scp} items [物品名|-i]", color);
            plr.SendMessage("输物品名,存在则移除,不在则添加", color);
            plr.SendMessage("注:物品名也可以是物品id,电脑玩家支持:Alt+鼠标左键选择物品图标", color);
            plr.SendMessage($"/{Scp} items -i 则获取手上选择的物品来决定添加或移除", color);
            return;
        }

        // 检查是否为 -i 参数
        if (parm[1].ToLower() == "-i")
        {
            // 检查是否为真人玩家
            if (!plr.RealPlayer)
            {
                plr.SendMessage("控制台不能使用 -i 参数", color);
                return;
            }

            // 获取玩家当前手持物品
            var sel = plr.SelectedItem;

            // 检查物品是否有效
            if (sel == null || sel.type <= 0 || sel.stack <= 0)
            {
                plr.SendMessage("请先选择有效的物品", color);
                plr.SendMessage("提示:手持物品或Alt+鼠标左键选择物品图标", color);
                return;
            }

            UpdateItems(plr, sel.type);
        }
        else
        {
            var item = GetItem(plr, parm[1]);
            if (item == null) return;

            UpdateItems(plr, item.netID);
        }
    }

    private static Item? GetItem(TSPlayer plr, string itemName)
    {
        var items = TShock.Utils.GetItemByIdOrName(itemName);

        if (items.Count == 0)
        {
            plr.SendErrorMessage($"未找到物品: {itemName}");
            return null;
        }

        if (items.Count > 1)
        {
            plr.SendMultipleMatchError(items.Select(i => $"{i.Name}(ID:{i.netID})"));
            return null;
        }

        return items[0];
    }
    #endregion

    #region 刷新所有玩家解锁状态方法
    private static void UpdateItems(TSPlayer plr, int itemId)
    {
        string itemDisplay = plr.RealPlayer ?
            Utils.ItemIcon(itemId) :
            Lang.GetItemNameValue(itemId);

        string msg;
        if (Config.ItemList.Contains(itemId))
        {
            Config.ItemList.Remove(itemId);
            msg = $"[c/F5636F:已移除]研究排除物品: {itemDisplay}";
        }
        else
        {
            Config.ItemList.Add(itemId);
            msg = $"[c/508DC8:已添加]研究排除物品: {itemDisplay}";
        }

        Config.Write();
        plr.SendMessage(msg, color);

        // 刷新所有在线玩家的解锁状态
        var item = new Item();

        // 遍历所有在线玩家
        foreach (var plr2 in TShock.Players)
        {
            // 检查玩家是否在名单中或是管理员 都不是直接跳过
            if (!Config.PlayerNames.Contains(plr2.Name) && !IsAdmin(plr2)) continue;

            if (plr2 != null && plr2.Active && plr2.RealPlayer)
            {
                // 检查玩家是否开启了菜单
                if (plr2.GetData<bool>(NeedFix))
                {
                    // 一次遍历刷新所有物品
                    for (int i = 0; i < ItemID.Count; i++)
                    {
                        item.SetDefaults(i);
                        int count = Config.ItemList.Contains(i) ? 0 : item.maxStack;
                        var response = NetCreativeUnlocksModule.SerializeItemSacrifice(i, count);
                        NetManager.Instance.SendToClient(response, plr2.Index);
                    }

                    // 只发给其他玩家
                    if (plr != plr2)
                        plr2.SendMessage("[c/41D396:研究物品列表已刷新]\n" + msg, color);
                }
            }
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

            // 检查玩家是否在名单中或为管理员 不是则返回
            if (!Config.PlayerNames.Contains(plr.Name) && !IsAdmin(plr))
            {
                var lines = Utils.WarpLines(Config.PlayerNames, Config.Lines);
                if (lines.Count == 0)
                {
                    plr.SendInfoMessage("您不在[c/4196D3:旅途力量名单]中,[c/E44660:无法使用]本插件指令！");
                    return;
                }
                lines.Insert(0, "您不在[c/4196D3:旅途力量名单]中,[c/E44660:无法使用]本插件指令");
                plr.SendMessage(string.Join("\n", lines), color);
                return;
            }

            // 显示普通玩家的指令内容
            mess.Append($"/{Scp} on - 开启你的力量菜单\n" +
                        $"/{Scp} off - 关闭你的力量菜单\n" +
                        $"/{Scp} ls - 查看玩家名单\n" +
                        $"/{Scp} i - 查看排除物品表\n");


            // 显示管理员的指令内容
            if (IsAdmin(plr))
            {
                mess.Append($"/{Scp} is - 修改排除物品表\n" +
                            $"/{Scp} j - 切换进服自动开启\n" +
                            $"/{Scp} pe - 切换手游是否显示\n" +
                            $"/{Scp} md <玩家名> - 修改玩家名单");
            }

            // 渐变色菜单
            Utils.GradMess(plr, mess.ToString());
        }
        else
        {
            plr.SendMessage("《显示旅途力量》\n" +
                            $"/{Scp} j - 切换进服自动开启\n" +
                            $"/{Scp} pe - 切换手游是否显示\n" +
                            $"/{Scp} ls - 查看名单\n" +
                            $"/{Scp} i - 查看排除物品表\n" +
                            $"/{Scp} is - 修改排除物品表\n" +
                            $"/{Scp} md <玩家名> - 添加与移除玩家到名单", color);
        }
    }
    #endregion

}