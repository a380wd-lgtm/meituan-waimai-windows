# 美团外卖 (Meituan Waimai v8.84.2) 逆向重建可行性与信任边界矩阵 (Rebuild Feasibility Matrix)

- **目标制品**: `package/meituan-waimai-latest.apk` (`com.sankuai.meituan.takeoutnew`)
- **版本信息**: `versionName = 8.84.2`, `versionCode = 800840002`, `minSdk = 21`, `targetSdk = 30`, `compileSdk = 34`, `build_number = 91293`
- **APK SHA-256**: `0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82`
- **REA 根图谱证据**: `ev_be1290dfa2c741e42395581c4b54199e71b70507069ea9f54461220c6c18c17b` (`agm_8b3711f4dc7a2ff01d24db57bbee82a0ae4444b87930e98e08c86e10aa7f7db9`)
- **REA 包摘要证据**: `ev_dfa491886da7142bfa8e2e6f09ec0c62cac8cb51f751061a79062e52881b6e91` (`ev_a7b3ea6c1c77ae38e76306d98031dbea6b724e20f686da1517ff7312dfd54880`)
- **分析基准**: 严格遵循 `rebuild-feasibility` 与 `reverse-engineer-anything` 技能规范。所有结论均锚定于可复验的静态制品证据（Confidence: `observed` / `derived`），严禁伪造安全签名或绕过设备证明。

---

## 一、P0–P3 核心准入与重建可行性评估 (P0–P3 Feasibility Gate)

