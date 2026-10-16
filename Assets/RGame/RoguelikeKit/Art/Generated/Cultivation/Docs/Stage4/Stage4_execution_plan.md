# 阶段四执行计划：HUD/UI 收尾

日期：2024-10-14
工程：`D:\GameDemo\survivors-roguelike-kit`
Unity：6000.6.4f1

## 执行策略

基于 `Stage4_available_resources.md` 的扫描结果，采用"先复用、后评估、最后生成"的策略：

### 第一轮：直接复用（确定可用）

| 序号 | 目标槽位 | 当前资源 | 替换资源 | 预期效果 |
|---|---|---|---|---|
| 1 | 角色解锁价格图标 | `GoldCoin.png` | `Xianxia_SpiritStone_v2.png` | 统一灵石风格 |
| 2 | 强化面板灵石图标 | `PowerUpIcon2.png` | `Xianxia_SpiritStone_v2.png` | 统一灵石风格 |
| 3 | 天火流星投射物 | `RandomAttack.png` | `TianHuoLiuXing/Main.png` | 修仙术法风格 |
| 4 | 阴阳剑气投射物 | `White.png` | `YinYangJianQi/Main.png` | 修仙剑气风格 |

### 第二轮：候选评估（需预览确认）

| 序号 | 目标槽位 | 当前资源 | 候选资源 | 评估标准 |
|---|---|---|---|---|
| 5 | 击杀计数图标 | `SkullUI.png` | `Passive/Curse.png` 或新生成 | 是否符合修仙题材 + 可读性 |
| 6 | ScrollView 背景 | 内置 `Background` | 组合现有 UI 框架资源 | 是否保持九宫格正确 + 滚动区域可见 |
| 7 | ScrollView 滑块/滑轨 | 内置 `Scrollbar` | 组合现有进度条资源 | 是否可交互 + 视觉清晰 |

### 第三轮：按需生成（如果候选不符合）

| 序号 | 资源 | 生成条件 | 规格要求 |
|---|---|---|---|
| 8 | 击杀计数图标 | 如果 `Curse.png` 风格不符 | 256×256，修仙元素（法器/符纸），清晰可辨 |
| 9 | 强化勾选标记 | 必须生成（无候选） | 128×128，修仙勾选样式 |
| 10 | ScrollView 专用皮肤 | 如果组合失败 | 九宫格切片，符合现有 UI 风格 |

### 暂缓项（不在本阶段）

- 设置行比例修正
- 关卡卡片比例修正
- 被动技能图标接入（14 个，需配合技能系统重构）
- 其他技能资源接入（7 套完整技能，需玩法设计）

---

## 子阶段详细计划

### 4.0 建立基线与备份 ✅（当前）

**目标**：
1. 创建备份点
2. 记录所有目标文件的当前状态
3. 建立回退清单

**文件清单**：
- 场景：`GamePlay.unity`、`MainMenu.unity`
- Prefab：`CharacterSelect_Item.prefab`、`PowerUpItemLevel.prefab`、`RandomSkill.prefab`、`BothSideSkill.prefab`、`LevelSelect_Panel.prefab`、`PowerUp_Panel.prefab`、`Canvas_MainMenu.prefab`（如存在）
- 旧资源：`SkullUI.png`、`GoldCoin.png`、`PowerUpIcon2.png`、`check.png`、`RandomAttack.png`、`White.png`

