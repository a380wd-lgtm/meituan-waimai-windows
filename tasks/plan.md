# 任务拆解与双 AI 并行实施计划 (tasks/plan.md)

## 阶段一：制品获取与 REA 确定性逆向分析 (已完成)
- [x] 获取官方最新版美团外卖 APK (`com.sankuai.meituan.takeoutnew` v8.84.2, `versionCode=800840002`, SHA-256: `0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82`)。
- [x] 使用 REA MCP 执行 `open_binary`、`inspect_android_package`、`inspect_artifact`、`extract_artifact`、`inspect_analysis_view`、`search_android_classes`、`inspect_android_class`、`inspect_android_method`、`export_evidence_bundle`。
- [x] 提取 `AndroidManifest.decoded.xml`、12 个核心类契约、7 个核心方法源码、14 个 APK 内置 JSON 配置文件及 60+ 原生高清 PNG 视觉资产。
- [x] 编写 `deliverables/rebuild-feasibility-matrix.md`、`deliverables/rea-reverse-engineering-report.md`、`SPEC.md`、`PROJECT.md`。

## 阶段二：双 AI 模式 2 (Git Worktree 并行开发 + 方案 PK)
- [ ] **创建独立 Git Worktree 沙箱**：创建 `meituan-waimai-windows-wt-codex` 分支工作树，供 Codex Worker (`gpt-6-astra` ultra) 独立并行编码。
- [ ] **Codex Worker 派单（底层契约模型、购物车满减/神券算价引擎、OrderListResponse 协议解析器与离线自检套件）**：
  - 在 `windows-client/waimai-client/Core/WaimaiContractsAndEngine.cs` 中实现 1:1 锚定 Android 字段的契约模型 (`PoiContract`, `GoodsSpuContract`, `ShopCartItemContract`, `ShopCartPriceContract`, `OrderListResponseContract`, `OrderContract`, `WmAddressContract`)、多口袋/满减/膨胀神券/N件X折算价引擎 `ShopCartCalculatorEngine`、DPAPI 异步安全存储 `WaimaiStorageService` 以及 15+ 项契约级自检断言 `WaimaiContractSelfTests`。
- [ ] **Antigravity 主工作区并行开发（WPF 高性能 1:1 视觉壳层、5-Tab 动态导航、EasyGo 双栏平行视界、门店点餐、骑手地图轨迹、IM 群聊与离屏渲染截图器）**：
  - 在 `windows-client/waimai-client/` 构建 `WaimaiClient.csproj`、`App.xaml(.cs)`、`MainWindow.xaml(.cs)`、`ViewModels/WaimaiMainViewModel.cs`、`Services/ApkAssetLoader.cs`、`build-waimai.ps1`。

## 阶段三：双 AI 双向交叉审查 (Red-Team Mutual Audit)
- [ ] Antigravity 审查 Codex 在独立工作树提交的 `WaimaiContractsAndEngine.cs`，合并入主工作区并编译运行 `--selftest`。
- [ ] 执行 `exhaustive-code-audit` 全量正则扫描（验证 ECA-01 ~ ECA-07 零违规）。
- [ ] 调用 Codex Worker (`mode: "review"`) 对主工作区完整 `git diff` 与用户需求原话进行铁面红队终审，修复任何发现的缺陷直至获得 `APPROVED` 签字。

## 阶段四：端到端构建验证、离屏渲染截图视觉验收与 GitHub 备份
- [ ] 运行 `build-waimai.ps1` 完成 Release 编译、通过全部离线契约与 UI 状态机自检 (`--selftest`)。
- [ ] 运行 `--render-screenshots` 生成全套高分屏 PNG 截图（首页外卖、门店点餐+购物车、神券会员、大促神抢手、订单骑手地图+IM群聊、我的中心、EasyGo 平行视界双栏分屏模式），并通过 `read_file` 逐张进行视觉审查。
- [ ] 整理 `VERSION-CONTROL.txt`、`READINESS.txt`、`deliverables/windows-client-architecture.md`，提交 Git 仓库并推送备份至 GitHub 远程仓库。
