# 仙途生存

一款基于 Unity 6 的仙侠题材 2D 生存类 Roguelike 游戏。玩家选择角色与关卡，通过移动躲避敌人、自动攻击、收集经验和选择强化，组合五行技能，完成渡劫与神通突破，并挑战最终 Boss。

本项目基于 **Survivors Roguelike Kit** 改造扩展，保留原有基础框架，在此基础上加入修仙主题、五行战斗和局内成长机制。

本仓库提供完整 Unity 工程源码，需使用 Unity Editor 运行；当前未提供可直接下载的游戏安装包。

## 环境要求

- Unity **6000.6.4f1**
- Built-in Render Pipeline
- Windows 开发环境

工程使用 Addressables、Input System、Localization、UGUI、TextMesh Pro 和 DOTween。Unity 包由工程依赖清单管理，DOTween 已随工程附带。

## 快速开始

克隆仓库并进入项目目录：

```bash
git clone https://github.com/Travler7/xian-tu-survival.git
cd xian-tu-survival
```

1. 打开 Unity Hub，添加克隆后的 `xian-tu-survival` 项目目录。
2. 使用 Unity **6000.6.4f1** 打开项目，等待依赖解析与资源导入完成。
3. 打开启动场景：`Assets/RGame/RoguelikeKit/Scenes/Initialization.unity`。
4. 点击 **Play**，在主菜单选择角色与关卡，进入游戏。

基础操作：

| 操作 | 按键 |
| --- | --- |
| 移动 | WASD 或方向键 |
| 暂停 / 返回 | Esc |
| 攻击 | 由技能系统自动执行 |

## 与原项目的关系

- 原项目：[Survivors Roguelike Kit](https://github.com/Roo-Roo-Roo/survivors-roguelike-kit)
- 原项目文档：[Survivors Roguelike Kit Documentation](https://roofen-game.gitbook.io/roofen-game/survivors-roguelike-kit)
- 本项目：[仙途生存](https://github.com/Travler7/xian-tu-survival)

原项目提供了角色与关卡选择、自动攻击、敌人行为、刷怪、经验升级、掉落、Buff、存档、场景加载和对象池等基础能力。本项目继续使用其 ScriptableObject 配置与事件通道架构，工程中保留了 `RGame`、`RoguelikeKit` 等原有命名。

原项目文档可用于了解基础框架；本项目新增玩法与界面以当前工程为准。

## 主要差异

| 方面 | 原项目基础 | 本项目的改造与扩展 |
| --- | --- | --- |
| 游戏主题 | 通用 2D 生存类 Roguelike 模板 | 修仙主题角色、敌人、地图、技能与中文界面 |
| 战斗机制 | 技能攻击、伤害与 Buff 系统 | 五行印记、10 组双元素反应、5 种单元素共鸣 |
| 局内成长 | 经验升级、技能选择与强化 | 本命灵根、渡劫事件与奖励、神通突破 |
| 精英与 Boss | 敌人行为和关卡刷怪框架 | 邪染护体、反应破盾与限时易伤窗口 |
| 界面交互 | 菜单、选择、设置和结算界面 | 多弹窗暂停协调、设置界面宽高比适配 |
| 测试与采样 | 原有对象池与运行时框架 | 对象池统计、自动化测试和 80 敌人性能采样场景 |

以下三项是本项目在原模板基础上新增的玩法：

### 五行反应与共鸣

技能命中可为敌人附加元素印记，不同元素组合触发冻结、灼烧、范围伤害等反应；同元素命中达到阈值后可触发共鸣，使技能的元素搭配成为局内选择的一部分。

### 渡劫与神通突破

战斗中定时出现渡劫挑战，完成目标后可选择技能变异或灵根觉醒奖励。技能触发反应并累计进度后，还可解锁对应的神通突破，强化已有反应效果。

### 邪染护体与破染窗口

精英和最终 Boss 拥有独立护体值，元素反应能够更快消耗护体。击破后开启限时易伤窗口，让元素组合与输出时机共同影响战斗。

## 资源来源

原有资源的来源说明参见原项目。本项目新增的修仙主题素材包含 AI 辅助生成与后期加工内容。

## 许可证

本项目使用 **MIT License**，保留[原项目版权声明](https://github.com/Roo-Roo-Roo/survivors-roguelike-kit/blob/main/LICENSE)，完整条款见 [LICENSE](LICENSE)。

[DOTween](https://dotween.demigiant.com/) 遵循其[官方许可](https://dotween.demigiant.com/license.php)，Unity 包及其他第三方内容遵循各自的许可条款。
