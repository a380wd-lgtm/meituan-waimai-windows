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
                if (! ReferenceEquals(_iconSource, value))
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
    /// 美团外卖 Windows 1:1 桌面重构版主视图模型。
    /// 对齐 APK v8.84.2 (build 91293) 核心状态机：
    /// 1. 底部 5 大动态导航 Tab（首页外卖 / 神券膨胀 / 神抢手 / 订单轨迹与IM / 我的美团）。
    /// 2. EasyGo 平行视界双栏大屏模式与手机 1:1 单栏模式无缝切换。
    /// 3. 门店筛选、排序、多规格 SKU 弹窗、多口袋购物车引擎、一键下单、骑手 GPS 实时轨迹模拟与三方 IM 群聊。
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
        private ObservableCollection<PoiContract> _filteredPois;
        private ObservableCollection<string> _currentMenuCategories;
        private ObservableCollection<GoodsSpuContract> _filteredFoods;
        private ObservableCollection<ShopCartItemContract> _activeCartItems;
        private ObservableCollection<ShenQuanCouponContract> _coupons;
        private ObservableCollection<PromoFlashDealContract> _flashDeals;
        private ObservableCollection<DeepEatRecommendationContract> _deepEatRecommendations;
        private ObservableCollection<OrderContract> _orders;
        private ObservableCollection<WmAddressContract> _addresses;
        private ObservableCollection<SelfTestItem> _selfTestItems;

        private int _selectedTabBusinessId;
        private bool _isEasyGoDualPane;
        private bool _isMobileRestaurantDetailOpen;
        private string _searchKeyword;
        private int _selectedKingKongCategory;
        private string _selectedSortMode;
        private PoiContract _selectedPoi;
        private string _selectedMenuCategory;
        private string _selectedPoiSubTab;
        private int _selectedPocketId;
        private bool _isCartDrawerOpen;

        // 多规格弹窗状态
        private bool _isSpecModalOpen;
        private GoodsSpuContract _specModalFood;
        private GoodsSkuContract _specModalSelectedSku;

        // 购物车价格结算快照
        private ShopCartPriceContract _cartPrice;
        private ShenQuanCouponContract _selectedCoupon;

        // 订单与 IM 状态
        private OrderContract _selectedOrder;
        private string _imDraftText;
        private bool _isRiderSimulationActive;

        // 地址与个人中心状态
        private WmAddressContract _currentAddress;
        private MineProfileContract _mineProfile;
        private EasyGoMagicWindowContract _easyGoContract;

        // DeepEat AI 助手对话框状态
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
            _filteredPois = new ObservableCollection<PoiContract>();
            _currentMenuCategories = new ObservableCollection<string>();
            _filteredFoods = new ObservableCollection<GoodsSpuContract>();
            _activeCartItems = new ObservableCollection<ShopCartItemContract>();
            _coupons = new ObservableCollection<ShenQuanCouponContract>();
            _flashDeals = new ObservableCollection<PromoFlashDealContract>();
            _deepEatRecommendations = new ObservableCollection<DeepEatRecommendationContract>();
            _orders = new ObservableCollection<OrderContract>();
            _addresses = new ObservableCollection<WmAddressContract>();
            _selfTestItems = new ObservableCollection<SelfTestItem>();

            _selectedTabBusinessId = 1;
            _isEasyGoDualPane = true;
            _isMobileRestaurantDetailOpen = false;
            _searchKeyword = string.Empty;
            _selectedKingKongCategory = 0;
            _selectedSortMode = "综合排序";
            _selectedMenuCategory = "全部菜单";
            _selectedPoiSubTab = "点菜";
            _selectedPocketId = 1;
            _isCartDrawerOpen = false;
            _isSpecModalOpen = false;
            _isDeepEatDrawerOpen = false;
            _imDraftText = string.Empty;
            _deepEatQueryText = "今晚加班想吃点热乎又不胖的晚餐，预算35元以内";
            _deepEatResponseText = "小美 DeepEat AI 推荐：已为您匹配「老乡鸡·农家蒸菜」招牌肥西老母鸡汤套餐，低脂高蛋白，搭配膨胀神券立减 ¥8，预计 28 分钟送达！";
            _statusBannerText = string.Format(
                "已加载官方 APK v{0} (build {1}) 逆向契约 · EasyGo 平行视界双栏模式已就绪",
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

        public ObservableCollection<PoiContract> FilteredPois
        {
            get { return _filteredPois; }
        }

        public ObservableCollection<string> CurrentMenuCategories
        {
            get { return _currentMenuCategories; }
        }

        public ObservableCollection<GoodsSpuContract> FilteredFoods
        {
            get { return _filteredFoods; }
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

        public ObservableCollection<WmAddressContract> Addresses
        {
            get { return _addresses; }
        }

        public ObservableCollection<SelfTestItem> SelfTestItems
        {
            get { return _selfTestItems; }
        }

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
                    ? "EasyGo 平行视界 (双栏)"
                    : "手机 1:1 单栏视图";
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
            get { return !_isEasyGoDualPane && _isMobileRestaurantDetailOpen; }
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
                    OnSelectedPoiChanged();
                }
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
                return string.Format("已选：{0}{1}  |  ¥{2:0.00} (放入{3}号口袋)", skuName, attrPart, price, _selectedPocketId);
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
                    return "E 116.4810°  N 40.0016°";
                }
                double lat = _selectedOrder.Latitude / 1000000.0;
                double lng = _selectedOrder.Longitude / 1000000.0;
                return string.Format("实时高德/美团骑手坐标：N {0:0.0000}° · E {1:0.0000}° ({2}%)",
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
                }
            }
        }

        public string CurrentAddressDisplayText
        {
            get
            {
                if (_currentAddress == null)
                {
                    return "北京市朝阳区望京恒电大厦 B座";
                }
                return string.Format("{0} ({1})", _currentAddress.AddressDetail, _currentAddress.TagLabel);
            }
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

        public ICommand SwitchTabCommand { get; private set; }
        public ICommand ToggleLayoutModeCommand { get; private set; }
        public ICommand BackToMobileFeedCommand { get; private set; }
        public ICommand SelectKingKongCommand { get; private set; }
        public ICommand ResetFilterCommand { get; private set; }
        public ICommand ChangeSortModeCommand { get; private set; }
        public ICommand SelectPoiCommand { get; private set; }
        public ICommand SelectMenuCategoryCommand { get; private set; }
        public ICommand SelectPoiSubTabCommand { get; private set; }
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
        public ICommand InflateCouponCommand { get; private set; }
        public ICommand ApplyCouponCommand { get; private set; }
        public ICommand CheckoutOrderCommand { get; private set; }
        public ICommand ApplyFlashDealCommand { get; private set; }
        public ICommand ApplyDeepEatCommand { get; private set; }
        public ICommand ToggleDeepEatDrawerCommand { get; private set; }
        public ICommand AskDeepEatAiCommand { get; private set; }
        public ICommand SelectOrderCommand { get; private set; }
        public ICommand AdvanceRiderStepCommand { get; private set; }
        public ICommand SendImMessageCommand { get; private set; }
        public ICommand SendSmartReplyCommand { get; private set; }
        public ICommand CycleAddressCommand { get; private set; }
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
                    ? "已切换至 EasyGo 平行视界双栏模式（对齐 APK assets/easygo.json magicwindow 契约）"
                    : "已切换至手机 1:1 单栏视图模式（390px 经典黄金移动端比例）";
                SaveStateSnapshotFireAndForget();
            });

            BackToMobileFeedCommand = new RelayCommand(delegate
            {
                IsMobileRestaurantDetailOpen = false;
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
                    StatusBannerText = string.Format("商家列表已按「{0}」重新重排", mode);
                }
            });

            SelectPoiCommand = new RelayCommand(delegate(object param)
            {
                PoiContract poi = param as PoiContract;
                if (poi != null)
                {
                    SelectedPoi = poi;
                    if (!IsEasyGoDualPane)
                    {
                        IsMobileRestaurantDetailOpen = true;
                    }
                    StatusBannerText = string.Format("正在浏览「{0}」· 评分 {1} · {2}",
                        poi.Name, poi.FormattedPoiScore, poi.MtDeliveryTime);
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
                    StatusBannerText = string.Format("已选用神券「{0}」(减¥{1:0.##})，快去挑选美食吧",
                        coupon.Title, coupon.Amount);
                }
            });

            CheckoutOrderCommand = new RelayCommand(delegate
            {
                SubmitCheckoutOrder();
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

            SelectOrderCommand = new RelayCommand(delegate(object param)
            {
                OrderContract order = param as OrderContract;
                if (order != null)
                {
                    SelectedOrder = order;
                    StatusBannerText = string.Format("已切换至订单「{0}」· {1}", order.PoiName, order.StatusDescription);
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
            string[] safeTabGlyphs = new string[] { "★", "❖", "⚡", "≡", "☺" };
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

            if (_assetLoader != null)
            {
                LauncherIcon = _assetLoader.TryGetFrozenIcon("wm_ic_launcher.png", 128);
                MascotIcon = _assetLoader.TryGetFrozenIcon("wm_kangaroo_mascot.png", 128);
            }

            _allPois = WaimaiSeedDataFactory.CreateDefaultPois();

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

            _mineProfile = WaimaiSeedDataFactory.CreateDefaultMineProfile();

            RefreshFilteredPois();
            if (_filteredPois.Count > 0)
            {
                SelectedPoi = _filteredPois[0];
            }

            // 默认预加购第一家门店招牌菜，开箱即可看到完整满减与凑单结算条
            if (_selectedPoi != null && _selectedPoi.Foods != null && _selectedPoi.Foods.Count > 0)
            {
                AddFoodToCartInternal(_selectedPoi.Foods[0], null, null);
            }
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
        /// 从 DPAPI 解密后的持久化快照恢复用户视图状态、地址簿、神券钱包、购物车与历史订单。
        /// 支持空集合精确还原并同步清理选中项。
        /// </summary>
        public void ApplyPersistedSnapshot(WaimaiPersistedSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            IsEasyGoDualPane = snapshot.IsEasyGoDualPane;

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
                    return p.BuzType == targetCat;
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
            else if (string.Equals(_selectedSortMode, "速度最快", StringComparison.Ordinal))
            {
                query = query.OrderBy(delegate(PoiContract p) { return p.DeliveryMinutes; });
            }
            else if (string.Equals(_selectedSortMode, "评分最高", StringComparison.Ordinal))
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
                // 触发 ObservableCollection 刷新
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

        private void SubmitCheckoutOrder()
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
                    Price = ci.LineTotal
                });
                foodNames.Add(string.Format("{0}x{1}", ci.FoodName, ci.Count));
                totalProducts += ci.Count;
            }

            string nowTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            string shortTime = DateTime.Now.ToString("HH:mm");
            long newOrderId = 8842000000L + (DateTime.UtcNow.Ticks % 899999L);

            OrderContract newOrder = new OrderContract
            {
                OrderId = newOrderId,
                HashId = "WM" + newOrderId.ToString(),
                PoiId = _selectedPoi.Id,
                PoiIdStr = _selectedPoi.PoiIdStr,
                PoiName = _selectedPoi.Name,
                Status = 4,
                StatusDescription = "美团专送骑手正赶往商家",
                DeliveryStatus = 2,
                PayStatus = 3,
                OrderTime = nowTime,
                FormatOrderTime = nowTime,
                Total = _cartPrice.TotalDiscountedAndBoxPrice,
                BoxFeeTotal = _cartPrice.TotalBoxPrice,
                ShippingFee = _cartPrice.FinalShippingFee,
                DiscountTotal = _cartPrice.TotalSavedAmount,
                ArrivalTimeText = string.Format("预计 {0} 送达 (准时宝保障中)", DateTime.Now.AddMinutes(_selectedPoi.DeliveryMinutes).ToString("HH:mm")),
                RiderName = "张建国 (美团王牌专送)",
                RiderPhone = "138****9527",
                Latitude = 40001820,
                Longitude = 116480910,
                RiderProgressRatio = 0.25,
                RecipientName = _currentAddress != null ? _currentAddress.ContactName : "陈先生",
                RecipientPhone = _currentAddress != null ? _currentAddress.Phone : "139****8801",
                RecipientAddress = _currentAddress != null ? (_currentAddress.AddressDetail + " " + _currentAddress.HouseNumber) : "望京恒电大厦 B座 18层",
                Caution = "请放前台外卖柜，谢谢骑手师傅！",
                FoodNames = string.Join(" + ", foodNames.ToArray()),
                ProductCount = totalProducts,
                IsComment = false,
                CanRefund = true,
                FoodItems = orderFoods,
                TrackingSteps = new List<RiderTrackingStepContract>
                {
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "订单已提交并支付成功", Subtitle = "微信支付已扣款，享受美团准时宝保障", IsCompleted = true, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "商家已接单，后厨正在现做", Subtitle = _selectedPoi.Name + " 承诺食材新鲜现制", IsCompleted = true, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = shortTime, Title = "美团专送骑手张建国已接单", Subtitle = "骑手正火速赶往商家取餐，距商家 320m", IsCompleted = false, IsCurrent = true },
                    new RiderTrackingStepContract { TimeText = "--:--", Title = "骑手配送中", Subtitle = "取餐后将沿最优路线直达收货地址", IsCompleted = false, IsCurrent = false },
                    new RiderTrackingStepContract { TimeText = "--:--", Title = "预计准时送达", Subtitle = "支持无接触配送至外卖柜", IsCompleted = false, IsCurrent = false }
                },
                ImMessages = new List<ImChatMessageContract>
                {
                    new ImChatMessageContract { SenderRole = "商家", SenderName = _selectedPoi.Name, TimeText = shortTime, Content = "亲，您的订单后厨已经开始现做啦，打包袋已做双层保温密封！", IsSelf = false },
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
            SelectedOrder = newOrder;

            // 清空当前商家已下单购物车项
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

            // 更新底栏订单未读角标并跳转至订单中心
            for (int i = 0; i < _bottomTabs.Count; i++)
            {
                if (_bottomTabs[i].BusinessId == 2)
                {
                    _bottomTabs[i].UnreadBadgeCount += 1;
                }
            }
            SelectedTabBusinessId = 2;

            StatusBannerText = string.Format("下单成功！订单号 {0}，实付 ¥{1:0.00}（已省 ¥{2:0.00}），骑手实时轨迹已开启",
                newOrder.HashId, newOrder.Total, newOrder.DiscountTotal);

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
                if (!IsEasyGoDualPane)
                {
                    IsMobileRestaurantDetailOpen = true;
                }
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
                if (!IsEasyGoDualPane)
                {
                    IsMobileRestaurantDetailOpen = true;
                }
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
                q.IndexOf("汉堡", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Name.IndexOf("塔斯汀", StringComparison.OrdinalIgnoreCase) >= 0; }) ?? bestPoi;
            }
            else if (q.IndexOf("咖啡", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("茶", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("下午", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Name.IndexOf("霸王茶姬", StringComparison.OrdinalIgnoreCase) >= 0; }) ?? bestPoi;
            }
            else if (q.IndexOf("面", StringComparison.OrdinalIgnoreCase) >= 0 ||
                     q.IndexOf("饺", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                bestPoi = _allPois.FirstOrDefault(delegate(PoiContract p) { return p.Name.IndexOf("和府捞面", StringComparison.OrdinalIgnoreCase) >= 0; }) ?? bestPoi;
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

            // 智能模拟骑手或商家实时回复
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

            // 触发绑定刷新
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
                next = 0.20; // 循环演示实时配送轨迹
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
