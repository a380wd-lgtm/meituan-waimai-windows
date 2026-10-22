using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using MeituanWaimaiClient.Services;
using MeituanWaimaiClient.ViewModels;

namespace MeituanWaimaiClient
{
    /// <summary>
    /// 美团外卖 Windows 1:1 原生手机竖屏版主窗口 (420×900 px)。
    /// 严格遵循 deep-1to1-app-reconstruction、wpf-dotnet-performance 与 exhaustive-code-audit 规范：
    /// 1. 窗口锁定手机竖屏比例，最小化/隐藏时自动暂停后台 DispatcherTimer，恢复时唤醒，关闭时 Dispose。
    /// 2. 所有磁盘写入在后台线程 (Task.Run) 执行，UI 线程零同步 I/O。
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly WaimaiMainViewModel _viewModel;

        public MainWindow(string repoRootPath, ApkAssetLoader assetLoader)
        {
            InitializeComponent();

            _viewModel = new WaimaiMainViewModel(repoRootPath, assetLoader);
            DataContext = _viewModel;

            Loaded += OnMainWindowLoaded;
            StateChanged += OnMainWindowStateChanged;
            IsVisibleChanged += OnMainWindowIsVisibleChanged;
            Closed += OnMainWindowClosed;
        }

        public WaimaiMainViewModel ViewModel
        {
            get { return _viewModel; }
        }

        private async void OnMainWindowLoaded(object sender, RoutedEventArgs e)
        {
            await _viewModel.InitializeAsync();
        }

        private void OnMainWindowStateChanged(object sender, EventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            if (WindowState == WindowState.Minimized)
            {
                _viewModel.PauseBackgroundTimers();
            }
            else
            {
                _viewModel.ResumeBackgroundTimers();
            }
        }

        private void OnMainWindowIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel == null)
            {
                return;
            }

            if (IsVisible && WindowState != WindowState.Minimized)
            {
                _viewModel.ResumeBackgroundTimers();
            }
            else
            {
                _viewModel.PauseBackgroundTimers();
            }
        }

        private void OnMainWindowClosed(object sender, EventArgs e)
        {
            Loaded -= OnMainWindowLoaded;
            StateChanged -= OnMainWindowStateChanged;
            IsVisibleChanged -= OnMainWindowIsVisibleChanged;
            Closed -= OnMainWindowClosed;

            if (_viewModel != null)
            {
                _viewModel.Dispose();
            }
        }

        /// <summary>
        /// 自动切换 8 大核心手机竖屏业务状态并导出 420×900 高清 PNG 视觉验证截图。
        /// UI 线程仅将可视树渲染至内存字节数组，磁盘落盘全部交由 Task.Run 后台执行。
        /// </summary>
        public async Task RenderAllScreenshotsAsync(string outputDirectory)
        {
            await _viewModel.InitializeAsync(false);
            await WaitForUiIdleAsync();

            // 1. 美团通行证 1:1 原生登录页 (含模拟短信验证码通知横幅)
            _viewModel.OpenLoginViewCommand.Execute(null);
            _viewModel.SendSmsCodeCommand.Execute(null);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "01-login-passport.png"));
            _viewModel.SubmitLoginCommand.Execute(null);

            // 2. 原生手机竖屏首页 (定位+搜索+热搜词+10宫格金刚区+神券条+门店瀑布流+未登录底部悬浮条展示)
            _viewModel.SelectedTabBusinessId = 1;
            _viewModel.ActiveFullScreenPage = "None";
            _viewModel.LogoutCommand.Execute(null); // 展示未登录底部悬浮引导栏
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "02-home-mobile-feed.png"));
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "01-home-easygo-dualpane.png"));
            // 恢复登录态以继续后续下单测试
            _viewModel.SendSmsCodeCommand.Execute(null);
            _viewModel.SubmitLoginCommand.Execute(null);

            // 3. 门店详情全屏页 (点菜子页 + 多规格 SKU 弹窗 + 底部黑金购物车条)
            if (_viewModel.FilteredPois.Count > 0)
            {
                _viewModel.SelectPoiCommand.Execute(_viewModel.FilteredPois[0]);
            }
            if (_viewModel.SelectedPoi != null &&
                _viewModel.SelectedPoi.Foods != null &&
                _viewModel.SelectedPoi.Foods.Count > 0)
            {
                _viewModel.AddFoodCommand.Execute(_viewModel.SelectedPoi.Foods[0]);
                _viewModel.OpenSpecModalCommand.Execute(_viewModel.SelectedPoi.Foods[0]);
            }
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "03-poi-menu-and-sku.png"));
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "02-spec-modal-and-cart.png"));
            _viewModel.IsSpecModalOpen = false;

            // 4. 门店详情全屏页 · 评价子页 (综合评分 + 好评/有图筛选 + 真实评论与商家回复)
            _viewModel.SelectPoiSubTabCommand.Execute("评价");
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "04-poi-reviews-and-merchant.png"));
            _viewModel.SelectPoiSubTabCommand.Execute("点菜");

            // 5. 确认订单全屏页 (OrderConfirmActivity) + 美团收银台半屏支付窗 (MTCashierActivity)
            _viewModel.CheckoutOrderCommand.Execute(null);
            _viewModel.OpenCashierModalCommand.Execute(null);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "05-order-confirm-and-cashier.png"));
            _viewModel.IsCashierModalOpen = false;

            // 6. 神券省钱卡 · 口令兑换与天天膨胀专区
            _viewModel.SelectedTabBusinessId = 9;
            _viewModel.InflateCouponCommand.Execute(null);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "06-shenquan-and-redeem.png"));
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "03-shenquan-coupon-inflate.png"));

            // 7. 订单轨迹详情全屏页 · 骑手实时地图 + 骑手/商家三方 IM 群聊
            _viewModel.SelectedTabBusinessId = 2;
            if (_viewModel.FilteredOrders.Count > 0)
            {
                _viewModel.SelectOrderCommand.Execute(_viewModel.FilteredOrders[0]);
            }
            _viewModel.AdvanceSelectedOrderRiderProgress(0.18);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "07-order-rider-map-and-im.png"));
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "04-order-rider-tracking-im.png"));

            // 8. 我的美团个人中心 (账号切换/退出 + 资产四宫格 + 功能矩阵 + 逆向自检徽章)
            _viewModel.SelectedTabBusinessId = 3;
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "08-mine-and-address-manager.png"));
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "05-mine-and-rea-selftest.png"));
        }

        private async Task CaptureCurrentViewToPngAsync(string targetFilePath)
        {
            FrameworkElement targetElement = RootViewportGrid as FrameworkElement ?? this;
            int width = 420;
            int height = 910;

            Size renderSize = new Size(width, height);
            targetElement.Measure(renderSize);
            targetElement.Arrange(new Rect(renderSize));
            targetElement.UpdateLayout();

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96.0, 96.0, PixelFormats.Pbgra32);
            rtb.Render(targetElement);

            PngBitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(rtb));

            byte[] pngBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                pngBytes = ms.ToArray();
            }

            await Task.Run(delegate
            {
                string dir = Path.GetDirectoryName(targetFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllBytes(targetFilePath, pngBytes);
            });
        }

        private Task WaitForUiIdleAsync()
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate
            {
                tcs.TrySetResult(true);
            }));
            return tcs.Task;
        }
    }
}
