using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using MeituanWaimai.Core;
using MeituanWaimaiClient.Services;
using MeituanWaimaiClient.ViewModels;

namespace MeituanWaimaiClient
{
    /// <summary>
    /// 美团外卖 Windows 1:1 原生手机竖屏版应用程序入口。
    /// 支持交互模式、--selftest 自动化契约与 35 项 OBLIGATIONS 状态机自检模式、--render-screenshots 高清多视图截图导出模式。
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string[] args = e != null && e.Args != null ? e.Args : new string[0];
            bool isSelfTestMode = HasFlag(args, "--selftest");
            bool isRenderScreenshotsMode = HasFlag(args, "--render-screenshots");

            string repoRoot = await ResolveRepositoryRootAsync();
            string assetsDir = Path.Combine(repoRoot, "ui-layout", "assets");

            ApkAssetLoader assetLoader = new ApkAssetLoader(assetsDir);
            await assetLoader.PreloadAssetsAsync(96);

            if (isSelfTestMode)
            {
                int exitCode = await ExecuteHeadlessSelfTestAsync(repoRoot, assetLoader);
                Shutdown(exitCode);
                return;
            }

            MainWindow mainWindow = new MainWindow(repoRoot, assetLoader);
            MainWindow = mainWindow;
            mainWindow.Show();

