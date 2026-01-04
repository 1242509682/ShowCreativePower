using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using TShockAPI;
using static Plugin.ShowCreativePower;

namespace Plugin;

internal class Utils
{
    #region 性能测试
    public static void Debug()
    {
        TSPlayer[] plrs = TShock.Players;
        string name = "灵乐";

        // 预热
        TSPlayer.All.SendMessage($"测试开始", color);

        // 创建总计时器
        var totalSw = Stopwatch.StartNew();

        // 创建计时器
        var sw = Stopwatch.StartNew();

        TSPlayer? plr = null;

        // 1. Array.Find
        sw.Start();
        plr = Array.Find(plrs, p => p != null && p.Active && p.Name == name);
        sw.Stop();
        plr?.SendMessage($"1.[Array] Array.Find: [c/4297D2:{sw.Elapsed}]", color);

        // 2. LINQ FirstOrDefault
        sw.Restart();
        plr = plrs.FirstOrDefault(p => p != null && p.Active && p.Name == name);
        sw.Stop();
        plr?.SendMessage($"2.[LinQ] FirstOrDefault: [c/E24763:{sw.Elapsed}]", color);

        // 3. 倒序for循环
        sw.Restart();
        for (int j = plrs.Length - 1; j >= 0; j--)
        {
            var p = plrs[j];
            if (p != null && p.Active && p.Name == name)
            {
                plr = p;
                break;
            }
        }
        sw.Stop();
        plr?.SendMessage($"3.[Loop] 倒序For循环: [c/42D295:{sw.Elapsed}]", color);

        // 4. foreach循环
        sw.Restart();
        foreach (var p in plrs)
        {
            if (p != null && p.Active && p.Name == name)
            {
                plr = p;
                break;
            }
        }
        sw.Stop();
        plr?.SendMessage($"4.[Loop] Foreach循环: [c/4296D2:{sw.Elapsed}]", color);

        // 5. while循环
        sw.Restart();
        int index = 0;
        while (index < plrs.Length)
        {
            var p = plrs[index];
            if (p != null && p.Active && p.Name == name)
            {
                plr = p;
                break;
            }
            index++;
        }
        sw.Stop();
        plr?.SendMessage($"5.[Loop] While循环: [c/F36E4C:{sw.Elapsed}]", color);

        // 6. Array.FindIndex
        sw.Restart();
        int index2 = Array.FindIndex(plrs, p => p != null && p.Active && p.Name == name);
        plr = index2 >= 0 ? plrs[index2] : null;
        sw.Stop();
        plr?.SendMessage($"6.[Array] FindIndex: [c/42D297:{sw.Elapsed}]", color);

        // 7. Span遍历
        sw.Restart();
        var span = plrs.AsSpan();
        for (int j = 0; j < span.Length; j++)
        {
            var p = span[j];
            if (p != null && p.Active && p.Name == name)
            {
                plr = p;
                break;
            }
        }
        sw.Stop();
        plr?.SendMessage($"7.[Span] Span遍历: [c/4296D2:{sw.Elapsed}]", color);

        // 8. Parallel并行遍历
        sw.Restart();
        // 用异步避免卡住主线程
        Task.Run(() =>
        {
            Parallel.ForEach(Partitioner.Create(0, plrs.Length), range =>
            {
                for (int j = range.Item1; j < range.Item2 && plr == null; j++)
                {
                    var p = plrs[j];
                    if (p != null && p.Active && p.Name == name)
                    {
                        plr = p;
                    }
                }
            });
        });
        sw.Stop();
        plr?.SendMessage($"8.[Parallel] 并行查找: [c/E34761:{sw.Elapsed}]", color);

        // 9. Array.Find + 静态方法
        sw.Restart();
        plr = Array.Find(plrs, p => FindPlayerByName(p, name));
        sw.Stop();
        plr?.SendMessage($"9.[Array] Find+静态方法: [c/D242C6:{sw.Elapsed}]", color);

        // 停止总计时器
        totalSw.Stop();

        plr?.SendMessage($"总计耗时: [c/F39F4C:{totalSw.Elapsed}]", color);
        plr?.SendMessage($"已完成: 查找1次 名为[c/4296D2:{name}] 的 [c/E34761:玩家] 性能测试", color);
    }

    private static bool FindPlayerByName(TSPlayer p, string name) => p != null && p.Active && p.Name == name;
    #endregion

    #region 逐行渐变色方法
    public static void GradMess(TSPlayer plr, string mess)
    {
        // 处理空值或空字符串
        if (string.IsNullOrEmpty(mess))
            return;

        var lines = mess.Split('\n');
        var GradMess = new StringBuilder();
        var start = new Color(166, 213, 234);
        var end = new Color(245, 247, 175);

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (!string.IsNullOrEmpty(line))
            {
                float ratio = (float)i / (lines.Length - 1);
                var gradColor = Color.Lerp(start, end, ratio);
                string colorHex = $"{gradColor.R:X2}{gradColor.G:X2}{gradColor.B:X2}";

                // 检查是否已有颜色标签
                if (line.Contains("[c/"))
                {
                    // 已有颜色标签，只处理图标
                    GradMess.AppendLine(ReplaceIconsOnly(line));
                }
                else
                {
                    // 没有颜色标签，处理渐变和图标
                    GradMess.AppendLine(ProcessLine(line, colorHex));
                }
            }
            else
            {
                // 空行保留
                GradMess.AppendLine();
            }
        }

