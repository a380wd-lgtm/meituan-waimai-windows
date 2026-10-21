# 美团外卖 (Meituan Waimai v8.84.2) REA 深度逆向工程分析报告

## 1. 样本身份与密码学锚点 (Artifact Identity & Digest)

- **文件名**: `package/meituan-waimai-latest.apk`
- **包名 (Package Name)**: `com.sankuai.meituan.takeoutnew`
- **版本名称 (Version Name)**: `8.84.2`
- **版本号 (Version Code)**: `800840002` (`build_number = 91293`)
- **SDK 级别**: `minSdkVersion = 21` (Android 5.0), `targetSdkVersion = 30` (Android 11), `compileSdkVersion = 34` (Android 14)
- **文件大小**: `66,874,352` 字节 (63.78 MB)
- **SHA-256 摘要**: `0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82`
- **REA 确定性证据包**: `rea-evidence-bundle.json` (共 83 条证据记录，`unknowns = 0`)
- **核心 REA Evidence IDs**:
  - `inspect_android_package`: `ev_dfa491886da7142bfa8e2e6f09ec0c62cac8cb51f751061a79062e52881b6e91` (父证据 `ev_a7b3ea6c1c77ae38e76306d98031dbea6b724e20f686da1517ff7312dfd54880`)
  - `inspect_artifact`: `ev_be1290dfa2c741e42395581c4b54199e71b70507069ea9f54461220c6c18c17b` (图谱哈希 `74305f73952acc99d6c34809988a5dee4f6a7e58a4787cf87545b740eba0a383`)
  - `extract_artifact`: `ev_6e91cb560046c0f101384efb3d68a8ea7bfa379725a7c23d0b73ae7b5d4c4921`
  - `AndroidManifest.xml` 完整视图: `ev_d1a81ccb27c6a74628890aed08526a3064292ec669468b315078ba4f5137facf`

---

## 2. 制品容器与 DEX / 资源拓扑结构 (Container & DEX Topology)

通过 REA `inspect_artifact` 与 `inspect_android_package` 对 `meituan-waimai-latest.apk` 进行全量解包与图谱建模：
- **图谱节点数 (`node_count`)**: `6,200`
- **图谱实例数 (`occurrence_count`)**: `6,232`
- **图谱边数 (`edge_count`)**: `6,231`
- **完整性矛盾数 (`integrity_contradictions`)**: `0`
- **已解析 Java/Kotlin 类总数 (`class_count`)**: `42,257`
- **已解析资源条目数 (`resource_count`)**: `6,231`（采用 AndResGuard 混淆路径 `r/*`，其中包含 `2,062` 个 WebP/PNG 图像资产与 `45` 个 `assets/` 预置配置）
- **热修复桩机制**: 全量类方法头部织入了美团 Robust 热修复分派器 `com.meituan.robust.ChangeQuickRedirect` 与 `PatchProxy.isSupport(...)`。

---

## 3. 启动链路与主导航状态机逆向 (Boot Chain & Main Navigation)

### 3.1 启动链 (Boot Sequence)
1. **Application 入口**: `com.sankuai.meituan.takeoutnew.AppApplication` (`android:largeHeap="true"`, `android:hardwareAccelerated="true"`)
2. **Launcher Activity**: `com.sankuai.meituan.takeoutnew.ui.page.boot.WelcomeActivity` (`@style/AppTheme.Welcome`)
   - 负责隐私合规弹窗 (`privacy_preset_config.json`)、初始化定位服务 (`com.amap.api.location.APSService` / `MeituanMapSDK`)、通过 WMRouter `"welcome"` (`IWelcomeProvider`) 调度闪屏广告 `SplashAdActivity`，随后路由至主界面 `MainActivity`。
3. **统一深链中转器**: `com.sankuai.waimai.foundation.core.base.activity.transfer.TransferActivity`
   - 拦截自定义 Scheme 以及 `http(s)://c.dpurl.cn` 通用链接。

