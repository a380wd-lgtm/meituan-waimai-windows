# 当前任务清单 (tasks/todo.md)

- [x] 下载最新版美团外卖官方 APK (`v8.84.2`, `800840002`, `build 91293`) 并校验 SHA-256 (`0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82`)
- [x] 完成 REA MCP 全套静态逆向取证并导出 83 条证据链 (`rea-evidence-bundle.json`, `unknowns = 0`)
- [x] 提取 APK 原生高清图标 (`wm_ic_launcher.png`, `wm_kangaroo_mascot.png` 等 134 张) 与内置 JSON 配置 (`future_king_kong.json`, `easygo.json`)
- [x] 完成 `deliverables/rebuild-feasibility-matrix.md`、`deliverables/rea-reverse-engineering-report.md`、`deliverables/windows-client-architecture.md`、`SPEC.md`
- [x] 初始化初始 Git 提交并创建 Codex 独立 Git Worktree (`meituan-waimai-windows-wt-codex`)
- [x] 双 AI 并行开发：Codex 编写契约/算价/存储/自检核心库 (`WaimaiContractsAndEngine.cs`) + Antigravity 编写 WPF 1:1 界面与 EasyGo 平行视界交互 (`MainWindow.xaml` / `WaimaiMainViewModel.cs` / `ApkAssetLoader.cs`)
- [x] 合并双 AI 代码、执行 `exhaustive-code-audit` 扫描与 `--selftest` (`44/44 Passed`, `FailedCount = 0`)
- [x] 生成全视图离屏渲染截图 (`deliverables/screenshots/01..05.png`) 并完成双 AI 红队复审
- [x] 推送完整工程备份至 GitHub (`a380wd-lgtm/meituan-waimai-windows`)