        plr.SendMessage(GradMess.ToString(), color);
    }

    // 处理单行渐变和图标
    private static string ProcessLine(string line, string colorHex)
    {
        var result = new StringBuilder();
        int index = 0;
        int length = line.Length;
        int charCount = 0;

        // 先计算需要渐变的总字符数（排除图标标签）
        for (int i = 0; i < length; i++)
        {
            if (line[i] == '[' && i + 1 < length && line[i + 1] == 'i')
            {
                // 跳过整个图标标签
                int end = line.IndexOf(']', i);
                if (end != -1)
                {
                    i = end;
                    continue;
                }
            }
            charCount++;
        }

        // 重置索引，开始处理
        for (int i = 0; i < length; i++)
        {
            char c = line[i];

            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (c == '[' && i + 1 < length && line[i + 1] == 'i')
            {
                int end = line.IndexOf(']', i);
                if (end != -1)
                {
                    string tag = line.Substring(i, end - i + 1);

                    // 解析物品图标标签
                    if (TryParseItemTag(tag, out string iconTag))
                    {
                        result.Append(iconTag);
                    }
                    else
                    {
                        result.Append(tag); // 无效标签保留原样
                    }

                    i = end; // 跳过整个标签
                }
                else
                {
                    // 不完整的标签，按普通字符处理
                    result.Append($"[c/{colorHex}:{c}]");
                    charCount++;
                }
            }
            else
            {
                // 普通字符，使用渐变色
                result.Append($"[c/{colorHex}:{c}]");
                charCount++;
            }
        }

        return result.ToString();
    }
    #endregion

    #region 渐变着色方法 + 物品图标解析
    public static string TextGradient(string text)
    {
        // 处理空值或空字符串
        if (string.IsNullOrEmpty(text))
            return text;

        // 如果文本中已包含 [c/xxx:] 自定义颜色标签，则不做渐变，只替换图标
        if (text.Contains("[c/"))
        {
            return ReplaceIconsOnly(text);
        }

        var name = new StringBuilder();
        int length = text.Length;
        int gradientIndex = 0; // 渐变索引，排除换行符

        // 首先计算需要渐变的总字符数（排除换行符和图标标签）
        int gradientCharCount = 0;
        for (int i = 0; i < length; i++)
        {
            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (text[i] == '[' && i + 2 < length && text[i + 1] == 'i')
            {
                // 跳过整个图标标签
                int end = text.IndexOf(']', i);
                if (end != -1)
                {
                    i = end;
                    continue;
                }
            }
            else if (text[i] != '\n' && text[i] != '\r')
            {
                gradientCharCount++;
            }
        }

        // 重置索引
        for (int i = 0; i < length; i++)
        {
            char c = text[i];

            // 处理换行符 - 直接保留
            if (c == '\n' || c == '\r')
            {
                name.Append(c);
                continue;
            }

            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (c == '[' && i + 1 < length && text[i + 1] == 'i')
            {
                int end = text.IndexOf(']', i);
                if (end != -1)
                {
                    string tag = text.Substring(i, end - i + 1);

                    // 解析物品图标标签
                    if (TryParseItemTag(tag, out string iconTag))
                    {
                        name.Append(iconTag);
                    }
                    else
                    {
                        name.Append(tag); // 无效标签保留原样
                    }

                    i = end; // 跳过整个标签
                }
                else
                {
                    name.Append(c);
                    gradientIndex++;
                }
            }
            else
            {
                // 渐变颜色计算，排除换行符
                var start = new Color(166, 213, 234);
                var endColor = new Color(245, 247, 175);
                float ratio = gradientCharCount <= 1 ? 0.5f : (float)gradientIndex / (gradientCharCount - 1);
                var color = Color.Lerp(start, endColor, ratio);

                name.Append($"[c/{color.Hex3()}:{c}]");
                gradientIndex++;
            }
        }

        return name.ToString();
    }

    // 解析物品图标标签
    private static bool TryParseItemTag(string tag, out string result)
    {
        result = tag;

        // 匹配 [i:物品ID] 格式
        var match1 = Regex.Match(tag, @"^\[i:(\d+)\]$");
        if (match1.Success)
        {
            if (int.TryParse(match1.Groups[1].Value, out int itemID))
            {
                result = ItemIcon(itemID);
                return true;
            }
        }

        // 匹配 [i/s数量:物品ID] 格式
        var match2 = Regex.Match(tag, @"^\[i/s(\d+):(\d+)\]$");
        if (match2.Success)
        {
            if (int.TryParse(match2.Groups[2].Value, out int itemID))
            {
                int stack = int.Parse(match2.Groups[1].Value);
                result = ItemIconStack(itemID, stack);
                return true;
            }
        }

        return false;
    }
    #endregion

    #region 只替换图标，不做渐变
    public static string ReplaceIconsOnly(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        var result = new StringBuilder();
        int index = 0;
        int length = text.Length;

        while (index < length)
        {
            char c = text[index];

            // 检查是否是图标标签 [i:xxx] 或 [i/s数量:xxx]
            if (c == '[' && index + 1 < length && text[index + 1] == 'i')
            {
                int end = text.IndexOf(']', index);
                if (end != -1)
                {
                    string tag = text.Substring(index, end - index + 1);

                    if (TryParseItemTag(tag, out string iconTag))
                    {
                        result.Append(iconTag);
                    }
                    else
                    {
                        result.Append(tag);
                    }

                    index = end + 1;
                }
                else
                {
                    result.Append(c);
                    index++;
                }
            }
            else
            {
                result.Append(c);
                index++;
            }
        }

        return result.ToString();
    }
    #endregion

    #region 优化广播消息（处理换行符）
    public static string BroadcastGradient(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        // 按行分割，每行单独处理渐变
        var lines = text.Split('\n');
        var result = new StringBuilder();

        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex];

            // 去除可能的回车符
            line = line.TrimEnd('\r');

            if (!string.IsNullOrEmpty(line))
            {
                // 对每一行进行渐变处理
                result.Append(TextGradient(line));
            }

            // 添加换行符（除了最后一行）
            if (lineIndex < lines.Length - 1)
            {
                result.Append('\n');
            }
        }

        return result.ToString();
    }
    #endregion

    #region 返回物品图标方法
    // 方法：ItemIcon，根据给定的物品对象返回插入物品图标的格式化字符串
    public static string ItemIcon(Item item)
    {
        return ItemIcon(item.type);
    }

    // 方法：ItemIcon，根据给定的物品ID返回插入物品图标的格式化字符串
    public static string ItemIcon(ItemID itemID)
    {
        return ItemIcon(itemID);
    }

    // 方法：ItemIcon，根据给定的物品整型ID返回插入物品图标的格式化字符串
    public static string ItemIcon(int itemID)
    {
        return $"[i:{itemID}]";
    }

    // 方法：ItemIconStack，返回带数量的物品图标
    public static string ItemIconStack(int itemID, int stack)
    {
        return $"[i/s{stack}:{itemID}]";
    }
    #endregion

    #region 将字符串换行
    public static List<string> WarpLines(List<string> lines, int column = 5)
    {
        List<string> li1 = new();
        List<string> li2 = new();
        foreach (var line in lines)
        {
            if (li2.Count % column == 0)
            {
                if (li2.Count > 0)
                {
                    li1.Add(string.Join(", ", li2));
                    li2.Clear();
                }
            }
            li2.Add(line);
        }
        if (li2.Any())
        {
            li1.Add(string.Join(", ", li2));
        }
        return li1;
    }
    #endregion

    #region 西江的世界数据包
    internal struct BytePacket
    {
        public static (int intLength, int value) Read7BitEncodedInt(Span<byte> data)
        {
            uint num = 0u;
            byte b;
            var idx = 0;
            for (int i = 0; i < 28; i += 7)
            {
                b = data[idx++];
                num |= (uint)((b & 0x7F) << i);
                if (b <= 127u)
                {
                    return (idx, (int)num);
                }
            }
            b = data[idx++];
            if (b > 15u)
            {
                throw new FormatException("Bad7BitInt");
            }
            return (idx, (int)num | (b << 28));
        }

        #region 世界数据包结构
        internal ref struct WorldData
        {
            private readonly byte[] data;

            public WorldData(byte[] byteData)
            {
                data = byteData;
            }

            // 计算GameMode字段的偏移量
            private int GetGameModeOffset()
            {
                int offset = 3; // 跳过包头

                // 跳过时间(4) + 标志(1) + 月相(1)
                offset += 4 + 1 + 1;

                // 跳过世界尺寸(2+2) + 出生点(2+2) + 地表(2) + 岩石层(2) + 世界ID(4)
                offset += 2 + 2 + 2 + 2 + 2 + 2 + 4;

                // 跳过世界名称（7位编码长度前缀 + 字符串）
                offset += SkipString(data, offset);

                return offset; // 这就是GameMode字段的位置
            }

            private static int SkipString(byte[] data, int offset)
            {
                int idx = 0;
                int length = 0;

                // 读取7位编码的长度
                while (true)
                {
                    byte b = data[offset + idx];
                    length |= (b & 0x7F) << (7 * idx);
                    idx++;

                    if ((b & 0x80) == 0) break;
                }

                return idx + length;
            }

            // GameMode属性
            public byte GameMode
            {
                get => data[GetGameModeOffset()];
                set => data[GetGameModeOffset()] = value;
            }
        }
        #endregion
    }
    #endregion
}
