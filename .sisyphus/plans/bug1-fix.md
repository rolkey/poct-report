# Bug1: PluginManager 可选参数匹配修复

## TL;DR

> **快速总结**: 修复 `PluginManager.Invoke` 中因为严格参数计数匹配导致带可选默认值参数的方法无法被调用的 Bug。当调用 `GenerateReport("Test1", "pdf")`（2参数）时，方法签名有3个参数（2个有默认值），当前代码因 `3 != 2` 而跳过匹配，引发"找不到函数"错误。
>
> **交付物**:
> - 修改 `TrayApp/PluginManager.cs:46` — 参数匹配守卫条件
> - 修改 `TrayApp/PluginManager.cs:52` — 循环边界（耦合修改，否则引发越界）
> - 新增可选参数默认值填充逻辑
>
> **预估工作量**: 小（30分钟）
> **并行执行**: 单任务（1个文件修改）
> **关键路径**: 单一任务，无依赖

---

## Context

### 原始需求
> 见 `docs/specs/bug1/requirements.md`
> - Bug：调用 `GenerateReport("Test1", "pdf")` 时报错"找不到函数"
> - 建议修复：修改 `PluginManager.Invoke` 中 `if (pis.Length != (parameters?.Length ?? 0)) continue;` 这一行

### 调研总结
**问题分析**:
- `ReportPlugin.GenerateReport(string reportName, string format = "pdf", string? outputPath = null)` — 3个参数，2个有默认值
- WebSocket 调用传 `parameters: ["Test1", "pdf"]` — 仅2个参数
- `PluginManager.cs:46`: `pis.Length (3) != parameters.Length (2)` → `continue` → 无匹配 → 抛出异常

**受影响的方法**（所有带可选参数的插件方法）:
| 方法 | 签名 | 可选参数数 |
|------|------|-----------|
| `GenerateReport` | `(string, string="pdf", string?=null)` | 2 |
| `PrintReport` | `(string, string?=null)` | 1 |
| `PreviewReport` | `(string, string="png")` | 1 |
| `ConfigureReport` | `(string, string?=null)` | 1 |

**无关的方法**:
| 方法 | 签名 | 原因 |
|------|------|------|
| ExamplePlugin 所有方法 | 无可选参数 | 不受影响 |

### Metis Review
**发现的关键问题**:

| # | 问题 | 严重程度 | 处理方式 |
|---|------|---------|---------|
| 1 | **第52行循环边界越界**：当前 `for (int i = 0; i < pis.Length; i++)` 当 `parameters.Length < pis.Length` 时 `parameters[i]` 会越界 | CRITICAL | 必须与第46行耦合修复 |
| 2 | **DBNull.Value 处理**：`ParameterInfo.DefaultValue` 对 `string? x = null` 返回 `DBNull.Value`，不能直接赋值 | CRITICAL | 必须映射为 null |
| 3 | **必填参数计数**：需要计算 `!p.IsOptional` 的个数作为下限 | CRITICAL | 拒绝参数过少的调用 |
| 4 | **参数过多**：调用参数 > 方法参数总数时应拒绝 | CRITICAL | 保留错误边界 |
| 5 | **无测试基础设施**：项目无测试工程 | MINOR | 需要在计划中确定测试策略 |
| 6 | **重载优先级**：如果精确匹配和可选匹配同时存在，优先选精确匹配 | MINOR | 当前无重载方法，加安全注释 |

---

## Work Objectives

### Core Objective
修复 `PluginManager.Invoke` 方法参数匹配逻辑，使其正确处理带可选默认值参数的方法。

### 具体交付物
1. 修改 `TrayApp/PluginManager.cs` 第 46 行守卫条件
2. 修改同方法第 52 行循环边界
3. 添加默认值填充逻辑（处理 `DBNull.Value` → `null` 映射）

### Definition of Done
- [ ] `GenerateReport("Test1", "pdf")` 调用成功返回文件路径
- [ ] `GenerateReport("Test1")` 调用成功（1参数，使用2个默认值）
- [ ] `GenerateReport("Test1", "pdf", "/tmp")` 调用成功（全参数，回归测试）
- [ ] `greet("World")`、`add(1, 2)`（无可选参数）仍然正常
- [ ] 参数过多（4参数vs3）返回"Parameter mismatch"
- [ ] 拼写错误的方法名返回"Method not found"

