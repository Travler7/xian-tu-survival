# 仙途生存

一款基于 Unity 6 的仙侠题材 2D 生存类 Roguelike 游戏。

玩家选择角色进入关卡，在持续生成的敌人中战斗，收集经验和资源，选择主动技能与被动强化，逐步提升战斗能力并完成关卡结算。

## 当前版本

当前版本重点完成了以下内容：

- 游戏名称统一为“仙途生存”
- 中文化主菜单、角色选择、关卡选择、设置和升级界面
- 角色：顾长风、影刃·夜绫、灵修·云璃
- 关卡：青云外域、黄泉古道
- 仙侠风格的角色、技能、地图、特效和界面资源
- 主动技能、被动强化和武器进化系统
- 灼烧、冻结、减速等 Buff 效果
- 经验、掉落、拾取、宝箱、永久强化和结算流程
- 浮动伤害数字和音频、画面、语言等设置项
- Addressables、对象池、ScriptableObject 和事件通道驱动的运行结构

## 游戏流程

```text
主菜单
  -> 角色选择
  -> 关卡选择
  -> 战斗与刷怪
  -> 升级和技能选择
  -> 宝箱与奖励
  -> 结算与存档
```

## 环境要求

- Unity `6000.6.4f1`
- Built-in Render Pipeline
- Windows 开发环境
- Unity 包依赖由 `Packages/manifest.json` 和 `Packages/packages-lock.json` 管理

## 快速开始

```bash
git clone https://github.com/Travler7/xian-tu-survival.git
```

1. 使用 Unity Hub 打开项目目录。
2. 使用 Unity `6000.6.4f1` 导入项目。
3. 打开 `Assets/RGame/RoguelikeKit/Scenes/Initialization.unity`。
4. 点击 Play。
5. 在主菜单中选择角色和关卡，进入战斗。

首次打开项目时，Unity 会在本地生成 `Library/` 等缓存目录，这些目录不属于版本库内容。

## 项目结构

- `Assets/RGame/RoguelikeKit/Scenes/`：启动、菜单、玩法和管理器场景
- `Assets/RGame/RoguelikeKit/ScriptableObjects/`：角色、技能、关卡和运行时配置
- `Assets/RGame/RoguelikeKit/Art/Generated/`：当前版本确认交付的新增美术资源
- `Assets/RGame/RoguelikeKit/Scripts/`：玩法逻辑、系统和显示层脚本
- `Assets/RGame/RoguelikeKit/Prefabs/`：角色、敌人、UI 和地图预制体
- `Packages/`：Unity 包依赖清单
- `ProjectSettings/`：Unity 项目设置和版本信息

## 资源与版本库规则

发布仓库只保留当前项目使用的资源，以及已经确认属于本项目交付成果的新增素材。

已经被完全替换并确认没有引用的旧资源只保留在本地归档中，不作为运行依赖上传到 GitHub。Unity 资源的 `.meta` 文件必须与对应资源一起维护。

项目中的 `Library/`、`Temp/`、`Obj/`、`Logs/`、`UserSettings/`、构建产物和本机工具状态均不属于版本库内容。

## 当前验证状态

- Unity Editor `6000.6.4f1` 可以正常打开项目
- 当前版本 C# 编译错误为 0
- 主菜单、角色选择、关卡选择和战斗流程已完成当前版本改造
- 最终运行效果应以本地 Unity Editor 和目标平台的实际测试为准

## 许可证

本项目使用 MIT License，详见 [LICENSE](LICENSE)。

DOTween、Unity packages 及其他第三方内容可能有独立的授权条款，使用前请分别确认。