            if (isRenderScreenshotsMode)
            {
                string screenshotsDir = Path.Combine(repoRoot, "deliverables", "screenshots");
                await mainWindow.RenderAllScreenshotsAsync(screenshotsDir);
                mainWindow.Close();
                Shutdown(0);
            }
        }

        private static async Task<int> ExecuteHeadlessSelfTestAsync(string repoRoot, ApkAssetLoader assetLoader)
        {
            SelfTestReport report = await WaimaiContractSelfTests.RunAllAsync(repoRoot);
            if (report == null)
            {
                return 1;
            }

            // 追加 8 大 UI ViewModel 全链路状态机与 35 项 OBLIGATIONS 实测断言
            using (WaimaiMainViewModel vm = new WaimaiMainViewModel(repoRoot, assetLoader))
            {
                await vm.InitializeAsync(false);

                // ST-UI-01: 图标预加载与原生手机 1:1 竖屏单栏默认模式校验 (OBL-01)
                bool iconsAndPortraitOk = assetLoader.CachedImageCount >= 10 && vm.HasLauncherIcon && !vm.IsEasyGoDualPane && vm.IsRootTabLayerVisible;
                AppendTestItem(report, "ST-UI-01", "NATIVE_PORTRAIT_AND_ASSETS", "原生手机 1:1 竖屏单栏默认模式与 APK 冻结图标缓存校验",
                    iconsAndPortraitOk,
                    string.Format("CachedImages={0}, IsEasyGoDualPane={1}, ActivePage={2}", assetLoader.CachedImageCount, vm.IsEasyGoDualPane, vm.ActiveFullScreenPage));

                // ST-UI-02: 美团通行证登录全链路校验 (退出 -> 底部未登录悬浮条 -> 隐私协议拦截 -> 错误验证码拦截 -> 错误密码拦截 -> 扫码刷新 -> 正确短信码登录, OBL-02~OBL-07)
                vm.LogoutCommand.Execute(null);
                bool loggedOutGuestBar = !vm.IsUserLoggedIn && vm.IsGuestFloatingBarVisible;
                vm.OpenLoginViewCommand.Execute(null);
                vm.IsPrivacyAccepted = false;
                vm.SubmitLoginCommand.Execute(null);
                bool privacyBlocked = vm.IsPrivacyConfirmDialogOpen && !vm.IsUserLoggedIn;
                vm.IsPrivacyConfirmDialogOpen = false;
                vm.IsPrivacyAccepted = true;

                // 校验错误短信码必须被拦截
                vm.SendSmsCodeCommand.Execute(null);
                string correctSms = vm.LoginSmsCodeInput;
                vm.LoginSmsCodeInput = "000000";
                vm.SubmitLoginCommand.Execute(null);
                bool wrongSmsRejected = !vm.IsUserLoggedIn;

                // 校验错误密码必须被拦截
                vm.SwitchLoginModeCommand.Execute("Password");
                vm.LoginPasswordInput = "wrongpass";
                vm.SubmitLoginCommand.Execute(null);
                bool wrongPwdRejected = !vm.IsUserLoggedIn;
                vm.LoginPasswordInput = "mt884291";

                // 校验扫码刷新与正确验证码登录成功
                vm.SwitchLoginModeCommand.Execute("QrCode");
                string oldQr = vm.QrCodeTokenText;
                vm.RefreshQrCodeCommand.Execute(null);
                bool qrRefreshed = !string.Equals(oldQr, vm.QrCodeTokenText, StringComparison.Ordinal);

                vm.SwitchLoginModeCommand.Execute("Sms");
                vm.LoginSmsCodeInput = correctSms;
                bool smsGenerated = vm.HasSimulatedSmsBanner && !string.IsNullOrEmpty( vm.LoginSmsCodeInput);
                vm.ConfirmPrivacyAndLoginCommand.Execute(null);
                bool loginFlowPassed = loggedOutGuestBar && privacyBlocked && wrongSmsRejected && wrongPwdRejected && qrRefreshed && smsGenerated && vm.IsUserLoggedIn && !vm.IsLoginViewOpen;
                AppendTestItem(report, "ST-UI-02", "PASSPORT_LOGIN_FLOW", "美团通行证退出登录、未登录悬浮条、隐私协议拦截、错误验证码/密码拦截、扫码刷新与登录成功校验",
                    loginFlowPassed,
                    string.Format("GuestBar={0}, PrivacyDialog={1}, WrongSmsBlocked={2}, WrongPwdBlocked={3}, QrRefreshed={4}, LoggedIn={5}", loggedOutGuestBar, privacyBlocked, wrongSmsRejected, wrongPwdRejected, qrRefreshed, vm.IsUserLoggedIn));

                // ST-UI-03: 门店详情全屏压栈与点菜/评价/商家三子页切换 + 收藏切换校验 (OBL-13~OBL-17)
                vm.SelectPoiCommand.Execute(vm.FilteredPois[0]);
                bool poiPushed = vm.IsPoiDetailPageOpen && vm.IsPoiMenuSubTab;
                vm.SelectPoiSubTabCommand.Execute("评价");
                bool reviewSubTabOk = vm.IsPoiReviewSubTab && vm.FilteredPoiReviews.Count >= 4;
                vm.SelectPoiSubTabCommand.Execute("商家");
                bool merchantSubTabOk = vm.IsPoiMerchantSubTab && vm.SelectedPoi.MerchantArchive != null && !string.IsNullOrEmpty(vm.SelectedPoi.MerchantArchive.LicenseNumber);
                vm.SelectPoiSubTabCommand.Execute("点菜");
                bool favBefore = vm.SelectedPoi.IsFavorite;
                vm.TogglePoiFavoriteCommand.Execute(null);
                bool favToggled = vm.SelectedPoi.IsFavorite != favBefore;
                bool poiDetailPassed = poiPushed && reviewSubTabOk && merchantSubTabOk && favToggled;
                AppendTestItem(report, "ST-UI-03", "POI_DETAIL_SUBTABS", "门店详情全屏页压栈、点菜/评价/商家三子页切换与门店收藏校验",
                    poiDetailPassed,
                    string.Format("PoiPushed={0}, Reviews={1}, License={2}, FavToggled={3}", poiPushed, vm.FilteredPoiReviews.Count, vm.SelectedPoi.MerchantArchive.LicenseNumber, favToggled));

                // ST-UI-04: 神券膨胀与口令红包兑换联动算价校验 (OBL-23)
                int couponsBefore = vm.Coupons.Count;
                vm.CouponRedeemCodeInput = "MT888";
                vm.RedeemCouponCodeCommand.Execute(null);
                bool couponRedeemed = vm.Coupons.Count == couponsBefore + 1 && vm.SelectedCoupon != null && vm.SelectedCoupon.Amount >= 10;
                AppendTestItem(report, "ST-UI-04", "SHENQUAN_REDEEM_INFLATE", "神券省钱卡膨胀指令与口令兑换大额红包联动算价校验",
                    couponRedeemed,
                    string.Format("Coupons {0} -> {1}, ActiveCouponAmount={2:0.0}", couponsBefore, vm.Coupons.Count, vm.SelectedCoupon != null ? vm.SelectedCoupon.Amount : 0.0));

                // ST-UI-05: 确认订单全屏页 (OrderConfirmActivity) -> 美团收银台 (MTCashierActivity) 倒计时跳秒 -> 确认支付生成订单并核销神券 (OBL-20~OBL-22, OBL-26)
                if (vm.SelectedPoi != null && vm.SelectedPoi.Foods != null && vm.SelectedPoi.Foods.Count > 0)
                {
                    vm.AddFoodCommand.Execute(vm.SelectedPoi.Foods[0]);
                    vm.AddFoodCommand.Execute(vm.SelectedPoi.Foods[0]);
                }
                ShenQuanCouponContract couponToUse = vm.SelectedCoupon;
                int orderCountBefore = vm.Orders.Count;
                vm.CheckoutOrderCommand.Execute(null);
                bool enteredOrderConfirm = vm.IsOrderConfirmPageOpen && vm.OrderConfirmFinalPayable > 0;
                vm.OpenCashierModalCommand.Execute(null);
                string countdownBefore = vm.CashierCountdownText;
                vm.AdvanceCashierCountdown(5);
                string countdownAfter = vm.CashierCountdownText;
                bool countdownTicked = !string.Equals(countdownBefore, countdownAfter, StringComparison.Ordinal);
                bool openedCashier = vm.IsCashierModalOpen && vm.CashierFinalPayable > 0 && countdownTicked;
                vm.ConfirmCashierPaymentCommand.Execute(null);
                int orderCountAfter = vm.Orders.Count;
                bool couponConsumed = couponToUse != null && couponToUse.IsUsed && vm.SelectedOrder != null && string.Equals(vm.SelectedOrder.UsedCouponId, couponToUse.CouponId, StringComparison.Ordinal);
                bool checkoutPassed = enteredOrderConfirm && openedCashier && couponConsumed && orderCountAfter == orderCountBefore + 1 && vm.IsOrderTrackPageOpen;
                AppendTestItem(report, "ST-UI-05", "ORDER_CONFIRM_AND_CASHIER", "确认订单全屏页、美团收银台倒计时跳秒、神券核销与自动跳转轨迹页校验",
                    checkoutPassed,
                    string.Format("EnteredConfirm={0}, Countdown={1}->{2}, CouponConsumed={3}, Orders {4}->{5}", enteredOrderConfirm, countdownBefore, countdownAfter, couponConsumed, orderCountBefore, orderCountAfter));

                // ST-UI-06: 骑手/商家三方 IM 群聊 + 确认收货 + 订单评价写回 + 再来一单原餐品还原 + 退款归还神券校验 (OBL-27~OBL-30)
                OrderContract createdOrder = vm.SelectedOrder;
                int expectedReorderCount = createdOrder != null ? createdOrder.ProductCount : 0;
                int imCountBefore = createdOrder != null && createdOrder.ImMessages != null ? createdOrder.ImMessages.Count : 0;
                vm.SendSmartReplyCommand.Execute("辛苦了，放前台外卖柜即可");
                int imCountAfter = createdOrder != null && createdOrder.ImMessages != null ? createdOrder.ImMessages.Count : 0;
                int poiReviewsBefore = vm.SelectedPoi.Reviews.Count;
                vm.ConfirmOrderReceivedCommand.Execute(null);
                vm.OpenOrderReviewModalCommand.Execute(null);
                vm.ReviewRatingStars = 5;
                vm.ReviewCommentInput = "自动化自检五星好评：味道好極了！";
                vm.SubmitOrderReviewCommand.Execute(null);
                int poiReviewsAfter = vm.SelectedPoi.Reviews.Count;

                // 校验再来一单严格还原原订单商品与件数
                vm.ReorderCommand.Execute(createdOrder);
                bool reorderExactMatch = vm.CartPrice != null && vm.CartPrice.TotalItemCount == expectedReorderCount;

                // 校验取消/退款自动归还已核销神券
                vm.CancelOrderCommand.Execute(createdOrder);
                bool couponRefunded = couponToUse != null && !couponToUse.IsUsed && createdOrder.Status == 70;

                bool imAndReviewPassed = (imCountAfter >= imCountBefore + 2) && createdOrder.IsComment && (poiReviewsAfter == poiReviewsBefore + 1) && reorderExactMatch && couponRefunded;
                AppendTestItem(report, "ST-UI-06", "IM_CHAT_REORDER_REFUND_LOOP", "三方IM群聊、评价写回门店、再来一单原商品还原及退款归还神券校验",
                    imAndReviewPassed,
                    string.Format("ImMessages {0}->{1}, PoiReviews {2}->{3}, ReorderItems={4}, CouponRefunded={5}", imCountBefore, imCountAfter, poiReviewsBefore, poiReviewsAfter, vm.CartPrice != null ? vm.CartPrice.TotalItemCount : 0, couponRefunded));

                // ST-UI-07: 收货地址新增、已有地址编辑保存与消息通知中心全部已读校验 (OBL-32~OBL-33)
                int addrBefore = vm.Addresses.Count;
                vm.EditingAddressId = string.Empty;
                vm.EditAddressContactName = "自检陈同学";
                vm.EditAddressDetail = "深圳市南山区粤海街道科技园大厦";
                vm.EditAddressHouseNumber = "18层1801";
                vm.SaveNewAddressCommand.Execute(null);
                int addrAfterAdd = vm.Addresses.Count;

                // 编辑刚才新增的地址并原地保存
                WmAddressContract addedAddr = vm.Addresses[0];
                vm.StartEditAddressCommand.Execute(addedAddr);
                vm.EditAddressHouseNumber = "26层2608室(已编辑)";
                vm.SaveNewAddressCommand.Execute(null);
                bool addrEditedInPlace = (vm.Addresses.Count == addrAfterAdd) && string.Equals(vm.Addresses[0].HouseNumber, "26层2608室(已编辑)", StringComparison.Ordinal);

                vm.MarkAllNotificationsReadCommand.Execute(null);
                bool addrAndMsgPassed = (addrAfterAdd == addrBefore + 1) && addrEditedInPlace && vm.UnreadNotificationCount == 0;
                AppendTestItem(report, "ST-UI-07", "ADDRESS_CRUD_AND_MESSAGES", "收货地址新增、已有地址原地编辑保存及消息通知中心一键已读校验",
                    addrAndMsgPassed,
                    string.Format("Addresses {0}->{1}, EditedHouse={2}, UnreadMessages={3}", addrBefore, addrAfterAdd, vm.Addresses[0].HouseNumber, vm.UnreadNotificationCount));

                // ST-UI-08: DPAPI 快照加密持久化与恢复校验 (OBL-34)
                string tempSnapPath = Path.Combine(Path.GetTempPath(), "waimai-ui-restore-" + Guid.NewGuid().ToString("N") + ".dpapi");
                WaimaiPersistedSnapshot snap = new WaimaiPersistedSnapshot
                {
                    SavedAtUtc = DateTime.UtcNow.ToString("o"),
                    CurrentAddressId = vm.Addresses.Count > 1 ? vm.Addresses[1].AddressId : "addr-2",
                    IsEasyGoDualPane = false,
                    CurrentUser = vm.CurrentUser,
                    CartItems = new List<ShopCartItemContract>(),
                    Orders = new List<OrderContract>(),
                    Coupons = new List<ShenQuanCouponContract>(),
                    Addresses = new List<WmAddressContract>(vm.Addresses)
                };
                await WaimaiStorageService.SaveSnapshotAsync(snap, tempSnapPath);
                await vm.InitializeAsync(true, tempSnapPath);
                bool restorePassed = !vm.IsEasyGoDualPane &&
                                     vm.IsUserLoggedIn &&
                                     vm.Orders.Count == 0 &&
                                     vm.SelectedOrder == null &&
                                     vm.Coupons.Count == 0 &&
                                     vm.SelectedCoupon == null &&
                                     vm.ActiveCartItems.Count == 0 &&
                                     vm.CurrentAddress != null &&
                                     string.Equals(vm.CurrentAddress.AddressId, snap.CurrentAddressId, StringComparison.Ordinal);
                AppendTestItem(report, "ST-UI-08", "VIEWMODEL_SNAPSHOT_RESTORE", "ViewModel 启动入口 InitializeAsync 恢复 DPAPI 快照及登录态边界校验",
                    restorePassed,
                    string.Format("LoggedIn={0}, Orders={1}, Coupons={2}, Address={3}", vm.IsUserLoggedIn, vm.Orders.Count, vm.Coupons.Count, vm.CurrentAddress != null ? vm.CurrentAddress.AddressId : "null"));

                await Task.Run(delegate
                {
                    if (File.Exists(tempSnapPath))
                    {
                        File.Delete(tempSnapPath);
                    }
                });
            }

            // ST-UI-09: 真实加载 MainWindow XAML 可视树/逻辑树，校验 420×910 锁定尺寸与 100% 按钮有效绑定（零死按钮）
            MainWindow xamlWin = new MainWindow(repoRoot, assetLoader);
            xamlWin.Measure(new Size(420, 910));
            xamlWin.Arrange(new Rect(0, 0, 420, 910));
            xamlWin.UpdateLayout();

            bool viewportLocked = (int)xamlWin.Width == 420 &&
                                  (int)xamlWin.Height == 910 &&
                                  (int)xamlWin.MinWidth == 420 &&
                                  (int)xamlWin.MaxWidth == 420 &&
                                  (int)xamlWin.MinHeight == 910 &&
                                  (int)xamlWin.MaxHeight == 910 &&
                                  xamlWin.ResizeMode == ResizeMode.CanMinimize;

            int totalButtons = 0;
            int deadButtons = 0;
            ScanButtonsRecursive(xamlWin, xamlWin.ViewModel, ref totalButtons, ref deadButtons);
            xamlWin.Close();

            bool xamlAuditPassed = viewportLocked && totalButtons >= 35 && deadButtons == 0;
            AppendTestItem(report, "ST-UI-09", "XAML_VISUAL_TREE_ZERO_DEAD_BUTTONS", "真实加载 MainWindow XAML 校验 420×910 锁定比例与全树按钮 100% 绑定零死按钮",
                xamlAuditPassed,
                string.Format("ViewportLocked(420x910)={0}, TotalButtons={1}, DeadButtons={2}", viewportLocked, totalButtons, deadButtons));

            await Task.Run(delegate
            {
                string reportPath = Path.Combine(repoRoot, "deliverables", "selftest-report.json");
                string dir = Path.GetDirectoryName(reportPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                serializer.MaxJsonLength = int.MaxValue;
                string json = serializer.Serialize(report);
                File.WriteAllBytes(reportPath, System.Text.Encoding.UTF8.GetBytes(json));
            });

            return report.FailedCount == 0 && report.PassedCount >= 30 ? 0 : 1;
        }

        private static void ScanButtonsRecursive(DependencyObject parent, WaimaiMainViewModel vm, ref int totalButtons, ref int deadButtons)
        {
            if (parent == null)
            {
                return;
            }

            System.Windows.Controls.Button btn = parent as System.Windows.Controls.Button;
            if (btn != null)
            {
                totalButtons++;
                bool hasValidCommand = btn.Command != null;
                if (!hasValidCommand && vm != null)
                {
                    System.Windows.Data.Binding binding = System.Windows.Data.BindingOperations.GetBinding(
                        btn, System.Windows.Controls.Primitives.ButtonBase.CommandProperty);
                    if (binding != null && binding.Path != null && !string.IsNullOrEmpty(binding.Path.Path))
                    {
                        string propName = binding.Path.Path;
                        if (propName.StartsWith("DataContext.", StringComparison.Ordinal))
                        {
                            propName = propName.Substring("DataContext.".Length);
                        }
                        System.Reflection.PropertyInfo pi = vm.GetType().GetProperty(propName);
                        if (pi != null && pi.GetValue(vm, null) is System.Windows.Input.ICommand)
                        {
                            hasValidCommand = true;
                        }
                    }
                }
                if (!hasValidCommand)
                {
                    deadButtons++;
                }
            }

            foreach (object child in LogicalTreeHelper.GetChildren(parent))
            {
                DependencyObject depChild = child as DependencyObject;
                if (depChild != null)
                {
                    ScanButtonsRecursive(depChild, vm, ref totalButtons, ref deadButtons);
                }
            }
        }

        private static void AppendTestItem(SelfTestReport report, string id, string category, string name, bool passed, string detail)
        {
            if (report.Items == null)
            {
                report.Items = new List<SelfTestItem>();
            }
            report.Items.Add(new SelfTestItem
            {
                Id = id,
                Category = category,
                Name = name,
                Passed = passed,
                EvidenceDetail = detail
            });
            report.TotalCount = report.Items.Count;
            int passSum = 0;
            int failSum = 0;
            for (int i = 0; i < report.Items.Count; i++)
            {
                if (report.Items[i].Passed)
                {
                    passSum++;
                }
                else
                {
                    failSum++;
                }
            }
            report.PassedCount = passSum;
            report.FailedCount = failSum;
        }

        private static Task<string> ResolveRepositoryRootAsync()
        {
            return Task.Run(delegate
            {
                string current = AppDomain.CurrentDomain.BaseDirectory;
                for (int i = 0; i < 7; i++)
                {
                    if (string.IsNullOrEmpty(current))
                    {
                        break;
                    }
                    string marker = Path.Combine(current, "package-analysis.json");
                    if (File.Exists(marker))
                    {
                        return current;
                    }
                    DirectoryInfo parent = Directory.GetParent(current);
                    current = parent != null ? parent.FullName : null;
                }
                return AppDomain.CurrentDomain.BaseDirectory;
            });
        }

        private static bool HasFlag(string[] args, string flag)
        {
            if (args == null)
            {
                return false;
            }
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
