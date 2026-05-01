# testVue/src

**源码目录：** Vue 3 前端的所有业务逻辑。

## 文件职责
| 文件 | 职责 |
|------|------|
| `main.ts` | 应用入口，`createApp(App).mount('#app')` |
| `App.vue` | 根组件，仅渲染 `<HelloWorld />` |
| `style.css` | 全局样式 + light/dark CSS 变量主题 |
| `components/HelloWorld.vue` | 全部 UI：WS 连接、报表操作、PDF 预览弹窗 |
| `utils/websocket.ts` | `useWebSocket()` 组合式函数 |
| `assets/` | 静态资源 |

## 约定
- `main.ts` 保持最小 — 不添加全局注册或插件
- `App.vue` 仅做布局容器 — 业务逻辑在子组件
- `style.css` 使用 CSS 变量（`--text`、`--bg`、`--accent` 等）实现主题
- `websocket.ts` 是唯一的 WS 抽象层 — 组件不直接操作 WebSocket

## 反模式（禁止）
- 不要在 `main.ts` 中添加业务逻辑
- 不要在 `App.vue` 中直接使用 `useWebSocket()` — 下放到子组件
- 不要绕过 `useWebSocket()` 直接创建 WebSocket 实例
