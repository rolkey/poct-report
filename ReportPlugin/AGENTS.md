# ReportPlugin

**插件：** FastReport.OpenSource 封装 — 生成、预览和配置报表。

## 关键 API
| 方法 | 参数 | 返回值 |
|------|------|--------|
| `GenerateReport` | reportType, format, outputPath? | 文件路径字符串 |
| `PreviewReport` | reportType, format? | Base64 数据 URI |
| `PrintReport` | reportType, printerName? | PDF 文件路径 |
| `ConfigureReport` | reportType, configJson? | 配置 JSON |
| `GetReportTypes` | — | `["SimpleList", "Group"]` |

## 约定
- 报表类型：`SimpleList`（扁平员工列表）、`Group`（按部门分组）
- 输出格式：`pdf`、`jpg`、`png`
- 演示数据：8 名员工，4 个部门（硬编码在 `CreateDemoDataSet`）
- FastReport.OpenSource 不支持直接打印 — 改为生成 PDF

## 反模式（禁止）
- 不要添加真实数据库连接 — 仅使用演示 DataSet
- 不要移除 `finally` 块中的 `report.Dispose()`
