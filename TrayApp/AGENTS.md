# TrayApp

**核心：** WPF 托盘程序，内嵌 HttpListener WebSocket 服务 + 基于反射的插件加载器。

## 文件职责
| 文件 | 职责 |
|------|------|
| `App.xaml.cs` | 应用生命周期：托盘模式 (Windows) / 服务模式 (Linux) |
| `App.xaml` | WPF 入口声明 |
| `WebSocketServer.cs` | HttpListener + WS 升级，JSON 命令分发到 PluginManager |
| `PluginManager.cs` | 反射加载 DLL 插件，方法参数自动转换 |
| `Logger.cs` | 简单文件日志 |
| `plugins/` | 开源插件输出目录（gitignored） |
| `designPlugs/` | 商业版插件输出目录（gitignored） |

## 架构要点
- **双模式启动**: Windows 有 GUI 时走托盘模式（系统托盘图标 + 右键菜单），否则走服务模式（纯控制台，Linux）
- **线程模型**: Accept 循环不阻塞（`Task.Run`），每个 WS 连接独立 `Task.Run`，命令处理也 `Task.Run`
- **插件加载**: `PluginManager.TryLoadPlugin` 按 `designPlugs/` → `plugins/` 优先级搜索 DLL，使用 `Assembly.LoadFrom` + 约定命名 `{Name}.{Name}` 反射创建实例
- **shutdown 命令**: 特殊处理 `plugin=system, method=shutdown` — 调用 `App.RequestShutdown()` 硬退出

## 约定
- 日志写入 `TrayApp.log`（文件追加）
- `PluginManager.Invoke` 自动转换 JSON 参数为目标方法参数类型（支持 `Newtonsoft.Json.Linq.JToken` → 字符串转换）
- 方法匹配按参数数量 + 类型兼容性 + 可选参数默认值

## 反模式（禁止）
- 不要在 AcceptConnections 循环中同步等待 — 使用 `Task.Run`
- 不要硬编码插件路径 — 始终使用 `_pluginDirs` 配置
- 不要在 WebSocket 线程中直接创建 WPF 窗口（MTA vs STA 问题）