### Must Have
- 支持 `parameters.Length` 在 `[requiredCount, totalCount]` 范围内匹配
- 缺失的可选参数从 `ParameterInfo.DefaultValue` 获取默认值
- `DBNull.Value` 正确映射为 `null`
- 所有现有插件（ExamplePlugin）行为不变

### Must NOT Have（防护栏）
- 不改动 `Invoke` 方法签名或返回类型
- 不改动任何插件代码（ReportPlugin, ExamplePlugin）
- 不改动 `WebSocketServer.cs`
- 不改动类型转换逻辑（`Convert.ChangeType`，第54-78行）
- 不新增 NuGet 依赖
- 不引入方法元数据缓存
- 不新增重载解析优先级逻辑（当前无重载，以后再说）

---

## Verification Strategy

> **验证目标**: 修改完成后确认 `dotnet build` 打包成功即可。

### 测试决策
- **测试基础设施存在**: NO
- **自动化测试**: 无（用户仅要求确认打包成功）
- **Agent 执行 QA**: 编译验证 + 关键场景手动调试验证（见 QA Scenarios）

---

## Execution Strategy

### 并行执行波浪

```
Wave 1（单任务，1个文件修改）:
└── Task 1: 修改 PluginManager.cs 参数匹配逻辑 + QA 验证

Wave FINAL（验证）:
├── F1: Plan Compliance Audit (oracle)
├── F2: Code Quality Review (unspecified-high)
├── F3: Real Manual QA (unspecified-high)
└── F4: Scope Fidelity Check (deep)
  → 展示结果 → 等待用户显式确认
```

---

## TODOs

- [x] 1. 修复 PluginManager.Invoke 可选参数匹配逻辑

  **What to do**:
  - 修改 `TrayApp/PluginManager.cs` 第 46 行守卫条件
  - 修改第 52 行循环边界
  - 添加默认值填充逻辑
  - 编译验证

  **具体修改步骤**:
  
  1. **第46行 — 守卫条件修改**：
     
     原代码：
     ```csharp
     if (pis.Length != (parameters?.Length ?? 0))
         continue;
     ```
     
     新代码：
     ```csharp
     int paramCount = parameters?.Length ?? 0;
     int requiredCount = pis.Count(p => !p.IsOptional);
     if (paramCount < requiredCount || paramCount > pis.Length)
         continue;
     ```
     
     说明：
     - `requiredCount` = 没有默认值的必需参数数量
     - `paramCount < requiredCount` → 参数太少，跳过
     - `paramCount > pis.Length` → 参数太多，跳过
     - `paramCount` 在 `[requiredCount, pis.Length]` 范围内 → 允许匹配

  2. **第52行 — 循环边界修改**：
     
     原代码：
     ```csharp
     for (int i = 0; i < pis.Length; i++)
     ```
     
     新代码：
     ```csharp
     for (int i = 0; i < paramCount; i++)
     ```
     
     说明：循环只迭代实际传入的参数，避免越界访问 `parameters[i]`。

  3. **添加默认值填充（转换循环之后）**：
     
     在转换循环结束后、`targetMethod` 赋值之前，添加：
     ```csharp
     // 填充可选参数的默认值
     for (int i = paramCount; i < pis.Length; i++)
     {
         var defaultVal = pis[i].DefaultValue;
         converted[i] = defaultVal == DBNull.Value ? null : defaultVal;
     }
     ```
     
     说明：
     - 对调用方未提供的可选参数，从 `ParameterInfo.DefaultValue` 获取默认值
     - `DBNull.Value`（对应 `string? = null`）映射为 `null`
     - `"pdf"`、`"png"` 等实际值直接使用

  **Must NOT do**:
  - 不要修改类型转换逻辑（`Convert.ChangeType` 部分）
  - 不要修改插件代码
  - 不要添加 NuGet 包
  - 不要修改方法签名

  **Recommended Agent Profile**:
  > 单文件 C# 修改，逻辑清晰，无需专业技能
  - **Category**: `quick`
    - 理由：单文件修改，逻辑简单明确，不需要深度研究
  - **Skills**: 无
  - **Skills Evaluated but Omitted**: 所有 — 纯反射逻辑修改，不需要前端/设计/写作技能

  **Parallelization**:
  - **Can Run In Parallel**: NO（单文件单修改）
  - **Blocks**: Final Verification Wave
  - **Blocked By**: None

  **References**:

  **Pattern References**:
  - `TrayApp/PluginManager.cs:40-86` — 整个 Invoke 方法，理解当前匹配算法的完整流程

  **API/Type References**:
  - MSDN: `ParameterInfo.IsOptional` — 判断参数是否可选
  - MSDN: `ParameterInfo.HasDefaultValue` — 判断参数是否有默认值
  - MSDN: `ParameterInfo.DefaultValue` — 获取默认值（注意 `DBNull.Value` 行为）
  - MSDN: `System.DBNull.Value` — 需要检查并映射为 null

  **External References**:
  - `https://learn.microsoft.com/en-us/dotnet/api/system.reflection.parameterinfo.isoptional`
  - `https://learn.microsoft.com/en-us/dotnet/api/system.reflection.parameterinfo.defaultvalue`

  **WHY Each Reference Matters**:
  - `PluginManager.cs:40-86`: 完整上下文 — 修改必须在理解整个方法流程（参数匹配 → 类型转换 → 调用）后进行
  - `ParameterInfo.IsOptional`: 这是计算必需参数数量的关键 API
  - `ParameterInfo.DefaultValue` + `DBNull.Value`: 这是正确填充缺省参数的关键 — 误将 `DBNull.Value` 传递给方法会引发异常

  **Acceptance Criteria**:

  **QA Scenarios（MANDATORY — 验证修复正确 + 不影响现有功能）**:

  ```
  Scenario A: Build 验证
    Tool: Bash
    Preconditions: 代码修改完成
    Steps:
      1. dotnet build TrayApp/TrayApp.csproj
    Expected Result: Build succeeded, 0 errors, 0 warnings
    Evidence: .sisyphus/evidence/task-1-build.txt

  Scenario B: 关键场景验证 — 修改后启动 TrayApp，通过 WebSocket 调用 GenerateReport
    Tool: Bash
    Preconditions: TrayApp 编译成功，插件已放置
    Steps:
      1. 如果环境允许：启动 TrayApp，通过 ws 发送 {"plugin":"ReportPlugin","method":"GenerateReport","parameters":["SimpleList","pdf"]}
      2. 如果环境不允许图形/WSL：仅确认 dotnet build 通过 + 代码逻辑正确
    Expected Result: 目标环境可用时验证调用成功
    Evidence: .sisyphus/evidence/task-1-functional.txt
  ```

  **证据收集**:
  - [ ] `dotnet build` 输出保存到 `.sisyphus/evidence/task-1-build.txt`

  **Commit**: YES
  - Message: `fix(PluginManager): support optional parameters in method matching`
  - Files: `TrayApp/PluginManager.cs`
  - Pre-commit: `dotnet build TrayApp/TrayApp.csproj`

