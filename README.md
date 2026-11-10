# 交子赶工记 · 完整第一关试玩版

Windows 同机双人原型。Unity **6000.3.28f1**，URP **17.3.0**，Input System **1.17.0**。包含标题、第一关地图、纸团与印桃选角、分步教学、正式回合、结算、组合收藏、设置与本地存档。角色、票面和场景仍使用占位表现，不依赖制作中的模型。第二三关、其他角色技能和 Boss 尚未开放。

## 打开与游玩

Unity Hub 添加本目录，打开 `Assets/Scenes/Graybox.unity` 后播放。Windows 构建入口为 `Builds/Windows/JiaoziGraybox.exe`，整个 Windows 文件夹必须一起保留。

从标题选择开始游戏 → 第一关 → 双人选角。P1 用 A/D 切换、F 确认；P2 用左右键切换、主键盘 / 确认。两人不能选择同一工匠。也可鼠标点击，或用上下键和 Enter 选择按钮。已确认者再次确认可取消。

| 玩家 | 移动 | 拿取、放置、递交 | 加工 |
|---|---|---|---|
| P1 | WASD | F | 按住 G |
| P2 | 方向键 | 主键盘 / | 按住 . |

先朝向工位或搭档，让目标高亮，再操作。每人只拿一件；物件不能扔地上。若加工中断，继续按住或由搭档续做。教学不计时，完成一张票后按 Enter 开始，或直接 Enter 跳过。

流程：抄纸槽按住 2 秒 → 取湿纸 → 放纸架自动等 4 秒 → 取干纸放印台加工 2 秒 → 取黑票放盖印台加工 1 秒 → 取朱票送窗口。

Esc 暂停/继续或返回上一页；菜单用 ↑↓ 选择、Enter 确认，鼠标也可点击。暂停页支持继续、重开本关、重新选角、设置、返回地图。暂停同时冻结两人、所有工位和计时；未结算退出不保存本轮成绩。

第一次进入有不计时的逐步教学，黄色标签标出下一步工位；加工完必须再拿起纸张。按 Enter 开始正式回合或跳过教学。真正完成教学并结算后，之后进入会跳过；未学完的教学仍会再次提示。

正式轮次至少交一张票后结算，解锁本局组合交子；P1/P2 交换角色不重复收藏。当前只有纸团＋印桃可解锁，图鉴列出其余十四款作为后续内容。票面提供底纸、黑墨和朱印资源接口，当前仅占位预览。

存档位于 `%USERPROFILE%/AppData/LocalLow/JiaoziWorkshop/Jiaozi Graybox/progress.json`。保存成绩、星级、收藏、教学完成和设置；自动保留 `.bak` 备份，损坏主文件会尝试读取备份。无法写盘时界面提示，当前会话保留进度。

正式轮次 180 秒，订单数量循环 1/1/2；每张 10 分，整单另加 10 分。2/4/6 单对应 1/2/3 星。结束后不接受新交付。

## 开发与验证

- `WorkshopRules`：纯规则与所有权、工位加工、订单回合；`WorkshopInput`：两套 Input System 操作。
- `Graybox`：可配置布局、角色移动、可见目标筛选、表现与日志；`WorkshopHud`：事件驱动的中文 UI Toolkit 界面。
- `GameFlow` 管理标题至结算的页面与游戏输入边界；`GameProgress` 和 `ProgressStore` 管理组合去重与原子存档；`FirstChapterMenu` 实现菜单界面。
- `Assets/Resources/FirstChapter.asset` 配置第一关、星级门槛、出生点、工位布局、角色外观/动画/手持点和票面分层。接入角色预制体时，动画控制器可提供 Speed 浮点、Carrying 与 Working 布尔参数；缺少参数时不会强行设置。
- 参数在场景 Graybox 组件配置，工位位置、操作偏移、尺寸和隔断位置可调。
- Test Runner → EditMode 运行 `WorkshopRulesTests`。
- 一键测试与构建：PowerShell 执行 `Tools/Verify.ps1 -Build`。
- 批处理构建：`Unity.exe -batchmode -nographics -quit -projectPath <本目录> -executeMethod GrayboxBuild.Build -logFile <日志>`。
- 开发构建增加 `--jiaozi-smoke` 参数，可执行脚本化双人输入、碰撞、递交、完整流程和两轮回归；会临时缩短工序与回合，普通游玩不受影响。
- `--jiaozi-flow-smoke` 检查完整页面流程、选角冲突、教学、结算、收藏与设置；`--jiaozi-flow-reload` 在新进程检查此前测试存档。测试使用独立的 `SmokeProfile` 目录，不写正常存档。

Windows 中文字体使用系统微软雅黑/黑体，不把系统字体复制到仓库。

音乐与操作音目前为程序合成的占位声音，可在设置中分别调节；正式音频尚待制作。

测试结果和未完成的真人验收见 [验证报告](Docs/验证报告.md)，双人试玩填写 [试玩记录](Docs/双人试玩记录.md)。

本次菜单、选角、存档和设置的测试结果见 [完整第一关验证报告](Docs/完整第一关验证报告.md)；美术交付后参考 [正式资源接入说明](Docs/正式资源接入说明.md)。

试玩日志位于 `%USERPROFILE%/AppData/LocalLow/JiaoziWorkshop/Jiaozi Graybox/Playtests`；含首次交付、拿放、错误交互、受阻与重开时的静止时长。静止仅表示没有移动和加工，不等同于无事可做。

## 仓库状态

计划远程仓库：`2568383756/jiaozi-gangongji-game`，私有，与设计素材仓库分离。当前 GitHub CLI 未登录，远程创建与推送须在登录后完成。本地先保留 Git 提交。

设计来源：[素材与设计仓库](https://github.com/2568383756/jiaozi-gangongji)。定稿仅供美术和镜头参考，灰盒布局以机制验证为准。
