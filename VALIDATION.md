# RealChute 汉化校验报告（VALIDATION）

对应版本：RealChute **1.4.9.5**（源码 `RealChute-master`，分发目录 `RealChute`）
校验日期：2026-10-09

所有结论均由 `verify_localization.py` 与 `verify_dll_strings.py` 自动产出，可随时重跑。

---

## 1. 字典一致性（`verify_localization.py`）

```
en-us 键数: 223
zh-cn 键数: 223
源码引用的键数: 223 (C# 207 + cfg 16)

校验通过 / PASSED (0 个警告)
```

| 检查项 | 结果 |
|---|---|
| en-us / zh-cn 键集合完全一致（无缺失、无多余） | ✅ 223 / 223 |
| 源码引用的键全部有定义 | ✅ 223 / 223 |
| 定义的键全部被引用（无孤儿键） | ✅ 0 孤儿 |
| 占位符序号从 1 连续、中英一致 | ✅ 通过 |
| 值不以 `#` 开头、不含真实换行 | ✅ 通过 |
| UTF-8 无 BOM | ✅ 通过 |

---

## 2. DLL 字面量核对（`verify_dll_strings.py`）

```
DLL: Output\GameData\RealChute\Plugins\RealChute.dll
  大小 / size: 142336 bytes
C# 直接引用 Localization.Get 的键 : 192
C# 以 #RealChute_x 引用的键      : 15
cfg 引用的键                     : 16

OK: DLL 内含全部 192 个 Localization.Get 字面量 + 15 个带 # 的字面量
OK: en-us.cfg 的 223 个键全部被引用，cfg 引用全部有定义
校验通过 / PASSED
```

> 实现要点：Roslyn 把运行期字符串字面量放进 `#US` 堆（UTF-16LE），而
> `[KSPAction("...")]` / `[KSPEvent(guiName="...")]` 的**特性实参**放进元数据
> `#Strings` 堆（UTF-8）。因此逐键核对时两种编码都要查，脚本已按此实现。

---

## 3. 残留硬编码英文

对 DLL 逐条检查原版界面文本，**全部已消失**（改为本地化键）：

| 原硬编码文本 | 在新 DLL 中 |
|---|---|
| `Automatically arm when staging` | ❌ 不存在 |
| `Must go down to deploy:` | ❌ 不存在 |
| `Deploy Chute` | ❌ 不存在 |
| `Show Parachute Editor` | ❌ 不存在 |
| `Parachute deployment failed.` | ❌ 不存在 |
| `Main chute` / `Secondary chute` | ❌ 不存在 |
| `No saved presets` | ❌ 不存在 |
| `Apply settings` | ❌ 不存在 |
| `Choose material` | ❌ 不存在 |
| `Parachute material` | ❌ 不存在 |
| `Copy to others chutes` | ❌ 不存在 |
| `Toggle info` | ❌ 不存在 |

---

## 4. 编码与文件完整性

最终分发目录 `RealChute/` 下全部被改动的 9 个 cfg：

| 文件 | BOM | UTF-8 合法 | 字节 |
|---|---|---|---|
| `Localization/en-us.cfg` | 无 | ✅ | 17368 |
| `Localization/zh-cn.cfg` | 无 | ✅ | 17089 |
| `Materials/ParachuteMaterials.cfg` | 无 | ✅ | 1771 |
| `Subcategories/parachutes.cfg` | 无 | ✅ | 971 |
| `Agencies/Agents.cfg` | 无 | ✅ | 735 |
| `Parts/cone_chute.cfg` | 无 | ✅ | 6149 |
| `Parts/cone_double_chute.cfg` | 无 | ✅ | 7085 |
| `Parts/radial_chute.cfg` | 无 | ✅ | 5516 |
| `Parts/stack_chute.cfg` | 无 | ✅ | 7234 |

---

## 5. 程序集