---

## Final Verification Wave

- [x] F1. **Plan Compliance Audit** — `oracle`
  检查 Must Have 是否全部实现，Must NOT Have 是否有违规，证据文件是否存在。
  Output: `Must Have [N/N] | Must NOT Have [N/N] | Tasks [N/N] | VERDICT: APPROVE/REJECT`

- [x] F2. **Code Quality Review** — `unspecified-high`
  运行 `dotnet build`。检查改动质量：是否引入 `as any`/空 catch/硬编码等反模式。
  Output: `Build [PASS/FAIL] | Files [N clean/N issues] | VERDICT`

- [x] F3. **Real Manual QA** — `unspecified-high`
  从干净的编译状态开始，执行所有 QA 场景，验证每个场景通过。
  Output: `Scenarios [N/N pass] | VERDICT`

- [x] F4. **Scope Fidelity Check** — `deep`
  对比 spec 和实际 diff，确认只修改了必要代码，没有 scope creep。
  Output: `Tasks [N/N compliant] | Contamination [CLEAN/N issues] | VERDICT`

---

## Commit Strategy

- **1**: `fix(PluginManager): support optional parameters in method matching` — `TrayApp/PluginManager.cs`, `dotnet build TrayApp/TrayApp.csproj`

---

## Success Criteria

### Verification Commands
```bash
dotnet build TrayApp/TrayApp.csproj
# Expected: Build succeeded (0 errors)
```

### Final Checklist
- [ ] `GenerateReport("Test1", "pdf")` 调用成功
- [ ] 所有现有方法（无可选参数）调用正常
- [ ] 参数太少/太多场景被正确拒绝
- [ ] `dotnet build` 通过
- [ ] 所有 QA 场景证据文件已生成
