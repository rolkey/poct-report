# testVue

**前端：** Vue 3 + TypeScript WebSocket 客户端，用于调用 C# 插件生成/预览报表。

## 目录结构
```
testVue/
├── src/
│   ├── main.ts               # 入口：createApp(App)
│   ├── App.vue               # 根组件（仅渲染 HelloWorld）
│   ├── style.css             # 全局样式 + CSS 变量主题（light/dark）
│   ├── components/
│   │   └── HelloWorld.vue    # 全部 UI：连接、报表操作、PDF 预览弹窗
│   ├── utils/
│   │   └── websocket.ts      # useWebSocket() 组合式函数
│   └── assets/               # 静态资源
├── package.json              # Vue 3.5 + Vite 8 + TypeScript 6
├── vite.config.ts            # 最小配置（仅 Vue 插件）
├── tsconfig.json             # 项目引用：app + node
├── tsconfig.app.json         # DOM 类型、严格 linting
└── tsconfig.node.json        # bundler 模式、verbatimModuleSyntax
```

## 关键代码索引
| 关注点 | 文件 | 关键 |
|--------|------|------|
| WS 连接 | `src/utils/websocket.ts` | `useWebSocket()` — connect/send/onMessage |
| 报表操作 | `src/components/HelloWorld.vue:66-96` | generateReport / previewReport / designReport |
| PDF 预览 | `src/components/HelloWorld.vue:146-160` | iframe + Blob URL 弹窗 |
| 主题 | `src/style.css` | CSS 变量 + `prefers-color-scheme: dark` |

## 约定
- `<script setup lang="ts">` SFC 风格
- `useXxx()` 组合式函数模式（`useWebSocket`）
- TypeScript 严格：`noUnusedLocals`、`noUnusedParameters`、`noFallthroughCasesInSwitch`
- `verbatimModuleSyntax` — 类型导入必须用 `import type`
- CSS scoped 组件样式 + 全局 CSS 变量主题
- WebSocket 请求格式：`{"command":"...","plugin":"...","method":"...","params":[...]}`

## 反模式（禁止）
- 不要在组件中直接创建 WebSocket — 始终使用 `useWebSocket()`
- 不要硬编码 WebSocket URL 以外的配置 — 使用 `wsUrl` ref
- 不要忘记 `onUnmounted` 清理 — composable 已自动处理
