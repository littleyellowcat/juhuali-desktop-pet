# 构建说明

## 环境要求

- Windows
- .NET SDK
- 支持 WPF 的 .NET Windows 桌面环境

## 构建源码

源码目录：

```text
src/JuHuaLiPet
```

构建：

```bash
dotnet build src/JuHuaLiPet/JuHuaLiPet.csproj -c Release
```

## 发布单文件 exe

```bash
dotnet publish src/JuHuaLiPet/JuHuaLiPet.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -o dist
```

发布后生成：

```text
dist/JuHuaLiPet.exe
```

## 主要源码文件

| 文件 | 说明 |
|---|---|
| `MainWindow.xaml` / `MainWindow.xaml.cs` | 桌宠主窗口、拖动、右键菜单、随机行为 |
| `SpriteAnimator.cs` | 动画图集播放 |
| `PetSoundPlayer.cs` | 音效加载和播放 |
| `TodoDialog.cs` | 添加待办窗口 |
| `TodosWindow.cs` | 待办列表窗口 |
| `ReminderWindow.cs` | 到点提醒窗口 |
| `TodoStore.cs` | 待办事项本地存储 |
| `SettingsStore.cs` | 大小和音量设置存储 |

## 本地数据

运行时数据保存到：

```text
%APPDATA%\JuHuaLiPet
```

包括：

- `todos.json`
- `settings.json`
- 释放出的音效文件
