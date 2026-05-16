# 安装说明

本项目提供两种使用方式：Windows 独立桌宠和 Codex Pets 自定义宠物。

## 方式一：Windows 独立版

适合想直接运行桌宠的用户。

1. 下载：

```text
release-assets/JuHuaLiPet.exe
```

2. 双击运行。`v1.0.1` 及之后的版本是单文件运行版，不需要把额外 DLL 放在同一目录。

3. 使用方式：

- 左键拖动宠物。
- 双击宠物触发互动音效和动作。
- 右键宠物打开菜单。
- 右键菜单中可以添加待办、查看待办、调整大小和音量。

## 方式二：Codex Pets 自定义宠物

适合已经在使用 Codex Pets 的用户。

1. 下载：

```text
release-assets/juhuali-codex-pet.zip
```

2. 解压后确认目录结构：

```text
juhuali/
├─ pet.json
└─ spritesheet.webp
```

3. 复制到 Codex Pets 目录：

```text
%USERPROFILE%\.codex\pets\juhuali
```

4. 打开 Codex Pets 页面，点击“刷新”。

5. 在自定义宠物中选择“菊花梨”。

## 常见问题

### Codex Pets 中没有显示菊花梨

请检查：

- `pet.json` 和 `spritesheet.webp` 是否在同一个 `juhuali` 文件夹中。
- `pet.json` 是否是 UTF-8 无 BOM。
- `pet.json` 中的 `spritesheetPath` 是否为 `spritesheet.webp`。
- `spritesheet.webp` 是否完整。

### Windows 提示未知发布者

这是未签名的个人项目 exe，Windows 可能会弹出安全提醒。如果你不放心，可以从源码自行构建。

### 设置保存在哪里

Windows 独立版会把用户数据保存到：

```text
%APPDATA%\JuHuaLiPet
```

其中包括待办事项和大小、音量设置。