| 优先级 | 评估维度 | 原版 APK 逆向证据 (Evidence-Backed) | 风险与信任边界分析 | Windows 端 1:1 重构落地策略 | 结论状态 |
|---|---|---|---|---|---|
| **P0** | **应用准入、设备证明与反作弊网关 (MTGuard / Candy)** | `AndroidManifest.xml` (`ev_d1a81ccb27c6a74628890aed08526a3064292ec669468b315078ba4f5137facf`) 声明：<br>• `mtguard_appkey`: `e68c4c18-35ec-4972-84e4-32a0836455a8`<br>• `mtguard_pic`: `ms_com.sankuai.meituan.takeoutnew`<br>• `mtguard_sec`: `ppd_com.sankuai.meituan.takeoutnew.xbt`<br>• Native 库含 `libmtguard.so` | 原生 RPC 接口受 MTGuard 硬件指纹与 Native 签名保护。根据 `rebuild-feasibility` 铁律，**严禁逆向破解或伪造 MTGuard 签名**。 | 采用**双引擎架构**：<br>1. **官方 H5 WebView2 会话桥接引擎**：内嵌官方 `https://h5.waimai.meituan.com/waimai/mindex/home`，由官方同源页面在浏览器沙箱内完成合规登录与风控校验，零凭据截获。<br>2. **1:1 原生 WPF 契约引擎**：完整复刻 `Poi`、`GoodsSpu`、`ShopCartPrice`、`OrderListResponse` 原生契约状态机与交互。 | **FEASIBLE (双模合规桥接)** |
| **P0** | **账号体系与会话持久化 (Passport / UserCenter)** | `MineMPFragment` (`ev_7e6b29c783a3a0e4f4569849b5ce76686196059501a404c6744d68edfcf6661e`) 监听 `com.sankuai.waimai.foundation.core.service.user.b$a` / `b$b` (`onAccountInfoUpdate`)；启动链经过 `WelcomeActivity` (`ev_d7c2e8ffe851e0687abcff59f208e5a1ee875169566a9c9eef69c83f0f84421d`)。 | 第三方客户端不得拦截明文密码或伪造 OAuth `weixin_key` (`wx9f6523d23a33a5b3`) 签名。 | 本地会话状态与收货地址薄 (`WmAddress`) 使用 **Windows DPAPI (`ProtectedData` CurrentUser)** 异步加密存储于 `%LocalAppData%\MeituanWaimaiWindows\`，并在后台线程 (`Task.Run`) 执行读写。 | **FEASIBLE (DPAPI 安全持久化)** |
| **P1** | **核心业务闭环：首页 Feed、金刚区、门店点餐、购物车算价、订单追踪** | • `MainActivity` (`ev_0063a0c6f104e543d38943d632c596951f7e026ba44437b8c64e936f02eda476`) 驱动 5 大 Tab (`businessId`: `1`=外卖首页, `9/11`=神券/会员, `10`=大促, `2`=订单, `3`=我的)<br>• `Poi` (`ev_9cddb4225370c92468d1c8ee37ad1fdba0e594dc88644c2e8cb8037ad8f89e4b`)<br>• `GoodsSpu` (`ev_dfec8e80b5dfb1a1a0ea6f595797ce868c8c35022b6523fb0551f0ab56150f0d`)<br>• `ShopCartPrice` (`ev_1dbf43e51054328202990f59c919bfc49db842e928dee0b1bc56572a1bb0941f`) | 原版使用 `HomePageNestedScrollRecyclerView` + MachPro + MRN (`rn_waimai_wm-good-detail_3.0.709`)。桌面端需解决长列表流畅度与复杂满减/神券/打包费算价一致性。 | 1. **算价与满减引擎**：1:1 映射 `ShopCartItem` (`nxItemCount`, `pocketId`, `OrderedFood`) 与 `ShopCartPrice` (`mTotalAndBoxPrice`, `mTotalBoxPrice`, `mTotalDiscountedAndBoxPrice`, `mThresholdPrice`, `shippingFeeReduce`)。<br>2. **WPF 高性能虚拟化视图**：强制启用 `VirtualizingStackPanel` (`Recycling` + `ScrollUnit="Pixel"` + `DecodePixelWidth` 硬件缩略图解码)。 | **FEASIBLE (100% 契约级还原)** |
| **P2** | **实时骑手轨迹、IM 群聊与 AI 助餐 (`DeepEat`)** | • `OrderListResponse.fromJson` (`ev_85cbd0da4f81234469001e25c484a966098bd41bafe88fa5e8d9f76304cb4668`) 解析 `digestlist`, `wait_time`, `tracking_info`<br>• `MainActivity` 挂载 `R.id.deep_eat_ai_container` (`DeepEatStateManager`)<br>• `WmGroupChatFragment` 骑手/商家三方群聊 | 原版使用美团地图 SDK (`49058660-1de4-4c13-a401-a7f237e85065`) 与 Pike 长连接通道。 | 在 Windows 客户端实现：**骑手实时配送轨迹矢量画布 (`CustomImCardMapView` 1:1 映射)**、**骑手/商家智能回复 IM 抽屉 (`SmartReplyPlugin`)** 以及 **DeepEat AI 智能点餐助手面板**，所有动画定时器均绑定页面可见性生命周期。 | **FEASIBLE (原生交互复刻)** |
| **P3** | **Android 专属硬件特性与平行视界 (`EasyGo`)** | `assets/easygo.json` 定义了 `magicwindow` 平行视界：`mainPages = MainActivity`, `relatedPage = KingKongActivity`, `isDraggable = true`。 | NFC、蓝牙信标、计步传感器在 Windows 桌面无直接业务意义；而 `EasyGo` 平行视界非常适合 Windows 宽屏桌面！ | 1:1 实现 **移动端紧凑单栏视口 (390×844 标准比例)** 与 **EasyGo 平行视界桌面双栏分屏模式 (左栏首页/导航 + 右栏门店点餐/订单详情/H5 桥接)** 一键切换！ | **FEASIBLE (原生支持 EasyGo)** |

---

## 二、核心类与协议数据契约映射表 (Contract Mapping Ledger)

| 业务域 | Android 原生类 (JADX 实证) | 关键字段契约 (Observed Fields) | Windows C# 契约模型 (`MeituanWaimai.Contracts`) |
|---|---|---|---|
| **底部导航与动态 Tab** | `com.sankuai.waimai.business.page.homepage.MainActivity` (`v0`) | `businessId` (`1`=首页, `9/11`=会员/神券, `10`=大促, `2`=订单, `3`=我的), `DynamicTabListInfoResponse`, `isShowVipCardTab` | `DynamicTabContract`, `BottomNavigationStateMachine` |
| **首页容器与筛选** | `com.sankuai.waimai.business.page.home.HomePageFragment` | `WmHomeRooView`, `HomePageNestedScrollRecyclerView`, `FilterBarViewController`, `SliderSelectData`, `FuTiaoMatrixView`, `DeepEatStateManager` | `HomeFeedViewModel`, `KingKongChannelContract`, `FilterBarCriteria` |
| **商家 POI 模型** | `com.sankuai.waimai.platform.domain.core.poi.Poi` | `id`, `poiIDStr`, `name`, `poiScore`, `monthSalesTip`, `min_price`, `shippingFee`, `originShippingFee`, `mtDeliveryTime`, `distance`, `brandType`, `poiActivityTags`, `mPoiCouponEntity` | `PoiContract`, `PoiCouponSummary`, `PoiPromotionTag` |
| **商品 SPU/SKU 模型** | `com.sankuai.waimai.platform.domain.core.goods.GoodsSpu` | `id`, `name`, `minPrice`, `originPrice`, `fullDiscountPrice`, `monthSaledContent`, `praiseContent`, `unit`, `activityTag`, `skus`, `attrs`, `status` | `GoodsSpuContract`, `GoodsSkuContract`, `GoodsAttrGroupContract` |
| **购物车与算价引擎** | `com.sankuai.waimai.business.restaurant.base.shopcart.ShopCartItem` / `ShopCartPrice` | `food`, `nxItemCount`, `pocketId`, `mThresholdPrice`, `mTotalAndBoxPrice`, `mTotalBoxPrice`, `mTotalDiscountedAndBoxPrice`, `shippingFeeReduce`, `previewSubTip` | `ShopCartItemContract`, `ShopCartPriceContract`, `ShopCartCalculatorEngine` |
| **订单列表与轨迹** | `com.sankuai.waimai.business.order.api.model.OrderListResponse` / `Order` | `digestlist`, `uncomment_count`, `refund_count`, `orderId`, `poiName`, `status`, `statusDescription`, `deliveryStatus`, `total`, `arrivalTime`, `latitude`, `longitude`, `buttonTypeList` | `OrderListResponseContract`, `OrderContract`, `OrderDeliveryTracker` |
| **个人中心 (`MachPro`)** | `com.sankuai.waimai.business.page.mine.MineMPFragment` | `MachMap o3()`, `updateUnReadCount`, `onAccountInfoUpdate`, `WmAddress` | `MineProfileContract`, `ShenQuanWalletContract`, `WmAddressContract` |
