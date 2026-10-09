# RealChute 汉化说明（KSP 官方 Localization 机制）

本仓库是 **RealChute**（v1.4.9.5）的本地化分支。原版把全部界面文本硬编码在
C# 源码与 `.cfg` 里，本分支将其重构为 KSP 官方 Localization 机制，并提供完整的
简体中文。

---

## 1. 做了什么

| 项目 | 数值 |
|---|---|
| 处理的 C# 文件 | 10 个（含新增的 `Localization.cs`） |
| 处理的 cfg 文件 | 9 个 |
| 硬编码用户可见文本 | 207 处（C#） |
| 抽出的本地化键 | **223 个唯一键** |
| 新增文件 | `RealChute/Localization/en-us.cfg`、`zh-cn.cfg`、`RealChute/Localization.cs` |
| 重构后残留硬编码英文 | **0** |
| 与原版的结构差异 | 仅「被本地化的字段」与「新增的 Localization 节点」 |

玩法、物理、数值、条件、奖励逻辑**完全未改动**；只替换文本。

> 说明：调试日志（`Debug.Log`/`print`）、异常信息、以及 `TextureLibrary.cfg` 里的
> 贴图/模型名（`Main`、`Drogue`、`Single chute` 等）**有意保留英文**——后者是查找
> 用的标识符，翻译会导致引用失效。材质名（`Silk`/`Nylon`/`Kevlar`）同为标识符，
> 保留英文；其**说明文字**已本地化。

---

## 2. 键名规则

命名空间统一为 `#RealChute_`，与官方 `#autoLOC_*` 及其他模组完全隔离。

| 场景 | 键形式 | 示例 |
|---|---|---|
| 公用（按钮、开关） | `#RealChute_Common_<名>` | `#RealChute_Common_Close` |
| 编辑器界面 | `#RealChute_Editor_<名>` | `#RealChute_Editor_MustGoDown` |
| 伞参数界面 | `#RealChute_Template_<名>` | `#RealChute_Template_DeployedDiameter` |
| 校验错误项 | `#RealChute_Error_<名>` | `#RealChute_Error_LandingSpeed` |
| 右键/动作组名称 | `#RealChute_Event_*` / `#RealChute_Action_*` | `#RealChute_Action_CutMainChute` |
| 飞行提示 | `#RealChute_Flight_<名>` | `#RealChute_Flight_DeploymentFailed` |
| 部件信息（编辑器） | `#RealChute_Info_<名>` | `#RealChute_Info_CaseMass` |
| 飞行信息窗口 | `#RealChute_Window_<名>` | `#RealChute_Window_PartName` |
| 单伞飞行信息 | `#RealChute_Chute_<名>` | `#RealChute_Chute_Safety_Safe` |
| cfg 文本（部件/机构/材质） | `#RealChute_Part_*` / `#RealChute_Agent_*` / `#RealChute_Material_*` | `#RealChute_Part_Cone_Title` |

---

## 3. 动态文本如何拼接

含运行时数值的文本不能整句硬编码，也不能用 `string.Format` 把值塞进句子里
（那样中文语序无法调整）。做法是把整句放进本地化文件，用序号占位符
`<<1>>`、`<<2>>`（与官方 `dictionary.cfg` 完全相同的写法），运行时替换：

```csharp
// 原版
this.screenMessage = $"Deployment in {time:0.0}s";

// 现在（标签 + 参数，值先按格式转成字符串）
this.screenMessage = Localization.Get("Flight_DeploymentInSeconds", time.ToString("0.0"));
```

```cfg
#RealChute_Flight_DeploymentInSeconds = Deployment in <<1>>s
#RealChute_Flight_DeploymentInSeconds = <<1>> 秒后展开
```

配套的取值 API 是 `KSP.Localization.Localizer.Format`。**注意**：KSP 的
`Localizer.Format` 会把 `\"` 还原成裸 `"`，从而吞掉紧跟其后的 `<<1>>`，所以
英文里引用预设名时使用单引号（`The '<<1>>' preset ...`），中文使用全角引号
（`预设“<<1>>”`）。这是本地化文本的一条硬性约束，新增键时请遵守。

---

## 4. 编码与转义规则（重要）

| 规则 | 说明 |
|---|---|
| 编码 | **UTF-8 无 BOM**。含中文，切勿用 GBK/ANSI 另存 |
| 换行 | 值内用 `\n`（反斜杠 + n），KSP 解析时还原为真实换行 |
| 双引号 | 不要在 `<<n>>` 前紧邻使用 `\"`（见第 3 节）；值内 `"` 写作 `\"` 仅限无占位符场景 |
| 禁止 | 值以 `#` 开头；值内含真实换行；值内含 `//`（会被当注释） |
| 占位符 | `<<1>>` 从 1 开始连续编号；中英两版的占位符数量必须一致 |