| 项目 | 值 |
|---|---|
| 名称 | `RealChute` |
| 程序集版本 | `1.4.9778.*`（源项目用通配符 `1.4.*`，与原版同规则） |
| 产品版本 | `1.4.9.5`（与原版一致） |
| 大小 | 142336 字节（原版 140288 字节，差异来自新增本地化调用） |
| 引用 | `Assembly-CSharp`、`UnityEngine.*`、`ClickThroughBlocker`、`ToolbarControl` |
| 依赖声明 | `KSPAssemblyDependency("ClickThroughBlocker",1,0)`、`KSPAssemblyDependency("ToolbarController",1,0)` 保留 |

---

## 6. 机制同源证据

本地化写法与游戏本体及其他模组一致，非自创机制：

| 证据 | 结果 |
|---|---|
| `KSP.Localization.Localizer` 提供 `TryGetStringByTag` / `Format` | ✅ 反射确认存在 |
| `<<1>>` 序号占位符写法 | ✅ 与 `Squad/Localization/dictionary.cfg` 一致 |
| `Localization/` 目录约定 | ✅ 本机 GameData 内 `localization` 目录 **279** 个 cfg 采用 |
| 内嵌 `Localization` 节点（cfg 文本就地维护） | ✅ 本机 **139** 个 cfg 采用 |
| 内嵌节点同时含 `en-us` 与 `zh-cn` | ✅ 本机 **11** 个 cfg 采用 |
| `title`/`description`/`manufacturer` 用 `#` 键 | ✅ Squad 分别有 543 / 565 / 391 处 |
| 部件动作/事件名用 `#autoLOC_*` | ✅ Squad 中 `actionGUIName` 28 处、`startEventGUIName` 25 处 |
| 模组 DLL 内嵌本地化键字面量 | ✅ 本机 **23** 个模组 DLL 采用（KerbalPowerPlants 16 个键等） |

---

## 7. 未做 / 遗留

| 项目 | 说明 |
|---|---|
| 游戏内实测 | **未做**。本机校验均为静态（源码、字典、DLL 字面量、编码）。请按第 8 节装载后实测 |
| 预设名（`PRESET.name`） | 有意保留英文。预设名写入 `Plugins/PluginData/Presets.cfg`，改名会与已有存档不一致；若希望汉化，需连同已有存档内的预设名一并处理 |
| 贴图/模型名 | 有意保留英文（查找用标识符，翻译会导致引用失效） |
| 调试日志/异常信息 | 有意保留英文（非玩家可见文本） |
| 其他语言 | 未做。按 `LOCALIZATION.md` 第 6 节新增即可 |

---

## 8. 实测步骤（建议）

1. 确认 KSP 为 **1.12.3**（本机 `G:\Kerbal Space Program`，build id 03190）；
2. 备份 `GameData/RealChute/`；
3. 用 `K:\AI代码\RealChute\` 覆盖 `GameData/RealChute/`；
4. 启动游戏，把语言设为**简体中文**；
5. 需要检查的位置：
   - **机库**：部件列表里 4 个 RealChute 部件的名称/厂商/说明；
   - **机库**：装配后右键「显示降落伞编辑器」，编辑器内全部标签、预设窗口、材质窗口；
   - **机库**：部件信息面板（`GetInfo`）与「部件信息」按钮；
   - **飞行**：右键菜单（展开/切断/上保险/重新打包/切换信息窗口）、动作组；
   - **飞行**：信息窗口、各伞参数、展开安全性、温度；
   - **飞行**：分级后屏幕中央提示、展开失败原因、工程师等级不足提示；
   - **航天中心**：工具栏 RealChute 设置窗口；
   - **机构**：RealChute 与温克尔公司的名称与简介。
6. 切换回 English 复核英文回退是否正常。

> 若某处仍显示 `#RealChute_xxx` 形式的原文，说明该键未被游戏词典命中——
> 把该键名反馈即可，`verify_localization.py` 会立刻定位到它的引用位置。
