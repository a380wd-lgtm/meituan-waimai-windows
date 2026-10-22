using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MeituanWaimai.Core;
using MeituanWaimaiClient.Services;

namespace MeituanWaimaiClient.ViewModels
{
    /// <summary>
    /// 轻量级 WPF 命令实现（兼容 C# 5 与 .NET Framework 4.8）。
    /// </summary>
    public sealed class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            EventHandler handler = CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>
    /// 金刚区展示包装项（附带从 APK 解包的冻结图标位图）。
    /// </summary>
    public sealed class KingKongItemViewModel : INotifyPropertyChanged
    {
        private readonly KingKongChannelContract _contract;
        private BitmapSource _iconSource;
        private bool _isSelected;

        public KingKongItemViewModel(KingKongChannelContract contract, BitmapSource iconSource)
        {
            _contract = contract;
            _iconSource = iconSource;
            _isSelected = false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public KingKongChannelContract Contract
        {
            get { return _contract; }
        }

        public int ChannelId
        {
            get { return _contract.ChannelId; }
        }

        public string Title
        {
            get { return _contract.Title; }
        }

        public string Subtitle
        {
            get { return _contract.Subtitle; }
        }

        public string BadgeText
        {
            get { return _contract.BadgeText; }
        }

        public bool HasBadge
        {
            get { return !string.IsNullOrEmpty(_contract.BadgeText); }
        }

        public string AccentHex
        {
            get { return _contract.AccentHex; }
        }

        public string IconBgHex
        {
            get { return _contract.IconBgHex; }
        }

        public string IconGlyph
        {
            get { return _contract.IconGlyph; }
        }

        public int CategoryBuzType
        {
            get { return _contract.CategoryBuzType; }
        }

        public BitmapSource IconSource
        {
            get { return _iconSource; }
            set
            {
                if (!ReferenceEquals(_iconSource, value))
                {
                    _iconSource = value;
                    RaisePropertyChanged("IconSource");
                    RaisePropertyChanged("HasBitmapIcon");
                }
            }
        }

        public bool HasBitmapIcon
        {
            get { return _iconSource != null; }
        }

        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    RaisePropertyChanged("IsSelected");
                }
            }
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    /// <summary>
    /// 美团外卖 Windows 1:1 原生手机竖屏重构版主视图模型。
    /// 严格对齐 APK v8.84.2 (build 91293) 35 项重建义务 (OBL-01 ~ OBL-35)：
    /// 1. 原生手机竖屏单栏比例 (412×892 dp) + 三层页面栈 (Root 5-Tab -> FullScreen Push Pages -> BottomSheet Modals)。
    /// 2. 美团通行证 1:1 登录系统 (com.meituan.passport.LoginActivity)：短信验证码/密码/扫码三模式、模拟短信通知条、协议拦截、未登录底栏悬浮引导、切换账号与退出登录。
    /// 3. 门店详情全屏页：点菜 / 评价(好中差与有图筛选) / 商家(食安A级档案与执照) 三子页真实切换与门店收藏。
    /// 4. 确认订单全屏页 (OrderConfirmActivity) + 美团收银台半屏支付 (MTCashierActivity) + 订单轨迹/骑手地图/三方IM + 评价晒单回流 + 地址簿增删改查。
    /// </summary>
    public sealed class WaimaiMainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly string _repoRootPath;
        private readonly ApkAssetLoader _assetLoader;
        private readonly DispatcherTimer _riderTrackingTimer;
        private bool _isDisposed;
        private bool _isWindowVisible;

        private BitmapSource _launcherIcon;
        private BitmapSource _mascotIcon;

        private List<PoiContract> _allPois;
        private ObservableCollection<DynamicTabContract> _bottomTabs;
        private ObservableCollection<KingKongItemViewModel> _kingKongItems;
        private ObservableCollection<string> _hotSearchKeywords;
        private ObservableCollection<PoiContract> _filteredPois;
        private ObservableCollection<PoiContract> _favoritePois;
        private ObservableCollection<string> _currentMenuCategories;
        private ObservableCollection<GoodsSpuContract> _filteredFoods;
        private ObservableCollection<PoiReviewItemContract> _filteredPoiReviews;
        private ObservableCollection<ShopCartItemContract> _activeCartItems;
        private ObservableCollection<ShenQuanCouponContract> _coupons;
        private ObservableCollection<PromoFlashDealContract> _flashDeals;
        private ObservableCollection<DeepEatRecommendationContract> _deepEatRecommendations;
        private ObservableCollection<OrderContract> _orders;
        private ObservableCollection<OrderContract> _filteredOrders;
        private ObservableCollection<WmAddressContract> _addresses;
        private ObservableCollection<PassportUserContract> _presetUsers;
        private ObservableCollection<CashierPaymentMethodContract> _paymentMethods;
        private ObservableCollection<NotificationMessageContract> _notifications;
        private ObservableCollection<SelfTestItem> _selfTestItems;

        // 导航与页面压栈状态
        private int _selectedTabBusinessId;
        private bool _isEasyGoDualPane;
        private bool _isMobileRestaurantDetailOpen;
        private string _activeFullScreenPage; // "None", "PoiDetail", "OrderConfirm", "OrderTrack", "AddressManager", "Favorites", "Messages"

        // 美团通行证登录状态 (OBL-02 ~ OBL-07)
        private PassportUserContract _currentUser;
        private bool _isLoginViewOpen;
        private string _loginMode; // "Sms", "Password", "QrCode"
        private string _loginPhoneInput;
        private string _loginSmsCodeInput;
        private string _generatedSmsCode;
        private string _simulatedSmsBannerText;
        private bool _hasSimulatedSmsBanner;
        private int _smsCountdownSeconds;
        private string _loginPasswordInput;
        private string _qrCodeTokenText;
        private string _qrCodeStatusText;
        private bool _isPrivacyAccepted;
        private bool _isPrivacyConfirmDialogOpen;
        private bool _pendingCheckoutAfterLogin;

        // 首页与门店详情状态 (OBL-08 ~ OBL-19)
        private string _searchKeyword;
        private int _selectedKingKongCategory;
        private string _selectedSortMode;
        private PoiContract _selectedPoi;
        private string _selectedMenuCategory;
        private string _selectedPoiSubTab; // "点菜", "评价", "商家"
        private string _selectedReviewFilter; // "全部", "好评", "有图", "差评"
        private int _selectedPocketId;
        private bool _isCartDrawerOpen;

        // 多规格弹窗状态 (OBL-18)
        private bool _isSpecModalOpen;
        private GoodsSpuContract _specModalFood;
        private GoodsSkuContract _specModalSelectedSku;

        // 购物车与确认订单页状态 (OBL-19 ~ OBL-22)
        private ShopCartPriceContract _cartPrice;
        private ShenQuanCouponContract _selectedCoupon;
        private bool _isSelfPickupMode;
        private string _orderNoteInput;
        private string _selectedTablewareOption;
        private bool _isPrivacyPhoneProtected;
        private bool _isCashierModalOpen;
        private int _cashierRemainingSeconds;
        private CashierPaymentMethodContract _selectedPaymentMethod;

        // 神券兑换状态 (OBL-23)
        private string _couponRedeemCodeInput;

        // 订单、轨迹、IM 与评价弹窗状态 (OBL-25 ~ OBL-30)
        private string _selectedOrderFilter; // "全部", "进行中", "待评价", "退款售后"
        private OrderContract _selectedOrder;
        private string _imDraftText;
        private bool _isRiderSimulationActive;
        private bool _isReviewOrderModalOpen;
        private int _reviewRatingStars;
        private string _reviewCommentInput;

        // 地址管理与新增/编辑表单状态 (OBL-32)
        private WmAddressContract _currentAddress;
        private string _editingAddressId;
        private string _editAddressContactName;
        private string _editAddressGender;
        private string _editAddressPhone;
        private string _editAddressDetail;
        private string _editAddressHouseNumber;
        private string _editAddressTag;

        // 个人中心与 AI 助手状态
        private MineProfileContract _mineProfile;
        private EasyGoMagicWindowContract _easyGoContract;
        private bool _isDeepEatDrawerOpen;
        private string _deepEatQueryText;
        private string _deepEatResponseText;

        // 状态通知栏与自检状态
        private string _statusBannerText;
        private string _selfTestSummaryText;

