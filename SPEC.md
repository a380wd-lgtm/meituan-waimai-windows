# 美团外卖 Windows 原生桌面重构版 (Meituan Waimai Windows 1:1 Specification)

## 1. 目标与范围 (Objective & Scope)
基于美团外卖官方最新版 Android 客户端 (`com.sankuai.meituan.takeoutnew` v8.84.2, `versionCode=800840002`, SHA-256: `0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82`) 的 REA 静态逆向证据，使用 C# / WPF (.NET Framework 4.8 / Windows 原生桌面技术栈) 1:1 还原美团外卖客户端的视觉体系、5-Tab 动态导航架构、EasyGo 平行视界双栏分屏模式、门店点餐与多口袋购物车算价引擎、订单实时骑手轨迹与 IM 群聊、以及官方 H5 WebView2 会话桥接能力。

## 2. 模块能力地图 (Module Capability Map)

| 模块编号 | 模块名称 | 对应 Android 逆向锚点 | 核心职责与验收标准 |
|---|---|---|---|
| **M1** | **契约与算价核心库 (`Contracts` & `ShopCartCalculator`)** | `Poi`, `GoodsSpu`, `ShopCartItem`, `ShopCartPrice`, `OrderListResponse`, `Order`, `WmAddress` | 1:1 字段对齐；精确实现起送价差额、满减阶梯、膨胀神券、多规格属性加价、打包费 (`mTotalBoxPrice`)、N件X折 (`nxItemCount`) 与配送费减免 (`shippingFeeReduce`) 计算；支持 `OrderListResponse` 真实 JSON 键反序列化。 |
| **M2** | **安全持久化与数据仓储 (`Storage` & `DPAPI`)** | `WelcomeActivity`, `MineMPFragment`, `AddressBean` | 使用 Windows `ProtectedData` (DPAPI CurrentUser) + `Task.Run` 异步读写本地购物车快照、订单历史、收货地址簿与神券钱包，UI 线程零同步 I/O。 |
| **M3** | **主壳与 5-Tab + EasyGo 平行视界视图 (`MainWindow`)** | `MainActivity`, `BottomTabView`, `assets/easygo.json` | 1:1 复刻底部 5-Tab（首页外卖、神券会员、大促神抢手、订单、我的）；支持一键切换 **Mobile 1:1 单栏模式** 与 **EasyGo 桌面平行视界双栏分屏模式**。 |
| **M4** | **外卖首页与金刚区/筛选/DeepEat AI (`HomeView`)** | `WMHomePageFragment`, `HomePageFragment`, `FilterBarViewController`, `DeepEatStateManager` | 顶部袋鼠视觉头部 (`WmHomeRooView`)、地址切换、搜索框、10 宫格金刚区 (`future_king_kong.json`)、4 维排序+价格滑块筛选 (`SliderSelectData`)、虚拟化 POI 列表 (`VirtualizingStackPanel`)、DeepEat AI 智能点餐助手面板。 |
| **M5** | **门店点餐、规格弹窗与购物车抽屉 (`RestaurantDetailView`)** | `MPRestaurantShopCartComponent`, `GoodsSpu`, `ShopCartPrice` | 门店头部招牌、神券领取、左侧分类锚点 + 右侧虚拟化菜品列表、多规格/属性选择弹窗 (`skus` + `attrs`)、底部黑金购物车悬浮条与凑单满减提示 (`previewSubTip`)、一键下单生成真实订单。 |
| **M6** | **订单列表、骑手实时地图轨迹与 IM 群聊 (`OrdersView`)** | `OrderListMPFragment`, `CustomImCardMapView`, `WmGroupChatFragment`, `SmartReplyPlugin` | 支持全部/待评价/退款筛选、订单 JSON 导入导出校验、骑手配送实时矢量地图轨迹展示、骑手/商家三方 IM 群聊与智能快捷回复。 |
| **M7** | **神券会员、大促神抢手、我的中心与官方 H5 桥接 (`Vip/Promo/Mine/WebBridge`)** | `WMVIPCardKNBFragment`, `TabKnbFragment`, `MineMPFragment` | 神券膨胀/领取并联动购物车抵扣、神抢手限时秒杀加购、我的资产/地址管理、以及合规的官方 H5 (`https://h5.waimai.meituan.com/waimai/mindex/home`) 桥接模式。 |
| **M8** | **自动化契约自检与离屏渲染截图套件 (`SelfTest` & `OffscreenRender`)** | `verification-before-completion`, `exhaustive-code-audit` | 命令行 `--selftest` 自动运行全套契约/算价/JSON 解析/状态机/异步持久化测试；`--render-screenshots` 离屏渲染全部核心视图高清 PNG 截图用于视觉验收。 |

## 3. 性能与代码审计硬性约束 (`wpf-dotnet-performance` & `exhaustive-code-audit`)
1. **ECA-01 (零 Sync-over-Async)**: 严禁在 UI 代码中出现 `.Result`、`.Wait()` 或 `.GetAwaiter().GetResult()`。
2. **ECA-02 / ECA-03 (零 UI 线程同步 I/O 与 DPAPI)**: 所有文件读写与 `ProtectedData.Protect/Unprotect` 必须封装在 `await Task.Run(...)` 后台线程中。
3. **ECA-04 (后台 JSON 反序列化)**: 所有 JSON 解析均在后台线程完成后再切回 Dispatcher 绑定。
4. **ECA-05 (定时器生命周期治理)**: 骑手位置更新定时器 (`DispatcherTimer`) 必须在视图不可见或窗口卸载 (`Unloaded` / Tab 切换离开订单页) 时自动 `Stop()`，杜绝后台空转。
5. **长列表虚拟化与硬件解码**: POI 列表与菜品列表必须配置 `VirtualizingStackPanel` (`IsVirtualizing="True"`, `VirtualizationMode="Recycling"`, `ScrollUnit="Pixel"`)，所有本地图片加载必须指定 `DecodePixelWidth` 并调用 `Freeze()`。
