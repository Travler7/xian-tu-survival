# 仙途生存

一款基于 Unity 6 的仙侠题材 2D 生存类 Roguelike 游戏。玩家选择角色进入关卡，在持续生成的敌人中战斗，收集经验和资源，选择功法与强化，完成渡劫、神通突破和最终 Boss 结算。

## 当前版本

当前可交付内容覆盖阶段 0–6：

- 中文化主菜单、角色选择、关卡选择、升级、宝箱和结算流程；
- 两张关卡、阶段 0 的普通敌人、精英和最终紫色乌鸦 Boss 时间线；
- 五行元素、10 组双元素反应、元素印记、反应文本与池化 VFX；
- 三名角色本命灵根和五种单元素共鸣；
- 十组双元素反应专属神通突破和五行通用保底突破；
- 4:00、8:00 渡劫，10:30 最终劫相，11:00 Boss，12:00 结算；
- 精英和最终 Boss 邪染护体、破染窗口和神通突破；
- Windows 64 位构建与 80 敌人性能基线场景。

阶段 0 的 Boss、正式 SpawnSet、PoolKey、碰撞层、结算、地图和属性初始化流程作为基线保留。项目没有新增敌人五行属性、敌人抗性、敌人主动五行攻击、第三次渡劫或新攻击动画系统。

## 核心循环

```text
主菜单
  -> 角色选择
  -> 关卡选择
  -> 自动攻击与移动生存
  -> 功法选择与五行反应
  -> 4:00 / 8:00 渡劫与局内奖励
  -> 精英护体、破染窗口与神通突破
  -> 11:00 最终 Boss
  -> 胜利、失败或 12:00 超时结算
```

## 环境要求

- Unity `6000.6.4f1`；
- Built-in Render Pipeline；
- Windows 开发环境；
- 依赖由 `Packages/manifest.json` 和 `Packages/packages-lock.json` 管理。

## 快速开始

```bash
git clone https://github.com/Travler7/xian-tu-survival.git
```

1. 使用 Unity Hub 打开项目目录。
2. 使用 Unity `6000.6.4f1` 导入项目。
3. 打开 `Assets/RGame/RoguelikeKit/Scenes/Initialization.unity`。
4. 点击 Play。
5. 在主菜单选择角色和关卡，进入战斗。

Windows 构建位于 [`Builds/Stage6/SurvivorsRoguelike.exe`](Builds/Stage6/SurvivorsRoguelike.exe)。

## 项目结构

- `Assets/RGame/RoguelikeKit/Scenes/`：启动、菜单、玩法和管理器场景；
- `Assets/RGame/RoguelikeKit/ScriptableObjects/`：角色、技能、关卡和运行时配置；
- `Assets/RGame/RoguelikeKit/Scripts/`：玩法逻辑、元素、渡劫、护体、突破和显示层脚本；
- `Assets/RGame/RoguelikeKit/Prefabs/`：角色、敌人、UI 和地图预制体；
- `Assets/Tests/Stage3/`：阶段 3 PlayMode 隔离烟测和可重复测试入口；
- `Assets/Tests/Stage4/`：80 敌人性能场景和采样器；
- `Assets/RGame/RoguelikeKit/Art/Generated/`：本项目新增的修仙主题美术资源；
- `ProfilerCaptures/Stage4/`：阶段 4 9 次有效采样 JSON；
- `Docs/`：阶段报告、资源台账和验收记录；
- `Packages/`：Unity 包依赖清单；
- `ProjectSettings/`：Unity 项目设置和版本信息。

## 测试与证据

- Unity 编译：当前构建记录 0 Error；
- EditMode：最新全量记录 42/42 通过，结果见 `Temp/pipeline_test_status.json`；
- PlayMode：独立隔离测试 1/1 通过，烟测报告包含 21/21 项检查，结果见 `Temp/Stage3PlayModeSmoke.json`；
- PlayMode 独立结果快照：`Temp/pipeline_playmode_test_status.json`；
- 阶段 4：三个场景各 3 次有效采样，预热 5 秒、采样 30 秒；
- Windows BuildReport：0 Error、529 条历史/环境类 Warning；
- 阶段 3–5 的正式菜单到结算完整 Play 和最终展示录像仍由用户在最后验收时完成。

详细记录：

- [`Docs/Stage3_Closure_Report_2026-10-17.md`](Docs/Stage3_Closure_Report_2026-10-17.md)
- [`Docs/Stage4_Performance_Baseline_2026-10-17.md`](Docs/Stage4_Performance_Baseline_2026-10-17.md)
- [`Docs/Stage5_Implementation_Report_2026-10-17.md`](Docs/Stage5_Implementation_Report_2026-10-17.md)
- [`Docs/Stage6_Release_Report_2026-10-17.md`](Docs/Stage6_Release_Report_2026-10-17.md)
- [`Docs/Stage6_Showcase_Capture_Checklist.md`](Docs/Stage6_Showcase_Capture_Checklist.md)

## 性能基线摘要

阶段 4 的有效样本来自 Unity Editor PlayMode，不代表目标硬件发布性能：

| 场景 | 平均帧时间 | 平均 P95 | 最低 FPS | GC Alloc/帧 | 反应次数 | VFX 创建 | 对象池命中率 |
|---|---:|---:|---:|---:|---:|---:|---:|
| Ordinary80 | 3.20 ms | 4.35 ms | 33.94 | 12.90 KiB | 0 | 0 | 100% |
| HighHpReaction80 | 3.54 ms | 4.97 ms | 41.16 | 13.24 KiB | 1120 | 0 | 100% |
| FeedbackWard80 | 3.96 ms | 5.57 ms | 45.99 | 13.27 KiB | 1120 | 0 | 100% |

GC Alloc 尚未达到每帧接近零，阶段 4 的结论是“基线已完成，性能优化验收暂未完成”。

## 内容与资源归属

### 模板和上游内容

`Assets/RGame/RSOFramework/`、原有 RoguelikeKit 的敌人/技能/刷怪/升级/结算/对象池框架、Unity 官方包、DOTween 等第三方插件属于模板或上游内容，各自授权条款以原始仓库和包声明为准。

### 本项目自制系统

阶段 0–5 的元素、印记、反应、共鸣、渡劫、奖励、邪染护体、神通突破、验收测试和性能采样脚本属于本项目的功能改造与验证内容。它们继续复用原有敌人和刷怪底层框架。

### AI 辅助或本项目生成素材

修仙主题角色、敌人、技能、地图、UI、宝箱和结算素材集中在 `Assets/RGame/RoguelikeKit/Art/Generated/`。详细的来源、引用和交付分类见 [`Docs/Stage5/Stage5_classification.csv`](Docs/Stage5/Stage5_classification.csv) 与 [`Docs/Stage5/Stage5_delivery_manifest.md`](Docs/Stage5/Stage5_delivery_manifest.md)。资源分类不表示每一张图片都在当前构建中被实际引用。

## 已知限制

- WebGL Build Support 未安装，当前只提供 Windows 64 位构建；
- GC Alloc 仍需后续优化；
- 性能样本来自 Editor PlayMode，尚未替代目标硬件测试；
- 正式主菜单到结算的完整 12 分钟流程、胜利/失败/超时状态矩阵和阶段 2/3/5 展示录像，留待用户最后 Play 验收并补录；
- 不用静态截图或编辑器内存快照冒充实机流程或发布性能证据。

## 许可证

本项目使用 MIT License，详见 [`LICENSE`](LICENSE)。DOTween、Unity packages 及其他第三方内容可能有独立授权条款，使用前请分别确认。
