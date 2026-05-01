# 项目知识库

**生成时间:** 2026-05-01
**技术栈:** .NET 8 (C# WPF) + Vue 3 (TypeScript) + FastReport (子模块)

## 概述
跨平台桌面托盘程序，内置 WebSocket 服务，通过动态加载 DLL 插件执行命令。前端 Vue 3 客户端通过 WebSocket 调用 C# 插件（报表生成、演示命令等）。

## 目录结构
```
./
├── TrayApp/          # 核心：WPF 托盘程序 + WebSocket 服务 + 插件加载器
├── ReportPlugin/     # 插件：基于 FastReport 的报表生成（PDF/PNG/JPG）
├── ExamplePlugin/    # 插件：简单演示插件（greet/echo/add）
├── testVue/          # 前端：Vue 3 + TypeScript WebSocket 客户端
├── FastReport/       # 子模块：FastReport.OpenSource 库
├── FastReport.Net Demo/  # 第三方示例（非项目代码）
├── docs/             # 设计文档/规格
└── publish/          # 构建输出产物（gitignored）
```

## 关键文件索引
| 任务 | 位置 | 说明 |
|------|------|------|
| 应用入口/生命周期 | `TrayApp/App.xaml.cs` | 托盘模式 vs 服务模式 |
| WebSocket 协议 | `TrayApp/WebSocketServer.cs` | JSON 命令分发 |
| 插件加载 | `TrayApp/PluginManager.cs` | 动态 DLL 反射 |
| 报表生成 | `ReportPlugin/ReportPlugin.cs` | FastReport 封装 |
| 前端界面 | `testVue/src/` | Vue 3 SFC |
| 前端 WS 客户端 | `testVue/src/utils/websocket.ts` | WebSocket 组合式函数 |

## 约定
- 插件 DLL 命名规则：`{命名空间}.{类名}`（如 `ExamplePlugin.ExamplePlugin`）
- 插件方法使用基于约定的反射（无接口/抽象类）
- WebSocket JSON 格式：`{"plugin":"...","method":"...","params":[...]}`
- 响应格式：`{"success":true/false, "data":..., "error":"..."}`
- C#：文件范围命名空间、可空类型启用、隐式 using、`_camelCase` 私有字段
- Vue：`<script setup lang="ts">` SFC 风格、`useXxx()` 组合式函数模式
- 无 CI/CD、无 Docker、无测试项目 — 全部手动构建

## 反模式（禁止）
- 不要新增插件接口/抽象类 — 仅使用基于约定的反射
- 不要硬编码插件路径 — 使用 `plugins/` 相对目录
- 不要在非 Windows 代码路径中使用 `System.Windows.Forms`
- 不要阻塞 WS 接受循环 — 每个连接使用 `Task.Run`

## 命令
```bash
# 构建 TrayApp
dotnet build TrayApp/TrayApp.csproj

# 构建插件
dotnet build ExamplePlugin/ExamplePlugin.csproj
dotnet build ReportPlugin/ReportPlugin.csproj

# 启动 Vue 前端
cd testVue && npm run dev

# 发布 TrayApp（独立部署）
dotnet publish TrayApp/TrayApp.csproj -c Release -o publish/
```

## 注意事项
- FastReport 是 git 子模块 — 使用 `git submodule update --init` 初始化
- TrayApp 在 Windows 上运行托盘模式（GUI），Linux 上运行服务模式（无界面）
- WebSocket 端口：8761（Windows localhost，Linux 0.0.0.0）
- 插件在首次调用时懒加载
- 无根 `.sln` — 每个项目单独构建
- 无 `Directory.Build.props` — NuGet 版本在 3 个 `.csproj` 中重复
