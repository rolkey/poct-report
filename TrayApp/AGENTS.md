# TrayApp

**核心：** WPF 系统托盘程序 + WebSocket 服务 + 动态插件加载器。

## 目录结构
```
TrayApp/
├── App.xaml / App.xaml.cs    # 入口：托盘模式/服务模式启动
├── WebSocketServer.cs        # HTTP→WS 升级、JSON 命令分发
├── PluginManager.cs          # 动态 DLL 反射加载器
├── Logger.cs                 # 文件日志（每日滚动）
├── icons/                    # 托盘图标资源
├── plugins/                  # 运行时插件 DLL 目录
└── .vscode/                  # VS Code 启动/任务配置
```

## 关键代码索引
| 关注点 | 文件 | 关键 |
|--------|------|------|
| 启动模式 | `App.xaml.cs:59-117` | `StartupTrayMode()` / `StartupServiceMode()` |
| WS 协议 | `WebSocketServer.cs:139-177` | `ProcessCommand()` — JSON 分发 |
| 插件加载 | `PluginManager.cs:114-157` | `TryLoadPlugin()` — Assembly.LoadFrom |
| 参数转换 | `PluginManager.cs:40-95` | `Convert.ChangeType` + 可选参数填充 |
| 退出清理 | `App.xaml.cs:137-159` | 优雅关闭 + Environment.Exit(0) |

## 约定
- 插件 DLL 放在应用基目录的 `plugins/` 子目录
- 插件类：`{插件名}.{插件名}`（命名空间.类名）
- 方法匹配：`BindingFlags.IgnoreCase`，按参数数量+类型转换最佳匹配
- 日志：每日滚动文件 `TrayApp_yyyyMMdd.log`，INFO/WARN/ERROR 三级
- WS 端口：8761（Windows localhost，Linux 0.0.0.0）
- 私有字段：`_camelCase` 前缀
- 异常处理：`catch (Exception ex)` + `Logger.Error(...)` 模式

## 反模式（禁止）
- 不要添加插件接口 — 仅使用反射约定
- 不要阻塞 WS 接受循环 — 每个连接使用 `Task.Run`
- 不要在服务模式路径中使用 `System.Windows.Forms`
- 不要在 `AcceptConnections` 中吞掉 `OperationCanceledException`
