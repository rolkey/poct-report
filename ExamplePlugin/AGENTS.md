# ExamplePlugin

**插件：** 演示插件 — 简单的 greet/echo/add 命令，用于验证插件加载机制。

## 关键 API
| 方法 | 参数 | 返回值 |
|------|------|--------|
| `greet` | name: string | `"Hello, {name}!"` |
| `echo` | message: string | 原样返回 |
| `add` | a: int, b: int | 两数之和 |

## 约定
- 无外部依赖（仅 `Newtonsoft.Json` 来自项目模板）
- 方法名小写开头（与 `ReportPlugin` 的 PascalCase 不同 — 反射 `IgnoreCase` 兼容）
- 单文件实现（19 行），适合作为插件开发参考

## 反模式（禁止）
- 不要添加复杂逻辑 — 保持作为最小可工作示例
- 不要添加 NuGet 依赖 — 保持零依赖
