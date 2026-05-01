# ReportDesignPlugin

**插件：** FastReport 可视化报表设计器 — 在 Windows 上打开 FastReport 设计界面进行报表格式设置。

## 设计参考
参照 FastReport.Net Demo 中的以下示例实现：
- `Demos/C#/CustomDesigner/` — 在自定义窗体中嵌入 `DesignerControl`
- `Demos/C#/MdiDesigner/` — 使用 `Office2007DesignerForm` 启动完整设计器
- `Demos/C#/Main/Form1.cs` — `report.Design()` 简单调用

## 关键 API
| 方法 | 参数 | 返回值 |
|------|------|--------|
| `DesignReport` | reportFilePath: string | JSON: `{saved, filePath, reportXml, message}` |
| `CreateNewReport` | — | JSON: `{saved, filePath, reportXml, message}` |
| `GetReportTemplates` | — | `string[]` 模板文件名列表 |
| `GetTemplateDirectoryPath` | — | 模板目录路径 |
| `SetTemplateDirectoryPath` | path: string | void |
| `GetInfo` | — | 插件版本信息 |
| `IsDesignerAvailable` | — | bool（当前平台是否支持） |

## 依赖
- **FastReport.dll**（商业版）— 来自 `FastReport.Net Demo/` 目录
- **FastReport.Bars.dll** — 工具栏/停靠窗口库
- **FastReport.Editor.dll** — 代码编辑器库
- **Newtonsoft.Json** 13.0.3

## 构建与部署
```bash
dotnet build ReportDesignPlugin/ReportDesignPlugin.csproj -c Debug
```
构建产物输出到 `designPlugs/` 目录（含 `ReportDesignPlugin.dll` + 依赖的 FastReport 商业版 DLL）。

`PluginManager` 按优先级搜索：`designPlugs/` → `plugins/`。因此 `ReportDesignPlugin` 从 `designPlugs/` 加载，使用商业版 `FastReport.dll`；其他插件从 `plugins/` 加载，使用 OpenSource 版 `FastReport.dll`，两者互不干扰。

## 约定
- **仅 Windows** — 依赖 WinForms/WPF，Linux 服务模式下不可用
- 设计器在独立 STA 线程上运行（WebSocket 线程池是 MTA，不能直接创建 WPF 窗口）
- 通过 `WindowsFormsHost` 在 WPF 窗口中嵌入 WinForms 的 `DesignerControl`
- 设计器关闭后返回 JSON 结果，包含保存状态和报表 XML

## 反模式（禁止）
- 不要在非 Windows 平台上调用设计器方法 — 先检查 `IsDesignerAvailable()`
- 不要在设计器打开时通过 WebSocket 发送其他命令 — 设计器是模态的
- 不要修改 `FastReport.Net Demo/` 中的 DLL — 仅作为引用使用
