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
    /// 美团外卖 Windows 桌面重构版主窗口。
    /// 严格遵循 wpf-dotnet-performance 与 exhaustive-code-audit 规范：
    /// 1. 窗口最小化/隐藏时自动暂停后台 DispatcherTimer，恢复时唤醒，关闭时 Dispose。
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
        /// 自动切换 5 大核心业务状态并导出高清 PNG 视觉验证截图。
        /// UI 线程仅将可视树渲染至内存字节数组，磁盘落盘全部交由 Task.Run 后台执行。
        /// </summary>
        public async Task RenderAllScreenshotsAsync(string outputDirectory)
        {
            await _viewModel.InitializeAsync(false);
            await WaitForUiIdleAsync();

            // 1. 首页 + 门店点餐 EasyGo 平行视界双栏视图
            _viewModel.SelectedTabBusinessId = 1;
            _viewModel.IsEasyGoDualPane = true;
            _viewModel.IsSpecModalOpen = false;
            _viewModel.IsCartDrawerOpen = false;
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "01-home-easygo-dualpane.png"));

            // 2. 菜品多规格 SKU 与属性选择弹窗视图
            if (_viewModel.SelectedPoi != null &&
                _viewModel.SelectedPoi.Foods != null &&
                _viewModel.SelectedPoi.Foods.Count > 0)
            {
                _viewModel.OpenSpecModalCommand.Execute(_viewModel.SelectedPoi.Foods[0]);
            }
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "02-spec-modal-and-cart.png"));
            _viewModel.IsSpecModalOpen = false;

            // 3. 神券会员 · 天天膨胀专区视图
            _viewModel.SelectedTabBusinessId = 9;
            _viewModel.InflateCouponCommand.Execute(null);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "03-shenquan-coupon-inflate.png"));

            // 4. 订单中心 · 骑手实时地图轨迹 + 骑手/商家三方 IM 群聊视图
            _viewModel.SelectedTabBusinessId = 2;
            _viewModel.AdvanceSelectedOrderRiderProgress(0.18);
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "04-order-rider-tracking-im.png"));

            // 5. 我的美团 · 个人资产 + 地址簿 + REA 15+ 项逆向契约自检报告视图
            _viewModel.SelectedTabBusinessId = 3;
            await WaitForUiIdleAsync();
            await CaptureCurrentViewToPngAsync(Path.Combine(outputDirectory, "05-mine-and-rea-selftest.png"));
        }

        private async Task CaptureCurrentViewToPngAsync(string targetFilePath)
        {
            FrameworkElement targetElement = RootViewportGrid as FrameworkElement ?? this;
            int width = (int)Math.Max(1360, targetElement.ActualWidth > 100 ? targetElement.ActualWidth : Width);
            int height = (int)Math.Max(860, targetElement.ActualHeight > 100 ? targetElement.ActualHeight : Height);

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
