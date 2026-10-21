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
    /// 美团外卖 Windows 桌面端应用程序入口。
    /// 支持交互模式、--selftest 自动化契约与 UI 状态机自检模式、--render-screenshots 高清多视图截图导出模式。
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

            // 追加 UI ViewModel 与 APK 资源加载器端到端实测断言
            using (WaimaiMainViewModel vm = new WaimaiMainViewModel(repoRoot, assetLoader))
            {
                await vm.InitializeAsync(false);

                bool iconsLoaded = assetLoader.CachedImageCount >= 10 && vm.HasLauncherIcon;
                AppendTestItem(report, "ST-UI-01", "APK_ASSET_LOADER", "APK 原始图标后台冻结预加载与启动图标绑定校验",
                    iconsLoaded,
                    string.Format("CachedImages={0}, HasLauncherIcon={1}", assetLoader.CachedImageCount, vm.HasLauncherIcon));

                double beforeAmount = vm.SelectedCoupon != null ? vm.SelectedCoupon.Amount : 0.0;
                vm.InflateCouponCommand.Execute(null);
                double afterAmount = vm.SelectedCoupon != null ? vm.SelectedCoupon.Amount : 0.0;
                bool inflatePassed = afterAmount > beforeAmount && vm.CartPrice != null && vm.CartPrice.TotalItemCount > 0;
                AppendTestItem(report, "ST-UI-02", "VIEWMODEL_SHENQUAN", "神券膨胀指令与购物车实时算价联动校验",
                    inflatePassed,
                    string.Format("Coupon {0:0.0} -> {1:0.0}, CartTotal={2:0.00}", beforeAmount, afterAmount, vm.CartPrice != null ? vm.CartPrice.TotalDiscountedAndBoxPrice : 0.0));

                // 凑够起送价并测试一键下单与骑手 IM 群聊
                if (vm.SelectedPoi != null && vm.SelectedPoi.Foods != null && vm.SelectedPoi.Foods.Count > 0)
                {
                    vm.AddFoodCommand.Execute(vm.SelectedPoi.Foods[0]);
                    vm.AddFoodCommand.Execute(vm.SelectedPoi.Foods[0]);
                }
                int orderCountBefore = vm.Orders.Count;
                vm.CheckoutOrderCommand.Execute(null);
                int orderCountAfter = vm.Orders.Count;
                bool checkoutPassed = orderCountAfter == orderCountBefore + 1 && vm.SelectedTabBusinessId == 2;
                AppendTestItem(report, "ST-UI-03", "VIEWMODEL_CHECKOUT", "一键结算生成真实订单并自动跳转骑手轨迹中心校验",
                    checkoutPassed,
                    string.Format("Orders {0} -> {1}, ActiveTab={2}", orderCountBefore, orderCountAfter, vm.SelectedTabBusinessId));

                int imCountBefore = vm.SelectedOrder != null && vm.SelectedOrder.ImMessages != null ? vm.SelectedOrder.ImMessages.Count : 0;
                vm.SendSmartReplyCommand.Execute("辛苦了，放前台外卖柜即可");
                int imCountAfter = vm.SelectedOrder != null && vm.SelectedOrder.ImMessages != null ? vm.SelectedOrder.ImMessages.Count : 0;
                bool imPassed = imCountAfter >= imCountBefore + 2;
                AppendTestItem(report, "ST-UI-04", "VIEWMODEL_IM_CHAT", "骑手与商家三方群聊发送及自动智能回复校验",
                    imPassed,
                    string.Format("ImMessages {0} -> {1}", imCountBefore, imCountAfter));

                if (vm.SelectedPoi != null && vm.SelectedPoi.Foods != null && vm.SelectedPoi.Foods.Count > 0)
                {
                    vm.AddFoodCommand.Execute(vm.SelectedPoi.Foods[0]);
                }
                string tempSnapPath = Path.Combine(Path.GetTempPath(), "waimai-ui-restore-" + Guid.NewGuid().ToString("N") + ".dpapi");
                WaimaiPersistedSnapshot snap = new WaimaiPersistedSnapshot
                {
                    SavedAtUtc = DateTime.UtcNow.ToString("o"),
                    CurrentAddressId = vm.Addresses.Count > 1 ? vm.Addresses[1].AddressId : "addr-2",
                    IsEasyGoDualPane = false,
                    CartItems = new List<ShopCartItemContract>(),
                    Orders = new List<OrderContract>(),
                    Coupons = new List<ShenQuanCouponContract>(),
                    Addresses = new List<WmAddressContract>(vm.Addresses)
                };
                await WaimaiStorageService.SaveSnapshotAsync(snap, tempSnapPath);
                await vm.InitializeAsync(true, tempSnapPath);
                bool restorePassed = !vm.IsEasyGoDualPane &&
                                     vm.Orders.Count == 0 &&
                                     vm.SelectedOrder == null &&
                                     vm.Coupons.Count == 0 &&
                                     vm.SelectedCoupon == null &&
                                     vm.ActiveCartItems.Count == 0 &&
                                     vm.CurrentAddress != null &&
                                     string.Equals(vm.CurrentAddress.AddressId, snap.CurrentAddressId, StringComparison.Ordinal);
                AppendTestItem(report, "ST-UI-05", "VIEWMODEL_SNAPSHOT_RESTORE", "ViewModel 启动入口 InitializeAsync 恢复 DPAPI 快照及空集合边界校验",
                    restorePassed,
                    string.Format("IsEasyGoDualPane={0}, Orders={1}, Coupons={2}, Address={3}", vm.IsEasyGoDualPane, vm.Orders.Count, vm.Coupons.Count, vm.CurrentAddress != null ? vm.CurrentAddress.AddressId : "null"));

                await Task.Run(delegate
                {
                    if (File.Exists(tempSnapPath))
                    {
                        File.Delete(tempSnapPath);
                    }
                });
            }

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

            return report.FailedCount == 0 && report.PassedCount >= 15 ? 0 : 1;
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