        public WaimaiMainViewModel(string repoRootPath, ApkAssetLoader assetLoader)
        {
            _repoRootPath = repoRootPath ?? AppDomain.CurrentDomain.BaseDirectory;
            _assetLoader = assetLoader;
            _isWindowVisible = true;

            _allPois = new List<PoiContract>();
            _bottomTabs = new ObservableCollection<DynamicTabContract>();
            _kingKongItems = new ObservableCollection<KingKongItemViewModel>();
            _hotSearchKeywords = new ObservableCollection<string>();
            _filteredPois = new ObservableCollection<PoiContract>();
            _favoritePois = new ObservableCollection<PoiContract>();
            _currentMenuCategories = new ObservableCollection<string>();
            _filteredFoods = new ObservableCollection<GoodsSpuContract>();
            _filteredPoiReviews = new ObservableCollection<PoiReviewItemContract>();
            _activeCartItems = new ObservableCollection<ShopCartItemContract>();
            _coupons = new ObservableCollection<ShenQuanCouponContract>();
            _flashDeals = new ObservableCollection<PromoFlashDealContract>();
            _deepEatRecommendations = new ObservableCollection<DeepEatRecommendationContract>();
            _orders = new ObservableCollection<OrderContract>();
            _filteredOrders = new ObservableCollection<OrderContract>();
            _addresses = new ObservableCollection<WmAddressContract>();
            _presetUsers = new ObservableCollection<PassportUserContract>();
            _paymentMethods = new ObservableCollection<CashierPaymentMethodContract>();
            _notifications = new ObservableCollection<NotificationMessageContract>();
            _selfTestItems = new ObservableCollection<SelfTestItem>();

            // 默认强制 1:1 手机竖屏单栏模式 (OBL-01)
            _selectedTabBusinessId = 1;
            _isEasyGoDualPane = false;
            _isMobileRestaurantDetailOpen = false;
            _activeFullScreenPage = "None";

            // 登录状态初始化：默认支持一键打开或未登录悬浮条体验
            _isLoginViewOpen = false;
            _loginMode = "Sms";
            _loginPhoneInput = "13800130001";
            _loginSmsCodeInput = string.Empty;
            _generatedSmsCode = "884291";
            _simulatedSmsBannerText = string.Empty;
            _hasSimulatedSmsBanner = false;
            _smsCountdownSeconds = 0;
            _loginPasswordInput = "mt884291";
            _qrCodeTokenText = "MT-QR-91293-8842";
            _qrCodeStatusText = "请使用手机【美团 / 美团外卖 APP】扫码授权";
            _isPrivacyAccepted = true;
            _isPrivacyConfirmDialogOpen = false;
            _pendingCheckoutAfterLogin = false;

            _searchKeyword = string.Empty;
            _selectedKingKongCategory = 0;
            _selectedSortMode = "综合排序";
            _selectedMenuCategory = "全部菜单";
            _selectedPoiSubTab = "点菜";
            _selectedReviewFilter = "全部";
            _selectedPocketId = 1;
            _isCartDrawerOpen = false;
            _isSpecModalOpen = false;

            _isSelfPickupMode = false;
            _orderNoteInput = "请放门口外卖柜，谢谢骑手师傅";
            _selectedTablewareOption = "无需餐具 (得16g绿色能量)";
            _isPrivacyPhoneProtected = true;
            _isCashierModalOpen = false;
            _cashierRemainingSeconds = 900;

            _couponRedeemCodeInput = "MT888";
            _selectedOrderFilter = "全部";
            _imDraftText = string.Empty;
            _isReviewOrderModalOpen = false;
            _reviewRatingStars = 5;
            _reviewCommentInput = "味道超赞，分量很足，骑手送达也很准时！";

            _editingAddressId = string.Empty;
            _editAddressContactName = "王先生";
            _editAddressGender = "先生";
            _editAddressPhone = "13688886666";
            _editAddressDetail = "深圳市南山区粤海街道深圳湾科技生态园";
            _editAddressHouseNumber = "10栋B座1608室";
            _editAddressTag = "公司";

            _isDeepEatDrawerOpen = false;
            _deepEatQueryText = "今晚加班想吃点热乎又不胖的晚餐，预算35元以内";
            _deepEatResponseText = "小美 DeepEat AI 推荐：已为您匹配「巷口小馆·家常菜」招牌黄焖鸡米饭套餐，搭配膨胀神券立减 ¥10，预计 28 分钟送达！";
            _statusBannerText = string.Format(
                "美团外卖 v{0} (Build {1}) · 1:1 原生手机竖屏已就绪",
                ReverseEvidenceManifest.VersionName,
                ReverseEvidenceManifest.BuildNumber);
            _selfTestSummaryText = "契约自检准备就绪";

            InitializeCommands();
            LoadSeedDataSynchronously();

            // 严格遵守 wpf-dotnet-performance：使用 Background 优先级且支持窗口生命周期暂停/恢复
            _riderTrackingTimer = new DispatcherTimer(DispatcherPriority.Background);
            _riderTrackingTimer.Interval = TimeSpan.FromSeconds(1.8);
            _riderTrackingTimer.Tick += OnRiderTrackingTimerTick;
            _isRiderSimulationActive = true;
            _riderTrackingTimer.Start();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #region 公开属性（绑定至 XAML）

        public string AppVersionBadge
        {
            get
            {
                return string.Format("v{0} (Build {1})", ReverseEvidenceManifest.VersionName, ReverseEvidenceManifest.BuildNumber);
            }
        }

        public string PackageIdentityText
        {
            get
            {
                return string.Format("{0} · SHA256: {1}...",
                    ReverseEvidenceManifest.PackageName,
                    ReverseEvidenceManifest.ApkSha256.Substring(0, 16));
            }
        }

        public BitmapSource LauncherIcon
        {
            get { return _launcherIcon; }
            private set
            {
                if (!ReferenceEquals(_launcherIcon, value))
                {
                    _launcherIcon = value;
                    RaisePropertyChanged("LauncherIcon");
                    RaisePropertyChanged("HasLauncherIcon");
                }
            }
        }

        public bool HasLauncherIcon
        {
            get { return _launcherIcon != null; }
        }

        public BitmapSource MascotIcon
        {
            get { return _mascotIcon; }
            private set
            {
                if (!ReferenceEquals(_mascotIcon, value))
                {
                    _mascotIcon = value;
                    RaisePropertyChanged("MascotIcon");
                    RaisePropertyChanged("HasMascotIcon");
                }
            }
        }

        public bool HasMascotIcon
        {
            get { return _mascotIcon != null; }
        }

        public ObservableCollection<DynamicTabContract> BottomTabs
        {
            get { return _bottomTabs; }
        }

        public ObservableCollection<KingKongItemViewModel> KingKongItems
        {
            get { return _kingKongItems; }
        }

        public ObservableCollection<string> HotSearchKeywords
        {
            get { return _hotSearchKeywords; }
        }

        public ObservableCollection<PoiContract> FilteredPois
        {
            get { return _filteredPois; }
        }

        public ObservableCollection<PoiContract> FavoritePois
        {
            get { return _favoritePois; }
        }

        public ObservableCollection<string> CurrentMenuCategories
        {
            get { return _currentMenuCategories; }
        }

        public ObservableCollection<GoodsSpuContract> FilteredFoods
        {
            get { return _filteredFoods; }
        }

        public ObservableCollection<PoiReviewItemContract> FilteredPoiReviews
        {
            get { return _filteredPoiReviews; }
        }

        public ObservableCollection<ShopCartItemContract> ActiveCartItems
        {
            get { return _activeCartItems; }
        }

        public ObservableCollection<ShenQuanCouponContract> Coupons
        {
            get { return _coupons; }
        }

        public ObservableCollection<PromoFlashDealContract> FlashDeals
        {
            get { return _flashDeals; }
        }

        public ObservableCollection<DeepEatRecommendationContract> DeepEatRecommendations
        {
            get { return _deepEatRecommendations; }
        }

        public ObservableCollection<OrderContract> Orders
        {
            get { return _orders; }
        }

        public ObservableCollection<OrderContract> FilteredOrders
        {
            get { return _filteredOrders; }
        }

        public ObservableCollection<WmAddressContract> Addresses
        {
            get { return _addresses; }
        }

        public ObservableCollection<PassportUserContract> PresetUsers
        {
            get { return _presetUsers; }
        }

        public ObservableCollection<CashierPaymentMethodContract> PaymentMethods
        {
            get { return _paymentMethods; }
        }

        public ObservableCollection<NotificationMessageContract> Notifications
        {
            get { return _notifications; }
        }

        public ObservableCollection<SelfTestItem> SelfTestItems
        {
            get { return _selfTestItems; }
        }

        // ==================== 美团通行证登录属性 (OBL-02 ~ OBL-07) ====================

        public PassportUserContract CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                SyncMineProfileWithCurrentUser();
                RaisePropertyChanged("CurrentUser");
                RaisePropertyChanged("IsUserLoggedIn");
                RaisePropertyChanged("IsGuestFloatingBarVisible");
                RaisePropertyChanged("TopLoginPillText");
                RaisePropertyChanged("MineHeaderUserTitle");
                RaisePropertyChanged("MineHeaderUserSubtitle");
                RaisePropertyChanged("MineProfile");
            }
        }

        public bool IsUserLoggedIn
        {
            get { return _currentUser != null && _currentUser.IsLoggedIn; }
        }

        public bool IsGuestFloatingBarVisible
        {
            get { return !IsUserLoggedIn && !_isLoginViewOpen && IsRootTabLayerVisible; }
        }

        public string TopLoginPillText
        {
            get
            {
                return IsUserLoggedIn
                    ? string.Format("{0} {1}", _currentUser.AvatarEmoji ?? "🦘", _currentUser.Nickname)
                    : "🔑 立即登录";
            }
        }

        public string MineHeaderUserTitle
        {
            get
            {
                return IsUserLoggedIn ? _currentUser.Nickname : "点击登录 / 注册美团账号";
            }
        }

        public string MineHeaderUserSubtitle
        {
            get
            {
                return IsUserLoggedIn
                    ? string.Format("{0} · 手机号 {1}", _currentUser.VipBadgeText, _currentUser.MobileMasked)
                    : "登录后同步神券红包、历史订单与收货地址";
            }
        }

        public bool IsLoginViewOpen
        {
            get { return _isLoginViewOpen; }
            set
            {
                if (_isLoginViewOpen != value)
                {
                    _isLoginViewOpen = value;
                    RaisePropertyChanged("IsLoginViewOpen");
                    RaisePropertyChanged("IsGuestFloatingBarVisible");
                }
            }
        }

        public string LoginMode
        {
            get { return _loginMode; }
            set
            {
                string next = value ?? "Sms";
                if (!string.Equals(_loginMode, next, StringComparison.Ordinal))
                {
                    _loginMode = next;
                    RaisePropertyChanged("LoginMode");
                    RaisePropertyChanged("IsSmsLoginMode");
                    RaisePropertyChanged("IsPasswordLoginMode");
                    RaisePropertyChanged("IsQrCodeLoginMode");
                }
            }
        }

        public bool IsSmsLoginMode
        {
            get { return string.Equals(_loginMode, "Sms", StringComparison.Ordinal); }
        }

        public bool IsPasswordLoginMode
        {
            get { return string.Equals(_loginMode, "Password", StringComparison.Ordinal); }
        }

        public bool IsQrCodeLoginMode
        {
            get { return string.Equals(_loginMode, "QrCode", StringComparison.Ordinal); }
        }

        public string LoginPhoneInput
        {
            get { return _loginPhoneInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_loginPhoneInput, next, StringComparison.Ordinal))
                {
                    _loginPhoneInput = next;
                    RaisePropertyChanged("LoginPhoneInput");
                }
            }
        }

        public string LoginSmsCodeInput
        {
            get { return _loginSmsCodeInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_loginSmsCodeInput, next, StringComparison.Ordinal))
                {
                    _loginSmsCodeInput = next;
                    RaisePropertyChanged("LoginSmsCodeInput");
                }
            }
        }

        public string SimulatedSmsBannerText
        {
            get { return _simulatedSmsBannerText; }
            set
            {
                _simulatedSmsBannerText = value ?? string.Empty;
                RaisePropertyChanged("SimulatedSmsBannerText");
            }
        }

        public bool HasSimulatedSmsBanner
        {
            get { return _hasSimulatedSmsBanner; }
            set
            {
                if (_hasSimulatedSmsBanner != value)
                {
                    _hasSimulatedSmsBanner = value;
                    RaisePropertyChanged("HasSimulatedSmsBanner");
                }
            }
        }

        public string SmsSendButtonText
        {
            get
            {
                return _smsCountdownSeconds > 0
                    ? string.Format("重新获取({0}s)", _smsCountdownSeconds)
                    : "获取验证码";
            }
        }

        public string LoginPasswordInput
        {
            get { return _loginPasswordInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_loginPasswordInput, next, StringComparison.Ordinal))
                {
                    _loginPasswordInput = next;
                    RaisePropertyChanged("LoginPasswordInput");
                }
            }
        }

        public string QrCodeTokenText
        {
            get { return _qrCodeTokenText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_qrCodeTokenText, next, StringComparison.Ordinal))
                {
                    _qrCodeTokenText = next;
                    RaisePropertyChanged("QrCodeTokenText");
                }
            }
        }

        public string QrCodeStatusText
        {
            get { return _qrCodeStatusText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_qrCodeStatusText, next, StringComparison.Ordinal))
                {
                    _qrCodeStatusText = next;
                    RaisePropertyChanged("QrCodeStatusText");
                }
            }
        }

        public bool IsPrivacyAccepted
        {
            get { return _isPrivacyAccepted; }
            set
            {
                if (_isPrivacyAccepted != value)
                {
                    _isPrivacyAccepted = value;
                    RaisePropertyChanged("IsPrivacyAccepted");
                }
            }
        }

        public bool IsPrivacyConfirmDialogOpen
        {
            get { return _isPrivacyConfirmDialogOpen; }
            set
            {
                if (_isPrivacyConfirmDialogOpen != value)
                {
                    _isPrivacyConfirmDialogOpen = value;
                    RaisePropertyChanged("IsPrivacyConfirmDialogOpen");
                }
            }
        }

        // ==================== 一级 Tab 与二级全屏页面栈状态 ====================

        public int SelectedTabBusinessId
        {
            get { return _selectedTabBusinessId; }
            set
            {
                if (_selectedTabBusinessId != value)
                {
                    _selectedTabBusinessId = value;
                    for (int i = 0; i < _bottomTabs.Count; i++)
                    {
                        _bottomTabs[i].IsSelected = (_bottomTabs[i].BusinessId == value);
                    }
                    // 切换底部主 Tab 时自动回到一级根视图
                    ActiveFullScreenPage = "None";
                    RaisePropertyChanged("SelectedTabBusinessId");
                    RaisePropertyChanged("IsHomeTabActive");
                    RaisePropertyChanged("IsShenQuanTabActive");
                    RaisePropertyChanged("IsFlashSaleTabActive");
                    RaisePropertyChanged("IsOrderTabActive");
                    RaisePropertyChanged("IsMineTabActive");
                    RaisePropertyChanged("BottomTabs");
                }
            }
        }

        public string ActiveFullScreenPage
        {
            get { return _activeFullScreenPage; }
            set
            {
                string next = string.IsNullOrEmpty(value) ? "None" : value;
                if (!string.Equals(_activeFullScreenPage, next, StringComparison.Ordinal))
                {
                    _activeFullScreenPage = next;
                    _isMobileRestaurantDetailOpen = string.Equals(next, "PoiDetail", StringComparison.Ordinal);
                    RaisePropertyChanged("ActiveFullScreenPage");
                    RaisePropertyChanged("IsRootTabLayerVisible");
                    RaisePropertyChanged("IsPoiDetailPageOpen");
                    RaisePropertyChanged("IsOrderConfirmPageOpen");
                    RaisePropertyChanged("IsOrderTrackPageOpen");
                    RaisePropertyChanged("IsAddressManagerPageOpen");
                    RaisePropertyChanged("IsFavoritesPageOpen");
                    RaisePropertyChanged("IsMessagesPageOpen");
                    RaisePropertyChanged("IsMobileRestaurantDetailOpen");
                    RaisePropertyChanged("IsHomeLeftFeedVisible");
                    RaisePropertyChanged("IsHomeRightDetailVisible");
                    RaisePropertyChanged("IsMobileBackVisible");
                    RaisePropertyChanged("IsGuestFloatingBarVisible");
                }
            }
        }

        public bool IsRootTabLayerVisible
        {
            get { return string.Equals(_activeFullScreenPage, "None", StringComparison.Ordinal); }
        }

        public bool IsPoiDetailPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "PoiDetail", StringComparison.Ordinal); }
        }

        public bool IsOrderConfirmPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "OrderConfirm", StringComparison.Ordinal); }
        }

        public bool IsOrderTrackPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "OrderTrack", StringComparison.Ordinal); }
        }

        public bool IsAddressManagerPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "AddressManager", StringComparison.Ordinal); }
        }

        public bool IsFavoritesPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "Favorites", StringComparison.Ordinal); }
        }

        public bool IsMessagesPageOpen
        {
            get { return string.Equals(_activeFullScreenPage, "Messages", StringComparison.Ordinal); }
        }

        public bool IsHomeTabActive
        {
            get { return _selectedTabBusinessId == 1; }
        }

        public bool IsShenQuanTabActive
        {
            get { return _selectedTabBusinessId == 9; }
        }

        public bool IsFlashSaleTabActive
        {
            get { return _selectedTabBusinessId == 10; }
        }

        public bool IsOrderTabActive
        {
            get { return _selectedTabBusinessId == 2; }
        }

        public bool IsMineTabActive
        {
            get { return _selectedTabBusinessId == 3; }
        }

        public bool IsEasyGoDualPane
        {
            get { return _isEasyGoDualPane; }
            set
            {
                if (_isEasyGoDualPane != value)
                {
                    _isEasyGoDualPane = value;
                    if (_isEasyGoDualPane)
                    {
                        _isMobileRestaurantDetailOpen = false;
                    }
                    RaisePropertyChanged("IsEasyGoDualPane");
                    RaisePropertyChanged("LayoutModeButtonText");
                    RaisePropertyChanged("IsHomeLeftFeedVisible");
                    RaisePropertyChanged("IsHomeRightDetailVisible");
                    RaisePropertyChanged("IsMobileBackVisible");
                }
            }
        }

        public string LayoutModeButtonText
        {
            get
            {
                return _isEasyGoDualPane
                    ? "EasyGo 平行视界"
                    : "手机 1:1 竖屏";
            }
        }

        public bool IsMobileRestaurantDetailOpen
        {
            get { return _isMobileRestaurantDetailOpen; }
            set
            {
                if (_isMobileRestaurantDetailOpen != value)
                {
                    _isMobileRestaurantDetailOpen = value;
                    if (value)
                    {
                        ActiveFullScreenPage = "PoiDetail";
                    }
                    else if (string.Equals(_activeFullScreenPage, "PoiDetail", StringComparison.Ordinal))
                    {
                        ActiveFullScreenPage = "None";
                    }
                    RaisePropertyChanged("IsMobileRestaurantDetailOpen");
                    RaisePropertyChanged("IsHomeLeftFeedVisible");
                    RaisePropertyChanged("IsHomeRightDetailVisible");
                    RaisePropertyChanged("IsMobileBackVisible");
                }
            }
        }

        public bool IsHomeLeftFeedVisible
        {
            get { return _isEasyGoDualPane || !_isMobileRestaurantDetailOpen; }
        }

        public bool IsHomeRightDetailVisible
        {
            get { return _isEasyGoDualPane || _isMobileRestaurantDetailOpen; }
        }

        public bool IsMobileBackVisible
        {
            get { return !_isEasyGoDualPane && !IsRootTabLayerVisible; }
        }

        public string SearchKeyword
        {
            get { return _searchKeyword; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_searchKeyword, next, StringComparison.Ordinal))
                {
                    _searchKeyword = next;
                    RaisePropertyChanged("SearchKeyword");
                    RaisePropertyChanged("HasActiveCategoryFilter");
                    RefreshFilteredPois();
                }
            }
        }

        public int SelectedKingKongCategory
        {
            get { return _selectedKingKongCategory; }
            set
            {
                if (_selectedKingKongCategory != value)
                {
                    _selectedKingKongCategory = value;
                    for (int i = 0; i < _kingKongItems.Count; i++)
                    {
                        _kingKongItems[i].IsSelected = (_kingKongItems[i].CategoryBuzType == value && value != 0);
                    }
                    RaisePropertyChanged("SelectedKingKongCategory");
                    RaisePropertyChanged("HasActiveCategoryFilter");
                    RefreshFilteredPois();
                }
            }
        }

        public bool HasActiveCategoryFilter
        {
            get { return _selectedKingKongCategory != 0 || !string.IsNullOrEmpty(_searchKeyword); }
        }

        public string SelectedSortMode
        {
            get { return _selectedSortMode; }
            set
            {
                string next = value ?? "综合排序";
                if (!string.Equals(_selectedSortMode, next, StringComparison.Ordinal))
                {
                    _selectedSortMode = next;
                    RaisePropertyChanged("SelectedSortMode");
                    RefreshFilteredPois();
                }
            }
        }

        public PoiContract SelectedPoi
        {
            get { return _selectedPoi; }
            set
            {
                if (!ReferenceEquals(_selectedPoi, value))
                {
                    _selectedPoi = value;
                    RaisePropertyChanged("SelectedPoi");
                    RaisePropertyChanged("SelectedPoiFavoriteText");
                    RaisePropertyChanged("SelectedPoiReviewCountText");
                    OnSelectedPoiChanged();
                }
            }
        }

        public string SelectedPoiFavoriteText
        {
            get
            {
                return (_selectedPoi != null && _selectedPoi.IsFavorite) ? "❤ 已收藏" : "♡ 收藏";
            }
        }

        public string SelectedPoiReviewCountText
        {
            get
            {
                int count = (_selectedPoi != null && _selectedPoi.Reviews != null) ? _selectedPoi.Reviews.Count : 0;
                return string.Format("评价({0})", count);
            }
        }

        public string SelectedMenuCategory
        {
            get { return _selectedMenuCategory; }
            set
            {
                string next = value ?? "全部菜单";
                if (!string.Equals(_selectedMenuCategory, next, StringComparison.Ordinal))
                {
                    _selectedMenuCategory = next;
                    RaisePropertyChanged("SelectedMenuCategory");
                    RefreshFilteredFoods();
                }
            }
        }

        public string SelectedPoiSubTab
        {
            get { return _selectedPoiSubTab; }
            set
            {
                string next = value ?? "点菜";
                if (!string.Equals(_selectedPoiSubTab, next, StringComparison.Ordinal))
                {
                    _selectedPoiSubTab = next;
                    RaisePropertyChanged("SelectedPoiSubTab");
                    RaisePropertyChanged("IsPoiMenuSubTab");
                    RaisePropertyChanged("IsPoiReviewSubTab");
                    RaisePropertyChanged("IsPoiMerchantSubTab");
                    RefreshFilteredPoiReviews();
                }
            }
        }

        public bool IsPoiMenuSubTab
        {
            get { return string.Equals(_selectedPoiSubTab, "点菜", StringComparison.Ordinal); }
        }

        public bool IsPoiReviewSubTab
        {
            get { return string.Equals(_selectedPoiSubTab, "评价", StringComparison.Ordinal); }
        }

        public bool IsPoiMerchantSubTab
        {
            get { return string.Equals(_selectedPoiSubTab, "商家", StringComparison.Ordinal); }
        }

        public string SelectedReviewFilter
        {
            get { return _selectedReviewFilter; }
            set
            {
                string next = value ?? "全部";
                if (!string.Equals(_selectedReviewFilter, next, StringComparison.Ordinal))
                {
                    _selectedReviewFilter = next;
                    RaisePropertyChanged("SelectedReviewFilter");
                    RefreshFilteredPoiReviews();
                }
            }
        }

        public int SelectedPocketId
        {
            get { return _selectedPocketId; }
            set
            {
                int normalized = value == 2 ? 2 : 1;
                if (_selectedPocketId != normalized)
                {
                    _selectedPocketId = normalized;
                    RaisePropertyChanged("SelectedPocketId");
                    RaisePropertyChanged("ActivePocketLabel");
                }
            }
        }

        public string ActivePocketLabel
        {
            get { return string.Format("{0}号口袋", _selectedPocketId); }
        }

        public bool IsCartDrawerOpen
        {
            get { return _isCartDrawerOpen; }
            set
            {
                if (_isCartDrawerOpen != value)
                {
                    _isCartDrawerOpen = value;
                    RaisePropertyChanged("IsCartDrawerOpen");
                }
            }
        }

        public bool IsSpecModalOpen
        {
            get { return _isSpecModalOpen; }
            set
            {
                if (_isSpecModalOpen != value)
                {
                    _isSpecModalOpen = value;
                    RaisePropertyChanged("IsSpecModalOpen");
                }
            }
        }

        public GoodsSpuContract SpecModalFood
        {
            get { return _specModalFood; }
            private set
            {
                if (!ReferenceEquals(_specModalFood, value))
                {
                    _specModalFood = value;
                    RaisePropertyChanged("SpecModalFood");
                }
            }
        }

        public GoodsSkuContract SpecModalSelectedSku
        {
            get { return _specModalSelectedSku; }
            set
            {
                if (!ReferenceEquals(_specModalSelectedSku, value))
                {
                    _specModalSelectedSku = value;
                    RaisePropertyChanged("SpecModalSelectedSku");
                    RaisePropertyChanged("SpecModalPreviewText");
                }
            }
        }

        public string SpecModalPreviewText
        {
            get
            {
                if (_specModalFood == null)
                {
                    return string.Empty;
                }
                double price = _specModalSelectedSku != null ? _specModalSelectedSku.Price : _specModalFood.MinPrice;
                string skuName = _specModalSelectedSku != null ? _specModalSelectedSku.SpecName : "标准规格";
                List<string> attrs = new List<string>();
                if (_specModalFood.AttrGroups != null)
                {
                    for (int i = 0; i < _specModalFood.AttrGroups.Count; i++)
                    {
                        GoodsAttrGroupContract group = _specModalFood.AttrGroups[i];
                        if (!string.IsNullOrEmpty(group.SelectedOption))
                        {
                            attrs.Add(group.SelectedOption);
                        }
                    }
                }
                string attrPart = attrs.Count > 0 ? (" / " + string.Join(" · ", attrs.ToArray())) : string.Empty;
                return string.Format("已选：{0}{1}  |  ¥{2:0.00} ({3}号口袋)", skuName, attrPart, price, _selectedPocketId);
            }
        }

        public ShopCartPriceContract CartPrice
        {
            get { return _cartPrice; }
            private set
            {
                _cartPrice = value;
                RaisePropertyChanged("CartPrice");
                RaisePropertyChanged("HasCartItems");
                RaisePropertyChanged("OrderConfirmFinalPayable");
                RaisePropertyChanged("CashierFinalPayable");
            }
        }

        public bool HasCartItems
        {
            get { return _cartPrice != null && _cartPrice.TotalItemCount > 0; }
        }

        public ShenQuanCouponContract SelectedCoupon
        {
            get { return _selectedCoupon; }
            set
            {
                if (!ReferenceEquals(_selectedCoupon, value))
                {
                    _selectedCoupon = value;
                    RaisePropertyChanged("SelectedCoupon");
                    RaisePropertyChanged("SelectedCouponSummary");
                    RecalculateCartPrice();
                }
            }
        }

        public string SelectedCouponSummary
        {
            get
            {
                if (_selectedCoupon == null || _selectedCoupon.IsUsed)
                {
                    return "暂无可用神券";
                }
                return string.Format("{0}: 满¥{1:0}减¥{2:0.##}{3}",
                    _selectedCoupon.Title,
                    _selectedCoupon.Threshold,
                    _selectedCoupon.Amount,
                    _selectedCoupon.IsInflated ? " (已膨胀)" : " (可膨胀)");
            }
        }

        // ==================== 确认订单页与美团收银台属性 (OBL-20 ~ OBL-22) ====================

        public bool IsSelfPickupMode
        {
            get { return _isSelfPickupMode; }
            set
            {
                if (_isSelfPickupMode != value)
                {
                    _isSelfPickupMode = value;
                    RaisePropertyChanged("IsSelfPickupMode");
                    RaisePropertyChanged("IsDeliveryMode");
                    RaisePropertyChanged("OrderConfirmFinalPayable");
                    RaisePropertyChanged("CashierFinalPayable");
                }
            }
        }

        public bool IsDeliveryMode
        {
            get { return !_isSelfPickupMode; }
        }

        public double OrderConfirmFinalPayable
        {
            get
            {
                if (_cartPrice == null)
                {
                    return 0.0;
                }
                double shipping = _isSelfPickupMode ? 0.0 : _cartPrice.FinalShippingFee;
                return Math.Max(0.01, Math.Round(_cartPrice.TotalDiscountedAndBoxPrice + shipping, 2));
            }
        }

        public double CashierFinalPayable
        {
            get
            {
                double basePay = OrderConfirmFinalPayable;
                double channelDiscount = (_selectedPaymentMethod != null) ? _selectedPaymentMethod.InstantDiscount : 0.0;
                return Math.Max(0.01, Math.Round(basePay - channelDiscount, 2));
            }
        }

        public string OrderNoteInput
        {
            get { return _orderNoteInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_orderNoteInput, next, StringComparison.Ordinal))
                {
                    _orderNoteInput = next;
                    RaisePropertyChanged("OrderNoteInput");
                }
            }
        }

        public string SelectedTablewareOption
        {
            get { return _selectedTablewareOption; }
            set
            {
                string next = value ?? "1份餐具";
                if (!string.Equals(_selectedTablewareOption, next, StringComparison.Ordinal))
                {
                    _selectedTablewareOption = next;
                    RaisePropertyChanged("SelectedTablewareOption");
                }
            }
        }

        public bool IsPrivacyPhoneProtected
        {
            get { return _isPrivacyPhoneProtected; }
            set
            {
                if (_isPrivacyPhoneProtected != value)
                {
                    _isPrivacyPhoneProtected = value;
                    RaisePropertyChanged("IsPrivacyPhoneProtected");
                }
            }
        }

        public bool IsCashierModalOpen
        {
            get { return _isCashierModalOpen; }
            set
            {
                if (_isCashierModalOpen != value)
                {
                    _isCashierModalOpen = value;
                    if (value)
                    {
                        _cashierRemainingSeconds = 900;
                        RaisePropertyChanged("CashierCountdownText");
                    }
                    RaisePropertyChanged("IsCashierModalOpen");
                    RaisePropertyChanged("CashierFinalPayable");
                }
            }
        }

        public string CashierCountdownText
        {
            get
            {
                int sec = Math.Max(0, _cashierRemainingSeconds);
                int mins = sec / 60;
                int rem = sec % 60;
                return string.Format("支付剩余时间 {0:00}:{1:00}", mins, rem);
            }
        }

        public void AdvanceCashierCountdown(int deltaSeconds)
        {
            if (deltaSeconds <= 0)
            {
                return;
            }
            _cashierRemainingSeconds = Math.Max(0, _cashierRemainingSeconds - deltaSeconds);
            RaisePropertyChanged("CashierCountdownText");
        }

        public CashierPaymentMethodContract SelectedPaymentMethod
        {
            get { return _selectedPaymentMethod; }
            set
            {
                if (!ReferenceEquals(_selectedPaymentMethod, value))
                {
                    _selectedPaymentMethod = value;
                    for (int i = 0; i < _paymentMethods.Count; i++)
                    {
                        _paymentMethods[i].IsSelected = ReferenceEquals(_paymentMethods[i], value);
                    }
                    RaisePropertyChanged("SelectedPaymentMethod");
                    RaisePropertyChanged("PaymentMethods");
                    RaisePropertyChanged("CashierFinalPayable");
                }
            }
        }

        public string CouponRedeemCodeInput
        {
            get { return _couponRedeemCodeInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_couponRedeemCodeInput, next, StringComparison.Ordinal))
                {
                    _couponRedeemCodeInput = next;
                    RaisePropertyChanged("CouponRedeemCodeInput");
                }
            }
        }

        // ==================== 订单中心、轨迹与评价晒单属性 (OBL-25 ~ OBL-30) ====================

        public string SelectedOrderFilter
        {
            get { return _selectedOrderFilter; }
            set
            {
                string next = value ?? "全部";
                if (!string.Equals(_selectedOrderFilter, next, StringComparison.Ordinal))
                {
                    _selectedOrderFilter = next;
                    RaisePropertyChanged("SelectedOrderFilter");
                    RefreshFilteredOrders();
                }
            }
        }

        public OrderContract SelectedOrder
        {
            get { return _selectedOrder; }
            set
            {
                if (!ReferenceEquals(_selectedOrder, value))
                {
                    _selectedOrder = value;
                    RaisePropertyChanged("SelectedOrder");
                    RaisePropertyChanged("SelectedOrderProgressPercent");
                    RaisePropertyChanged("SelectedOrderCoordinatesText");
                }
            }
        }

        public int SelectedOrderProgressPercent
        {
            get
            {
                if (_selectedOrder == null)
                {
                    return 0;
                }
                return (int)Math.Round(_selectedOrder.RiderProgressRatio * 100.0);
            }
        }

        public string SelectedOrderCoordinatesText
        {
            get
            {
                if (_selectedOrder == null)
                {
                    return "E 113.9512°  N 22.5391°";
                }
                double lat = _selectedOrder.Latitude / 1000000.0;
                double lng = _selectedOrder.Longitude / 1000000.0;
                return string.Format("实时美团专送GPS：N {0:0.0000}° · E {1:0.0000}° (进度 {2}%)",
                    lat, lng, SelectedOrderProgressPercent);
            }
        }

        public string ImDraftText
        {
            get { return _imDraftText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_imDraftText, next, StringComparison.Ordinal))
                {
                    _imDraftText = next;
                    RaisePropertyChanged("ImDraftText");
                }
            }
        }

        public bool IsReviewOrderModalOpen
        {
            get { return _isReviewOrderModalOpen; }
            set
            {
                if (_isReviewOrderModalOpen != value)
                {
                    _isReviewOrderModalOpen = value;
                    RaisePropertyChanged("IsReviewOrderModalOpen");
                }
            }
        }

        public int ReviewRatingStars
        {
            get { return _reviewRatingStars; }
            set
            {
                int clamped = Math.Max(1, Math.Min(5, value));
                if (_reviewRatingStars != clamped)
                {
                    _reviewRatingStars = clamped;
                    RaisePropertyChanged("ReviewRatingStars");
                }
            }
        }

        public string ReviewCommentInput
        {
            get { return _reviewCommentInput; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_reviewCommentInput, next, StringComparison.Ordinal))
                {
                    _reviewCommentInput = next;
                    RaisePropertyChanged("ReviewCommentInput");
                }
            }
        }

        // ==================== 收货地址与消息中心属性 (OBL-32 ~ OBL-33) ====================

        public WmAddressContract CurrentAddress
        {
            get { return _currentAddress; }
            set
            {
                if (!ReferenceEquals(_currentAddress, value))
                {
                    _currentAddress = value;
                    if (_currentAddress != null)
                    {
                        for (int i = 0; i < _addresses.Count; i++)
                        {
                            _addresses[i].IsDefault = string.Equals(_addresses[i].AddressId, _currentAddress.AddressId, StringComparison.Ordinal);
                        }
                    }
                    RaisePropertyChanged("CurrentAddress");
                    RaisePropertyChanged("CurrentAddressDisplayText");
                    RaisePropertyChanged("Addresses");
                }
            }
        }

        public string CurrentAddressDisplayText
        {
            get
            {
                if (_currentAddress == null)
                {
                    return "深圳市南山区科技园·科苑花园";
                }
                return string.Format("{0} ({1})", _currentAddress.AddressDetail, _currentAddress.TagLabel);
            }
        }

        public string EditAddressContactName
        {
            get { return _editAddressContactName; }
            set { _editAddressContactName = value ?? string.Empty; RaisePropertyChanged("EditAddressContactName"); }
        }

        public string EditAddressGender
        {
            get { return _editAddressGender; }
            set { _editAddressGender = value ?? "先生"; RaisePropertyChanged("EditAddressGender"); }
        }

        public string EditAddressPhone
        {
            get { return _editAddressPhone; }
            set { _editAddressPhone = value ?? string.Empty; RaisePropertyChanged("EditAddressPhone"); }
        }

        public string EditAddressDetail
        {
            get { return _editAddressDetail; }
            set { _editAddressDetail = value ?? string.Empty; RaisePropertyChanged("EditAddressDetail"); }
        }

        public string EditAddressHouseNumber
        {
            get { return _editAddressHouseNumber; }
            set { _editAddressHouseNumber = value ?? string.Empty; RaisePropertyChanged("EditAddressHouseNumber"); }
        }

        public string EditAddressTag
        {
            get { return _editAddressTag; }
            set { _editAddressTag = value ?? "家"; RaisePropertyChanged("EditAddressTag"); }
        }

        public string EditingAddressId
        {
            get { return _editingAddressId; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_editingAddressId, next, StringComparison.Ordinal))
                {
                    _editingAddressId = next;
                    RaisePropertyChanged("EditingAddressId");
                    RaisePropertyChanged("IsEditingExistingAddress");
                    RaisePropertyChanged("AddressFormTitleText");
                    RaisePropertyChanged("AddressFormSubmitText");
                }
            }
        }

        public bool IsEditingExistingAddress
        {
            get { return !string.IsNullOrEmpty(_editingAddressId); }
        }

        public string AddressFormTitleText
        {
            get { return IsEditingExistingAddress ? "✏️ 编辑收货地址" : "➕ 新增收货地址"; }
        }

        public string AddressFormSubmitText
        {
            get { return IsEditingExistingAddress ? "保存修改的收货地址" : "保存并使用该收货地址"; }
        }

        public int UnreadNotificationCount
        {
            get
            {
                return _notifications.Count(delegate(NotificationMessageContract m) { return m.IsUnread; });
            }
        }

        public bool HasUnreadNotifications
        {
            get { return UnreadNotificationCount > 0; }
        }

        public MineProfileContract MineProfile
        {
            get { return _mineProfile; }
        }

        public EasyGoMagicWindowContract EasyGoContract
        {
            get { return _easyGoContract; }
        }

        public bool IsDeepEatDrawerOpen
        {
            get { return _isDeepEatDrawerOpen; }
            set
            {
                if (_isDeepEatDrawerOpen != value)
                {
                    _isDeepEatDrawerOpen = value;
                    RaisePropertyChanged("IsDeepEatDrawerOpen");
                }
            }
        }

        public string DeepEatQueryText
        {
            get { return _deepEatQueryText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_deepEatQueryText, next, StringComparison.Ordinal))
                {
                    _deepEatQueryText = next;
                    RaisePropertyChanged("DeepEatQueryText");
                }
            }
        }

        public string DeepEatResponseText
        {
            get { return _deepEatResponseText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_deepEatResponseText, next, StringComparison.Ordinal))
                {
                    _deepEatResponseText = next;
                    RaisePropertyChanged("DeepEatResponseText");
                }
            }
        }

        public string StatusBannerText
        {
            get { return _statusBannerText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_statusBannerText, next, StringComparison.Ordinal))
                {
                    _statusBannerText = next;
                    RaisePropertyChanged("StatusBannerText");
                }
            }
        }

        public string SelfTestSummaryText
        {
            get { return _selfTestSummaryText; }
            set
            {
                string next = value ?? string.Empty;
                if (!string.Equals(_selfTestSummaryText, next, StringComparison.Ordinal))
                {
                    _selfTestSummaryText = next;
                    RaisePropertyChanged("SelfTestSummaryText");
                }
            }
        }

        #endregion

        #region 命令定义

        // 导航与通用返回命令
        public ICommand SwitchTabCommand { get; private set; }
        public ICommand ToggleLayoutModeCommand { get; private set; }
        public ICommand BackToMobileFeedCommand { get; private set; }
        public ICommand OpenFullScreenPageCommand { get; private set; }

        // 美团通行证登录命令 (OBL-02 ~ OBL-07)
        public ICommand OpenLoginViewCommand { get; private set; }
        public ICommand CloseLoginViewCommand { get; private set; }
        public ICommand SwitchLoginModeCommand { get; private set; }
        public ICommand SendSmsCodeCommand { get; private set; }
        public ICommand FillSimulatedSmsCodeCommand { get; private set; }
        public ICommand RefreshQrCodeCommand { get; private set; }
        public ICommand SimulateQrScanConfirmCommand { get; private set; }
        public ICommand TogglePrivacyAcceptedCommand { get; private set; }
        public ICommand SubmitLoginCommand { get; private set; }
        public ICommand ConfirmPrivacyAndLoginCommand { get; private set; }
        public ICommand ClosePrivacyDialogCommand { get; private set; }
        public ICommand QuickSelectPresetUserCommand { get; private set; }
        public ICommand LogoutCommand { get; private set; }

        // 首页搜索、金刚区、排序与门店详情命令 (OBL-08 ~ OBL-19)
        public ICommand ApplyHotKeywordCommand { get; private set; }
        public ICommand SelectKingKongCommand { get; private set; }
        public ICommand ResetFilterCommand { get; private set; }
        public ICommand ChangeSortModeCommand { get; private set; }
        public ICommand SelectPoiCommand { get; private set; }
        public ICommand TogglePoiFavoriteCommand { get; private set; }
        public ICommand ClaimPoiCouponCommand { get; private set; }
        public ICommand SelectMenuCategoryCommand { get; private set; }
        public ICommand SelectPoiSubTabCommand { get; private set; }
        public ICommand SelectReviewFilterCommand { get; private set; }
        public ICommand AddFoodCommand { get; private set; }
        public ICommand RemoveFoodCommand { get; private set; }
        public ICommand OpenSpecModalCommand { get; private set; }
        public ICommand SelectSpecSkuCommand { get; private set; }
        public ICommand CycleAttrOptionCommand { get; private set; }
        public ICommand ConfirmSpecModalCommand { get; private set; }
        public ICommand CloseSpecModalCommand { get; private set; }
        public ICommand SwitchPocketCommand { get; private set; }
        public ICommand ToggleCartDrawerCommand { get; private set; }
        public ICommand ClearCartCommand { get; private set; }

        // 神券、确认订单与收银台支付命令 (OBL-20 ~ OBL-24)
        public ICommand InflateCouponCommand { get; private set; }
        public ICommand ApplyCouponCommand { get; private set; }
        public ICommand RedeemCouponCodeCommand { get; private set; }
        public ICommand CheckoutOrderCommand { get; private set; }
        public ICommand SetDeliveryModeCommand { get; private set; }
        public ICommand AppendOrderNoteTagCommand { get; private set; }
        public ICommand CycleTablewareCommand { get; private set; }
        public ICommand TogglePrivacyPhoneCommand { get; private set; }
        public ICommand OpenCashierModalCommand { get; private set; }
        public ICommand CloseCashierModalCommand { get; private set; }
        public ICommand SelectPaymentMethodCommand { get; private set; }
        public ICommand ConfirmCashierPaymentCommand { get; private set; }
        public ICommand ApplyFlashDealCommand { get; private set; }
        public ICommand ApplyDeepEatCommand { get; private set; }
        public ICommand ToggleDeepEatDrawerCommand { get; private set; }
        public ICommand AskDeepEatAiCommand { get; private set; }

        // 订单管理、轨迹、IM 与评价晒单命令 (OBL-25 ~ OBL-30)
        public ICommand SelectOrderFilterCommand { get; private set; }
        public ICommand SelectOrderCommand { get; private set; }
        public ICommand AdvanceRiderStepCommand { get; private set; }
        public ICommand SendImMessageCommand { get; private set; }
        public ICommand SendSmartReplyCommand { get; private set; }
        public ICommand ReorderCommand { get; private set; }
        public ICommand CancelOrderCommand { get; private set; }
        public ICommand ConfirmOrderReceivedCommand { get; private set; }
        public ICommand OpenOrderReviewModalCommand { get; private set; }
        public ICommand CloseOrderReviewModalCommand { get; private set; }
        public ICommand SetReviewStarsCommand { get; private set; }
        public ICommand SubmitOrderReviewCommand { get; private set; }

        // 地址管理、收藏夹、消息中心与系统命令 (OBL-31 ~ OBL-35)
        public ICommand CycleAddressCommand { get; private set; }
        public ICommand SelectAddressCommand { get; private set; }
        public ICommand StartEditAddressCommand { get; private set; }
        public ICommand DeleteAddressCommand { get; private set; }
        public ICommand SetEditAddressGenderCommand { get; private set; }
        public ICommand SetEditAddressTagCommand { get; private set; }
        public ICommand SaveNewAddressCommand { get; private set; }
        public ICommand MarkAllNotificationsReadCommand { get; private set; }
        public ICommand LaunchOfficialH5Command { get; private set; }
        public ICommand RunSelfTestCommand { get; private set; }

        #endregion

        private void InitializeCommands()
        {
            SwitchTabCommand = new RelayCommand(delegate(object param)
            {
                int busId = ConvertToInt(param, 1);
                SelectedTabBusinessId = busId;
            });

            ToggleLayoutModeCommand = new RelayCommand(delegate
            {
                IsEasyGoDualPane = !IsEasyGoDualPane;
                StatusBannerText = IsEasyGoDualPane
                    ? "已切换至 EasyGo 平行视界模式"
                    : "已切换至原生手机 1:1 竖屏模式";
                SaveStateSnapshotFireAndForget();
            });

            BackToMobileFeedCommand = new RelayCommand(delegate
            {
                if (string.Equals(_activeFullScreenPage, "OrderConfirm", StringComparison.Ordinal))
                {
                    ActiveFullScreenPage = "PoiDetail";
                }
                else
                {
                    ActiveFullScreenPage = "None";
                }
            });

            OpenFullScreenPageCommand = new RelayCommand(delegate(object param)
            {
                string page = param as string;
                if (!string.IsNullOrEmpty(page))
                {
                    ActiveFullScreenPage = page;
                }
            });

            // ==================== 美团通行证登录命令实现 ====================

            OpenLoginViewCommand = new RelayCommand(delegate
            {
                IsLoginViewOpen = true;
                StatusBannerText = "已打开《美团通行证登录页 (com.meituan.passport.LoginActivity)》";
            });

            CloseLoginViewCommand = new RelayCommand(delegate
            {
                IsLoginViewOpen = false;
                _pendingCheckoutAfterLogin = false;
            });

            SwitchLoginModeCommand = new RelayCommand(delegate(object param)
            {
                string mode = param as string;
                if (!string.IsNullOrEmpty(mode))
                {
                    LoginMode = mode;
                }
            });

            SendSmsCodeCommand = new RelayCommand(delegate
            {
                string mobile = (_loginPhoneInput ?? string.Empty).Trim();
                if (mobile.Length < 7)
                {
                    mobile = "13800130001";
                    LoginPhoneInput = mobile;
                }
                int seed = Math.Abs(mobile.GetHashCode() + DateTime.Now.Second) % 900000 + 100000;
                _generatedSmsCode = seed.ToString("000000");
                LoginSmsCodeInput = _generatedSmsCode;
                _smsCountdownSeconds = 60;
                RaisePropertyChanged("SmsSendButtonText");

                SimulatedSmsBannerText = string.Format("💬【美团】验证码 {0}，用于手机 {1} 登录美团外卖（点击通知自动填入）",
                    _generatedSmsCode, PassportUserContract.MaskMobile(mobile));
                HasSimulatedSmsBanner = true;
                StatusBannerText = "已向 " + PassportUserContract.MaskMobile(mobile) + " 发送 6 位短信验证码：" + _generatedSmsCode;
            });

            FillSimulatedSmsCodeCommand = new RelayCommand(delegate
            {
                LoginSmsCodeInput = _generatedSmsCode;
                HasSimulatedSmsBanner = false;
            });

            RefreshQrCodeCommand = new RelayCommand(delegate
            {
                string suffix = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
                QrCodeTokenText = "MT-QR-91293-" + suffix;
                QrCodeStatusText = "安全二维码已刷新 (" + suffix + ")，请使用美团 APP 扫码";
                StatusBannerText = "已刷新美团通行证扫码安全令牌：" + QrCodeTokenText;
            });

            SimulateQrScanConfirmCommand = new RelayCommand(delegate
            {
                QrCodeStatusText = "✅ 手机端已扫码并确认授权登录";
                AttemptPassportLogin();
            });

            TogglePrivacyAcceptedCommand = new RelayCommand(delegate
            {
                IsPrivacyAccepted = !IsPrivacyAccepted;
            });

            SubmitLoginCommand = new RelayCommand(delegate
            {
                AttemptPassportLogin();
            });

            ConfirmPrivacyAndLoginCommand = new RelayCommand(delegate
            {
                IsPrivacyAccepted = true;
                IsPrivacyConfirmDialogOpen = false;
                AttemptPassportLogin();
            });

            ClosePrivacyDialogCommand = new RelayCommand(delegate
            {
                IsPrivacyConfirmDialogOpen = false;
            });

            QuickSelectPresetUserCommand = new RelayCommand(delegate(object param)
            {
                PassportUserContract preset = param as PassportUserContract;
                if (preset != null)
                {
                    IsPrivacyAccepted = true;
                    CompleteLoginWithUser(preset);
                }
            });

            LogoutCommand = new RelayCommand(delegate
            {
                CurrentUser = new PassportUserContract
                {
                    Id = 0,
                    Nickname = "未登录用户",
                    MobileMasked = "未绑定手机",
                    VipBadgeText = "登录后享神券特权",
                    AvatarEmoji = "🙂",
                    IsLoggedIn = false
                };
                StatusBannerText = "已退出当前美团账号，底部未登录悬浮引导条已激活";
                SaveStateSnapshotFireAndForget();
            });

            // ==================== 首页、搜索与门店详情命令实现 ====================

            ApplyHotKeywordCommand = new RelayCommand(delegate(object param)
            {
                string kw = param as string;
                if (!string.IsNullOrEmpty(kw))
                {
                    SearchKeyword = kw;
                    StatusBannerText = string.Format("已按热搜词「{0}」筛选门店与招牌菜", kw);
                }
            });

            SelectKingKongCommand = new RelayCommand(delegate(object param)
            {
                KingKongItemViewModel item = param as KingKongItemViewModel;
                if (item != null)
                {
                    if (SelectedKingKongCategory == item.CategoryBuzType)
                    {
                        SelectedKingKongCategory = 0;
                        StatusBannerText = "已取消金刚区分类筛选，展示全品类商家";
                    }
                    else
                    {
                        SelectedKingKongCategory = item.CategoryBuzType;
                        StatusBannerText = string.Format("已按金刚区「{0}」筛选附近品质商家", item.Title);
                    }
                }
            });

            ResetFilterCommand = new RelayCommand(delegate
            {
                _searchKeyword = string.Empty;
                RaisePropertyChanged("SearchKeyword");
                SelectedKingKongCategory = 0;
                SelectedSortMode = "综合排序";
                RefreshFilteredPois();
                StatusBannerText = "已重置全部搜索词与分类筛选条件";
            });

            ChangeSortModeCommand = new RelayCommand(delegate(object param)
            {
                string mode = param as string;
                if (!string.IsNullOrEmpty(mode))
                {
                    SelectedSortMode = mode;
                    StatusBannerText = string.Format("商家列表已按「{0}」重新排序", mode);
                }
            });

            SelectPoiCommand = new RelayCommand(delegate(object param)
            {
                PoiContract poi = param as PoiContract;
                if (poi != null)
                {
                    SelectedPoi = poi;
                    SelectedPoiSubTab = "点菜";
                    ActiveFullScreenPage = "PoiDetail";
                    StatusBannerText = string.Format("正在浏览「{0}」· 评分 {1} · {2}",
                        poi.Name, poi.FormattedPoiScore, poi.MtDeliveryTime);
                }
            });

            TogglePoiFavoriteCommand = new RelayCommand(delegate(object param)
            {
                PoiContract target = (param as PoiContract) ?? _selectedPoi;
                if (target != null)
                {
                    target.IsFavorite = !target.IsFavorite;
                    RefreshFavoritePois();
                    RaisePropertyChanged("SelectedPoi");
                    RaisePropertyChanged("SelectedPoiFavoriteText");
                    StatusBannerText = target.IsFavorite
                        ? string.Format("❤ 已收藏「{0}」，可在『我的 -> 我的收藏』查看", target.Name)
                        : string.Format("已取消收藏「{0}」", target.Name);
                    SaveStateSnapshotFireAndForget();
                }
            });

            ClaimPoiCouponCommand = new RelayCommand(delegate(object param)
            {
                PoiCouponContract poiCoupon = param as PoiCouponContract;
                if (poiCoupon != null && !poiCoupon.IsClaimed && _selectedPoi != null)
                {
                    poiCoupon.IsClaimed = true;
                    ShenQuanCouponContract walletCoupon = new ShenQuanCouponContract
                    {
                        CouponId = poiCoupon.CouponId,
                        Title = _selectedPoi.Name.Substring(0, Math.Min(4, _selectedPoi.Name.Length)) + "店铺券",
                        Subtitle = poiCoupon.Title,
                        BaseAmount = poiCoupon.Amount,
                        Amount = poiCoupon.Amount,
                        Threshold = poiCoupon.Threshold,
                        InflatedBonus = 2,
                        ApplicablePoiId = _selectedPoi.Id,
                        ExpireTimeText = "今日 23:59前有效",
                        TagText = "店铺专享券"
                    };
                    _coupons.Insert(0, walletCoupon);
                    SelectedCoupon = walletCoupon;
                    SyncMineProfileWithCurrentUser();
                    RaisePropertyChanged("SelectedPoi");
                    StatusBannerText = string.Format("成功领取「{0}」¥{1:0} 店铺优惠券，已自动为您勾选抵扣！", poiCoupon.Title, poiCoupon.Amount);
                }
            });

            SelectMenuCategoryCommand = new RelayCommand(delegate(object param)
            {
                string cat = param as string;
                if (!string.IsNullOrEmpty(cat))
                {
                    SelectedMenuCategory = cat;
                }
            });

            SelectPoiSubTabCommand = new RelayCommand(delegate(object param)
            {
                string tab = param as string;
                if (!string.IsNullOrEmpty(tab))
                {
                    SelectedPoiSubTab = tab;
                }
            });

            SelectReviewFilterCommand = new RelayCommand(delegate(object param)
            {
                string filter = param as string;
                if (!string.IsNullOrEmpty(filter))
                {
                    SelectedReviewFilter = filter;
                }
            });

            AddFoodCommand = new RelayCommand(delegate(object param)
            {
                GoodsSpuContract food = param as GoodsSpuContract;
                if (food != null)
                {
                    AddFoodToCartInternal(food, null, null);
                }
            });

            RemoveFoodCommand = new RelayCommand(delegate(object param)
            {
                GoodsSpuContract food = param as GoodsSpuContract;
                if (food != null)
                {
                    RemoveFoodFromCartInternal(food);
                }
            });

            OpenSpecModalCommand = new RelayCommand(delegate(object param)
            {
                GoodsSpuContract food = param as GoodsSpuContract;
                if (food != null)
                {
                    OpenSpecSelectionModal(food);
                }
            });

            SelectSpecSkuCommand = new RelayCommand(delegate(object param)
            {
                GoodsSkuContract sku = param as GoodsSkuContract;
                if (sku != null)
                {
                    SpecModalSelectedSku = sku;
                }
            });

            CycleAttrOptionCommand = new RelayCommand(delegate(object param)
            {
                GoodsAttrGroupContract group = param as GoodsAttrGroupContract;
                if (group != null && group.Options != null && group.Options.Count > 0)
                {
                    int idx = group.Options.IndexOf(group.SelectedOption);
                    int nextIdx = (idx + 1) % group.Options.Count;
                    group.SelectedOption = group.Options[nextIdx];
                    RaisePropertyChanged("SpecModalFood");
                    RaisePropertyChanged("SpecModalPreviewText");
                }
            });

            ConfirmSpecModalCommand = new RelayCommand(delegate
            {
                if (_specModalFood != null)
                {
                    List<string> attrs = new List<string>();
                    if (_specModalFood.AttrGroups != null)
                    {
                        for (int i = 0; i < _specModalFood.AttrGroups.Count; i++)
                        {
                            GoodsAttrGroupContract g = _specModalFood.AttrGroups[i];
                            if (!string.IsNullOrEmpty(g.SelectedOption))
                            {
                                attrs.Add(g.SelectedOption);
                            }
                        }
                    }
                    string customAttr = attrs.Count > 0 ? string.Join("/", attrs.ToArray()) : null;
                    AddFoodToCartInternal(_specModalFood, _specModalSelectedSku, customAttr);
                    IsSpecModalOpen = false;
                }
            });

            CloseSpecModalCommand = new RelayCommand(delegate
            {
                IsSpecModalOpen = false;
            });

            SwitchPocketCommand = new RelayCommand(delegate(object param)
            {
                int pocketId = ConvertToInt(param, 1);
                SelectedPocketId = pocketId;
                StatusBannerText = string.Format("已切换至「{0}号口袋」，后续加购商品将自动归入该口袋", SelectedPocketId);
            });

            ToggleCartDrawerCommand = new RelayCommand(delegate
            {
                IsCartDrawerOpen = !IsCartDrawerOpen;
            });

            ClearCartCommand = new RelayCommand(delegate
            {
                ClearCurrentPoiCart();
            });

            // ==================== 神券、确认订单与收银台支付命令实现 ====================

            InflateCouponCommand = new RelayCommand(delegate(object param)
            {
                ShenQuanCouponContract coupon = param as ShenQuanCouponContract ?? _selectedCoupon;
                if (coupon != null)
                {
                    coupon.Inflate();
                    SelectedCoupon = coupon;
                    RaisePropertyChanged("Coupons");
                    RaisePropertyChanged("SelectedCouponSummary");
                    RecalculateCartPrice();
                    StatusBannerText = string.Format("神券膨胀成功！「{0}」已膨胀至 ¥{1:0.0}，下单直接抵扣！",
                        coupon.Title, coupon.Amount);
                    SaveStateSnapshotFireAndForget();
                }
            });

            ApplyCouponCommand = new RelayCommand(delegate(object param)
            {
                ShenQuanCouponContract coupon = param as ShenQuanCouponContract;
                if (coupon != null && !coupon.IsUsed)
                {
                    SelectedCoupon = coupon;
                    SelectedTabBusinessId = 1;
                    ActiveFullScreenPage = "PoiDetail";
                    StatusBannerText = string.Format("已选用神券「{0}」(减¥{1:0.##})，快去选购美食吧",
                        coupon.Title, coupon.Amount);
                }
            });

            RedeemCouponCodeCommand = new RelayCommand(delegate
            {
                string code = (_couponRedeemCodeInput ?? string.Empty).Trim().ToUpperInvariant();
                if (string.IsNullOrEmpty(code))
                {
                    code = "MT888";
                }
                ShenQuanCouponContract redeemed = new ShenQuanCouponContract
                {
                    CouponId = "redeem-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    Title = "口令兑换神券 (" + code + ")",
                    Subtitle = "全场支持神券门店满29元可用",
                    BaseAmount = 8,
                    Amount = 12,
                    Threshold = 29,
                    InflatedBonus = 4,
                    IsInflated = true,
                    ApplicablePoiId = 0,
                    ExpireTimeText = "2026-10-31 23:59前有效",
                    TagText = "口令专享 · 已膨胀"
                };
                _coupons.Insert(0, redeemed);
                SelectedCoupon = redeemed;
                CouponRedeemCodeInput = string.Empty;
                SyncMineProfileWithCurrentUser();
                StatusBannerText = string.Format("🎉 兑换码「{0}」兑换成功！已获得 ¥12 大额膨胀神券！", code);
                SaveStateSnapshotFireAndForget();
            });

            CheckoutOrderCommand = new RelayCommand(delegate
            {
                OpenOrderConfirmOrSubmit();
            });

            SetDeliveryModeCommand = new RelayCommand(delegate(object param)
            {
                string mode = param as string;
                IsSelfPickupMode = string.Equals(mode, "Pickup", StringComparison.OrdinalIgnoreCase);
                StatusBannerText = IsSelfPickupMode
                    ? "已切换为「到店自取」模式，本单立省全部配送费！"
                    : "已切换为「美团专送 · 外卖配送」模式";
            });

            AppendOrderNoteTagCommand = new RelayCommand(delegate(object param)
            {
                string tag = param as string;
                if (!string.IsNullOrEmpty(tag))
                {
                    if (string.IsNullOrEmpty(_orderNoteInput))
                    {
                        OrderNoteInput = tag;
                    }
                    else if (_orderNoteInput.IndexOf(tag, StringComparison.Ordinal) < 0)
                    {
                        OrderNoteInput = _orderNoteInput + "，" + tag;
                    }
                }
            });

            CycleTablewareCommand = new RelayCommand(delegate
            {
                string[] options = new string[]
                {
                    "无需餐具 (得16g绿色能量)",
                    "1份餐具",
                    "2份餐具",
                    "3份餐具"
                };
                int idx = Array.IndexOf(options, _selectedTablewareOption);
                SelectedTablewareOption = options[(idx + 1) % options.Length];
            });

            TogglePrivacyPhoneCommand = new RelayCommand(delegate
            {
                IsPrivacyPhoneProtected = !IsPrivacyPhoneProtected;
            });

            OpenCashierModalCommand = new RelayCommand(delegate
            {
                IsCashierModalOpen = true;
                StatusBannerText = "已拉起《美团收银台 (MTCashierActivity)》，请选择支付方式确认付款";
            });

            CloseCashierModalCommand = new RelayCommand(delegate
            {
                IsCashierModalOpen = false;
            });

            SelectPaymentMethodCommand = new RelayCommand(delegate(object param)
            {
                CashierPaymentMethodContract method = param as CashierPaymentMethodContract;
                if (method != null)
                {
                    SelectedPaymentMethod = method;
                }
            });

            ConfirmCashierPaymentCommand = new RelayCommand(delegate
            {
                FinalizeAndCreateOrder();
            });

            ApplyFlashDealCommand = new RelayCommand(delegate(object param)
            {
                PromoFlashDealContract deal = param as PromoFlashDealContract;
                if (deal != null)
                {
                    ApplyFlashDealInternal(deal);
                }
            });

            ApplyDeepEatCommand = new RelayCommand(delegate(object param)
            {
                DeepEatRecommendationContract rec = param as DeepEatRecommendationContract;
                if (rec != null)
                {
                    ApplyDeepEatRecommendationInternal(rec);
                }
            });

            ToggleDeepEatDrawerCommand = new RelayCommand(delegate
            {
                IsDeepEatDrawerOpen = !IsDeepEatDrawerOpen;
            });

            AskDeepEatAiCommand = new RelayCommand(delegate
            {
                GenerateDeepEatAiResponse();
            });

            // ==================== 订单管理、轨迹、IM 与评价晒单命令实现 ====================

            SelectOrderFilterCommand = new RelayCommand(delegate(object param)
            {
                string filter = param as string;
                if (!string.IsNullOrEmpty(filter))
                {
                    SelectedOrderFilter = filter;
                }
            });

            SelectOrderCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = param as OrderContract;
                if (order != null)
                {
                    SelectedOrder = order;
                    ActiveFullScreenPage = "OrderTrack";
                    StatusBannerText = string.Format("正在查看订单「{0}」实时骑手轨迹与三方群聊", order.PoiName);
                }
            });

            AdvanceRiderStepCommand = new RelayCommand(delegate
            {
                AdvanceSelectedOrderRiderProgress(0.22);
            });

            SendImMessageCommand = new RelayCommand(delegate
            {
                SendImChatMessageInternal(_imDraftText);
                ImDraftText = string.Empty;
            });

            SendSmartReplyCommand = new RelayCommand(delegate(object param)
            {
                string reply = param as string;
                if (!string.IsNullOrEmpty(reply))
                {
                    SendImChatMessageInternal(reply);
                }
            });

            ReorderCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = (param as OrderContract) ?? _selectedOrder;
                if (order != null)
                {
                    PoiContract targetPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == order.PoiId; }) ?? _allPois.FirstOrDefault();
                    if (targetPoi != null)
                    {
                        SelectedPoi = targetPoi;
                        ClearCurrentPoiCart();
                        int addedCount = 0;
                        if (order.FoodItems != null && order.FoodItems.Count > 0 && targetPoi.Foods != null)
                        {
                            for (int i = 0; i < order.FoodItems.Count; i++)
                            {
                                OrderFoodItemContract item = order.FoodItems[i];
                                if (item == null)
                                {
                                    continue;
                                }
                                GoodsSpuContract matchedFood = targetPoi.Foods.FirstOrDefault(delegate(GoodsSpuContract f)
                                {
                                    return string.Equals(f.Name, item.FoodName, StringComparison.Ordinal);
                                }) ?? targetPoi.Foods.FirstOrDefault();

                                if (matchedFood != null)
                                {
                                    int copies = Math.Max(1, item.Count);
                                    for (int c = 0; c < copies; c++)
                                    {
                                        AddFoodToCartInternal(matchedFood, null, null);
                                        addedCount++;
                                    }
                                }
                            }
                        }
                        if (addedCount == 0 && targetPoi.Foods != null && targetPoi.Foods.Count > 0)
                        {
                            AddFoodToCartInternal(targetPoi.Foods[0], null, null);
                        }
                        ActiveFullScreenPage = "PoiDetail";
                        SelectedPoiSubTab = "点菜";
                        StatusBannerText = string.Format("已为您将订单「{0}」中的原选餐品 ({1}件) 重新加入购物车！", targetPoi.Name, _cartPrice != null ? _cartPrice.TotalItemCount : addedCount);
                    }
                }
            });

            CancelOrderCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = (param as OrderContract) ?? _selectedOrder;
                if (order != null)
                {
                    order.Status = 70;
                    order.StatusDescription = "已取消 (退款已原路退回)";
                    order.ArrivalTimeText = "退款 ¥" + order.Total.ToString("0.00") + " 已原路退回至原支付账户";
                    order.CanRefund = false;

                    // 若该订单核销了神券，退款时原路退回神券包
                    if (!string.IsNullOrEmpty(order.UsedCouponId))
                    {
                        ShenQuanCouponContract usedCoupon = _coupons.FirstOrDefault(delegate(ShenQuanCouponContract c)
                        {
                            return string.Equals(c.CouponId, order.UsedCouponId, StringComparison.Ordinal);
                        });
                        if (usedCoupon != null)
                        {
                            usedCoupon.IsUsed = false;
                            SelectedCoupon = usedCoupon;
                            RaisePropertyChanged("Coupons");
                            RaisePropertyChanged("SelectedCouponSummary");
                            SyncMineProfileWithCurrentUser();
                            RecalculateCartPrice();
                        }
                    }

                    if (order.TrackingSteps != null)
                    {
                        order.TrackingSteps.Add(new RiderTrackingStepContract
                        {
                            TimeText = DateTime.Now.ToString("HH:mm"),
                            Title = "订单已取消 · 极速退款成功",
                            Subtitle = "实付金额与已用神券已原路退回",
                            IsCompleted = true,
                            IsCurrent = true
                        });
                    }
                    RefreshFilteredOrders();
                    RaisePropertyChanged("SelectedOrder");
                    StatusBannerText = string.Format("订单「{0}」已取消，实付 ¥{1:0.00} 及优惠券已原路退回", order.PoiName, order.Total);
                    SaveStateSnapshotFireAndForget();
                }
            });

            ConfirmOrderReceivedCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = (param as OrderContract) ?? _selectedOrder;
                if (order != null)
                {
                    order.Status = 40;
                    order.StatusDescription = "待评价";
                    order.RiderProgressRatio = 1.0;
                    order.ArrivalTimeText = "已确认收货 · 评价可得团币奖励";
                    RefreshFilteredOrders();
                    RaisePropertyChanged("SelectedOrder");
                    RaisePropertyChanged("SelectedOrderProgressPercent");
                    StatusBannerText = string.Format("已确认收到「{0}」餐品，快去评价晒单领团币吧！", order.PoiName);
                    SaveStateSnapshotFireAndForget();
                }
            });

            OpenOrderReviewModalCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = (param as OrderContract) ?? _selectedOrder;
                if (order != null)
                {
                    SelectedOrder = order;
                    ReviewRatingStars = 5;
                    ReviewCommentInput = "包装严实没有撒漏，菜品热乎味道好，骑手小哥送餐准时！";
                    IsReviewOrderModalOpen = true;
                }
            });

            CloseOrderReviewModalCommand = new RelayCommand(delegate
            {
                IsReviewOrderModalOpen = false;
            });

            SetReviewStarsCommand = new RelayCommand(delegate(object param)
            {
                ReviewRatingStars = ConvertToInt(param, 5);
            });

            SubmitOrderReviewCommand = new RelayCommand(delegate
            {
                SubmitOrderReviewInternal();
            });

            // ==================== 地址管理与消息中心命令实现 ====================

            CycleAddressCommand = new RelayCommand(delegate
            {
                if (_addresses.Count > 0)
                {
                    int idx = _addresses.IndexOf(_currentAddress);
                    int nextIdx = (idx + 1) % _addresses.Count;
                    CurrentAddress = _addresses[nextIdx];
                    StatusBannerText = string.Format("收货地址已切换至：{0} ({1} {2})",
                        CurrentAddress.AddressDetail, CurrentAddress.ContactName, CurrentAddress.Phone);
                    SaveStateSnapshotFireAndForget();
                }
            });

            SelectAddressCommand = new RelayCommand(delegate(object param)
            {
                WmAddressContract addr = param as WmAddressContract;
                if (addr != null)
                {
                    CurrentAddress = addr;
                    StatusBannerText = string.Format("已选择收货地址：{0} {1}", addr.AddressDetail, addr.HouseNumber);
                    SaveStateSnapshotFireAndForget();
                }
            });

            StartEditAddressCommand = new RelayCommand(delegate(object param)
            {
                WmAddressContract addr = (param as WmAddressContract) ?? _currentAddress;
                if (addr != null)
                {
                    EditingAddressId = addr.AddressId;
                    EditAddressContactName = addr.ContactName;
                    EditAddressGender = addr.GenderText ?? "先生";
                    EditAddressPhone = addr.Phone;
                    EditAddressDetail = addr.AddressDetail;
                    EditAddressHouseNumber = addr.HouseNumber;
                    EditAddressTag = addr.TagLabel ?? "家";
                    StatusBannerText = "正在编辑收货地址：" + addr.AddressDetail;
                }
            });

            DeleteAddressCommand = new RelayCommand(delegate(object param)
            {
                WmAddressContract addr = param as WmAddressContract;
                if (addr != null && _addresses.Count > 1)
                {
                    _addresses.Remove(addr);
                    if (string.Equals(_editingAddressId, addr.AddressId, StringComparison.Ordinal))
                    {
                        EditingAddressId = string.Empty;
                    }
                    if (ReferenceEquals(_currentAddress, addr))
                    {
                        CurrentAddress = _addresses[0];
                    }
                    StatusBannerText = "已删除收货地址：" + addr.AddressDetail;
                    SaveStateSnapshotFireAndForget();
                }
            });

            SetEditAddressGenderCommand = new RelayCommand(delegate(object param)
            {
                string g = param as string;
                if (!string.IsNullOrEmpty(g))
                {
                    EditAddressGender = g;
                }
            });

            SetEditAddressTagCommand = new RelayCommand(delegate(object param)
            {
                string t = param as string;
                if (!string.IsNullOrEmpty(t))
                {
                    EditAddressTag = t;
                }
            });

            SaveNewAddressCommand = new RelayCommand(delegate
            {
                string name = (_editAddressContactName ?? string.Empty).Trim();
                string phone = (_editAddressPhone ?? string.Empty).Trim();
                string detail = (_editAddressDetail ?? string.Empty).Trim();
                string house = (_editAddressHouseNumber ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(detail))
                {
                    StatusBannerText = "请填写完整的收货人姓名与详细收货地址";
                    return;
                }
                if (string.IsNullOrEmpty(phone))
                {
                    phone = "138****8888";
                }

                if (!string.IsNullOrEmpty(_editingAddressId))
                {
                    int existingIdx = -1;
                    for (int i = 0; i < _addresses.Count; i++)
                    {
                        if (string.Equals(_addresses[i].AddressId, _editingAddressId, StringComparison.Ordinal))
                        {
                            existingIdx = i;
                            break;
                        }
                    }

                    if (existingIdx >= 0)
                    {
                        WmAddressContract updated = new WmAddressContract
                        {
                            AddressId = _addresses[existingIdx].AddressId,
                            ContactName = name,
                            GenderText = _editAddressGender ?? "先生",
                            Phone = PassportUserContract.MaskMobile(phone),
                            AddressDetail = detail,
                            HouseNumber = string.IsNullOrEmpty(house) ? "1单元101室" : house,
                            TagLabel = _editAddressTag ?? "家",
                            IsDefault = true,
                            Latitude = _addresses[existingIdx].Latitude,
                            Longitude = _addresses[existingIdx].Longitude
                        };
                        _addresses[existingIdx] = updated;
                        CurrentAddress = updated;
                        EditingAddressId = string.Empty;
                        StatusBannerText = string.Format("已保存修改后的收货地址：{0} {1} ({2})", updated.AddressDetail, updated.HouseNumber, updated.ContactName);
                        SaveStateSnapshotFireAndForget();
                        return;
                    }
                }

                WmAddressContract newAddr = new WmAddressContract
                {
                    AddressId = "addr-" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    ContactName = name,
                    GenderText = _editAddressGender ?? "先生",
                    Phone = PassportUserContract.MaskMobile(phone),
                    AddressDetail = detail,
                    HouseNumber = string.IsNullOrEmpty(house) ? "1单元101室" : house,
                    TagLabel = _editAddressTag ?? "家",
                    IsDefault = true,
                    Latitude = 22541000,
                    Longitude = 113953000
                };
                _addresses.Insert(0, newAddr);
                CurrentAddress = newAddr;
                EditingAddressId = string.Empty;
                StatusBannerText = string.Format("已新增并启用收货地址：{0} {1} ({2})", newAddr.AddressDetail, newAddr.HouseNumber, newAddr.ContactName);
                SaveStateSnapshotFireAndForget();
            });

            MarkAllNotificationsReadCommand = new RelayCommand(delegate
            {
                for (int i = 0; i < _notifications.Count; i++)
                {
                    _notifications[i].IsUnread = false;
                }
                List<NotificationMessageContract> copy = _notifications.ToList();
                _notifications.Clear();
                for (int i = 0; i < copy.Count; i++)
                {
                    _notifications.Add(copy[i]);
                }
                RaisePropertyChanged("UnreadNotificationCount");
                RaisePropertyChanged("HasUnreadNotifications");
                StatusBannerText = "消息中心全部通知已标记为已读";
            });

            LaunchOfficialH5Command = new RelayCommand(delegate
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = ReverseEvidenceManifest.OfficialH5HomeUrl,
                        UseShellExecute = true
                    };
                    Process.Start(psi);
                    StatusBannerText = "已在系统浏览器唤起美团外卖官方 H5 线上站：" + ReverseEvidenceManifest.OfficialH5HomeUrl;
                }
                catch (Exception ex)
                {
                    StatusBannerText = "唤起浏览器失败：" + ex.Message;
                }
            });

            RunSelfTestCommand = new RelayCommand(async delegate
            {
                await ExecuteSelfTestsAsync();
            });
        }

        private void LoadSeedDataSynchronously()
        {
            const string officialEasyGoJson = "{\"easyGoVersion\":\"1.0\",\"client\":\"com.sankuai.meituan.takeoutnew\",\"logicEntities\":[{\"head\":{\"function\":\"magicwindow\"},\"body\":{\"defaultDualActivities\":{\"mainPages\":\"com.sankuai.waimai.business.page.homepage.MainActivity\",\"relatedPage\":\"com.sankuai.waimai.business.page.kingkong.KingKongActivity\"},\"transActivities\":[\"com.sankuai.waimai.foundation.core.base.activity.transfer.TransferActivity\"],\"Activities\":[{\"name\":\"com.sankuai.waimai.business.page.homepage.MainActivity\",\"defaultFullScreen\":\"false\"}],\"UX\":{\"supportRotationUxCompat\":\"true\",\"isDraggable\":\"true\"}}}]}";
            _easyGoContract = EasyGoMagicWindowContract.ParseFromJson(officialEasyGoJson);

            List<DynamicTabContract> tabs = WaimaiSeedDataFactory.CreateDefaultTabs();
            string[] safeTabGlyphs = new string[] { "🏠", "🧧", "⚡", "🧾", "🦘" };
            _bottomTabs.Clear();
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].IconGlyph = safeTabGlyphs[i % safeTabGlyphs.Length];
                _bottomTabs.Add(tabs[i]);
            }

            List<KingKongChannelContract> channels = WaimaiSeedDataFactory.CreateKingKongChannels();
            _kingKongItems.Clear();
            for (int i = 0; i < channels.Count; i++)
            {
                _kingKongItems.Add(new KingKongItemViewModel(channels[i], null));
            }

            List<string> hotKeywords = WaimaiSeedDataFactory.CreateHotSearchKeywords();
            _hotSearchKeywords.Clear();
            for (int i = 0; i < hotKeywords.Count; i++)
            {
                _hotSearchKeywords.Add(hotKeywords[i]);
            }

            if (_assetLoader != null)
            {
                LauncherIcon = _assetLoader.TryGetFrozenIcon("wm_ic_launcher.png", 128);
                MascotIcon = _assetLoader.TryGetFrozenIcon("wm_kangaroo_mascot.png", 128);
            }

            _allPois = WaimaiSeedDataFactory.CreateDefaultPois();
            RefreshFavoritePois();

            List<ShenQuanCouponContract> seedCoupons = WaimaiSeedDataFactory.CreateDefaultCoupons();
            _coupons.Clear();
            for (int i = 0; i < seedCoupons.Count; i++)
            {
                _coupons.Add(seedCoupons[i]);
            }
            if (_coupons.Count > 0)
            {
                ShenQuanCouponContract uninflated = _coupons.FirstOrDefault(delegate(ShenQuanCouponContract c)
                {
                    return !c.IsInflated && !c.IsUsed;
                });
                _selectedCoupon = uninflated ?? _coupons[0];
            }

            List<PromoFlashDealContract> deals = WaimaiSeedDataFactory.CreatePromoFlashDeals();
            _flashDeals.Clear();
            for (int i = 0; i < deals.Count; i++)
            {
                _flashDeals.Add(deals[i]);
            }

            List<DeepEatRecommendationContract> recs = WaimaiSeedDataFactory.CreateDeepEatRecommendations();
            _deepEatRecommendations.Clear();
            for (int i = 0; i < recs.Count; i++)
            {
                _deepEatRecommendations.Add(recs[i]);
            }

            List<OrderContract> seedOrders = WaimaiSeedDataFactory.CreateDefaultOrders();
            _orders.Clear();
            for (int i = 0; i < seedOrders.Count; i++)
            {
                _orders.Add(seedOrders[i]);
            }
            RefreshFilteredOrders();
            if (_orders.Count > 0)
            {
                _selectedOrder = _orders[0];
            }

            List<WmAddressContract> seedAddresses = WaimaiSeedDataFactory.CreateDefaultAddresses();
            _addresses.Clear();
            for (int i = 0; i < seedAddresses.Count; i++)
            {
                _addresses.Add(seedAddresses[i]);
            }
            if (_addresses.Count > 0)
            {
                _currentAddress = _addresses[0];
            }

            List<PassportUserContract> presets = WaimaiSeedDataFactory.CreatePresetPassportUsers();
            _presetUsers.Clear();
            for (int i = 0; i < presets.Count; i++)
            {
                _presetUsers.Add(presets[i]);
            }

            List<CashierPaymentMethodContract> payMethods = WaimaiSeedDataFactory.CreateDefaultPaymentMethods();
            _paymentMethods.Clear();
            for (int i = 0; i < payMethods.Count; i++)
            {
                _paymentMethods.Add(payMethods[i]);
            }
            _selectedPaymentMethod = _paymentMethods.FirstOrDefault();

            List<NotificationMessageContract> msgs = WaimaiSeedDataFactory.CreateDefaultNotifications();
            _notifications.Clear();
            for (int i = 0; i < msgs.Count; i++)
            {
                _notifications.Add(msgs[i]);
            }

            _mineProfile = WaimaiSeedDataFactory.CreateDefaultMineProfile();
            _currentUser = WaimaiSeedDataFactory.CreateDefaultPassportUser();
            SyncMineProfileWithCurrentUser();

            RefreshFilteredPois();
            if (_filteredPois.Count > 0)
            {
                SelectedPoi = _filteredPois[0];
            }

            // 默认预加购第一家门店招牌菜，方便进入门店即可体验满减与去结算
            if (_selectedPoi != null && _selectedPoi.Foods != null && _selectedPoi.Foods.Count > 0)
            {
                AddFoodToCartInternal(_selectedPoi.Foods[0], null, null);
            }
        }

        private void AttemptPassportLogin()
        {
            if (!_isPrivacyAccepted)
            {
                IsPrivacyConfirmDialogOpen = true;
                StatusBannerText = "请先阅读并同意《美团用户协议》与《隐私政策》";
                return;
            }

            string rawPhone = (_loginPhoneInput ?? string.Empty).Trim();
            if (rawPhone.Length < 7)
            {
                rawPhone = "13800130001";
                LoginPhoneInput = rawPhone;
            }

            if (IsSmsLoginMode)
            {
                string smsInput = (_loginSmsCodeInput ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(smsInput))
                {
                    StatusBannerText = "❌ 请先点击『获取验证码』并输入 6 位短信验证码";
                    return;
                }
                if (!string.Equals(smsInput, _generatedSmsCode, StringComparison.Ordinal))
                {
                    StatusBannerText = "❌ 短信验证码不匹配，请核对美团短信通知中的 6 位数字 (" + _generatedSmsCode + ")";
                    return;
                }
            }
            else if (IsPasswordLoginMode)
            {
                string pwdInput = (_loginPasswordInput ?? string.Empty).Trim();
                if (pwdInput.Length < 6 ||
                    (!string.Equals(pwdInput, "mt884291", StringComparison.Ordinal) &&
                     !string.Equals(pwdInput, "123456", StringComparison.Ordinal)))
                {
                    StatusBannerText = "❌ 账号或密码错误，请重新输入（默认密码：mt884291）";
                    return;
                }
            }
            else if (IsQrCodeLoginMode)
            {
                if (string.IsNullOrEmpty(_qrCodeTokenText))
                {
                    RefreshQrCodeCommand.Execute(null);
                }
            }

            PassportUserContract matchedPreset = _presetUsers.FirstOrDefault(delegate(PassportUserContract u)
            {
                return string.Equals(u.Mobile, rawPhone, StringComparison.Ordinal);
            });

            PassportUserContract loggedInUser = matchedPreset ?? new PassportUserContract
            {
                Id = 884200000L + Math.Abs(rawPhone.GetHashCode() % 89999),
                Username = "mt_" + (rawPhone.Length >= 4 ? rawPhone.Substring(rawPhone.Length - 4) : "8842"),
                Nickname = "美团吃货·尾号" + (rawPhone.Length >= 4 ? rawPhone.Substring(rawPhone.Length - 4) : "0001"),
                Mobile = rawPhone,
                MobileMasked = PassportUserContract.MaskMobile(rawPhone),
                Token = "MT_PASSPORT_TK_" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpperInvariant(),
                Ticket = "ST-91293-LOGIN-OK",
                LoginAuthTicket = "LAT-91293",
                AvatarEmoji = "🦘",
                VipBadgeText = "黑金神券会员 · Lv.5",
                Growthlevel = 5,
                Growthvalue = 3800,
                Pointvalue = 1560,
                SaveAmount = 96.5,
                RedPacketBalance = 25.0,
                Haspassword = 1,
                IsLoggedIn = true
            };

            CompleteLoginWithUser(loggedInUser);
        }

        private void CompleteLoginWithUser(PassportUserContract user)
        {
            if (user == null)
            {
                return;
            }
            user.IsLoggedIn = true;
            CurrentUser = user;
            HasSimulatedSmsBanner = false;
            IsPrivacyConfirmDialogOpen = false;
            IsLoginViewOpen = false;

            StatusBannerText = string.Format("✅ 美团通行证登录成功！欢迎回来，{0} ({1})", user.Nickname, user.MobileMasked);
            SaveStateSnapshotFireAndForget();

            if (_pendingCheckoutAfterLogin)
            {
                _pendingCheckoutAfterLogin = false;
                ActiveFullScreenPage = "OrderConfirm";
            }
        }

        private void SyncMineProfileWithCurrentUser()
        {
            if (_mineProfile == null)
            {
                _mineProfile = WaimaiSeedDataFactory.CreateDefaultMineProfile();
            }

            int usableCoupons = _coupons.Count(delegate(ShenQuanCouponContract c) { return !c.IsUsed; });
            _mineProfile.ShenQuanCount = usableCoupons;

            if (_currentUser != null && _currentUser.IsLoggedIn)
            {
                _mineProfile.UserId = _currentUser.Id.ToString();
                _mineProfile.Nickname = _currentUser.Nickname;
                _mineProfile.VipLevelText = _currentUser.VipBadgeText;
                _mineProfile.PhoneMasked = _currentUser.MobileMasked;
                _mineProfile.TuanBiPoints = _currentUser.Pointvalue;
                _mineProfile.RedPacketBalance = _currentUser.RedPacketBalance;
                _mineProfile.MonthlySavedAmount = _currentUser.SaveAmount;
            }
            else
            {
                _mineProfile.UserId = "guest";
                _mineProfile.Nickname = "未登录用户 (点击登录)";
                _mineProfile.VipLevelText = "登录领25元神券礼包";
                _mineProfile.PhoneMasked = "未绑定手机号";
            }
            RaisePropertyChanged("MineProfile");
        }

        /// <summary>
        /// 异步初始化：预热 APK 图标缓存、恢复 DPAPI 持久化状态并执行后台契约自检。
        /// 严格遵守 zero sync-over-async 与 zero UI thread File I/O。
        /// </summary>
        public async Task InitializeAsync(bool restorePersistedState = true, string customSnapshotPath = null)
        {
            try
            {
                if (_assetLoader != null && !_assetLoader.IsPreloaded)
                {
                    await _assetLoader.PreloadAssetsAsync(96);
                }

                if (_assetLoader != null)
                {
                    LauncherIcon = _assetLoader.TryGetFrozenIcon("wm_ic_launcher.png", 128);
                    MascotIcon = _assetLoader.TryGetFrozenIcon("wm_kangaroo_mascot.png", 128);
                }

                if (restorePersistedState)
                {
                    WaimaiPersistedSnapshot snapshot = await WaimaiStorageService.LoadSnapshotAsync(customSnapshotPath);
                    if (snapshot != null)
                    {
                        ApplyPersistedSnapshot(snapshot);
                    }
                }

                await ExecuteSelfTestsAsync();
            }
            catch (Exception ex)
            {
                StatusBannerText = "异步初始化提醒：" + ex.Message;
            }
        }

        /// <summary>
        /// 从 DPAPI 解密后的持久化快照恢复用户视图状态、登录态、地址簿、神券钱包、购物车与历史订单。
        /// </summary>
        public void ApplyPersistedSnapshot(WaimaiPersistedSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            IsEasyGoDualPane = snapshot.IsEasyGoDualPane;

            if (snapshot.CurrentUser != null)
            {
                CurrentUser = snapshot.CurrentUser;
            }

            if (snapshot.FavoritePoiIds != null && snapshot.FavoritePoiIds.Count > 0)
            {
                HashSet<long> favSet = new HashSet<long>(snapshot.FavoritePoiIds);
                for (int i = 0; i < _allPois.Count; i++)
                {
                    _allPois[i].IsFavorite = favSet.Contains(_allPois[i].Id);
                }
                RefreshFavoritePois();
            }

            if (snapshot.Addresses != null)
            {
                _addresses.Clear();
                for (int i = 0; i < snapshot.Addresses.Count; i++)
                {
                    _addresses.Add(snapshot.Addresses[i]);
                }
                if (_addresses.Count > 0)
                {
                    WmAddressContract matchedAddr = _addresses.FirstOrDefault(delegate(WmAddressContract a)
                    {
                        return string.Equals(a.AddressId, snapshot.CurrentAddressId, StringComparison.Ordinal);
                    });
                    CurrentAddress = matchedAddr ?? _addresses[0];
                }
                else
                {
                    CurrentAddress = null;
                }
            }

            if (snapshot.Coupons != null)
            {
                _coupons.Clear();
                for (int i = 0; i < snapshot.Coupons.Count; i++)
                {
                    _coupons.Add(snapshot.Coupons[i]);
                }
                if (_coupons.Count > 0)
                {
                    ShenQuanCouponContract usable = _coupons.FirstOrDefault(delegate(ShenQuanCouponContract c)
                    {
                        return !c.IsUsed;
                    });
                    SelectedCoupon = usable ?? _coupons[0];
                }
                else
                {
                    SelectedCoupon = null;
                }
            }

            if (snapshot.Orders != null)
            {
                _orders.Clear();
                for (int i = 0; i < snapshot.Orders.Count; i++)
                {
                    _orders.Add(snapshot.Orders[i]);
                }
                RefreshFilteredOrders();
                SelectedOrder = _orders.Count > 0 ? _orders[0] : null;
            }

            if (snapshot.CartItems != null)
            {
                _activeCartItems.Clear();
                for (int i = 0; i < snapshot.CartItems.Count; i++)
                {
                    _activeCartItems.Add(snapshot.CartItems[i]);
                }
                SyncFoodSelectedCounts();
                RecalculateCartPrice();
            }

            SyncMineProfileWithCurrentUser();
        }

        public async Task ExecuteSelfTestsAsync()
        {
            SelfTestReport report = await WaimaiContractSelfTests.RunAllAsync(_repoRootPath);
            _selfTestItems.Clear();
            if (report != null && report.Items != null)
            {
                for (int i = 0; i < report.Items.Count; i++)
                {
                    _selfTestItems.Add(report.Items[i]);
                }
                SelfTestSummaryText = string.Format("逆向契约自检：{0}/{1} 项全部通过 (Failed: {2})",
                    report.PassedCount, report.TotalCount, report.FailedCount);
            }
        }

        private void RefreshFilteredPois()
        {
            IEnumerable<PoiContract> query = _allPois;

            if (_selectedKingKongCategory != 0)
            {
                int targetCat = _selectedKingKongCategory;
                query = query.Where(delegate(PoiContract p)
                {
                    return p.BuzType == targetCat || targetCat == 1;
                });
            }

            if (!string.IsNullOrEmpty(_searchKeyword))
            {
                string kw = _searchKeyword.Trim();
                query = query.Where(delegate(PoiContract p)
                {
                    if (p.Name != null && p.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    if (p.BrandSlogan != null && p.BrandSlogan.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    if (p.Foods != null)
                    {
                        for (int i = 0; i < p.Foods.Count; i++)
                        {
                            GoodsSpuContract f = p.Foods[i];
                            if (f.Name != null && f.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                return true;
                            }
                        }
                    }
                    return false;
                });
            }

            if (string.Equals(_selectedSortMode, "销量最高", StringComparison.Ordinal))
            {
                query = query.OrderByDescending(delegate(PoiContract p) { return p.MonthSales; });
            }
            else if (string.Equals(_selectedSortMode, "速度最快", StringComparison.Ordinal) ||
                     string.Equals(_selectedSortMode, "距离最近", StringComparison.Ordinal))
            {
                query = query.OrderBy(delegate(PoiContract p) { return p.DeliveryMinutes; });
            }
            else if (string.Equals(_selectedSortMode, "评分最高", StringComparison.Ordinal) ||
                     string.Equals(_selectedSortMode, "好评优先", StringComparison.Ordinal))
            {
                query = query.OrderByDescending(delegate(PoiContract p) { return p.PoiScore; });
            }
            else
            {
                query = query.OrderByDescending(delegate(PoiContract p) { return p.IsBrandGold; })
                             .ThenByDescending(delegate(PoiContract p) { return p.PoiScore; });
            }

            List<PoiContract> list = query.ToList();
            _filteredPois.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                _filteredPois.Add(list[i]);
            }

            if (_selectedPoi == null || !_filteredPois.Contains(_selectedPoi))
            {
                if (_filteredPois.Count > 0)
                {
                    SelectedPoi = _filteredPois[0];
                }
            }
        }

        private void RefreshFavoritePois()
        {
            _favoritePois.Clear();
            for (int i = 0; i < _allPois.Count; i++)
            {
                if (_allPois[i].IsFavorite)
                {
                    _favoritePois.Add(_allPois[i]);
                }
            }
            RaisePropertyChanged("FavoritePois");
        }

        private void OnSelectedPoiChanged()
        {
            _currentMenuCategories.Clear();
            _currentMenuCategories.Add("全部菜单");

            if (_selectedPoi != null && _selectedPoi.MenuCategories != null)
            {
                for (int i = 0; i < _selectedPoi.MenuCategories.Count; i++)
                {
                    string cat = _selectedPoi.MenuCategories[i];
                    if (!_currentMenuCategories.Contains(cat))
                    {
                        _currentMenuCategories.Add(cat);
                    }
                }
            }

            _selectedMenuCategory = "全部菜单";
            RaisePropertyChanged("SelectedMenuCategory");
            RefreshFilteredFoods();
            RefreshFilteredPoiReviews();
            SyncFoodSelectedCounts();
            RecalculateCartPrice();
        }

        private void RefreshFilteredFoods()
        {
            _filteredFoods.Clear();
            if (_selectedPoi == null || _selectedPoi.Foods == null)
            {
                return;
            }

            for (int i = 0; i < _selectedPoi.Foods.Count; i++)
            {
                GoodsSpuContract food = _selectedPoi.Foods[i];
                if (string.IsNullOrEmpty(_selectedMenuCategory) ||
                    string.Equals(_selectedMenuCategory, "全部菜单", StringComparison.Ordinal) ||
                    string.Equals(food.CategoryTag, _selectedMenuCategory, StringComparison.Ordinal))
                {
                    _filteredFoods.Add(food);
                }
            }
        }

        private void RefreshFilteredPoiReviews()
        {
            _filteredPoiReviews.Clear();
            if (_selectedPoi == null || _selectedPoi.Reviews == null)
            {
                return;
            }

            for (int i = 0; i < _selectedPoi.Reviews.Count; i++)
            {
                PoiReviewItemContract rev = _selectedPoi.Reviews[i];
                if (string.Equals(_selectedReviewFilter, "好评", StringComparison.Ordinal) && rev.Score < 4.5)
                {
                    continue;
                }
                if (string.Equals(_selectedReviewFilter, "有图", StringComparison.Ordinal) && !rev.HasPictures)
                {
                    continue;
                }
                if (string.Equals(_selectedReviewFilter, "差评", StringComparison.Ordinal) && rev.Score >= 4.0)
                {
                    continue;
                }
                _filteredPoiReviews.Add(rev);
            }
        }

        private void RefreshFilteredOrders()
        {
            _filteredOrders.Clear();
            for (int i = 0; i < _orders.Count; i++)
            {
                OrderContract ord = _orders[i];
                if (string.Equals(_selectedOrderFilter, "进行中", StringComparison.Ordinal))
                {
                    if (ord.Status != 4 && ord.Status != 20 && ord.Status != 30)
                    {
                        continue;
                    }
                }
                else if (string.Equals(_selectedOrderFilter, "待评价", StringComparison.Ordinal))
                {
                    if (ord.Status != 40 || ord.IsComment)
                    {
                        continue;
                    }
                }
                else if (string.Equals(_selectedOrderFilter, "退款售后", StringComparison.Ordinal))
                {
                    if (ord.Status != 70)
                    {
                        continue;
                    }
                }
                _filteredOrders.Add(ord);
            }
        }

        private void OpenSpecSelectionModal(GoodsSpuContract food)
        {
            SpecModalFood = food;
            if (food.Skus != null && food.Skus.Count > 0)
            {
                SpecModalSelectedSku = food.Skus[0];
            }
            else
            {
                SpecModalSelectedSku = new GoodsSkuContract
                {
                    SkuId = food.Id * 10 + 1,
                    SpecName = "标准份",
                    Price = food.MinPrice,
                    OriginPrice = food.OriginPrice,
                    BoxFee = food.BoxFee,
                    Stock = 99
                };
            }
            RaisePropertyChanged("SpecModalPreviewText");
            IsSpecModalOpen = true;
        }

        private void AddFoodToCartInternal(GoodsSpuContract food, GoodsSkuContract sku, string customAttrSummary)
        {
            if (_selectedPoi == null || food == null)
            {
                return;
            }

            long skuId = sku != null ? sku.SkuId : (food.Skus != null && food.Skus.Count > 0 ? food.Skus[0].SkuId : food.Id * 10 + 1);
            string specName = sku != null ? sku.SpecName : (food.Skus != null && food.Skus.Count > 0 ? food.Skus[0].SpecName : "标准份");
            if (!string.IsNullOrEmpty(customAttrSummary))
            {
                specName = specName + " / " + customAttrSummary;
            }
            double unitPrice = sku != null ? sku.Price : food.MinPrice;
            double originPrice = sku != null ? sku.OriginPrice : food.OriginPrice;
            double boxFee = sku != null ? sku.BoxFee : food.BoxFee;

            ShopCartItemContract existing = null;
            for (int i = 0; i < _activeCartItems.Count; i++)
            {
                ShopCartItemContract item = _activeCartItems[i];
                if (item.PoiId == _selectedPoi.Id &&
                    item.SpuId == food.Id &&
                    item.SkuId == skuId &&
                    item.PocketId == _selectedPocketId &&
                    string.Equals(item.SpecSummary, specName, StringComparison.Ordinal))
                {
                    existing = item;
                    break;
                }
            }

            if (existing != null)
            {
                existing.Count += 1;
                existing.NxItemCount = existing.Count;
                int index = _activeCartItems.IndexOf(existing);
                if (index >= 0)
                {
                    _activeCartItems[index] = existing;
                }
            }
            else
            {
                ShopCartItemContract newItem = new ShopCartItemContract
                {
                    PoiId = _selectedPoi.Id,
                    SpuId = food.Id,
                    SkuId = skuId,
                    FoodName = food.Name,
                    SpecSummary = specName,
                    UnitPrice = unitPrice,
                    OriginUnitPrice = originPrice > unitPrice ? originPrice : unitPrice,
                    BoxFeePerUnit = boxFee,
                    Count = 1,
                    NxItemCount = 1,
                    PocketId = _selectedPocketId
                };
                _activeCartItems.Add(newItem);
            }

            SyncFoodSelectedCounts();
            RecalculateCartPrice();
            StatusBannerText = string.Format("已将「{0} ({1})」加入 {2}号口袋", food.Name, specName, _selectedPocketId);
        }

        private void RemoveFoodFromCartInternal(GoodsSpuContract food)
        {
            if (_selectedPoi == null || food == null)
            {
                return;
            }

            for (int i = _activeCartItems.Count - 1; i >= 0; i--)
            {
                ShopCartItemContract item = _activeCartItems[i];
                if (item.PoiId == _selectedPoi.Id && item.SpuId == food.Id)
                {
                    if (item.Count > 1)
                    {
                        item.Count -= 1;
                        item.NxItemCount = item.Count;
                        _activeCartItems[i] = item;
                    }
                    else
                    {
                        _activeCartItems.RemoveAt(i);
                    }
                    break;
                }
            }

            SyncFoodSelectedCounts();
            RecalculateCartPrice();
        }

        private void ClearCurrentPoiCart()
        {
            if (_selectedPoi == null)
            {
                return;
            }

            for (int i = _activeCartItems.Count - 1; i >= 0; i--)
            {
                if (_activeCartItems[i].PoiId == _selectedPoi.Id)
                {
                    _activeCartItems.RemoveAt(i);
                }
            }

            IsCartDrawerOpen = false;
            SyncFoodSelectedCounts();
            RecalculateCartPrice();
            StatusBannerText = string.Format("已清空「{0}」购物车口袋", _selectedPoi.Name);
        }

        private void SyncFoodSelectedCounts()
        {
            if (_selectedPoi == null || _selectedPoi.Foods == null)
            {
                return;
            }

            for (int i = 0; i < _selectedPoi.Foods.Count; i++)
            {
                GoodsSpuContract food = _selectedPoi.Foods[i];
                int sum = 0;
                for (int j = 0; j < _activeCartItems.Count; j++)
                {
                    ShopCartItemContract ci = _activeCartItems[j];
                    if (ci.PoiId == _selectedPoi.Id && ci.SpuId == food.Id)
                    {
                        sum += ci.Count;
                    }
                }
                food.SelectedCount = sum;
            }
        }

        private void RecalculateCartPrice()
        {
            if (_selectedPoi == null)
            {
                CartPrice = new ShopCartPriceContract();
                return;
            }

            List<ShopCartItemContract> poiItems = new List<ShopCartItemContract>();
            for (int i = 0; i < _activeCartItems.Count; i++)
            {
                if (_activeCartItems[i].PoiId == _selectedPoi.Id)
                {
                    poiItems.Add(_activeCartItems[i]);
                }
            }

            CartPrice = ShopCartCalculatorEngine.Calculate(_selectedPoi, poiItems, _selectedCoupon);
        }

        /// <summary>
        /// 点击「去结算」：
        /// 1. 若未登录，先拦截并弹出《美团通行证登录页》(OBL-07)。
        /// 2. 若在点菜页，推入《确认订单页 (OrderConfirmActivity)》(OBL-20)。
        /// 3. 若已在确认订单页，弹出《美团收银台 (MTCashierActivity)》或完成建单。
        /// </summary>
        private void OpenOrderConfirmOrSubmit()
        {
            if (_selectedPoi == null || _cartPrice == null)
            {
                return;
            }

            if (!_cartPrice.CanCheckout)
            {
                StatusBannerText = "温馨提示：" + _cartPrice.CheckoutButtonText + "，可继续加购凑单商品";
                return;
            }

            if (!IsUserLoggedIn)
            {
                _pendingCheckoutAfterLogin = true;
                IsLoginViewOpen = true;
                StatusBannerText = "请先登录美团账号，登录成功后将自动进入《确认订单页》";
                return;
            }

            if (!string.Equals(_activeFullScreenPage, "OrderConfirm", StringComparison.Ordinal))
            {
                IsCartDrawerOpen = false;
                ActiveFullScreenPage = "OrderConfirm";
                StatusBannerText = "已进入《确认订单页 (OrderConfirmActivity)》，请核对收货地址与优惠明细";
                return;
            }

            IsCashierModalOpen = true;
        }

        /// <summary>
        /// 在《美团收银台》点击「确认支付」或通过快捷结算直接建单。
        /// </summary>
        public void FinalizeAndCreateOrder()
        {
            if (_selectedPoi == null || _cartPrice == null || !_cartPrice.CanCheckout)
            {
                return;
            }

            List<ShopCartItemContract> poiItems = _activeCartItems
                .Where(delegate(ShopCartItemContract c) { return c.PoiId == _selectedPoi.Id; })
                .ToList();

            if (poiItems.Count == 0)
            {
                return;
            }

            List<OrderFoodItemContract> orderFoods = new List<OrderFoodItemContract>();
            List<string> foodNames = new List<string>();
            int totalProducts = 0;

            for (int i = 0; i < poiItems.Count; i++)
            {
                ShopCartItemContract ci = poiItems[i];
                orderFoods.Add(new OrderFoodItemContract
                {
                    FoodName = ci.FoodName,
                    SpecText = string.Format("{0}号口袋 · {1}", ci.PocketId, ci.SpecSummary),
                    Count = ci.Count,
                    Price = ci.UnitPrice
                });
                foodNames.Add(string.Format("{0}x{1}", ci.FoodName, ci.Count));
                totalProducts += ci.Count;
            }

            string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string shortTime = DateTime.Now.ToString("HH:mm");
            long newOrderId = 8842000000L + (DateTime.UtcNow.Ticks % 899999L);
            string payMethodTitle = _selectedPaymentMethod != null ? _selectedPaymentMethod.Title : "美团月付";
            double payable = CashierFinalPayable;

            string consumedCouponId = null;
            if (_cartPrice.AppliedCouponAmount > 0 && _selectedCoupon != null && !_selectedCoupon.IsUsed)
            {
                consumedCouponId = _selectedCoupon.CouponId;
                _selectedCoupon.IsUsed = true;
            }

            OrderContract newOrder = new OrderContract
            {
                OrderId = newOrderId,
                HashId = "WM" + newOrderId.ToString(),
                PoiId = _selectedPoi.Id,
                PoiIdStr = _selectedPoi.PoiIdStr,
                PoiName = _selectedPoi.Name,
                Status = 30,
                StatusDescription = _isSelfPickupMode ? "到店自取 · 商家备餐中" : "美团专送 · 骑手正赶往商家",
                DeliveryStatus = 2,
                PayStatus = 3,
                OrderTime = nowTime,
                FormatOrderTime = nowTime,
                Total = payable,
                BoxFeeTotal = _cartPrice.TotalBoxPrice,
                ShippingFee = _isSelfPickupMode ? 0.0 : _cartPrice.FinalShippingFee,
                DiscountTotal = _cartPrice.TotalSavedAmount + (_selectedPaymentMethod != null ? _selectedPaymentMethod.InstantDiscount : 0.0),
                ArrivalTimeText = _isSelfPickupMode
                    ? "请凭取餐码 #8842 前往门店自取"
                    : string.Format("预计 {0} 送达 (准时宝保障中)", DateTime.Now.AddMinutes(_selectedPoi.DeliveryMinutes).ToString("HH:mm")),
                RiderName = "张建国 (美团王牌专送)",
                RiderPhone = "138****9527",
                Latitude = _currentAddress != null ? _currentAddress.Latitude : 22539120,
                Longitude = _currentAddress != null ? _currentAddress.Longitude : 113951240,
                RiderProgressRatio = 0.28,
                RecipientName = _currentAddress != null ? _currentAddress.ContactName : "林同学",
                RecipientPhone = _currentAddress != null ? _currentAddress.Phone : "138****0001",
                RecipientAddress = _currentAddress != null ? (_currentAddress.AddressDetail + " " + _currentAddress.HouseNumber) : "深圳市南山区科技园·科苑花园 3栋1202室",
                Caution = string.Format("{0} | {1}{2}",
                    string.IsNullOrEmpty(_orderNoteInput) ? "请放门口外卖柜" : _orderNoteInput,
                    _selectedTablewareOption,
                    _isPrivacyPhoneProtected ? " | 号码保护中" : ""),
                FoodNames = string.Join(" + ", foodNames.ToArray()),
                ProductCount = totalProducts,
                IsComment = false,
                CanRefund = true,
                UsedCouponId = consumedCouponId,
                FoodItems = orderFoods,
                TrackingSteps = new List<RiderTrackingStepContract>
                {
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "订单已提交 (" + payMethodTitle + "支付成功)", Subtitle = "实付 ¥" + payable.ToString("0.00") + "，享受美团准时宝与放心吃保障", IsCompleted = true, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "商家已接单，后厨正在现做", Subtitle = _selectedPoi.Name + " 承诺双层食安封签打包", IsCompleted = true, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "美团专送骑手张建国已接单", Subtitle = "骑手正火速赶往商家取餐，距商家 280m", IsCompleted = false, IsCurrent = true },
                    new RiderTrackingStepContract { TimeText = "--:--", Title = "骑手配送中", Subtitle = "取餐后将沿最优路线直达收货地址", IsCompleted = false, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = "--:--", Title = "预计准时送达", Subtitle = "支持无接触配送至门口或外卖柜", IsCompleted = false, IsCurrent = false }
                },
                ImMessages = new List<ImChatMessageContract>
                {
                    new ImChatMessageContract { SenderRole = "商家", SenderName = _selectedPoi.Name, TimeText = shortTime, Content = "亲，您的订单后厨已经开始现做啦，已按您的备注『" + _orderNoteInput + "』安排！", IsSelf = false },
                    new ImChatMessageContract { SenderRole = "骑手", SenderName = "骑手·张建国", TimeText = shortTime, Content = "您好！我是美团专送骑手张建国，已接单正赶往商家，取完餐第一时间给您送过去！", IsSelf = false }
                },
                SmartReplySuggestions = new List<string>
                {
                    "辛苦了，放前台外卖柜即可",
                    "请帮忙多拿一双筷子和餐巾纸",
                    "路上注意安全，不着急",
                    "到了请给我发消息"
                }
            };

            _orders.Insert(0, newOrder);
            SelectedOrderFilter = "全部";
            RefreshFilteredOrders();
            SelectedOrder = newOrder;

            // 清空当前商家已下单购物车项
            for (int i = _activeCartItems.Count - 1; i >= 0; i--)
            {
                if (_activeCartItems[i].PoiId == _selectedPoi.Id)
                {
                    _activeCartItems.RemoveAt(i);
                }
            }

            IsCashierModalOpen = false;
            IsCartDrawerOpen = false;
            if (!string.IsNullOrEmpty(consumedCouponId))
            {
                ShenQuanCouponContract nextUsable = _coupons.FirstOrDefault(delegate(ShenQuanCouponContract c) { return !c.IsUsed; });
                SelectedCoupon = nextUsable;
                RaisePropertyChanged("Coupons");
                RaisePropertyChanged("SelectedCouponSummary");
                SyncMineProfileWithCurrentUser();
            }
            SyncFoodSelectedCounts();
            RecalculateCartPrice();

            // 更新底栏订单未读角标并跳转至订单轨迹详情页
            for (int i = 0; i < _bottomTabs.Count; i++)
            {
                if (_bottomTabs[i].BusinessId == 2)
                {
                    _bottomTabs[i].UnreadBadgeCount += 1;
                }
            }
            SelectedTabBusinessId = 2;
            ActiveFullScreenPage = "OrderTrack";

            StatusBannerText = string.Format("🎉 支付成功 ({0})！订单号 {1}，实付 ¥{2:0.00}，骑手实时轨迹已开启",
                payMethodTitle, newOrder.HashId, newOrder.Total);

            SaveStateSnapshotFireAndForget();
        }

        private void SubmitOrderReviewInternal()
        {
            if (_selectedOrder == null)
            {
                return;
            }

            _selectedOrder.IsComment = true;
            _selectedOrder.Status = 50;
            _selectedOrder.StatusDescription = "已完成 (已评价)";

            PoiContract targetPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == _selectedOrder.PoiId; });
            if (targetPoi != null)
            {
                if (targetPoi.Reviews == null)
                {
                    targetPoi.Reviews = new List<PoiReviewItemContract>();
                }
                string stars = new string('★', _reviewRatingStars) + new string('☆', 5 - _reviewRatingStars);
                PoiReviewItemContract newReview = new PoiReviewItemContract
                {
                    ReviewId = "rev-user-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                    PoiId = targetPoi.Id,
                    UserName = IsUserLoggedIn ? _currentUser.Nickname : "美团匿名食客",
                    AvatarEmoji = IsUserLoggedIn ? (_currentUser.AvatarEmoji ?? "🦘") : "🙂",
                    VipLevelText = IsUserLoggedIn ? _currentUser.VipBadgeText : "美团会员",
                    TimeText = "刚刚",
                    Score = _reviewRatingStars,
                    StarText = string.Format("{0} {1:0.0}", stars, (double)_reviewRatingStars),
                    DeliveryTimeTip = "准时送达",
                    OrderedDishesText = _selectedOrder.FoodNames,
                    Content = string.IsNullOrWhiteSpace(_reviewCommentInput) ? "味道超赞，送餐很快，下次还会回购！" : _reviewCommentInput.Trim(),
                    MerchantReply = "商家回复：感谢您的真实晒单评价！祝您生活愉快~",
                    HasPictures = true,
                    PictureBadgeText = "📸 顾客实拍晒单"
                };
                targetPoi.Reviews.Insert(0, newReview);
                if (ReferenceEquals(_selectedPoi, targetPoi))
                {
                    RefreshFilteredPoiReviews();
                    RaisePropertyChanged("SelectedPoiReviewCountText");
                }
            }

            IsReviewOrderModalOpen = false;
            RefreshFilteredOrders();
            RaisePropertyChanged("SelectedOrder");
            StatusBannerText = "🌟 评价发布成功！已奖励 +50 团币，您的评价已同步展示在该门店『评价』专区";
            SaveStateSnapshotFireAndForget();
        }

        private void ApplyFlashDealInternal(PromoFlashDealContract deal)
        {
            PoiContract targetPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == deal.TargetPoiId; });
            if (targetPoi == null && _allPois.Count > 0)
            {
                targetPoi = _allPois[0];
            }

            if (targetPoi != null)
            {
                SelectedPoi = targetPoi;
                GoodsSpuContract targetFood = null;
                if (targetPoi.Foods != null)
                {
                    targetFood = targetPoi.Foods.FirstOrDefault(delegate(GoodsSpuContract f) { return f.Id == deal.TargetSpuId; });
                    if (targetFood == null && targetPoi.Foods.Count > 0)
                    {
                        targetFood = targetPoi.Foods[0];
                    }
                }

                if (targetFood != null)
                {
                    AddFoodToCartInternal(targetFood, null, "神抢手爆品特惠");
                }

                SelectedTabBusinessId = 1;
                ActiveFullScreenPage = "PoiDetail";
                StatusBannerText = string.Format("已抢购神抢手爆品「{0}」({1})，已自动加入购物车！", deal.Title, deal.DiscountBadge);
            }
        }

        private void ApplyDeepEatRecommendationInternal(DeepEatRecommendationContract rec)
        {
            PoiContract targetPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == rec.TargetPoiId; });
            if (targetPoi == null && _allPois.Count > 0)
            {
                targetPoi = _allPois[0];
            }

            if (targetPoi != null)
            {
                SelectedPoi = targetPoi;
                if (targetPoi.Foods != null && targetPoi.Foods.Count > 0)
                {
                    GoodsSpuContract food = targetPoi.Foods.FirstOrDefault(delegate(GoodsSpuContract f) { return f.Id == rec.TargetSpuId; })
                                            ?? targetPoi.Foods[0];
                    AddFoodToCartInternal(food, null, "小美AI优选");
                }

                SelectedTabBusinessId = 1;
                ActiveFullScreenPage = "PoiDetail";
                StatusBannerText = string.Format("小美 AI 已为您直达「{0}」并加购推荐餐品：{1}", targetPoi.Name, rec.SceneTitle);
            }
        }

        private void GenerateDeepEatAiResponse()
        {
            string q = (_deepEatQueryText ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(q))
            {
                q = "推荐高性价比工作餐";
            }

            PoiContract bestPoi = _allPois.FirstOrDefault();
            if (q.IndexOf("辣", StringComparison.OrdinalIgnoreCase) >= 0 ||
                q.IndexOf("夜宵", StringComparison.OrdinalIgnoreCase) >= 0 ||
                q.IndexOf("冒菜", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == 1003; }) ?? bestPoi;
            }
            else if (q.IndexOf("咖啡", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("茶", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("下午", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == 1005; }) ?? bestPoi;
            }
            else if (q.IndexOf("轻食", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("沙拉", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("不胖", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Id == 1004; }) ?? bestPoi;
            }

            if (bestPoi != null)
            {
                GoodsSpuContract dish = (bestPoi.Foods != null && bestPoi.Foods.Count > 0) ? bestPoi.Foods[0] : null;
                string dishName = dish != null ? dish.Name : "招牌必点套餐";
                double dishPrice = dish != null ? dish.MinPrice : 26.8;
                DeepEatResponseText = string.Format(
                    "【小美 DeepEat 智能助餐】针对您的需求「{0}」，已从附近高分商家中匹配「{1}」（评分 {2} · {3}）。强烈推荐『{4}』(¥{5:0.0})，叠加神券膨胀与满减后性价比极高！点击下方卡片可一键加购。",
                    q, bestPoi.Name, bestPoi.FormattedPoiScore, bestPoi.MtDeliveryTime, dishName, dishPrice);
            }
        }

        private void SendImChatMessageInternal(string content)
        {
            string trimmed = (content ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(trimmed) || _selectedOrder == null)
            {
                return;
            }

            if (_selectedOrder.ImMessages == null)
            {
                _selectedOrder.ImMessages = new List<ImChatMessageContract>();
            }

            string nowShort = DateTime.Now.ToString("HH:mm");
            _selectedOrder.ImMessages.Add(new ImChatMessageContract
            {
                SenderRole = "顾客",
                SenderName = "我",
                TimeText = nowShort,
                Content = trimmed,
                IsSelf = true
            });

            string autoReply;
            string replyRole = "骑手";
            string replySender = "骑手·" + (_selectedOrder.RiderName ?? "张师傅");
            if (trimmed.IndexOf("筷", StringComparison.OrdinalIgnoreCase) >= 0 ||
                trimmed.IndexOf("辣", StringComparison.OrdinalIgnoreCase) >= 0 ||
                trimmed.IndexOf("餐具", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                replyRole = "商家";
                replySender = _selectedOrder.PoiName;
                autoReply = "收到啦亲！已经在打包袋里为您额外备好餐具和纸巾，请放心用餐~";
            }
            else
            {
                autoReply = "收到！我一定按您的要求办好，预计准时送达，请保持电话畅通~";
            }

            _selectedOrder.ImMessages.Add(new ImChatMessageContract
            {
                SenderRole = replyRole,
                SenderName = replySender,
                TimeText = nowShort,
                Content = autoReply,
                IsSelf = false
            });

            OrderContract current = _selectedOrder;
            _selectedOrder = null;
            RaisePropertyChanged("SelectedOrder");
            _selectedOrder = current;
            RaisePropertyChanged("SelectedOrder");

            StatusBannerText = string.Format("已在订单三方群聊发送消息，{0}已即时响应", replySender);
        }

        private void OnRiderTrackingTimerTick(object sender, EventArgs e)
        {
            if (_isDisposed || !_isWindowVisible || !_isRiderSimulationActive)
            {
                return;
            }

            if (_smsCountdownSeconds > 0)
            {
                _smsCountdownSeconds = Math.Max(0, _smsCountdownSeconds - 2);
                RaisePropertyChanged("SmsSendButtonText");
            }

            if (_isCashierModalOpen && _cashierRemainingSeconds > 0)
            {
                AdvanceCashierCountdown(2);
            }

            if (_selectedOrder == null || (_selectedOrder.Status != 4 && _selectedOrder.Status != 30))
            {
                return;
            }

            AdvanceSelectedOrderRiderProgress(0.03);
        }

        public void AdvanceSelectedOrderRiderProgress(double delta)
        {
            if (_selectedOrder == null)
            {
                return;
            }

            double next = _selectedOrder.RiderProgressRatio + delta;
            if (next >= 1.0)
            {
                next = 0.20;
            }

            _selectedOrder.RiderProgressRatio = Math.Round(next, 2);
            _selectedOrder.Latitude += 120;
            _selectedOrder.Longitude += 160;

            if (_selectedOrder.TrackingSteps != null && _selectedOrder.TrackingSteps.Count >= 4)
            {
                int lastIdx = _selectedOrder.TrackingSteps.Count - 1;
                int activeStepIndex = Math.Min(2, lastIdx);
                if (next >= 0.85)
                {
                    activeStepIndex = lastIdx;
                    _selectedOrder.StatusDescription = "骑手即将到达收货点 (距您 85m)";
                }
                else if (next >= 0.45)
                {
                    activeStepIndex = Math.Min(2, lastIdx);
                    _selectedOrder.StatusDescription = "美团专送骑手正在火速配送中";
                }
                else
                {
                    activeStepIndex = Math.Min(1, lastIdx);
                    _selectedOrder.StatusDescription = "美团专送骑手已到店取餐";
                }

                for (int i = 0; i < _selectedOrder.TrackingSteps.Count; i++)
                {
                    _selectedOrder.TrackingSteps[i].IsCompleted = i < activeStepIndex;
                    _selectedOrder.TrackingSteps[i].IsCurrent = i == activeStepIndex;
                }
            }

            RaisePropertyChanged("SelectedOrder");
            RaisePropertyChanged("SelectedOrderProgressPercent");
            RaisePropertyChanged("SelectedOrderCoordinatesText");
        }

        /// <summary>
        /// 窗口最小化或隐藏时暂停后台定时器（遵守 wpf-dotnet-performance）。
        /// </summary>
        public void PauseBackgroundTimers()
        {
            _isWindowVisible = false;
            if (_riderTrackingTimer != null && _riderTrackingTimer.IsEnabled)
            {
                _riderTrackingTimer.Stop();
            }
        }

        /// <summary>
        /// 窗口恢复可见时重启后台定时器。
        /// </summary>
        public void ResumeBackgroundTimers()
        {
            if (_isDisposed)
            {
                return;
            }
            _isWindowVisible = true;
            if (_riderTrackingTimer != null && !_riderTrackingTimer.IsEnabled)
            {
                _riderTrackingTimer.Start();
            }
        }

        private async void SaveStateSnapshotFireAndForget()
        {
            try
            {
                WaimaiPersistedSnapshot snapshot = new WaimaiPersistedSnapshot
                {
                    SavedAtUtc = DateTime.UtcNow.ToString("o"),
                    CurrentAddressId = _currentAddress != null ? _currentAddress.AddressId : string.Empty,
                    IsEasyGoDualPane = _isEasyGoDualPane,
                    CurrentUser = _currentUser,
                    FavoritePoiIds = _allPois.Where(delegate(PoiContract p) { return p.IsFavorite; }).Select(delegate(PoiContract p) { return p.Id; }).ToList(),
                    Notifications = _notifications.ToList(),
                    CartItems = _activeCartItems.ToList(),
                    Orders = _orders.ToList(),
                    Coupons = _coupons.ToList(),
                    Addresses = _addresses.ToList()
                };
                await WaimaiStorageService.SaveSnapshotAsync(snapshot, null);
            }
            catch
            {
                // 忽略后台持久化非致命异常
            }
        }

        private static int ConvertToInt(object value, int fallback)
        {
            if (value == null)
            {
                return fallback;
            }
            if (value is int)
            {
                return (int)value;
            }
            int parsed;
            if (int.TryParse(value.ToString(), out parsed))
            {
                return parsed;
            }
            return fallback;
        }

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            _isDisposed = true;
            if (_riderTrackingTimer != null)
            {
                _riderTrackingTimer.Stop();
                _riderTrackingTimer.Tick -= OnRiderTrackingTimerTick;
            }
        }
    }
}
