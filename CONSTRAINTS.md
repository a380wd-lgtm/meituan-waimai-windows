# Constraints (`meituan-waimai-windows`)

Last reviewed: 2026-10-22 by Dual-AI (`Antigravity` + `Codex gpt-6-astra`)

## Floor (Always Enforced)

- **Native Mobile Viewport Parity (`412 × 892 dp`)**: Main window MUST default to native Android portrait dimensions (`Width="412" Height="892"`, aspect ratio ~`9:19.5`), strictly banning wide-screen PC admin dashboard layouts or side-by-side split tables.
- **Mandatory Startup & In-App Native Login (`com.meituan.passport.LoginActivity`)**: Startup screen and unauthenticated interceptors MUST render the 1:1 Meituan Passport Login surface (`+86` mobile input, 6-digit SMS code with 60s countdown & simulated SMS banner, password login tab, QR code login tab, privacy agreement checkbox with confirmation dialog, quick test account selector, and logout/switch-account actions).
- **Zero Fake Tabs & Zero Dead Buttons**: Every tab (`首页 / 神券 / 神抢手 / 订单 / 我的`), restaurant sub-tab (`点菜 / 评价 / 商家`), full-screen push page (`门店详情 / 确认订单 / 订单轨迹与IM / 收货地址管理与新增 / 我的收藏 / 消息中心`), and modal (`多规格SKU / 购物车抽屉 / 美团收银台 / 评价晒单 / 隐私协议弹窗`) MUST be 100% interactive and state-backed.
- **Zero UI-Thread Blocking (ECA-01 ~ ECA-07)**: No `.Result` / `.Wait()` / `.GetResult()` on UI thread, no synchronous `File.*` or `ProtectedData.*` on UI thread, all APK icons decoded with `DecodePixelWidth` + `BitmapCacheOption.OnLoad` + `Freeze()`.
- **No Unimplemented Stubs**: No `NotImplementedException`, empty command handlers, or weakened tests.

## Enforced with Numbers

| Dimension | Rule | Checked by | Runs at |
|---|---|---|---|
| Build | 0 compiler errors, 0 warnings | `powershell -ExecutionPolicy Bypass -File windows-client/waimai-client/build-waimai.ps1` | every edit |
| Obligation Ledger | 35 / 35 obligations (`OBL-01`..`OBL-35`) implemented & verified (`unknowns = 0`) | `MeituanWaimaiClient.exe --selftest` | task end |
| Self-Test Suite | 100% pass rate (`FailedCount == 0`, `PassedCount >= 50`) | `MeituanWaimaiClient.exe --selftest` | task end |
| Visual Parity Matrix | 8 high-DPI native `412×892` portrait screenshots covering Login, Home, Restaurant (Menu/Reviews/Merchant), OrderConfirm & Cashier, ShenQuan, OrderTracking & IM, Mine & Address Manager | `MeituanWaimaiClient.exe --render-screenshots` | task end |
| Dual-AI Red-Team Gate | Independent Codex (`gpt-6-astra`) review signs off `VERDICT: APPROVED` | `Invoke-CodexWorker.ps1` (`mode: "review"`) | before completion |
