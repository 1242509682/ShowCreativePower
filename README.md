# ShowCreativePower 显示旅途力量
- 作者: 羽学、西江小子
- 出处: TShock官方群816771079
- 这是一个Tshock服务器插件，主要用于：在非旅途模式下对配置名单内的玩家显示力量菜单，并支持手游。

## 更新日志

```
v1.0.0
在Hufang的ShowPowerMenu基础上进行加强
支持PE显示力量菜单（由西江小子贡献代码）
优化了移除名单后不再踢出玩家
目前存在BUG:
手游玩家的力量菜单偶尔会消失,需要重新使用指令/spm on 开启
```

## 指令

| 语法                             | 别名  |       权限       |                   说明                   |
| -------------------------------- | :---: | :--------------: | :--------------------------------------: |
| /spm  | 无 |   spm.use    |    指令菜单    |
| /spm on | 无 |   spm.use    |    开启自己的力量菜单    |
| /spm off | 无 |   spm.use    |    关闭自己的力量菜单    |
| /spm list | ls |   spm.use    |    列出力量名单    |
| /spm pe | 无 |   spm.admin    |    切换手游是否显示    |
| /spm auto | at |   spm.admin    |    切换进服开关    |
| /spm add <玩家名> | 无 |   spm.admin    |    添加玩家名单并开启    |
| /spm del <玩家名> | 无 |   spm.admin    |    移除玩家名单并关闭    |
| /reload  | 无 |   tshock.cfg.reload    |    重载配置文件    |

## 配置
> 配置文件位置：tshock/显示旅途力量.json
```json
{
  "插件开关": true,
  "允许手游显示力量菜单": true,
  "解锁全物品研究": true,
  "进服自动显示菜单": true,
  "旅途力量名单": [
    "羽学",
    "西江",
    "灵乐",
    "安安"
  ]
}
```
## 反馈
- 优先发issued -> 共同维护的插件库：https://github.com/UnrealMultiple/TShockPlugin
- 次优先：TShock官方群：816771079
- 大概率看不到但是也可以：国内社区trhub.cn ，bbstr.net , tr.monika.love