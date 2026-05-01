# ReportPlugin

**插件：** FastReport.OpenSource 封装 — 生成、预览和设计报表。

## 目录结构
```
ReportPlugin/
├── ReportPlugin.cs           # 全部逻辑（314 行）
├── ReportPlugin.csproj       # 引用 FastReport 子模块
└── ReportPlugin.sln          # 独立解决方案
```

## 关键 API
| 方法 | 参数 | 返回值 |
|------|------|--------|
| `GenerateReport` | reportName, format="pdf", outputPath? | 文件路径 |
| `PreviewReport` | reportName | Base64 PDF |
| `DesignReport` | reportName | 模板 XML JSON |
| `GetReportNames` | — | 7 种报表名称 |

## 约定
- 报表模板：`.frx` 文件放在 `templates/` 目录（含 nwind.xml 数据源）
- 代码生成报表：`SimpleList`（扁平列表）、`Group`（按部门分组）
- 输出格式：`pdf`、`jpg`、`png`（`SupportedFormats` 静态数组）
- 演示数据：8 名员工，4 个部门（硬编码在 `CreateDemoDataSet`）
- FastReport.OpenSource 不支持直接打印 — 改为生成 PDF
- 资源释放：`try/finally` 确保 `report.Dispose()`

## 反模式（禁止）
- 不要添加真实数据库连接 — 仅使用演示 DataSet
- 不要移除 `finally` 块中的 `report.Dispose()`
- 不要直接修改 `templates/` 下的 `.frx` 文件 — 通过 `DesignReport` API 编辑
