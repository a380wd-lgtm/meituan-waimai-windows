# PROJECT.md — 美团外卖 (Meituan Waimai) 逆向工程与 Windows 1:1 重构项目

## 项目概览
本项目采用 **双 AI 联合研发模式（模式 2：分工互审 · 独立 Git Worktree 并行开发与双向红队审查，Antigravity + OpenAI Codex `gpt-6-astra` ultra）**，严格遵循 `rebuild-feasibility`、`reverse-engineer-anything`、`spec-driven-development`、`wpf-dotnet-performance` 与 `exhaustive-code-audit` 技能规范，对官方最新版 **美团外卖 Android APK (`com.sankuai.meituan.takeoutnew` v8.84.2, `800840002`)** 进行深度静态逆向，并在 Windows 平台 1:1 重构出高性能原生 WPF 桌面客户端。

## 目录结构
- `package/`: 官方 APK 样本元数据 (`apk-download-metadata.json`，APK 本体因体积与合规通过 `.gitignore` 管理，保留完整 SHA-256 与可复现实验脚本 `tools/fetch_latest_apk.py`)
- `research/`: REA 逆向产物
  - `AndroidManifest.decoded.xml`: 147,301 字符完整解码 Manifest
  - `contracts/`: 12 个核心业务类（`MainActivity`, `HomePageFragment`, `Poi`, `GoodsSpu`, `ShopCartPrice`, `ShopCartItem`, `OrderListResponse`, `Order`, `MineMPFragment` 等）的字段与方法签名契约 JSON
  - `decompiled/`: 7 个关键业务方法的 JADX 反编译 Java 源码（带 REA Evidence ID 溯源头）
  - `apk-assets/`: 从 APK `assets/` 提取的 14 个原生 JSON 契约配置（含 `future_king_kong.json`, `easygo.json`, `iconfont_name.json`, `mrn_business.json` 等）
  - `apk-resource-inventory.json` & `extracted-visual-assets.json`
- `deliverables/`: 逆向与架构分析交付报告
  - `rebuild-feasibility-matrix.md`: P0–P3 重建可行性与信任边界评估矩阵
  - `rea-reverse-engineering-report.md`: REA 深度逆向工程分析报告
  - `windows-client-architecture.md`: Windows 客户端 1:1 架构设计与验证报告
- `ui-layout/assets/`: 从官方 APK 提取并转换的 60+ 原生高清 PNG 视觉资产（含 `wm_ic_launcher.png`、`wm_kangaroo_mascot.png` 等）
- `windows-client/waimai-client/`: Windows 原生 WPF 客户端源码、一键构建脚本 `build-waimai.ps1`、自动化契约自检与离屏截图引擎
- `.ai-collab/`: 双 AI 协同绑定配置、任务派单与双向审查记录