**输出**：
- `D:\GameDemo\_snapshot_stage4_pre_ui\` 备份目录
- `Stage4_backup_manifest.json` 文件清单与哈希
- `Stage4_reference_map.csv` 引用关系表

---

### 4.1 第一轮：灵石图标替换

**目标资源**：`Xianxia_SpiritStone_v2.png`

**步骤**：
1. 检查资源是否已导入，记录当前导入设置
2. 定位所有引用 `GoldCoin.png` 和 `PowerUpIcon2.png` 的槽位
3. 逐个替换引用，记录世界尺寸变化
4. 静态检查：无悬空引用、编译通过
5. 运行验证：进入角色选择界面、强化面板，截图对比

**预期问题**：
- 灵石图标尺寸可能与旧图标不同，需调整 RectTransform 或 preserveAspect
- 强化面板可能有多处引用，需逐一核对

**输出**：
- `Stage4_spirit_stone_replacement.csv` 替换记录
- 截图：`stage4_character_select.png`、`stage4_powerup_panel.png`

---

### 4.2 第一轮：技能投射物替换

**目标资源**：
- `TianHuoLiuXing/Main.png` → `RandomAttack.png`
- `YinYangJianQi/Main.png` → `White.png`

**步骤**：
1. 检查资源导入设置（PPU、spriteMode、pivot）
2. 定位 `RandomSkill.prefab` 和 `BothSideSkill.prefab` 的 Sprite 引用
3. 替换引用，按旧资源世界尺寸调整 PPU
4. 进入游戏战斗，触发两个技能，验证视觉表现
5. 记录投射物速度、尺寸、可识别性

**预期问题**：
- 投射物尺寸变化可能影响视觉识别
- 颜色/风格变化可能影响玩家对技能类型的判断

**输出**：
- `Stage4_skill_projectile_replacement.csv` 替换记录
- 截图：`stage4_tianhuoliuxing_combat.png`、`stage4_yinyangjianqi_combat.png`

---

### 4.3 第二轮：击杀图标评估与处理

**候选方案 A**：使用 `Passive/Curse.png`

**评估步骤**：
1. 在图片查看器中打开 `Curse.png`，评估风格
2. 临时替换到 `SkullUI.png` 槽位，进入战斗查看效果
3. 判断标准：
   - 是否符合修仙题材（不能是西式骷髅）
   - 在 HUD 中是否清晰可辨（尺寸、对比度）
   - 与击杀计数的语义是否匹配

**候选方案 B**：新生成专用图标

**生成提示词（如果需要）**：
```
修仙题材击杀计数图标，256x256，透明背景。
风格：中国水墨+淡金色线条，简洁图标风格。
元素：法器（如铃铛、令牌）或符纸，暗示"镇压""封印"的概念。
不要：骷髅、血腥元素、过于复杂的细节。
参考：道教符箓、封印法阵的简化图形。
```

**输出**：
- `Stage4_kill_icon_evaluation.md` 评估结论
- 截图：`stage4_kill_icon_hud.png`

---

### 4.4 第二轮：强化勾选标记生成

**生成需求**：
- 尺寸：128×128
- 风格：修仙题材，与已有 UI 一致
- 用途：强化等级勾选，需清晰表达"已激活"

**生成提示词**：
```
修仙题材勾选标记图标，128x128，透明背景。
风格：中国传统+金色光芒，符合修仙游戏 UI。
元素：圆形底座+中心勾选符号（可以是简化的"成"字、法印、或光点）。
色调：金色为主，可带淡青色装饰。
不要：现代扁平风格、纯色填充、过于简单的几何形状。
参考：道教法印、古代印章的简化形式。
```

**导入设置**：
- PPU：按旧 `check.png` 的世界尺寸反算
- spriteMode：Single
- pivot：Center
- filterMode：Bilinear

**输出**：
- `Stage4_check_mark_generation.md` 生成记录
- 候选资源 2–3 个，预览后选择最佳

---

### 4.5 第二轮：ScrollView 组件处理

**方案 A**：组合现有资源（优先）

**候选组合**：
- Background：`Xianxia_MainPanel_Frame.png`（九宫格）
- Scrollbar Background：`Xianxia_HUD_Bar_Track.png`（拉伸）
- Scrollbar Handle：`Xianxia_HUD_Bar_Fill.png` 或 `Xianxia_Button_Primary.png`（九宫格）

**测试步骤**：
1. 在编辑器中打开 `LevelSelect_Panel.prefab`
2. 替换 ScrollRect 的 Background 引用
3. 替换 Scrollbar 的 Background 和 Handle 引用
4. 检查九宫格切片设置（Border 值）
5. 运行测试：滚动是否流畅，可交互区域是否正确

**方案 B**：新生成专用资源（如果组合失败）

**生成需求**：
- ScrollView 背景：512×512，九宫格，中心透明或半透明
- 滑轨：256×32，可横向拉伸
- 滑块：64×64，九宫格

**输出**：
- `Stage4_scrollview_skin.md` 处理记录
- 截图：`stage4_level_select_scroll.png`、`stage4_powerup_scroll.png`

---

### 4.6 静态检查

**检查项**：
1. 编译状态：`compiling = false`、`compilationFailed = false`
2. 悬空引用：目标 Prefab 和场景中 `dangling = 0`
3. Console 错误：无新增 error
4. 备份对照：变更文件清单与预期一致

**脚本**：`stage4_verify_static.cs`

**输出**：
- `Stage4_static_check_report.txt`

---

### 4.7 运行验证

**验证路径**：
1. 启动游戏 → 主菜单 → 角色选择 → 检查解锁价格图标
2. 主菜单 → 强化面板 → 检查灵石图标、勾选标记、ScrollView 滚动
3. 主菜单 → 设置 → 检查 ScrollView 滚动
4. 选择角色 → 选择关卡 → 检查 ScrollView 滚动
5. 进入战斗 → 检查击杀计数图标、技能投射物
6. 升级选择技能 → 触发天火流星、阴阳剑气 → 截图验证

**截图清单**（至少 8 张）：
- `stage4_character_select.png`
- `stage4_powerup_panel.png`
- `stage4_powerup_scroll.png`
- `stage4_settings_scroll.png`
- `stage4_level_select.png`
- `stage4_kill_icon_ingame.png`
- `stage4_tianhuoliuxing.png`
- `stage4_yinyangjianqi.png`

**脚本**：`stage4_runtime_probe.cs`

**输出**：
- `Stage4_runtime_verify.txt`
- 截图目录：`Assets/Temp/AgentCapture/Stage4/`

---

### 4.8 编写报告

**报告结构**（参考 `Stage3_report.md`）：
1. 执行状态（子阶段完成情况）
2. 实际执行（导入、替换、配置）
3. 运行验证摘要
4. 关键发现（如有）
5. 未完成/待人工确认（如有）
6. 安全、回退与不变量
7. 交付物清单
8. 建议的下一步

**输出**：
- `Stage4_report.md`

---

## 风险预案

### 风险 1：灵石图标尺寸不匹配

**症状**：替换后图标过大/过小，破坏布局

**预案**：
1. 按旧资源世界尺寸反算 PPU
2. 如果源图尺寸差异过大，启用 `preserveAspect` 并调整 RectTransform
3. 记录所有尺寸调整，写入报告

---

### 风险 2：技能投射物识别度下降

**症状**：替换后玩家难以区分不同技能

**预案**：
1. 保留备份，随时可回退
2. 调整投射物的 SortingOrder 或 scale
3. 如果根本不符合，标注为"待重新生成"，暂时回退

---

### 风险 3：ScrollView 组合失败

**症状**：滑块不可交互、背景拉伸变形、滚动卡顿

**预案**：
1. 检查九宫格 Border 设置
2. 调整 Scrollbar 的 RectTransform 和 Direction
3. 如果组合无法满足需求，启动生成流程（方案 B）

---

### 风险 4：勾选标记生成结果不理想

**症状**：生成的图标风格不统一、尺寸不合适、语义不清

**预案**：
1. 生成 2–3 个候选，预览后选择最佳
2. 调整提示词，重新生成
3. 如果多次生成仍不理想，暂时保留旧资源，标注为"待设计师介入"

---

## 时间表

| 子阶段 | 预计工时 | 累计工时 |
|---|---|---|
| 4.0 建立基线与备份 | 0.5 天 | 0.5 天 |
| 4.1 灵石图标替换 | 0.5 天 | 1 天 |
| 4.2 技能投射物替换 | 0.5 天 | 1.5 天 |
| 4.3 击杀图标评估 | 0.5 天 | 2 天 |
| 4.4 勾选标记生成 | 1 天 | 3 天 |
| 4.5 ScrollView 处理 | 1 天 | 4 天 |
| 4.6 静态检查 | 0.5 天 | 4.5 天 |
| 4.7 运行验证 | 1 天 | 5.5 天 |
| 4.8 编写报告 | 0.5 天 | 6 天 |

**总预计工时：6 天**

---

## 下一步

**立即开始 4.0**：建立基线与备份

确认后我将执行：
1. 创建备份目录 `D:\GameDemo\_snapshot_stage4_pre_ui\`
2. 扫描并备份所有目标文件
3. 生成文件清单与哈希记录
4. 建立引用关系表

是否开始？
