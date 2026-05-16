# 菊花梨桌宠 Juhuali Desktop Pet

这是一个以《洛克王国：世界》菊花梨为灵感制作的 Windows 桌宠项目，包含两种使用方式：

- **Windows 独立版**：下载 `JuHuaLiPet.exe`，双击即可运行。
- **Codex Pets 自定义宠物版**：下载 `juhuali-codex-pet.zip`，安装到 Codex Pets。

> 说明：这是个人学习与桌面交互实践项目，非官方作品，不隶属于《洛克王国：世界》或其版权方。角色相关权益归原权利方所有，请勿用于商业用途。

## 下载

推荐从 GitHub Release 下载：

- [v1.0.0 Release](https://github.com/littleyellowcat/juhuali-desktop-pet/releases/tag/v1.0.0)

成品文件也同步放在仓库的 `release-assets/` 目录：

| 文件 | 用途 |
|---|---|
| `release-assets/JuHuaLiPet.exe` | Windows 独立桌宠 |
| `release-assets/juhuali-codex-pet.zip` | Codex Pets 自定义宠物包 |

如果仓库创建了 GitHub Release，建议优先从 Release 页面下载。

## 功能

- 桌面透明悬浮宠物
- 左键拖动
- 待机随机动作
- 自动小范围溜达
- 双击互动与专属叫声
- 右键创建待办事项
- 到点自动提醒
- 提醒时播放声音并触发动作
- 待办列表、添加待办、提醒弹窗
- 宠物大小调节
- 音量调节
- 设置自动保存

## Windows 独立版安装

1. 下载 `release-assets/JuHuaLiPet.exe`。
2. 双击运行。
3. 右键菊花梨打开菜单：
   - 添加待办
   - 查看待办
   - 测试提醒声音
   - 设置大小和音量
   - 退出菊花梨

更多说明见 [docs/INSTALL.md](docs/INSTALL.md)。

## Codex Pets 安装

1. 下载 `release-assets/juhuali-codex-pet.zip`。
2. 解压后得到：

```text
juhuali/
├─ pet.json
└─ spritesheet.webp
```

3. 放到：

```text
%USERPROFILE%\.codex\pets\juhuali
```

4. 打开 Codex Pets，点击刷新，然后选择“菊花梨”。

## 从源码构建

源码位于：

```text
src/JuHuaLiPet
```

构建：

```bash
dotnet build src/JuHuaLiPet/JuHuaLiPet.csproj -c Release
```

发布单文件 exe：

```bash
dotnet publish src/JuHuaLiPet/JuHuaLiPet.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist
```

更多开发说明见 [docs/BUILD.md](docs/BUILD.md)。

## 项目结构

```text
.
├─ codex-pet/
│  └─ juhuali/
│     ├─ pet.json
│     └─ spritesheet.webp
├─ docs/
│  ├─ INSTALL.md
│  ├─ FEATURES.md
│  └─ BUILD.md
├─ media/
│  └─ contact-sheet.png
├─ release-assets/
│  ├─ JuHuaLiPet.exe
│  └─ juhuali-codex-pet.zip
└─ src/
   └─ JuHuaLiPet/
```

## 截图

动画图集预览：

![菊花梨动画图集预览](media/contact-sheet.png)