### 3.2 主界面与 5-Tab 动态导航栏 (`MainActivity`)
根据 `MainActivity.onCreate` (`ev_0063a0c6f104e543d38943d632c596951f7e026ba44437b8c64e936f02eda476`) 与 `MainActivity.v0` (`ev_05148e135ba41d4a9f18fb39d2c3ea5134ebf9a6b5480c2a354da5510b9c1617`) 的反编译源码，主窗口加载 `R.layout.takeout_activity_main`，背景色为 `R.color.wm_restaurant_common_background_page`，底部挂载 `BottomTabView` (`R.id.pagerindicator`) 与 `FragmentTabHost` (`android.R.id.tabhost`)，由 `DynamicTabListInfoResponse` 驱动 5 个核心 Tab：

| Tab 索引 | `businessId` | 标题资源 / 名称 | 承载 Fragment 实现类 | 关键特性与子容器 |
|---|---|---|---|---|
| **Tab 0** | `1` | `R.string.title_section0` (**首页 / 外卖**) | `WMHomePageFragment` (继承自 `HomePageFragment`) | 顶部袋鼠头图 (`WmHomeRooView`)、定位/搜索栏、金刚区 (`future_king_kong.json`)、筛选栏 (`FilterBarViewController`)、嵌套瀑布流 (`HomePageNestedScrollRecyclerView`)、以及顶层挂载的 **DeepEat AI 助餐浮层 (`R.id.deep_eat_ai_container`)** |
| **Tab 1** | `9` 或 `11` | `R.string.title_section1` (**神券 / 会员 / 内容**) | `WMVIPCardKNBFragment` 或 `MarketingTabKNBFragment` | 根据 `DynamicTabListInfoResponse` 及 `isShowVipCardTab` 动态切换：膨胀神券包、美团神会员卡、必吃榜/特价团购 |
| **Tab 2** | `10` | **大促 / 神抢手** (`TabKnbFragment`) | `com.sankuai.waimai.business.page.home.tab.TabKnbFragment` | 异形凸起大促图标 (`R.id.iv_mt_cardview`, `R.id.iv_top_arc`, `R.id.iv_promotion`)，承载限时秒杀、神抢手爆品与直播券 |
| **Tab 3** | `2` | `R.string.title_section3` (**订单**) | `OrderListMPFragment` (`IOrderListProvider` 路由 `"wm_order"`) | 全部订单、待评价 (`uncomment_count`)、退款/售后 (`refund_count`)、最近常吃 (`RecentEatPoi`)、骑手实时配送卡片 (`OrderStatusBannerView`) |
| **Tab 4** | `3` | `R.string.title_section4` (**我的**) | `com.sankuai.waimai.business.page.mine.MineMPFragment` | 基于 MachPro (`MPBaseFragment`) 构建，通过 `MachMap o3()` 注入上下文，监听 `onAccountInfoUpdate`、`updateUnReadCount` 与 `WmAddress` 变更 |

---

## 4. 关键业务域与算价/状态机契约 (Core Domain Contracts)

### 4.1 商家 (`Poi`) 与商品 (`GoodsSpu`) 契约
- **`Poi` (`ev_9cddb4225370c92468d1c8ee37ad1fdba0e594dc88644c2e8cb8037ad8f89e4b`)**:
  - 包含 201 个字段：核心标识 `id` / `poiIDStr`、名称 `name`、评分 `poiScore` / `formattedPoiScore`、月售 `monthSalesTip`、起送价 `min_price` / `minPriceTip`、配送费 `shippingFee` / `originShippingFee`、配送时长 `mtDeliveryTime` / `shippingTime`、距离 `distance`、营业状态 `state` (`STATE_NORMAL`, `STATE_BUSY`, `STATE_CLOSED`)、业务类型 `mBuzType` (`BUZ_TYPE_BRAND_FOOD`, `BUZ_TYPE_DINNER`, `BUZ_TYPE_QUICK_MEAL`, `BUZ_TYPE_SNACK`, `TYPE_DRUG`)、神券实体 `mPoiCouponEntity`、满减活动标签 `poiActivityTags`。