---

## 5. 本地化文件的两种放置方式

RealChute 同时使用了 KSP 支持的两种写法，均可被游戏正确加载：

1. **独立文件**（推荐，便于维护）
   `RealChute/Localization/en-us.cfg`、`zh-cn.cfg`
2. **内嵌节点**（cfg 文本随文件就近维护）
   `RealChute/Materials/ParachuteMaterials.cfg` 末尾、`RealChute/Subcategories/parachutes.cfg` 末尾

两者都由 KSP 在加载时统一读入，互不冲突。

---

## 6. 新增其他语言

1. 复制 `RealChute/Localization/en-us.cfg` 为 `Localization/<语言代码>.cfg`
   （语言代码用 KSP 的取值，如 `ja`、`ru`、`de-de`、`es-es`）；
2. 把顶层语言段名 `en-us` 改成对应代码；
3. 翻译值，**键名与占位符不要动**；
4. 无需改动任何 C# 或部件 cfg。

缺失的键会回退显示为该键名本身（形如 `#RealChute_xxx`），便于一眼发现漏翻。

---

## 7. 有意保留为英文的标识符

以下都是**标识符**而非显示文本，翻译会导致引用失效，故保持原样：

- `MATERIAL` 的 `name`（`Silk`/`Nylon`/`Kevlar`）
- `TEXTURE_LIBRARY` 的 `name`（`RealChute`）、`CASE_TEXTURE.name`、`CANOPY_TEXTURE.name`、
  `CANOPY_MODEL.name`，以及 `types`、`textureURL`、`modelURL`、`transformName`、动画名
- `PART` 的 `name`、`tags`、`bulkheadProfiles`、`TechRequired`、`preferredStage`
- `AGENT` 的 `name`（`RealChute`、`Wenkel Corporation`）与 `logoURL`/`logoScaledURL`
- `PRESET` 的 `name` 与 `sizeID`（预设名是存档数据的一部分，改名会与已有存档不一致）
- `PARACHUTE` 的 `material`、`capName`、`parachuteName`、动画名
- 代码中的 `"Engineer"`（`VesselExtensions` 里比对工程师技能的特征名）

> 注意：`AGENT` 的 `title`/`description`、`PART` 的 `title`/`manufacturer`/`description`、
> `MATERIAL` 的 `description` **是**显示文本，已本地化。

---

## 8. 术语来源

中文译名优先采用**游戏本体官方译法**（取自 `Squad/Localization/dictionary.cfg`
官中词条），例如：

| 英文 | 官方中文 |
|---|---|
| parachute | 降落伞 |
| deploy / deployment | 展开 |
| Kerbal | 坎巴拉人 |
| Celestial Body | 天体 |

RealChute 特有的「半展开 / 全展开」体系（原文 `predeployment` / `deployment`）
统一译为**半展开**与**全展开**：RealChute 的伞先小面积弹出（predeploy）再完全张开
（deploy），直译「预展开」不符合玩家习惯且易与「预先」混淆。

机构名（RealChute、Wenkel Corporation）译名与人名（Bob/Bill/Jebediah、Wenkel Kerman）
按社区习惯处理：机构名译为中文（温克尔公司），人名保留原文。

---

## 9. 构建（重要）

本机**没有 .NET Framework 4.8 的引用程序集（targeting pack），也没有可还原工程的
MSBuild 环境**，因此无法用 `RealChute.csproj` 常规构建。构建脚本改用 Visual Studio
自带的 Roslyn 编译器 `csc.exe`，引用指向 KSP 安装目录里随游戏分发的程序集：

```
powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1
```

可覆盖参数：`-KspDir`、`-Csc`、`-SourceDir`、`-OutputDll`、`-FrameworkDir`。

- 编译语言版本 `langversion:12.0`（源码使用了集合表达式、`??=`、`is not` 等 C# 9–12 语法）
- 未加 `/deterministic`：源项目的程序集版本是 `1.4.*`，通配符版本与确定性编译互斥
- 输出覆盖 `Output/GameData/RealChute/Plugins/RealChute.dll`

---

## 10. 维护与校验工具

| 脚本 | 作用 |
|---|---|
| `build.ps1` | 调用 Roslyn 编译出 `RealChute.dll` |
| `verify_localization.py` | 字典结构/编码、en-us 与 zh-cn 键集合一致性、占位符一致性、源码引用与定义双向核对 |
| `verify_dll_strings.py` | 逐键核对 DLL 中确实内含全部本地化键字面量、无残留硬编码英文、无孤儿键 |

实测结果见 `VALIDATION.md`。
