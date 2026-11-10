# 交子赶工记 · 机制灰盒

Windows 同机双人原型。Unity **6000.3.28f1**，URP **17.3.0**，Input System **1.17.0**。模型使用胶囊和方块，不依赖美术仓库中的模型。

## 打开与游玩

Unity Hub 添加本目录，打开 `Assets/Scenes/Graybox.unity` 后播放。Windows 构建入口为 `Builds/Windows/JiaoziGraybox.exe`，整个 Windows 文件夹必须一起保留。

| 玩家 | 移动 | 拿取、放置、递交 | 加工 |
|---|---|---|---|
| P1 | WASD | F | 按住 G |
| P2 | 方向键 | 主键盘 / | 按住 . |

先朝向工位或搭档，让目标高亮，再操作。每人只拿一件；物件不能扔地上。若加工中断，继续按住或由搭档续做。教学不计时，完成一张票后按 Enter 开始，或直接 Enter 跳过。

流程：抄纸槽按住 2 秒 → 取湿纸 → 放纸架自动等 4 秒 → 取干纸放印台加工 2 秒 → 取黑票放盖印台加工 1 秒 → 取朱票送窗口。

Esc 暂停/继续；暂停或结算时 ↑↓ 选择、Enter 确认、R 重开教学。暂停同时冻结两人、所有工位和计时。

正式轮次 180 秒，订单数量循环 1/1/2；每张 10 分，整单另加 10 分。2/4/6 单对应 1/2/3 星。结束后不接受新交付。

## 开发与验证

- `WorkshopRules`：纯规则与所有权、工位加工、订单回合；`WorkshopInput`：两套 Input System 操作。
- `Graybox`：可配置布局、角色移动、可见目标筛选、表现与日志；`WorkshopHud`：事件驱动的中文 UI Toolkit 界面。
- 参数在场景 Graybox 组件配置，工位位置、操作偏移、尺寸和隔断位置可调。
- Test Runner → EditMode 运行 `WorkshopRulesTests`。
- 一键测试与构建：PowerShell 执行 `Tools/Verify.ps1 -Build`。
- 批处理构建：`Unity.exe -batchmode -nographics -quit -projectPath <本目录> -executeMethod GrayboxBuild.Build -logFile <日志>`。
- 开发构建增加 `--jiaozi-smoke` 参数，可执行脚本化双人输入、碰撞、递交、完整流程和两轮回归；会临时缩短工序与回合，普通游玩不受影响。

Windows 中文字体使用系统微软雅黑/黑体，不把系统字体复制到仓库。

测试结果和未完成的真人验收见 [验证报告](Docs/验证报告.md)，双人试玩填写 [试玩记录](Docs/双人试玩记录.md)。

试玩日志位于 `%USERPROFILE%/AppData/LocalLow/JiaoziWorkshop/Jiaozi Graybox/Playtests`；含首次交付、拿放、错误交互、受阻与重开时的静止时长。静止仅表示没有移动和加工，不等同于无事可做。

## 仓库状态

计划远程仓库：`2568383756/jiaozi-gangongji-game`，私有，与设计素材仓库分离。当前 GitHub CLI 未登录，远程创建与推送须在登录后完成。本地先保留 Git 提交。

设计来源：[素材与设计仓库](https://github.com/2568383756/jiaozi-gangongji)。定稿仅供美术和镜头参考，灰盒布局以机制验证为准。