- **`GoodsSpu` (`ev_dfec8e80b5dfb1a1a0ea6f595797ce868c8c35022b6523fb0551f0ab56150f0d`)**:
  - 包含 111 个字段：商品 `id`、`name`、`minPrice`、`originPrice`、神券/满减到手价 `fullDiscountPrice` (`showFullDiscountPriceStatus`)、月售 `monthSaledContent`、好评度 `praiseContent` / `likeRatioDesc`、规格列表 `skus` (`GoodsSku`)、属性组 `attrs` (`GoodsAttrList` 如辣度/温度/加料)、活动类型 `activityType` (`ACTIVITY_GOOD_COUPON`, `ACTIVITY_TYPE_DISCOUNT`, `ACTIVITY_TYPE_NEW_CUSTOMER`, `ACTIVITY_TYPE_NX`, `ACTIVITY_TYPE_PLUS_BUY`)。

### 4.2 购物车与价格计算器契约 (`ShopCartItem` & `ShopCartPrice`)
- **`ShopCartItem` (`ev_94a34d540d503cfb7f1ef28c32375589d599ea4635377cc1f9a5a86aac97b52a`)**:
  - 绑定 `OrderedFood food`、口袋索引 `pocketId`（支持多口袋/拼单分装）、N件X折计数 `nxItemCount`、活动标签 `actTags`。
- **`ShopCartPrice` (`ev_1dbf43e51054328202990f59c919bfc49db842e928dee0b1bc56572a1bb0941f`)**:
  - 起送门槛 `mThresholdPrice`
  - 商品原总价+打包费 `mTotalAndBoxPrice`
  - 打包费合计 `mTotalBoxPrice` (`totalBoxPrice`)
  - 折后商品+打包费总计 `mTotalDiscountedAndBoxPrice`
  - 不含神券折后价 `mTotalDiscountedAndBoxPriceWithoutGoodCoupon`
  - 配送费减免 `shippingFeeReduce` 与原配送费 `originalShippingFee`
  - 凑单/满减提示文案 `previewSubTip`、`crossTip` (`ShopCartApiService.getNetPriceCalculatorResult` 返回)。

### 4.3 订单列表与实时配送追踪 (`OrderListResponse` & `Order`)
- **`OrderListResponse.fromJson` (`ev_85cbd0da4f81234469001e25c484a966098bd41bafe88fa5e8d9f76304cb4668`)**:
  -精确解析 JSON 键：`"type"`, `"cursor"`, `"multi_cursor"`, `"hasmore"`, `"uncomment_count"`, `"has_unread_comment_order"`, `"unread_uncomment_tips"`, `"digestlist"`, `"wait_time"`, `"list_types"`, `"refund_count"`, `"bottom_tips"`, `"search_default_tips"`, `"has_unread_refund_order"`, `"share_type"`, `"tracking_info"`, `"envelope_share_tip"`, `"activity_share_tip"`, `"ugc_pop_ups_info"`。
- **`Order` (`ev_4a9be17fa5daf89e6752599968f215f33f404b26dc7d77d3463a41cb8a9fea88`)**:
  - 包含 74 个字段：`orderId`, `poiId`, `poiName`, `poiPic`, `status`, `statusDescription`, `payStatus`, `deliveryStatus`, `total`, `arrivalTime`, `latitude`, `longitude`, `recepientAddress`, `recepientPhone`, `foodList`, `buttonTypeList`, `subDeliveryInfoList`。

### 4.4 大屏平行视界契约 (`assets/easygo.json`)
- 原生 APK 内置 `assets/easygo.json` 配置文件，声明了 `magicwindow` 双栏平行视界能力：
  - 主左栏 (`mainPages`): `com.sankuai.waimai.business.page.homepage.MainActivity`
  - 关联右栏 (`relatedPage`): `com.sankuai.waimai.business.page.kingkong.KingKongActivity` / 门店点餐详情 / 订单追踪详情
  - 支持左右分栏拖拽调节 (`"isDraggable": "true"`)。
