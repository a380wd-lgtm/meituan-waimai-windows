using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace MeituanWaimai.Core
{
    // 本地契约依据 APK 提取证据整理，计价与演示数据不代表线上结算承诺。
    public static class ReverseEvidenceManifest
    {
        public const string PackageName = "com.sankuai.meituan.takeoutnew";
        public const string VersionName = "8.84.2";
        public const int VersionCode = 800840002;
        public const string ApkSha256 = "0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82";
        public const string BuildNumber = "91293";
        public const string OfficialH5HomeUrl = "https://h5.waimai.meituan.com/waimai/mindex/home";
        public const string MtGuardAppKey = "e68c4c18-35ec-4972-84e4-32a0836455a8";
        public const string PackageSummaryEvidenceId = "ev_dfa491886da7142bfa8e2e6f09ec0c62cac8cb51f751061a79062e52881b6e91";
        public const string MainActivityEvidenceId = "ev_0063a0c6f104e543d38943d632c596951f7e026ba44437b8c64e936f02eda476";
        public const string HomePageEvidenceId = "ev_b83d2da5bff9337004ff614bc26afffc1d30211ea6b8dabf80ba9423d46c2814";
        public const string PoiContractEvidenceId = "ev_9cddb4225370c92468d1c8ee37ad1fdba0e594dc88644c2e8cb8037ad8f89e4b";
        public const string GoodsSpuEvidenceId = "ev_dfec8e80b5dfb1a1a0ea6f595797ce868c8c35022b6523fb0551f0ab56150f0d";
        public const string ShopCartPriceEvidenceId = "ev_1dbf43e51054328202990f59c919bfc49db842e928dee0b1bc56572a1bb0941f";
        public const string OrderListResponseEvidenceId = "ev_85cbd0da4f81234469001e25c484a966098bd41bafe88fa5e8d9f76304cb4668";
        public const string PassportEvidenceId = "ev_6fff21e9e8dea7fc1040a9162dc0b7d60c0b13e76a014e29055c25c5c3c154a5";
        public const string PassportUserEvidenceId = "ev_38d81080c56670500535543d828fcedd65aeaf8ffb0bd75e812230aa5146d6de";
        public const string OrderConfirmEvidenceId = "ev_b65f6857888d614189f0348b8d6684aab37fc38a6174353441ad599ce4ade47a";
        public const string CashierEvidenceId = "ev_c1a89ccc72a297a053b3cabb246a645659e3fcaa19db579354494bad11246ff4";
        public const string AddressBeanEvidenceId = "ev_f943daf0fc1323f890c709cc157286df40aa7f945814090371dc18b00e3b6c60";
    }

    public class PassportUserContract
    {
        public long Id { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string Mobile { get; set; }
        public string MobileMasked { get; set; }
        public string Token { get; set; }
        public string Ticket { get; set; }
        public string LoginAuthTicket { get; set; }
        public string Avatarurl { get; set; }
        public string AvatarEmoji { get; set; }
        public string VipBadgeText { get; set; }
        public int Growthlevel { get; set; }
        public int Growthvalue { get; set; }
        public int Pointvalue { get; set; }
        public double SaveAmount { get; set; }
        public double RedPacketBalance { get; set; }
        public int Haspassword { get; set; }
        public bool IsLoggedIn { get; set; }

        public static string MaskMobile(string rawMobile)
        {
            string digits = (rawMobile ?? string.Empty).Trim();
            if (digits.Length >= 11)
            {
                return digits.Substring(0, 3) + "****" + digits.Substring(digits.Length - 4);
            }
            if (digits.Length >= 7)
            {
                return digits.Substring(0, 3) + "****" + digits.Substring(digits.Length - 2);
            }
            return "138****0001";
        }
    }

    public class PoiReviewItemContract
    {
        public string ReviewId { get; set; }
        public long PoiId { get; set; }
        public string UserName { get; set; }
        public string AvatarEmoji { get; set; }
        public string VipLevelText { get; set; }
        public string TimeText { get; set; }
        public double Score { get; set; }
        public string StarText { get; set; }
        public string DeliveryTimeTip { get; set; }
        public string OrderedDishesText { get; set; }
        public string Content { get; set; }
        public string MerchantReply { get; set; }
        public bool HasPictures { get; set; }
        public string PictureBadgeText { get; set; }
    }

    public class PoiMerchantArchiveContract
    {
        public string Address { get; set; }
        public string Phone { get; set; }
        public string BusinessHours { get; set; }
        public string DeliveryServiceText { get; set; }
        public string LicenseNumber { get; set; }
        public string FoodSafetyGrade { get; set; }
        public string KitchenInspectionText { get; set; }
        public List<string> ServiceGuarantees { get; set; }

        public PoiMerchantArchiveContract()
        {
            ServiceGuarantees = new List<string>();
        }
    }

    public class CashierPaymentMethodContract
    {
        public string MethodId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string RecommendTag { get; set; }
        public string IconEmoji { get; set; }
        public string AccentHex { get; set; }
        public double InstantDiscount { get; set; }
        public bool IsSelected { get; set; }
    }

    public class NotificationMessageContract
    {
        public string MessageId { get; set; }
        public string Category { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string TimeText { get; set; }
        public string IconEmoji { get; set; }
        public bool IsUnread { get; set; }
    }

    public class DynamicTabContract : INotifyPropertyChanged
    {
        private int unreadBadgeCount;
        private bool isSelected;

        public int TabIndex { get; set; }
        public int BusinessId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string FragmentClassName { get; set; }
        public string IconGlyph { get; set; }
        public string BubbleTagText { get; set; }
        public bool IsProminentArcTab { get; set; }

        public int UnreadBadgeCount
        {
            get { return unreadBadgeCount; }
            set
            {
                if (unreadBadgeCount != value)
                {
                    unreadBadgeCount = value;
                    RaisePropertyChanged("UnreadBadgeCount");
                }
            }
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                if (isSelected != value)
                {
                    isSelected = value;
                    RaisePropertyChanged("IsSelected");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class KingKongChannelContract
    {
        public int ChannelId { get; set; }
        public int CategoryBuzType { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string BadgeText { get; set; }
        public string AccentHex { get; set; }
        public string IconBgHex { get; set; }
        public string IconGlyph { get; set; }
        public string ModuleId { get; set; }
        public string FeedUrl { get; set; }
    }

    public class FullReductionTier
    {
        public double Threshold { get; set; }
        public double ReduceAmount { get; set; }
    }

    public class PoiActivityTagContract
    {
        public string TagText { get; set; }
        public string BorderHex { get; set; }
        public string TextHex { get; set; }
        public string BgHex { get; set; }
    }

    public class PoiCouponContract
    {
        public string CouponId { get; set; }
        public string Title { get; set; }
        public double Amount { get; set; }
        public double Threshold { get; set; }
        public bool IsClaimed { get; set; }
    }

    public class GoodsSkuContract
    {
        public long SkuId { get; set; }
        public string SpecName { get; set; }
        public double Price { get; set; }
        public double OriginPrice { get; set; }
        public double BoxFee { get; set; }
        public int Stock { get; set; }
    }

    public class GoodsAttrGroupContract
    {
        public string GroupName { get; set; }
        public string SelectedOption { get; set; }
        public List<string> Options { get; set; }

        public GoodsAttrGroupContract()
        {
            Options = new List<string>();
        }
    }

    public class GoodsSpuContract : INotifyPropertyChanged
    {
        public long Id { get; set; }
        public long PoiId { get; set; }
        public string CategoryTag { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MonthSaledContent { get; set; }
        public string PraiseContent { get; set; }
        public string LikeRatioDesc { get; set; }
        public string ActivityTag { get; set; }
        public string Unit { get; set; }
        public string ThumbAccentHex { get; set; }
        public string EmojiIcon { get; set; }
        public int MonthSaled { get; set; }
        public int ActivityType { get; set; }
        public int Status { get; set; }
        public double MinPrice { get; set; }
        public double OriginPrice { get; set; }
        public double FullDiscountPrice { get; set; }
        public double BoxFee { get; set; }
        public List<GoodsSkuContract> Skus { get; set; }
        public List<GoodsAttrGroupContract> AttrGroups { get; set; }

        public GoodsSpuContract()
        {
            Skus = new List<GoodsSkuContract>();
            AttrGroups = new List<GoodsAttrGroupContract>();
        }

        private int selectedCount;

        public int SelectedCount
        {
            get { return selectedCount; }
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException("value", "已选数量不能为负数。");
                if (selectedCount == value)
                    return;
                selectedCount = value;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("SelectedCount"));
                    handler(this, new PropertyChangedEventArgs("HasSelectedCount"));
                }
            }
        }

        public bool HasSelectedCount { get { return SelectedCount > 0; } }
        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class PoiContract
    {
        public long Id { get; set; }
        public string PoiIdStr { get; set; }
        public string Name { get; set; }
        public string FormattedPoiScore { get; set; }
        public string MonthSalesTip { get; set; }
        public string MinPriceTip { get; set; }
        public string MtDeliveryTime { get; set; }
        public string Distance { get; set; }
        public string BrandSlogan { get; set; }
        public string Bulletin { get; set; }
        public string DeliveryTypeText { get; set; }
        public string AveragePriceTip { get; set; }
        public string RankLabelInfo { get; set; }
        public string BannerAccentHex { get; set; }
        public string LogoEmoji { get; set; }
        public double PoiScore { get; set; }
        public double MinPrice { get; set; }
        public double ShippingFee { get; set; }
        public double OriginShippingFee { get; set; }
        public double DistanceKm { get; set; }
        public int MonthSales { get; set; }
        public int DeliveryMinutes { get; set; }
        public int BuzType { get; set; }
        public int State { get; set; }
        public bool IsBrandGold { get; set; }
        public bool SupportsShenQuan { get; set; }
        public bool IsFavorite { get; set; }
        public PoiMerchantArchiveContract MerchantArchive { get; set; }
        public List<PoiReviewItemContract> Reviews { get; set; }
        public List<PoiActivityTagContract> ActivityTags { get; set; }
        public List<PoiCouponContract> Coupons { get; set; }
        public List<FullReductionTier> FullReductionRules { get; set; }
        public List<string> MenuCategories { get; set; }
        public List<GoodsSpuContract> Foods { get; set; }

        public PoiContract()
        {
            MerchantArchive = new PoiMerchantArchiveContract();
            Reviews = new List<PoiReviewItemContract>();
            ActivityTags = new List<PoiActivityTagContract>();
            Coupons = new List<PoiCouponContract>();
            FullReductionRules = new List<FullReductionTier>();
            MenuCategories = new List<string>();
            Foods = new List<GoodsSpuContract>();
        }
    }

    public class ShopCartItemContract
    {
        public long SpuId { get; set; }
        public long SkuId { get; set; }
        public long PoiId { get; set; }
        public string FoodName { get; set; }
        public string SpecSummary { get; set; }
        public double UnitPrice { get; set; }
        public double OriginUnitPrice { get; set; }
        public double BoxFeePerUnit { get; set; }
        public int Count { get; set; }
        public int NxItemCount { get; set; }
        public int PocketId { get; set; }

        public ShopCartItemContract()
        {
            PocketId = 1;
        }

        public double LineTotal { get { return Math.Round(UnitPrice * Count, 2); } }
    }

    public class ShopCartPriceContract
    {
        public string ShoppingCartUniqueId { get; set; }
        public string PreviewSubTip { get; set; }
        public string CrossTip { get; set; }
        public string CheckoutButtonText { get; set; }
        public double ThresholdPrice { get; set; }
        public double TotalAndBoxPrice { get; set; }
        public double TotalBoxPrice { get; set; }
        public double TotalDiscountedAndBoxPrice { get; set; }
        public double TotalDiscountedAndBoxPriceWithoutGoodCoupon { get; set; }
        public double TotalDiscountedPrice { get; set; }
        public double OriginalShippingFee { get; set; }
        public double ShippingFeeReduce { get; set; }
        public double FinalShippingFee { get; set; }
        public double AppliedCouponAmount { get; set; }
        public double AppliedFullReductionAmount { get; set; }
        public double TotalSavedAmount { get; set; }
        public int TotalItemCount { get; set; }
        public bool CanCheckout { get; set; }
    }

    public class ShenQuanCouponContract
    {
        public string CouponId { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string ExpireTimeText { get; set; }
        public string TagText { get; set; }
        public double BaseAmount { get; set; }
        public double Amount { get; set; }
        public double Threshold { get; set; }
        public double InflatedBonus { get; set; }
        public bool IsInflated { get; set; }
        public bool IsUsed { get; set; }
        public long ApplicablePoiId { get; set; }

        public void Inflate()
        {
            // 以基础面额重新计算，重复调用不会累加膨胀金额。
            IsInflated = true;
            Amount = Math.Round(BaseAmount + InflatedBonus, 2);
            TagText = "已膨胀 · 神券立减";
        }
    }

    public class OrderFoodItemContract
    {
        public long SpuId { get; set; }
        public long SkuId { get; set; }
        public int PocketId { get; set; }
        public string FoodName { get; set; }
        public string SpecText { get; set; }
        public int Count { get; set; }
        public double Price { get; set; }

        public OrderFoodItemContract()
        {
            PocketId = 1;
        }
    }

    public class RiderTrackingStepContract
    {
        public string TimeText { get; set; }
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ImChatMessageContract
    {
        public string SenderRole { get; set; }
        public string SenderName { get; set; }
        public string TimeText { get; set; }
        public string Content { get; set; }
        public bool IsSelf { get; set; }
    }

    public class OrderContract
    {
        public long OrderId { get; set; }
        public long PoiId { get; set; }
        public string HashId { get; set; }
        public string PoiIdStr { get; set; }
        public string PoiName { get; set; }
        public string StatusDescription { get; set; }
        public string OrderTime { get; set; }
        public string FormatOrderTime { get; set; }
        public string ArrivalTimeText { get; set; }
        public string RiderName { get; set; }
        public string RiderPhone { get; set; }
        public string RecipientName { get; set; }
        public string RecipientPhone { get; set; }
        public string RecipientAddress { get; set; }
        public string Caution { get; set; }
        public string FoodNames { get; set; }
        public int Status { get; set; }
        public int DeliveryStatus { get; set; }
        public int PayStatus { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
        public int ProductCount { get; set; }
        public double Total { get; set; }
        public double BoxFeeTotal { get; set; }
        public double ShippingFee { get; set; }
        public double DiscountTotal { get; set; }
        public double RiderProgressRatio { get; set; }
        public bool IsComment { get; set; }
        public bool CanRefund { get; set; }
        public string UsedCouponId { get; set; }
        public List<OrderFoodItemContract> FoodItems { get; set; }
        public List<RiderTrackingStepContract> TrackingSteps { get; set; }
        public List<ImChatMessageContract> ImMessages { get; set; }
        public List<string> SmartReplySuggestions { get; set; }

        public OrderContract()
        {
            FoodItems = new List<OrderFoodItemContract>();
            TrackingSteps = new List<RiderTrackingStepContract>();
            ImMessages = new List<ImChatMessageContract>();
            SmartReplySuggestions = new List<string>();
        }
    }

    public class WmAddressContract
    {
        public string AddressId { get; set; }
        public string ContactName { get; set; }
        public string GenderText { get; set; }
        public string Phone { get; set; }
        public string AddressDetail { get; set; }
        public string HouseNumber { get; set; }
        public string TagLabel { get; set; }
        public bool IsDefault { get; set; }
        public int Latitude { get; set; }
        public int Longitude { get; set; }
    }

    public class DeepEatRecommendationContract
    {
        public string SceneId { get; set; }
        public string SceneTitle { get; set; }
        public string AiPromptText { get; set; }
        public string ReasonText { get; set; }
        public string HighlightTag { get; set; }
        public long TargetPoiId { get; set; }
        public long TargetSpuId { get; set; }
    }

    public class PromoFlashDealContract
    {
        public string DealId { get; set; }
        public string Title { get; set; }
        public string PoiName { get; set; }
        public string DiscountBadge { get; set; }
        public string StockTip { get; set; }
        public long TargetPoiId { get; set; }
        public long TargetSpuId { get; set; }
        public double FlashPrice { get; set; }
        public double OriginPrice { get; set; }
        public int SoldPercent { get; set; }
    }

    public class MineProfileContract
    {
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public string VipLevelText { get; set; }
        public string PhoneMasked { get; set; }
        public int ShenQuanCount { get; set; }
        public int TuanBiPoints { get; set; }
        public int MonthlySavedOrders { get; set; }
        public double RedPacketBalance { get; set; }
        public double MonthlySavedAmount { get; set; }
    }

    public class WaimaiPersistedSnapshot
    {
        public string SavedAtUtc { get; set; }
        public string CurrentAddressId { get; set; }
        public bool IsEasyGoDualPane { get; set; }
        public PassportUserContract CurrentUser { get; set; }
        public List<long> FavoritePoiIds { get; set; }
        public List<NotificationMessageContract> Notifications { get; set; }
        public List<ShopCartItemContract> CartItems { get; set; }
        public List<OrderContract> Orders { get; set; }
        public List<ShenQuanCouponContract> Coupons { get; set; }
        public List<WmAddressContract> Addresses { get; set; }

        public WaimaiPersistedSnapshot()
        {
            FavoritePoiIds = new List<long>();
            Notifications = new List<NotificationMessageContract>();
            CartItems = new List<ShopCartItemContract>();
            Orders = new List<OrderContract>();
            Coupons = new List<ShenQuanCouponContract>();
            Addresses = new List<WmAddressContract>();
        }
    }



    public class EasyGoMagicWindowContract
    {
        public string EasyGoVersion { get; set; }
        public string Client { get; set; }
        public string FunctionName { get; set; }
        public string MainPages { get; set; }
        public string RelatedPage { get; set; }
        public bool SupportRotationUxCompat { get; set; }
        public bool IsDraggable { get; set; }

        public static EasyGoMagicWindowContract ParseFromJson(string json)
        {
            Dictionary<string, object> root = ContractJson.ReadRoot(json);
            foreach (object item in ContractJson.Array(root, "logicEntities"))
            {
                Dictionary<string, object> entity = ContractJson.Object(item, "logicEntities[]");
                Dictionary<string, object> head = ContractJson.Object(ContractJson.Value(entity, "head"), "head");
                if (!String.Equals(ContractJson.String(head, "function"), "magicwindow", StringComparison.OrdinalIgnoreCase))
                    continue;
                Dictionary<string, object> body = ContractJson.Object(ContractJson.Value(entity, "body"), "body");
                Dictionary<string, object> pages = ContractJson.Object(ContractJson.Value(body, "defaultDualActivities"), "defaultDualActivities");
                Dictionary<string, object> ux = ContractJson.Object(ContractJson.Value(body, "UX"), "UX");
                return new EasyGoMagicWindowContract
                {
                    EasyGoVersion = ContractJson.String(root, "easyGoVersion"),
                    Client = ContractJson.String(root, "client"),
                    FunctionName = ContractJson.String(head, "function"),
                    MainPages = ContractJson.String(pages, "mainPages"),
                    RelatedPage = ContractJson.String(pages, "relatedPage"),
                    SupportRotationUxCompat = ContractJson.Boolean(ux, "supportRotationUxCompat"),
                    IsDraggable = ContractJson.Boolean(ux, "isDraggable")
                };
            }
            throw new FormatException("EasyGo 配置缺少 magicwindow 实体。");
        }
    }

    public class OrderListResponseContract
    {
        public string Type { get; set; }
        public string Cursor { get; set; }
        public string MultiCursor { get; set; }
        public int HasMore { get; set; }
        public int UnCommentCount { get; set; }
        public bool HasReadUnCommentOrder { get; set; }
        public string UnreadUncommentTips { get; set; }
        public int WaitTime { get; set; }
        public int RefundCount { get; set; }
        public string BottomTips { get; set; }
        public string SearchDefaultTips { get; set; }
        public bool HasReadUnRefundOrder { get; set; }
        public int ShareType { get; set; }
        public string TrackingInfo { get; set; }
        public List<object> ListTypes { get; set; }
        public Dictionary<string, object> EnvelopeShareTip { get; set; }
        public Dictionary<string, object> ActivityShareTip { get; set; }
        public string UgcPopUpsInfo { get; set; }
        public List<OrderContract> OrderList { get; set; }

        public OrderListResponseContract()
        {
            OrderList = new List<OrderContract>();
            ListTypes = new List<object>();
            WaitTime = -1;
        }

        public static OrderListResponseContract FromJson(string json)
        {
            Dictionary<string, object> root = ContractJson.ReadRoot(json);
            OrderListResponseContract result = new OrderListResponseContract
            {
                Type = ContractJson.String(root, "type"),
                Cursor = ContractJson.String(root, "cursor"),
                MultiCursor = ContractJson.String(root, "multi_cursor"),
                HasMore = ContractJson.Integer(root, 0, "hasmore", "hasMore", "HasMore"),
                UnCommentCount = ContractJson.Integer(root, 0, "uncomment_count", "unCommentCount", "UnCommentCount"),
                // 官方 fromJson 直接判断非零；属性名中的 Read 不改变线上 unread 键的含义。
                HasReadUnCommentOrder = ContractJson.Boolean(root, "has_unread_comment_order", "hasReadUnCommentOrder", "HasReadUnCommentOrder"),
                UnreadUncommentTips = ContractJson.String(root, "unread_uncomment_tips"),
                WaitTime = ContractJson.Integer(root, -1, "wait_time"),
                RefundCount = ContractJson.Integer(root, 0, "refund_count"),
                BottomTips = ContractJson.String(root, "bottom_tips"),
                SearchDefaultTips = ContractJson.String(root, "search_default_tips"),
                HasReadUnRefundOrder = ContractJson.Boolean(root, "has_unread_refund_order", "hasReadUnRefundOrder", "HasReadUnRefundOrder"),
                ShareType = ContractJson.Integer(root, 0, "share_type"),
                TrackingInfo = ContractJson.String(root, "tracking_info"),
                UgcPopUpsInfo = ContractJson.String(root, "ugc_pop_ups_info")
            };
            foreach (object typeItem in ContractJson.Array(root, "list_types"))
                result.ListTypes.Add(typeItem);
            object envTip = ContractJson.Value(root, "envelope_share_tip");
            if (envTip != null)
                result.EnvelopeShareTip = ContractJson.Object(envTip, "envelope_share_tip");
            object actTip = ContractJson.Value(root, "activity_share_tip");
            if (actTip != null)
                result.ActivityShareTip = ContractJson.Object(actTip, "activity_share_tip");
            foreach (object item in ContractJson.Array(root, "digestlist", "orderList", "OrderList"))
                result.OrderList.Add(ContractJson.ReadOrder(ContractJson.Object(item, "digestlist[]")));
            return result;
        }

        public string ToJson()
        {
            List<object> orders = new List<object>();
            foreach (OrderContract order in OrderList ?? new List<OrderContract>())
            {
                if (order == null)
                    throw new InvalidOperationException("订单列表不能包含空订单。");
                orders.Add(ContractJson.WriteOrder(order));
            }
            // 下列响应键均来自 OrderListResponse_fromJson.java 的原始字面量。
            Dictionary<string, object> root = new Dictionary<string, object>
            {
                { "type", Type }, { "cursor", Cursor }, { "multi_cursor", MultiCursor },
                { "hasmore", HasMore }, { "uncomment_count", UnCommentCount },
                { "has_unread_comment_order", HasReadUnCommentOrder ? 1 : 0 },
                { "unread_uncomment_tips", UnreadUncommentTips }, { "digestlist", orders },
                { "wait_time", WaitTime }, { "list_types", ListTypes ?? new List<object>() },
                { "refund_count", RefundCount },
                { "bottom_tips", BottomTips }, { "search_default_tips", SearchDefaultTips },
                { "has_unread_refund_order", HasReadUnRefundOrder ? 1 : 0 },
                { "share_type", ShareType }, { "tracking_info", TrackingInfo },
                { "envelope_share_tip", EnvelopeShareTip },
                { "activity_share_tip", ActivityShareTip },
                { "ugc_pop_ups_info", UgcPopUpsInfo }
            };
            return ContractJson.Serializer().Serialize(root);
        }
    }

    internal static class ContractJson
    {
        internal static JavaScriptSerializer Serializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = 16 * 1024 * 1024, RecursionLimit = 100 };
        }

        internal static Dictionary<string, object> ReadRoot(string json)
        {
            if (System.String.IsNullOrWhiteSpace(json))
                throw new FormatException("JSON 内容不能为空。");
            try
            {
                return Object(Serializer().DeserializeObject(json), "根节点");
            }
            catch (ArgumentException exception)
            {
                throw new FormatException("JSON 内容无效。", exception);
            }
            catch (InvalidOperationException exception)
            {
                throw new FormatException("JSON 内容或嵌套深度无效。", exception);
            }
        }

        internal static Dictionary<string, object> Object(object value, string field)
        {
            Dictionary<string, object> result = value as Dictionary<string, object>;
            if (result == null)
                throw new FormatException(field + " 必须是 JSON 对象。");
            return result;
        }

        internal static object Value(Dictionary<string, object> map, params string[] keys)
        {
            object value;
            foreach (string key in keys)
                if (map.TryGetValue(key, out value))
                    return value;
            // 本地导出历史记录同时接受 C# 属性名及常见 lowerCamelCase 名称。
            foreach (string key in keys)
            {
                string pascal = PascalCase(key);
                if (map.TryGetValue(pascal, out value))
                    return value;
                string camel = Char.ToLowerInvariant(pascal[0]) + pascal.Substring(1);
                if (map.TryGetValue(camel, out value))
                    return value;
            }
            return null;
        }

        private static string PascalCase(string value)
        {
            StringBuilder result = new StringBuilder();
            bool capitalize = true;
            foreach (char character in value)
            {
                if (character == '_')
                {
                    capitalize = true;
                    continue;
                }
                result.Append(capitalize ? Char.ToUpperInvariant(character) : character);
                capitalize = false;
            }
            return result.ToString();
        }

        internal static IEnumerable<object> Array(Dictionary<string, object> map, params string[] keys)
        {
            object value = Value(map, keys);
            if (value == null)
                return new object[0];
            IEnumerable<object> result = value as IEnumerable<object>;
            if (result == null)
                throw new FormatException(keys[0] + " 必须是 JSON 数组。");
            return result;
        }

        internal static string String(Dictionary<string, object> map, params string[] keys)
        {
            object value = Value(map, keys);
            if (value == null)
                return System.String.Empty;
            if (value is string || value is bool || IsNumber(value))
                return Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            throw new FormatException(keys[0] + " 必须是文本或标量。");
        }

        internal static int Integer(Dictionary<string, object> map, int fallback, params string[] keys)
        {
            long value = Long(map, fallback, keys);
            if (value < Int32.MinValue || value > Int32.MaxValue)
                throw new FormatException(keys[0] + " 超出 32 位整数范围。");
            return (int)value;
        }

        internal static long Long(Dictionary<string, object> map, long fallback, params string[] keys)
        {
            object value = Value(map, keys);
            if (value == null)
                return fallback;
            decimal number;
            if ((!IsNumber(value) && !(value is string)) ||
                !Decimal.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number) ||
                Decimal.Truncate(number) != number || number < Int64.MinValue || number > Int64.MaxValue)
                throw new FormatException(keys[0] + " 必须是范围内的整数。");
            return (long)number;
        }

        internal static double Number(Dictionary<string, object> map, params string[] keys)
        {
            object value = Value(map, keys);
            if (value == null)
                return 0;
            double number;
            if ((!IsNumber(value) && !(value is string)) ||
                !Double.TryParse(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture),
                    System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number) ||
                Double.IsNaN(number) || Double.IsInfinity(number))
                throw new FormatException(keys[0] + " 必须是有限数值。");
            return number;
        }

        internal static bool Boolean(Dictionary<string, object> map, params string[] keys)
        {
            object value = Value(map, keys);
            if (value == null)
                return false;
            if (value is bool)
                return (bool)value;
            bool boolean;
            string text = value as string;
            if (text != null && System.Boolean.TryParse(text, out boolean))
                return boolean;
            return Long(map, 0, keys) != 0;
        }

        private static bool IsNumber(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                value is int || value is uint || value is long || value is ulong ||
                value is float || value is double || value is decimal;
        }

        internal static OrderContract ReadOrder(Dictionary<string, object> map)
        {
            OrderContract order = new OrderContract
            {
                OrderId = Long(map, 0, "order_id"), PoiId = Long(map, 0, "poi_id"),
                HashId = String(map, "hash_id"), PoiIdStr = String(map, "poi_id_str"),
                PoiName = String(map, "poi_name"), StatusDescription = String(map, "status_description"),
                OrderTime = String(map, "order_time"), FormatOrderTime = String(map, "format_order_time"),
                ArrivalTimeText = String(map, "arrival_time_text"), RiderName = String(map, "rider_name"),
                RiderPhone = String(map, "rider_phone"), RecipientName = String(map, "recipient_name"),
                RecipientPhone = String(map, "recipient_phone"), RecipientAddress = String(map, "recipient_address"),
                Caution = String(map, "caution"), FoodNames = String(map, "food_names"),
                Status = Integer(map, 0, "status"), DeliveryStatus = Integer(map, 0, "delivery_status"),
                PayStatus = Integer(map, 0, "pay_status"), Latitude = Integer(map, 0, "latitude"),
                Longitude = Integer(map, 0, "longitude"), ProductCount = Integer(map, 0, "product_count"),
                Total = Number(map, "total"), BoxFeeTotal = Number(map, "box_fee_total"),
                ShippingFee = Number(map, "shipping_fee"), DiscountTotal = Number(map, "discount_total"),
                RiderProgressRatio = Number(map, "rider_progress_ratio"), IsComment = Boolean(map, "is_comment"),
                CanRefund = Boolean(map, "can_refund")
            };
            foreach (object item in Array(map, "food_items", "food_list"))
            {
                Dictionary<string, object> food = Object(item, "food_items[]");
                order.FoodItems.Add(new OrderFoodItemContract
                {
                    FoodName = String(food, "food_name", "name"), SpecText = String(food, "spec_text"),
                    Count = Integer(food, 0, "count"), Price = Number(food, "price")
                });
            }
            foreach (object item in Array(map, "tracking_steps"))
            {
                Dictionary<string, object> step = Object(item, "tracking_steps[]");
                order.TrackingSteps.Add(new RiderTrackingStepContract
                {
                    TimeText = String(step, "time_text"), Title = String(step, "title"), Subtitle = String(step, "subtitle"),
                    IsCompleted = Boolean(step, "is_completed"), IsCurrent = Boolean(step, "is_current")
                });
            }
            foreach (object item in Array(map, "im_messages"))
            {
                Dictionary<string, object> message = Object(item, "im_messages[]");
                order.ImMessages.Add(new ImChatMessageContract
                {
                    SenderRole = String(message, "sender_role"), SenderName = String(message, "sender_name"),
                    TimeText = String(message, "time_text"), Content = String(message, "content"), IsSelf = Boolean(message, "is_self")
                });
            }
            foreach (object item in Array(map, "smart_reply_suggestions"))
            {
                if (!(item is string))
                    throw new FormatException("smart_reply_suggestions[] 必须是文本。");
                order.SmartReplySuggestions.Add((string)item);
            }
            return order;
        }

        internal static Dictionary<string, object> WriteOrder(OrderContract order)
        {
            List<object> foods = new List<object>();
            foreach (OrderFoodItemContract food in order.FoodItems ?? new List<OrderFoodItemContract>())
            {
                if (food == null) throw new InvalidOperationException("订单食品列表不能包含空项目。");
                foods.Add(new Dictionary<string, object>
                {
                    { "food_name", food.FoodName }, { "spec_text", food.SpecText },
                    { "count", food.Count }, { "price", Finite(food.Price, "price") }
                });
            }
            List<object> steps = new List<object>();
            foreach (RiderTrackingStepContract step in order.TrackingSteps ?? new List<RiderTrackingStepContract>())
            {
                if (step == null) throw new InvalidOperationException("订单轨迹列表不能包含空项目。");
                steps.Add(new Dictionary<string, object>
                {
                    { "time_text", step.TimeText }, { "title", step.Title }, { "subtitle", step.Subtitle },
                    { "is_completed", step.IsCompleted }, { "is_current", step.IsCurrent }
                });
            }
            List<object> messages = new List<object>();
            foreach (ImChatMessageContract message in order.ImMessages ?? new List<ImChatMessageContract>())
            {
                if (message == null) throw new InvalidOperationException("聊天列表不能包含空消息。");
                messages.Add(new Dictionary<string, object>
                {
                    { "sender_role", message.SenderRole }, { "sender_name", message.SenderName },
                    { "time_text", message.TimeText }, { "content", message.Content }, { "is_self", message.IsSelf }
                });
            }
            List<string> suggestions = order.SmartReplySuggestions ?? new List<string>();
            if (suggestions.Any(item => item == null))
                throw new InvalidOperationException("快捷回复列表不能包含空文本。");
            // Order 仅有字段清单证据；这些字段及桌面扩展统一使用 snake_case 本地交换格式。
            return new Dictionary<string, object>
            {
                { "order_id", order.OrderId }, { "poi_id", order.PoiId }, { "hash_id", order.HashId },
                { "poi_id_str", order.PoiIdStr }, { "poi_name", order.PoiName }, { "status_description", order.StatusDescription },
                { "order_time", order.OrderTime }, { "format_order_time", order.FormatOrderTime },
                { "arrival_time_text", order.ArrivalTimeText }, { "rider_name", order.RiderName },
                { "rider_phone", order.RiderPhone }, { "recipient_name", order.RecipientName },
                { "recipient_phone", order.RecipientPhone }, { "recipient_address", order.RecipientAddress },
                { "caution", order.Caution }, { "food_names", order.FoodNames }, { "status", order.Status },
                { "delivery_status", order.DeliveryStatus }, { "pay_status", order.PayStatus },
                { "latitude", order.Latitude }, { "longitude", order.Longitude }, { "product_count", order.ProductCount },
                { "total", Finite(order.Total, "total") }, { "box_fee_total", Finite(order.BoxFeeTotal, "box_fee_total") },
                { "shipping_fee", Finite(order.ShippingFee, "shipping_fee") }, { "discount_total", Finite(order.DiscountTotal, "discount_total") },
                { "rider_progress_ratio", Finite(order.RiderProgressRatio, "rider_progress_ratio") },
                { "is_comment", order.IsComment }, { "can_refund", order.CanRefund },
                { "food_items", foods }, { "tracking_steps", steps }, { "im_messages", messages },
                { "smart_reply_suggestions", suggestions }
            };
        }

        private static double Finite(double value, string field)
        {
            if (Double.IsNaN(value) || Double.IsInfinity(value))
                throw new InvalidOperationException(field + " 必须是有限数值。");
            return value;
        }
    }

    public static class ShopCartCalculatorEngine
    {
        public static ShopCartPriceContract Calculate(PoiContract poi, IList<ShopCartItemContract> items, ShenQuanCouponContract selectedCoupon)
        {
            if (poi == null)
                throw new ArgumentNullException("poi");

            decimal threshold = Money(poi.MinPrice, "poi.MinPrice");
            decimal shipping = Money(poi.ShippingFee, "poi.ShippingFee");
            decimal originalShipping = Math.Max(shipping, Money(poi.OriginShippingFee, "poi.OriginShippingFee"));
            decimal originalGoods = 0;
            decimal goods = 0;
            decimal boxes = 0;
            int count = 0;
            List<string> identityParts = new List<string>();
            if (items != null)
            {
                foreach (ShopCartItemContract item in items)
                {
                    if (item == null)
                        throw new ArgumentException("购物车中不能包含空条目。", "items");
                    if (item.Count < 0)
                        throw new ArgumentOutOfRangeException("items", "商品数量不能为负数。");
                    if (item.Count == 0)
                        continue;
                    if (item.PoiId != 0 && item.PoiId != poi.Id)
                        throw new ArgumentException("购物车商品必须属于当前门店。", "items");
                    if (item.PocketId != 1 && item.PocketId != 2)
                        throw new ArgumentException("分装口袋编号仅支持 1 或 2。", "items");
                    decimal price = Money(item.UnitPrice, "items.UnitPrice");
                    decimal origin = Money(item.OriginUnitPrice, "items.OriginUnitPrice");
                    if (origin == 0)
                        origin = price;
                    decimal box = Money(item.BoxFeePerUnit, "items.BoxFeePerUnit");
                    goods += price * item.Count;
                    originalGoods += origin * item.Count;
                    // Count 是实际件数；口袋编号与 NxItemCount 不重复增加份数或打包费。
                    boxes += box * item.Count;
                    count = checked(count + item.Count);
                    identityParts.Add(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                        "{0}:{1}:{2}:{3}:{4}:{5}:{6}:{7}", item.SpuId, item.SkuId,
                        item.PocketId, item.Count, price, origin, box, item.SpecSummary ?? string.Empty));
                }
            }
            goods = Round(goods);
            originalGoods = Round(originalGoods);
            boxes = Round(boxes);
            // 起送使用折前商品价加打包费；活动门槛使用商品现价，不含打包和配送。
            decimal fullReduction = 0;
            FullReductionTier nextTier = null;
            if (poi.FullReductionRules != null)
            {
                foreach (FullReductionTier tier in poi.FullReductionRules)
                {
                    if (tier == null)
                        continue;
                    decimal tierThreshold = Money(tier.Threshold, "poi.FullReductionRules.Threshold");
                    decimal tierReduction = Money(tier.ReduceAmount, "poi.FullReductionRules.ReduceAmount");
                    if (goods >= tierThreshold && count > 0)
                        fullReduction = Math.Max(fullReduction, tierReduction);
                }
                foreach (FullReductionTier tier in poi.FullReductionRules)
                {
                    if (tier == null || Money(tier.Threshold, "poi.FullReductionRules.Threshold") <= goods ||
                        Money(tier.ReduceAmount, "poi.FullReductionRules.ReduceAmount") <= fullReduction)
                        continue;
                    if (nextTier == null || tier.Threshold < nextTier.Threshold ||
                        (tier.Threshold == nextTier.Threshold && tier.ReduceAmount > nextTier.ReduceAmount))
                        nextTier = tier;
                }
            }
            fullReduction = Math.Min(goods, Round(fullReduction));
            decimal couponAmount = 0;
            if (selectedCoupon != null && !selectedCoupon.IsUsed && count > 0 &&
                (selectedCoupon.ApplicablePoiId == 0 || selectedCoupon.ApplicablePoiId == poi.Id))
            {
                decimal couponThreshold = Money(selectedCoupon.Threshold, "selectedCoupon.Threshold");
                decimal faceValue = Money(selectedCoupon.Amount, "selectedCoupon.Amount");
                if (goods >= couponThreshold)
                    couponAmount = Math.Min(goods - fullReduction, faceValue);
            }
            couponAmount = Round(couponAmount);
            decimal discountedGoods = goods - fullReduction - couponAmount;
            decimal originalAndBoxes = originalGoods + boxes;
            bool canCheckout = count > 0 && originalAndBoxes >= threshold;
            decimal gap = Math.Max(0, threshold - originalAndBoxes);
            string preview = string.Empty;
            if (nextTier != null && count > 0)
            {
                preview = "再买¥" + TextMoney((decimal)nextTier.Threshold - goods) +
                    "可享满¥" + TextMoney((decimal)nextTier.Threshold) + "减¥" + TextMoney((decimal)nextTier.ReduceAmount);
            }
            else if (fullReduction > 0)
                preview = "已享满减¥" + TextMoney(fullReduction);

            // 空车不计算配送费；现价优惠、满减、神券和配送减免分别记账。
            if (count == 0)
            {
                shipping = 0;
                originalShipping = 0;
            }
            decimal shippingReduction = originalShipping - shipping;
            ShopCartPriceContract result = new ShopCartPriceContract
            {
                ShoppingCartUniqueId = CreateCartId(poi.Id, identityParts),
                ThresholdPrice = (double)threshold,
                TotalAndBoxPrice = (double)originalAndBoxes,
                TotalBoxPrice = (double)boxes,
                TotalDiscountedPrice = (double)discountedGoods,
                TotalDiscountedAndBoxPriceWithoutGoodCoupon = (double)(goods - fullReduction + boxes),
                TotalDiscountedAndBoxPrice = (double)(discountedGoods + boxes),
                OriginalShippingFee = (double)originalShipping,
                ShippingFeeReduce = (double)shippingReduction,
                FinalShippingFee = (double)shipping,
                AppliedCouponAmount = (double)couponAmount,
                AppliedFullReductionAmount = (double)fullReduction,
                TotalSavedAmount = (double)Math.Max(0, originalGoods - discountedGoods + shippingReduction),
                TotalItemCount = count,
                PreviewSubTip = preview,
                CanCheckout = canCheckout,
                CheckoutButtonText = canCheckout ? "去结算" : (gap > 0 ? "差¥" + TextMoney(gap) + "起送" : "请选择商品")
            };
            result.CrossTip = result.TotalSavedAmount > 0 ? "已优惠¥" + TextMoney((decimal)result.TotalSavedAmount) : string.Empty;
            return result;
        }

        private static decimal Money(double value, string parameter)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0 || value > (double)decimal.MaxValue)
                throw new ArgumentOutOfRangeException(parameter, "金额必须是有限的非负数。");
            try
            {
                return Round((decimal)value);
            }
            catch (OverflowException)
            {
                throw new ArgumentOutOfRangeException(parameter, "金额超过支持范围。");
            }
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string TextMoney(decimal value)
        {
            return Round(value).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string CreateCartId(long poiId, List<string> parts)
        {
            parts.Sort(StringComparer.Ordinal);
            string identity = poiId.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + string.Join("|", parts);
            using (SHA256 hash = SHA256.Create())
            {
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(identity))).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }

    public static class WaimaiStorageService
    {
        private static readonly object StorageLock = new object();

        public static Task SaveSnapshotAsync(WaimaiPersistedSnapshot snapshot, string customFilePath = null)
        {
            return Task.Run(delegate
            {
                if (snapshot == null)
                    throw new ArgumentNullException("snapshot");
                // 路径处理、序列化、加密和磁盘访问全部在线程池中执行。
                lock (StorageLock)
                {
                    string path = ResolvePath(customFilePath);
                    string directory = Path.GetDirectoryName(path);
                    Directory.CreateDirectory(directory);
                    JavaScriptSerializer serializer = CreateSerializer();
                    byte[] plaintext = Encoding.UTF8.GetBytes(serializer.Serialize(snapshot));
                    byte[] ciphertext;
                    try
                    {
                        ciphertext = ProtectedData.Protect(plaintext, null, DataProtectionScope.CurrentUser);
                    }
                    finally
                    {
                        Array.Clear(plaintext, 0, plaintext.Length);
                    }
                    string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    try
                    {
                        // 同目录仅写密文，再原子替换，避免中断时破坏上一份可读快照。
                        using (FileStream stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                        {
                            stream.Write(ciphertext, 0, ciphertext.Length);
                            stream.Flush(true);
                        }
                        if (File.Exists(path))
                            File.Replace(temporary, path, null);
                        else
                            File.Move(temporary, path);
                    }
                    finally
                    {
                        if (File.Exists(temporary))
                            File.Delete(temporary);
                    }
                }
            });
        }

        public static Task<WaimaiPersistedSnapshot> LoadSnapshotAsync(string customFilePath = null)
        {
            return Task.Run(delegate
            {
                lock (StorageLock)
                {
                    string path = ResolvePath(customFilePath);
                    byte[] ciphertext;
                    try
                    {
                        ciphertext = File.ReadAllBytes(path);
                    }
                    catch (FileNotFoundException)
                    {
                        return null;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        return null;
                    }
                    byte[] plaintext = ProtectedData.Unprotect(ciphertext, null, DataProtectionScope.CurrentUser);
                    try
                    {
                        WaimaiPersistedSnapshot snapshot = CreateSerializer().Deserialize<WaimaiPersistedSnapshot>(Encoding.UTF8.GetString(plaintext));
                        if (snapshot == null)
                            throw new InvalidDataException("快照内容为空或不是有效的状态对象。");
                        return snapshot;
                    }
                    finally
                    {
                        Array.Clear(plaintext, 0, plaintext.Length);
                    }
                }
            });
        }

        private static string ResolvePath(string customFilePath)
        {
            string path = customFilePath ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MeituanWaimaiWindows", "state.dpapi");
            return Path.GetFullPath(path);
        }

        private static JavaScriptSerializer CreateSerializer()
        {
            return new JavaScriptSerializer { MaxJsonLength = 16777216, RecursionLimit = 100 };
        }
    }

    // 以下全部为确定性的本地演示数据，不代表官方实时商家、优惠、销量或配送信息。
    public static class WaimaiSeedDataFactory
    {
        public static List<DynamicTabContract> CreateDefaultTabs()
        {
            // 首页、我的完整类名见 research/contracts；大促完整类名见逆向报告。
            // 现有证据仅提供会员与订单片段的短类名，因此保留短名而不猜测包路径。
            return new List<DynamicTabContract>
            {
                new DynamicTabContract { TabIndex = 0, BusinessId = 1, Title = "首页", Subtitle = "附近好店", FragmentClassName = "com.sankuai.waimai.business.page.home.WMHomePageFragment", IconGlyph = "\uE80F", IsSelected = true },
                new DynamicTabContract { TabIndex = 1, BusinessId = 9, Title = "神券会员", Subtitle = "天天有神券", FragmentClassName = "WMVIPCardKNBFragment", IconGlyph = "\uE734", BubbleTagText = "膨胀立减" },
                new DynamicTabContract { TabIndex = 2, BusinessId = 10, Title = "大促神抢手", Subtitle = "好价限时抢", FragmentClassName = "com.sankuai.waimai.business.page.home.tab.TabKnbFragment", IconGlyph = "\uE945", BubbleTagText = "9.9起", IsProminentArcTab = true },
                new DynamicTabContract { TabIndex = 3, BusinessId = 2, Title = "订单", Subtitle = "查看配送进度", FragmentClassName = "OrderListMPFragment", IconGlyph = "\uE8A5", UnreadBadgeCount = 2 },
                new DynamicTabContract { TabIndex = 4, BusinessId = 3, Title = "我的", Subtitle = "地址与资产", FragmentClassName = "com.sankuai.waimai.business.page.mine.MineMPFragment", IconGlyph = "\uE77B" }
            };
        }

        public static List<KingKongChannelContract> CreateKingKongChannels()
        {
            string[] titles = { "美食", "超市便利", "水果买菜", "甜品饮品", "鲜花绿植", "早餐", "医药健康", "跑腿代购", "到店自取", "神抢手" };
            string[] subtitles = { "附近好吃的", "日用随时补", "新鲜到家", "下午茶时光", "把心意送到", "热乎开启一天", "常备安心", "帮买帮送", "顺路带一份", "限时好价" };
            string[] glyphs = { "🍜", "🛒", "🍎", "🧋", "💐", "🥟", "💊", "🛵", "🥡", "⚡" };
            string[] accents = { "#F5A623", "#45A86B", "#79A72F", "#C78358", "#D76D92", "#D79236", "#4484C7", "#5275C4", "#BE8053", "#E66542" };
            string[] backgrounds = { "#FFF3D6", "#EAF7EE", "#F1F7E1", "#FFF0E6", "#FDEAF1", "#FFF4DF", "#EAF3FF", "#EDF1FF", "#FFF3E8", "#FFF0E8" };
            List<KingKongChannelContract> channels = new List<KingKongChannelContract>();
            for (int i = 0; i < titles.Length; i++)
            {
                channels.Add(new KingKongChannelContract
                {
                    ChannelId = i + 1,
                    CategoryBuzType = i == 0 ? 0 : i + 1,
                    Title = titles[i], Subtitle = subtitles[i], IconGlyph = glyphs[i],
                    AccentHex = accents[i], IconBgHex = backgrounds[i],
                    BadgeText = i == 0 ? "好店" : (i == 9 ? "限时抢" : ""),
                    // 对齐 APK 的金刚区推荐模块与分类信息流模块。
                    ModuleId = i == 0 ? "future_kingkong_rcmd" : "future_kingkong_tabs",
                    FeedUrl = i == 0 ? "mtmall/v1/home/market" : "mtmall/v1/home/feed"
                });
            }
            return channels;
        }

        public static List<PoiContract> CreateDefaultPois()
        {
            List<PoiContract> pois = new List<PoiContract>();
            PoiContract home = NewPoi(1001, "巷口小馆·家常菜（科技园店）", 4.8, 2368, 20, 28, 2.5, 0.8, "热锅现炒，认真做好每一餐", "#B96E37", "🍚");
            home.MenuCategories = new List<string> { "招牌下饭菜", "家常小炒", "汤羹与主食" };
            home.Foods.Add(NewFood(home.Id, 1, "招牌黄焖鸡米饭", "鸡腿肉搭配香菇、土豆慢焖，附一份米饭。", "招牌下饭菜", 26.8, "🍗", "rice"));
            home.Foods.Add(NewFood(home.Id, 2, "鱼香肉丝盖饭", "肉丝、木耳与笋丝现炒，酸甜微辣。", "招牌下饭菜", 23.8, "🍛", "rice"));
            home.Foods.Add(NewFood(home.Id, 3, "农家小炒肉", "五花肉配青椒大火快炒，附米饭。", "家常小炒", 28.8, "🥩", "rice"));
            home.Foods.Add(NewFood(home.Id, 4, "番茄炒鸡蛋盖饭", "熟番茄炒嫩鸡蛋，汤汁拌饭更香。", "家常小炒", 19.8, "🍅", "rice"));
            home.Foods.Add(NewFood(home.Id, 5, "香菇滑鸡蒸饭", "鸡肉腌制入味，连同香菇蒸制。", "汤羹与主食", 24.8, "🍲", "rice"));
            home.Foods.Add(NewFood(home.Id, 6, "玉米排骨汤", "排骨与甜玉米小火炖煮，清淡暖胃。", "汤羹与主食", 16.8, "🥣", "soup"));
            pois.Add(home);

            PoiContract noodles = NewPoi(1002, "陈记潮汕牛肉粿条（粤海店）", 4.9, 1816, 18, 25, 1.5, 1.2, "鲜汤现烫，一碗潮汕家乡味", "#B84F3E", "🍜");
            noodles.MenuCategories = new List<string> { "招牌粿条", "干拌与炒粉", "小吃汤品" };
            noodles.Foods.Add(NewFood(noodles.Id, 1, "鲜切牛肉汤粿条", "鲜切牛肉配手工粿条，汤底清鲜。", "招牌粿条", 29.8, "🍜", "noodle"));
            noodles.Foods.Add(NewFood(noodles.Id, 2, "牛肉丸双拼粿条", "牛肉丸与牛筋丸双拼，搭配生菜。", "招牌粿条", 27.8, "🍜", "noodle"));
            noodles.Foods.Add(NewFood(noodles.Id, 3, "沙茶牛肉拌粿条", "沙茶酱拌鲜切牛肉与粿条，酱香浓郁。", "干拌与炒粉", 31.8, "🥢", "noodle"));
            noodles.Foods.Add(NewFood(noodles.Id, 4, "潮汕炒粿条", "粿条搭配鸡蛋、豆芽与牛肉现炒。", "干拌与炒粉", 25.8, "🍝", "noodle"));
            noodles.Foods.Add(NewFood(noodles.Id, 5, "手打牛肉丸汤", "手打牛肉丸搭配白萝卜与芹菜末。", "小吃汤品", 18.8, "🥣", "soup"));
            noodles.Foods.Add(NewFood(noodles.Id, 6, "潮汕蚝仔烙", "鲜蚝配薯粉、鸡蛋煎制，外脆内嫩。", "小吃汤品", 32.8, "🦪", "snack"));
            pois.Add(noodles);

            PoiContract spicy = NewPoi(1003, "蜀味冒菜·鲜煮麻辣烫（高新园店）", 4.7, 3260, 22, 32, 3, 1.6, "菜品现选现煮，麻辣鲜香有滋味", "#AA3F32", "🌶️");
            spicy.MenuCategories = new List<string> { "人气冒菜", "双人分享", "川味小吃" };
            spicy.Foods.Add(NewFood(spicy.Id, 1, "肥牛冒菜单人餐", "肥牛、土豆、莲藕与时蔬，附米饭。", "人气冒菜", 29.9, "🥘", "spicy"));
            spicy.Foods.Add(NewFood(spicy.Id, 2, "藤椒鸡腿冒菜", "去骨鸡腿肉搭配藤椒汤底与四种时蔬。", "人气冒菜", 27.9, "🍗", "spicy"));
            spicy.Foods.Add(NewFood(spicy.Id, 3, "番茄菌菇冒菜", "番茄汤底配菌菇、豆腐与应季蔬菜。", "人气冒菜", 23.9, "🍅", "spicy"));
            spicy.Foods.Add(NewFood(spicy.Id, 4, "毛肚肥牛双人套餐", "毛肚、肥牛与蔬菜拼盘，附两份米饭。", "双人分享", 59.9, "🥘", "spicy"));
            spicy.Foods.Add(NewFood(spicy.Id, 5, "红糖糍粑", "糯米糍粑炸至金黄，另配红糖汁与黄豆粉。", "川味小吃", 12.9, "🍡", "snack"));
            spicy.Foods.Add(NewFood(spicy.Id, 6, "现炸酥肉", "猪肉条裹浆现炸，酥脆椒香。", "川味小吃", 16.9, "🥓", "snack"));
            pois.Add(spicy);

            PoiContract salad = NewPoi(1004, "小岛轻食·沙拉饭（科兴店）", 4.8, 1452, 25, 30, 2, 1.9, "谷物、蔬菜与优质蛋白，好好吃一餐", "#608758", "🥗");
            salad.MenuCategories = new List<string> { "能量谷物碗", "缤纷沙拉", "暖心配餐" };
            salad.Foods.Add(NewFood(salad.Id, 1, "香煎鸡胸藜麦碗", "鸡胸肉、藜麦糙米、西兰花与南瓜。", "能量谷物碗", 32.8, "🥗", "salad"));
            salad.Foods.Add(NewFood(salad.Id, 2, "照烧鸡腿糙米饭", "去骨鸡腿搭配糙米与三色蔬菜。", "能量谷物碗", 34.8, "🍱", "salad"));
            salad.Foods.Add(NewFood(salad.Id, 3, "香烤南瓜鹰嘴豆沙拉", "烤南瓜、鹰嘴豆、混合生菜与小番茄。", "缤纷沙拉", 28.8, "🥗", "salad"));
            salad.Foods.Add(NewFood(salad.Id, 4, "牛油果鲜虾沙拉", "鲜虾仁、牛油果、玉米与混合生菜。", "缤纷沙拉", 38.8, "🥑", "salad"));
            salad.Foods.Add(NewFood(salad.Id, 5, "黑椒牛肉全麦卷", "黑椒牛肉、生菜与彩椒包入全麦饼。", "暖心配餐", 29.8, "🌯", "salad"));
            salad.Foods.Add(NewFood(salad.Id, 6, "奶香南瓜浓汤", "南瓜蒸熟打成浓汤，加入少量牛奶。", "暖心配餐", 15.8, "🥣", "soup"));
            pois.Add(salad);

            PoiContract tea = NewPoi(1005, "茶间里·手作果茶（科技园店）", 4.8, 4216, 15, 22, 0, 0.6, "真茶现萃，鲜果现切", "#B9824B", "🧋");
            tea.BuzType = 3;
            tea.MenuCategories = new List<string> { "鲜果茶", "原叶奶茶", "纯茶与咖啡" };
            tea.Foods.Add(NewFood(tea.Id, 1, "手打柠檬茉莉茶", "香水柠檬手打出香，搭配茉莉茶底。", "鲜果茶", 15.9, "🍋", "tea"));
            tea.Foods.Add(NewFood(tea.Id, 2, "满杯红柚绿茶", "红西柚果肉与绿茶融合，清新回甘。", "鲜果茶", 19.9, "🍊", "tea"));
            tea.Foods.Add(NewFood(tea.Id, 3, "杨枝甘露", "芒果、西柚、西米与椰乳，口感丰富。", "鲜果茶", 22.9, "🥭", "tea"));
            tea.Foods.Add(NewFood(tea.Id, 4, "黑糖珍珠鲜奶", "慢熬黑糖珍珠加入鲜奶。", "原叶奶茶", 18.9, "🧋", "tea"));
            tea.Foods.Add(NewFood(tea.Id, 5, "桂花乌龙奶茶", "桂花乌龙茶香融合醇厚牛乳。", "原叶奶茶", 17.9, "🧋", "tea"));
            tea.Foods.Add(NewFood(tea.Id, 6, "生椰拿铁", "浓缩咖啡与生椰乳融合，椰香浓郁。", "纯茶与咖啡", 21.9, "☕", "tea"));
            pois.Add(tea);

            PoiContract burger = NewPoi(1006, "木炭先生·现烤汉堡（南山店）", 4.7, 2086, 20, 35, 2.8, 2.3, "肉饼现煎，面包现烤", "#715344", "🍔");
            burger.MenuCategories = new List<string> { "现煎汉堡", "小食拼盘", "一人套餐" };
            burger.Foods.Add(NewFood(burger.Id, 1, "经典安格斯牛肉堡", "安格斯牛肉饼、车达芝士、生菜与番茄。", "现煎汉堡", 29.9, "🍔", "burger"));
            burger.Foods.Add(NewFood(burger.Id, 2, "香辣脆鸡腿堡", "整块鸡腿肉现炸，搭配爽脆生菜。", "现煎汉堡", 23.9, "🍔", "burger"));
            burger.Foods.Add(NewFood(burger.Id, 3, "双层芝士牛肉堡", "双层牛肉饼搭配双份芝士与酸黄瓜。", "现煎汉堡", 39.9, "🍔", "burger"));
            burger.Foods.Add(NewFood(burger.Id, 4, "海盐粗薯条", "带皮粗薯条现炸，撒上海盐。", "小食拼盘", 12.9, "🍟", "snack"));
            burger.Foods.Add(NewFood(burger.Id, 5, "蜜汁烤鸡翅", "鸡翅蜜汁腌制后烤至金黄。", "小食拼盘", 19.9, "🍗", "snack"));
            burger.Foods.Add(NewFood(burger.Id, 6, "牛肉堡薯条单人餐", "经典牛肉堡搭配海盐薯条与无糖可乐。", "一人套餐", 39.9, "🍱", "burger"));
            pois.Add(burger);
            return pois;
        }

        public static List<ShenQuanCouponContract> CreateDefaultCoupons()
        {
            List<ShenQuanCouponContract> coupons = new List<ShenQuanCouponContract>
            {
                new ShenQuanCouponContract { CouponId = "demo-shenquan-001", Title = "会员通用神券", Subtitle = "支持神券的门店可用", BaseAmount = 6, Amount = 6, Threshold = 25, InflatedBonus = 4, ApplicablePoiId = 0 },
                new ShenQuanCouponContract { CouponId = "demo-shenquan-002", Title = "家常菜专享神券", Subtitle = "巷口小馆满35元可用", BaseAmount = 5, Amount = 5, Threshold = 35, InflatedBonus = 5, ApplicablePoiId = 1001 },
                new ShenQuanCouponContract { CouponId = "demo-shenquan-003", Title = "果茶下午茶神券", Subtitle = "茶间里满28元可用", BaseAmount = 4, Amount = 4, Threshold = 28, InflatedBonus = 3, ApplicablePoiId = 1005 },
                new ShenQuanCouponContract { CouponId = "demo-shenquan-004", Title = "美味加餐神券", Subtitle = "支持神券的门店满45元可用", BaseAmount = 8, Amount = 8, Threshold = 45, InflatedBonus = 4, ApplicablePoiId = 0 }
            };
            foreach (ShenQuanCouponContract coupon in coupons)
            {
                coupon.ExpireTimeText = "2026-10-31 23:59前有效";
                coupon.TagText = "可膨胀 · 领后再省";
            }
            coupons[0].Inflate();
            return coupons;
        }

        public static List<WmAddressContract> CreateDefaultAddresses()
        {
            return new List<WmAddressContract>
            {
                new WmAddressContract { AddressId = "demo-address-home", ContactName = "林同学", GenderText = "先生", Phone = "138****0001", AddressDetail = "深圳市南山区科技园南区·科苑花园", HouseNumber = "3栋1202室", TagLabel = "家", IsDefault = true, Latitude = 22539120, Longitude = 113951240 },
                new WmAddressContract { AddressId = "demo-address-work", ContactName = "林同学", GenderText = "先生", Phone = "138****0001", AddressDetail = "深圳市南山区科兴科学园", HouseNumber = "A栋8层前台", TagLabel = "公司", Latitude = 22546280, Longitude = 113946550 },
                new WmAddressContract { AddressId = "demo-address-family", ContactName = "陈女士", GenderText = "女士", Phone = "139****0002", AddressDetail = "深圳市南山区白石洲·沙河社区", HouseNumber = "2栋503室", TagLabel = "家人", Latitude = 22542680, Longitude = 113965320 }
            };
        }

        public static List<DeepEatRecommendationContract> CreateDeepEatRecommendations()
        {
            return new List<DeepEatRecommendationContract>
            {
                new DeepEatRecommendationContract { SceneId = "workday-lunch", SceneTitle = "工作日午餐", AiPromptText = "想吃热乎的家常饭，预算30元左右", ReasonText = "黄焖鸡搭配米饭，口味和分量均可选择。", HighlightTag = "热饭暖胃", TargetPoiId = 1001, TargetSpuId = 100101 },
                new DeepEatRecommendationContract { SceneId = "light-dinner", SceneTitle = "晚餐吃清爽一点", AiPromptText = "给我一份带蔬菜和鸡肉的轻食", ReasonText = "鸡胸、藜麦与多种蔬菜搭配，酱汁可另放。", HighlightTag = "荤素搭配", TargetPoiId = 1004, TargetSpuId = 100401 },
                new DeepEatRecommendationContract { SceneId = "tea-break", SceneTitle = "下午茶休息一下", AiPromptText = "想喝清爽果茶，少糖少冰", ReasonText = "柠檬与茉莉茶底清爽，可按喜好选择甜度和冰量。", HighlightTag = "清新果茶", TargetPoiId = 1005, TargetSpuId = 100501 },
                new DeepEatRecommendationContract { SceneId = "share-dinner", SceneTitle = "两个人一起吃", AiPromptText = "两个人想吃香辣的，带米饭", ReasonText = "毛肚肥牛双人餐包含两份米饭，辣度可选择。", HighlightTag = "双人分享", TargetPoiId = 1003, TargetSpuId = 100304 }
            };
        }

        public static List<PromoFlashDealContract> CreatePromoFlashDeals()
        {
            List<PoiContract> pois = CreateDefaultPois();
            double[] prices = { 19.9, 22.9, 19.9, 25.9, 9.9, 19.9 };
            int[] sold = { 76, 58, 83, 64, 91, 72 };
            List<PromoFlashDealContract> deals = new List<PromoFlashDealContract>();
            for (int i = 0; i < pois.Count; i++)
            {
                GoodsSpuContract food = pois[i].Foods[0];
                deals.Add(new PromoFlashDealContract
                {
                    DealId = "demo-flash-" + pois[i].PoiIdStr,
                    Title = food.Name, PoiName = pois[i].Name,
                    DiscountBadge = "神抢手专享", StockTip = "已抢" + sold[i] + "%",
                    TargetPoiId = pois[i].Id, TargetSpuId = food.Id,
                    FlashPrice = prices[i], OriginPrice = food.OriginPrice, SoldPercent = sold[i]
                });
            }
            return deals;
        }

        public static MineProfileContract CreateDefaultMineProfile()
        {
            return new MineProfileContract
            {
                UserId = "local-demo-001", Nickname = "认真吃饭的林同学", VipLevelText = "神券会员",
                PhoneMasked = "138****0001", ShenQuanCount = CreateDefaultCoupons().Count,
                TuanBiPoints = 1280, MonthlySavedOrders = 12, RedPacketBalance = 18.8, MonthlySavedAmount = 86.5
            };
        }

        public static PassportUserContract CreateDefaultPassportUser()
        {
            return new PassportUserContract
            {
                Id = 884201001L,
                Username = "mt_waimai_8842",
                Nickname = "认真吃饭的林同学",
                Mobile = "13800130001",
                MobileMasked = "138****0001",
                Token = "MT_PASSPORT_TK_8842_91293_AUTH",
                Ticket = "ST-91293-MEITUAN-PASSPORT",
                LoginAuthTicket = "LAT-8842-V8",
                AvatarEmoji = "🦘",
                VipBadgeText = "黑金神券会员 · Lv.5",
                Growthlevel = 5,
                Growthvalue = 3680,
                Pointvalue = 1280,
                SaveAmount = 86.5,
                RedPacketBalance = 18.8,
                Haspassword = 1,
                IsLoggedIn = true
            };
        }

        public static List<PassportUserContract> CreatePresetPassportUsers()
        {
            return new List<PassportUserContract>
            {
                CreateDefaultPassportUser(),
                new PassportUserContract
                {
                    Id = 884201002L,
                    Username = "mt_vip_chen",
                    Nickname = "南山干饭王·陈女士",
                    Mobile = "13900130002",
                    MobileMasked = "139****0002",
                    Token = "MT_PASSPORT_TK_8842_91293_VIP2",
                    Ticket = "ST-91293-MEITUAN-VIP2",
                    LoginAuthTicket = "LAT-8842-VIP2",
                    AvatarEmoji = "🧋",
                    VipBadgeText = "尊享神券卡 · Lv.6",
                    Growthlevel = 6,
                    Growthvalue = 6420,
                    Pointvalue = 2890,
                    SaveAmount = 216.0,
                    RedPacketBalance = 36.5,
                    Haspassword = 1,
                    IsLoggedIn = true
                },
                new PassportUserContract
                {
                    Id = 884201003L,
                    Username = "mt_newbie_zhang",
                    Nickname = "科兴夜宵达人·小张",
                    Mobile = "18600180003",
                    MobileMasked = "186****0003",
                    Token = "MT_PASSPORT_TK_8842_91293_NEW3",
                    Ticket = "ST-91293-MEITUAN-NEW3",
                    LoginAuthTicket = "LAT-8842-NEW3",
                    AvatarEmoji = "🍔",
                    VipBadgeText = "新晋吃货 · Lv.2",
                    Growthlevel = 2,
                    Growthvalue = 480,
                    Pointvalue = 320,
                    SaveAmount = 29.0,
                    RedPacketBalance = 12.0,
                    Haspassword = 1,
                    IsLoggedIn = true
                }
            };
        }

        public static List<CashierPaymentMethodContract> CreateDefaultPaymentMethods()
        {
            return new List<CashierPaymentMethodContract>
            {
                new CashierPaymentMethodContract
                {
                    MethodId = "mt_month_pay",
                    Title = "美团月付",
                    Subtitle = "先享后付 · 本单专享随机立减 ¥2.00",
                    RecommendTag = "推荐 · 立减¥2",
                    IconEmoji = "💛",
                    AccentHex = "#FF6B00",
                    InstantDiscount = 2.0,
                    IsSelected = true
                },
                new CashierPaymentMethodContract
                {
                    MethodId = "wechat_pay",
                    Title = "微信支付",
                    Subtitle = "推荐已安装微信客户端的用户使用",
                    RecommendTag = "常用",
                    IconEmoji = "💚",
                    AccentHex = "#07C160",
                    InstantDiscount = 0.0,
                    IsSelected = false
                },
                new CashierPaymentMethodContract
                {
                    MethodId = "alipay",
                    Title = "支付宝",
                    Subtitle = "支持花呗分期与余额宝极速支付",
                    RecommendTag = "",
                    IconEmoji = "💙",
                    AccentHex = "#1677FF",
                    InstantDiscount = 0.0,
                    IsSelected = false
                },
                new CashierPaymentMethodContract
                {
                    MethodId = "mt_bank_card",
                    Title = "美团联名储蓄卡 (尾号8842)",
                    Subtitle = "美团极速付 · 笔笔返团币",
                    RecommendTag = "返团币",
                    IconEmoji = "💳",
                    AccentHex = "#8A5A2B",
                    InstantDiscount = 0.0,
                    IsSelected = false
                }
            };
        }

        public static List<NotificationMessageContract> CreateDefaultNotifications()
        {
            return new List<NotificationMessageContract>
            {
                new NotificationMessageContract
                {
                    MessageId = "msg-001",
                    Category = "订单动态",
                    Title = "骑手小周正在为您火速配送中",
                    Content = "您在「巷口小馆·家常菜（科技园店）」购买的招牌黄焖鸡米饭距您仅剩 450m，请留意电话。",
                    TimeText = "刚刚",
                    IconEmoji = "🛵",
                    IsUnread = true
                },
                new NotificationMessageContract
                {
                    MessageId = "msg-002",
                    Category = "神券福利",
                    Title = "恭喜！您的会员通用神券已膨胀至 ¥10",
                    Content = "今日神券膨胀特权已生效，下单满 ¥25 即可直接立减 ¥10，有效期至今日 23:59。",
                    TimeText = "10分钟前",
                    IconEmoji = "🧧",
                    IsUnread = true
                },
                new NotificationMessageContract
                {
                    MessageId = "msg-003",
                    Category = "商家互动",
                    Title = "「茶间里·手作果茶」回复了您的五星好评",
                    Content = "商家回复：感谢林同学的喜爱！我们的香水柠檬每天清晨鲜切手打，欢迎常来品尝~",
                    TimeText = "昨天 19:15",
                    IconEmoji = "💬",
                    IsUnread = false
                },
                new NotificationMessageContract
                {
                    MessageId = "msg-004",
                    Category = "会员中心",
                    Title = "10月美团外卖省钱月报已生成",
                    Content = "本月您已通过神券省钱卡累计节省 ¥86.50，击败了南山区 92% 的干饭人！",
                    TimeText = "10-20",
                    IconEmoji = "👑",
                    IsUnread = false
                }
            };
        }

        public static List<string> CreateHotSearchKeywords()
        {
            return new List<string>
            {
                "黄焖鸡",
                "牛肉粿条",
                "果茶",
                "冒菜",
                "轻食沙拉",
                "现烤汉堡"
            };
        }

        public static List<OrderContract> CreateDefaultOrders()
        {
            List<PoiContract> pois = CreateDefaultPois();
            List<WmAddressContract> addresses = CreateDefaultAddresses();
            // 状态代码是本地视图演示约定，不能用作线上接口状态码的解释。
            int[] states = { 30, 20, 40, 50, 70 };
            string[] descriptions = { "配送中", "备餐中", "待评价", "已完成", "售后退款" };
            string[] times = { "2026-10-22 11:35:00", "2026-10-22 11:48:00", "2026-10-21 18:20:00", "2026-10-20 12:05:00", "2026-10-19 15:10:00" };
            List<OrderContract> orders = new List<OrderContract>();
            for (int i = 0; i < states.Length; i++)
            {
                PoiContract poi = pois[i];
                WmAddressContract address = addresses[i % addresses.Count];
                GoodsSpuContract first = poi.Foods[0];
                GoodsSpuContract second = poi.Foods[1];
                double foodTotal = first.Skus[0].Price + second.Skus[0].Price;
                double boxTotal = first.Skus[0].BoxFee + second.Skus[0].BoxFee;
                double reduction = 0;
                foreach (FullReductionTier tier in poi.FullReductionRules)
                {
                    if (foodTotal >= tier.Threshold)
                        reduction = Math.Max(reduction, tier.ReduceAmount);
                }
                OrderContract order = new OrderContract
                {
                    OrderId = 202610220001L + i, HashId = "demo-order-20261022-" + (i + 1),
                    PoiId = poi.Id, PoiIdStr = poi.PoiIdStr, PoiName = poi.Name,
                    Status = states[i], StatusDescription = descriptions[i],
                    DeliveryStatus = i == 0 ? 20 : (i == 1 ? 10 : 30), PayStatus = i == 4 ? 2 : 1,
                    OrderTime = times[i], FormatOrderTime = times[i].Substring(5, 11),
                    ArrivalTimeText = i == 0 ? "预计12:08送达" : (i == 1 ? "预计12:25送达" : (i == 4 ? "退款已原路退回" : "已按约定送达")),
                    RiderName = i == 1 ? "待分配骑手" : "骑手小周", RiderPhone = i == 1 ? "" : "170****0042",
                    RecipientName = address.ContactName, RecipientPhone = address.Phone,
                    RecipientAddress = address.AddressDetail + " " + address.HouseNumber,
                    Latitude = address.Latitude, Longitude = address.Longitude,
                    Caution = i == 1 ? "少辣，餐具按需提供" : "请放门口置物架，送达后留言",
                    FoodNames = first.Name + "、" + second.Name, ProductCount = 2,
                    BoxFeeTotal = Math.Round(boxTotal, 2), ShippingFee = poi.ShippingFee,
                    DiscountTotal = Math.Round(reduction, 2), Total = Math.Round(foodTotal + boxTotal + poi.ShippingFee - reduction, 2),
                    RiderProgressRatio = i == 0 ? 0.65 : (i == 1 ? 0 : 1),
                    IsComment = i == 3, CanRefund = i == 0 || i == 1 || i == 2
                };
                order.FoodItems.Add(NewOrderFood(first));
                order.FoodItems.Add(NewOrderFood(second));
                FillOrderProgress(order, i);
                orders.Add(order);
            }
            return orders;
        }

        private static PoiContract NewPoi(long id, string name, double score, int sales, double minimum, int minutes, double shipping, double distance, string slogan, string accent, string emoji)
        {
            PoiContract poi = new PoiContract
            {
                Id = id, PoiIdStr = id.ToString(), Name = name, PoiScore = score,
                FormattedPoiScore = score.ToString("0.0"), MonthSales = sales, MonthSalesTip = "月售" + sales + "+",
                MinPrice = minimum, MinPriceTip = "起送¥" + minimum,
                DeliveryMinutes = minutes, MtDeliveryTime = minutes + "分钟", DistanceKm = distance, Distance = distance + "km",
                ShippingFee = shipping, OriginShippingFee = shipping + 2, BrandSlogan = slogan,
                Bulletin = "欢迎光临！餐品现点现做，如有口味需求请在下单时备注。",
                DeliveryTypeText = "美团专送", AveragePriceTip = "人均¥" + (id == 1005 ? "20" : "30"),
                RankLabelInfo = id == 1001 ? "附近家常菜人气榜" : "附近好店",
                BannerAccentHex = accent, LogoEmoji = emoji, BuzType = 0, State = 1,
                IsBrandGold = id == 1001 || id == 1002 || id == 1005, SupportsShenQuan = true
            };
            poi.FullReductionRules.Add(new FullReductionTier { Threshold = 30, ReduceAmount = 5 });
            poi.FullReductionRules.Add(new FullReductionTier { Threshold = 50, ReduceAmount = 12 });
            poi.FullReductionRules.Add(new FullReductionTier { Threshold = 80, ReduceAmount = 22 });
            poi.ActivityTags.Add(new PoiActivityTagContract { TagText = "30减5 · 50减12 · 80减22", BorderHex = "#F3C4B6", TextHex = "#C85038", BgHex = "#FFF8F4" });
            poi.ActivityTags.Add(new PoiActivityTagContract { TagText = "神券可膨胀", BorderHex = "#F2D58C", TextHex = "#A87020", BgHex = "#FFF8DE" });
            poi.Coupons.Add(new PoiCouponContract { CouponId = "demo-poi-" + id + "-5", Title = "收藏店铺满35减5", Amount = 5, Threshold = 35, IsClaimed = false });
            poi.Coupons.Add(new PoiCouponContract { CouponId = "demo-poi-" + id + "-8", Title = "老友回购满60减8", Amount = 8, Threshold = 60, IsClaimed = true });
            poi.IsFavorite = (id == 1001 || id == 1005);
            poi.MerchantArchive = new PoiMerchantArchiveContract
            {
                Address = "深圳市南山区粤海街道科技园南区高新南四道 " + (id - 980) + " 号一层铺",
                Phone = "0755-86" + (id * 7 % 1000000).ToString("000000"),
                BusinessHours = "周一至周日 09:30 - 23:30 (支持夜宵)",
                DeliveryServiceText = "由 美团专送 提供高品质准时配送服务 · 准时宝保障",
                LicenseNumber = "JY24403050" + id + "8821",
                FoodSafetyGrade = "A级 · 餐饮服务食品安全笑脸公示",
                KitchenInspectionText = "已接入「美团明厨亮灶」后厨实时阳光公示 · 食安险承保中",
                ServiceGuarantees = new List<string>
                {
                    "食安封签 · 外卖专享双层防拆封条",
                    "放心吃 · 品质问题极速理赔",
                    "准时宝 · 超时10分钟起自动赔付",
                    "支持开发票 · 满50元可开具电子发票"
                }
            };
            poi.Reviews.Add(new PoiReviewItemContract
            {
                ReviewId = "rev-" + id + "-01",
                PoiId = id,
                UserName = "干饭人小林",
                AvatarEmoji = "🦘",
                VipLevelText = "黑金会员 Lv.5",
                TimeText = "今天 12:42",
                Score = 5.0,
                StarText = "★★★★★ 5.0 超赞",
                DeliveryTimeTip = minutes + "分钟送达",
                OrderedDishesText = "招牌必点套餐 · 标准份",
                Content = "在「" + name + "」点过好几次了，食材特别新鲜，分量很足，打包盒严实完全没有撒漏，美团专送骑手态度超好！",
                MerchantReply = "商家回复：感谢老顾客的五星认可！我们会继续坚持现点现做，祝您用餐愉快，天天开心~",
                HasPictures = true,
                PictureBadgeText = "📸 晒图×3 · 菜品实拍"
            });
            poi.Reviews.Add(new PoiReviewItemContract
            {
                ReviewId = "rev-" + id + "-02",
                PoiId = id,
                UserName = "南山程序员阿杰",
                AvatarEmoji = "🧑‍💻",
                VipLevelText = "神券卡会员",
                TimeText = "昨天 19:18",
                Score = 4.9,
                StarText = "★★★★★ 4.9 推荐",
                DeliveryTimeTip = (minutes - 3) + "分钟送达",
                OrderedDishesText = "人气双拼套餐",
                Content = "味道很正宗，用美团膨胀神券抵扣了10块钱，性价比无敌！中午工作餐首选这家。",
                MerchantReply = "商家回复：谢谢阿杰支持！用神券下单确实超划算，欢迎常来点餐！",
                HasPictures = true,
                PictureBadgeText = "📸 晒图×2 · 包装完好"
            });
            poi.Reviews.Add(new PoiReviewItemContract
            {
                ReviewId = "rev-" + id + "-03",
                PoiId = id,
                UserName = "爱吃美食的喵",
                AvatarEmoji = "🐱",
                VipLevelText = "尊享吃货 Lv.4",
                TimeText = "2026-10-20",
                Score = 4.5,
                StarText = "★★★★☆ 4.5 满意",
                DeliveryTimeTip = (minutes + 2) + "分钟送达",
                OrderedDishesText = "主厨特色推荐",
                Content = "口味一如既往地稳定，按备注做了少辣，很贴心，还送了餐巾纸。",
                MerchantReply = "商家回复：您的满意是我们最大的动力，备注需求店员都会仔细核对的~",
                HasPictures = false,
                PictureBadgeText = ""
            });
            poi.Reviews.Add(new PoiReviewItemContract
            {
                ReviewId = "rev-" + id + "-04",
                PoiId = id,
                UserName = "匿名用户",
                AvatarEmoji = "🙂",
                VipLevelText = "普通会员",
                TimeText = "2026-10-18",
                Score = 3.5,
                StarText = "★★★☆☆ 3.5 一般",
                DeliveryTimeTip = (minutes + 8) + "分钟送达",
                OrderedDishesText = "单人简餐",
                Content = "饭点高峰期稍微等了一会儿，不过送来还是热乎的，味道还可以。",
                MerchantReply = "商家回复：非常抱歉午高峰让您久等了，我们已增加后厨出餐人手，下次一定更快送达！",
                HasPictures = false,
                PictureBadgeText = ""
            });
            return poi;
        }

        private static GoodsSpuContract NewFood(long poiId, int index, string name, string description, string category, double price, string emoji, string kind)
        {
            long id = poiId * 100 + index;
            bool isTea = kind == "tea";
            double extra = isTea ? 3 : (kind == "soup" || kind == "snack" ? 5 : 6);
            double box = isTea ? 0.5 : 1;
            GoodsSpuContract food = new GoodsSpuContract
            {
                Id = id, PoiId = poiId, Name = name, Description = description, CategoryTag = category,
                MonthSaled = 128 + index * 63, MonthSaledContent = "月售" + (128 + index * 63),
                PraiseContent = "好评" + (91 + index) + "%", LikeRatioDesc = (91 + index) + "%好评",
                ActivityTag = index == 1 ? "招牌推荐" : (index == 2 ? "人气热卖" : ""),
                Unit = isTea ? "杯" : "份", ThumbAccentHex = isTea ? "#F5D8A7" : "#F0D4BA", EmojiIcon = emoji,
                ActivityType = index == 1 ? 1 : 0, Status = 1,
                MinPrice = price, OriginPrice = Math.Round(price + 4, 2), FullDiscountPrice = price, BoxFee = box
            };
            food.Skus.Add(new GoodsSkuContract { SkuId = id * 10 + 1, SpecName = isTea ? "中杯500ml" : "标准份", Price = price, OriginPrice = food.OriginPrice, BoxFee = box, Stock = 88 });
            food.Skus.Add(new GoodsSkuContract { SkuId = id * 10 + 2, SpecName = isTea ? "大杯700ml" : "加量份", Price = Math.Round(price + extra, 2), OriginPrice = Math.Round(price + extra + 4, 2), BoxFee = box, Stock = 66 });
            if (isTea)
            {
                food.AttrGroups.Add(NewAttr("甜度", "标准糖", "少糖", "半糖", "无额外加糖"));
                food.AttrGroups.Add(NewAttr("冰量", "正常冰", "少冰", "去冰"));
            }
            else if (kind == "salad")
            {
                food.AttrGroups.Add(NewAttr("酱汁", "油醋汁", "芝麻酱", "酱汁另放"));
                food.AttrGroups.Add(NewAttr("餐具", "需要餐具", "无需餐具"));
            }
            else if (kind == "noodle")
            {
                food.AttrGroups.Add(NewAttr("配料", "正常配料", "不要香菜", "不要芹菜", "不加葱"));
                food.AttrGroups.Add(NewAttr("辣度", "不辣", "微辣", "中辣"));
            }
            else if (kind == "soup" || kind == "snack")
            {
                food.AttrGroups.Add(NewAttr("口味", "标准口味", "清淡少盐"));
                food.AttrGroups.Add(NewAttr("餐具", "需要餐具", "无需餐具"));
            }
            else if (kind == "burger")
            {
                food.AttrGroups.Add(NewAttr("配料", "标准配料", "不要酸黄瓜", "不要番茄", "不要生菜"));
                food.AttrGroups.Add(NewAttr("酱料", "标准酱料", "少酱", "不加酱"));
            }
            else
            {
                food.AttrGroups.Add(NewAttr("辣度", "微辣", "不辣", "中辣", "重辣"));
                food.AttrGroups.Add(NewAttr("餐具", "需要餐具", "无需餐具"));
            }
            return food;
        }

        private static GoodsAttrGroupContract NewAttr(string name, params string[] options)
        {
            return new GoodsAttrGroupContract { GroupName = name, Options = new List<string>(options), SelectedOption = options[0] };
        }

        private static OrderFoodItemContract NewOrderFood(GoodsSpuContract food)
        {
            return new OrderFoodItemContract { FoodName = food.Name, SpecText = food.Skus[0].SpecName + " · " + food.AttrGroups[0].SelectedOption, Count = 1, Price = food.Skus[0].Price };
        }

        private static void FillOrderProgress(OrderContract order, int index)
        {
            DateTime created = new DateTime(2026, 10, 22, 11, 35, 0);
            if (index == 1) created = new DateTime(2026, 10, 22, 11, 48, 0);
            if (index == 2) created = new DateTime(2026, 10, 21, 18, 20, 0);
            if (index == 3) created = new DateTime(2026, 10, 20, 12, 5, 0);
            if (index == 4) created = new DateTime(2026, 10, 19, 15, 10, 0);
            bool preparing = index == 1;
            bool delivering = index == 0;
            bool refunded = index == 4;
            order.TrackingSteps.Add(new RiderTrackingStepContract { TimeText = created.ToString("HH:mm"), Title = "订单已提交", Subtitle = "支付成功，等待商家接单", IsCompleted = true });
            order.TrackingSteps.Add(new RiderTrackingStepContract { TimeText = created.AddMinutes(1).ToString("HH:mm"), Title = "商家备餐中", Subtitle = preparing ? "店员正在制作您的餐品" : "餐品已制作完成", IsCompleted = !preparing, IsCurrent = preparing });
            order.TrackingSteps.Add(new RiderTrackingStepContract { TimeText = preparing ? "待取餐" : created.AddMinutes(14).ToString("HH:mm"), Title = "骑手配送中", Subtitle = preparing ? "出餐后由骑手取餐" : (delivering ? "骑手已取餐，正在前往收货地址" : "骑手已完成配送"), IsCompleted = !preparing && !delivering, IsCurrent = delivering });
            order.TrackingSteps.Add(new RiderTrackingStepContract { TimeText = preparing || delivering ? "待送达" : created.AddMinutes(32).ToString("HH:mm"), Title = "餐品已送达", Subtitle = preparing || delivering ? "送达后请及时享用" : "已放置在约定位置", IsCompleted = !preparing && !delivering, IsCurrent = !preparing && !delivering && !refunded });
            if (refunded)
                order.TrackingSteps.Add(new RiderTrackingStepContract { TimeText = created.AddMinutes(65).ToString("HH:mm"), Title = "售后退款完成", Subtitle = "商家确认漏送，货款已原路退回", IsCompleted = true, IsCurrent = true });
            order.ImMessages.Add(new ImChatMessageContract { SenderRole = "system", SenderName = "订单助手", TimeText = created.ToString("HH:mm"), Content = "订单沟通群已建立，可在这里联系商家与骑手。" });
            order.ImMessages.Add(new ImChatMessageContract { SenderRole = "merchant", SenderName = order.PoiName, TimeText = created.AddMinutes(1).ToString("HH:mm"), Content = preparing ? "已收到订单，餐品正在制作中。" : "您的餐品已打包，感谢耐心等待。" });
            order.ImMessages.Add(new ImChatMessageContract { SenderRole = "customer", SenderName = "我", TimeText = created.AddMinutes(2).ToString("HH:mm"), Content = "麻烦送到后放门口置物架，留言就好，谢谢。", IsSelf = true });
            if (!preparing)
                order.ImMessages.Add(new ImChatMessageContract { SenderRole = "rider", SenderName = order.RiderName, TimeText = created.AddMinutes(delivering ? 15 : 32).ToString("HH:mm"), Content = delivering ? "已取餐出发，送达后会按备注放好。" : "餐品已放在约定位置，祝您用餐愉快。" });
            if (refunded)
                order.ImMessages.Add(new ImChatMessageContract { SenderRole = "merchant", SenderName = order.PoiName, TimeText = created.AddMinutes(65).ToString("HH:mm"), Content = "很抱歉漏送了餐品，售后退款已处理完成。" });
            order.SmartReplySuggestions.AddRange(preparing
                ? new[] { "请问还需要多久出餐？", "麻烦按备注少辣，谢谢", "请检查餐品是否齐全" }
                : (refunded ? new[] { "退款已收到，谢谢", "请问退款明细在哪里查看？", "联系平台客服" } : new[] { "请放门口置物架，谢谢", "到楼下请留言", "路上注意安全，不着急" }));
        }
    }

    public class SelfTestItem
    {
        public string Id { get; set; }
        public string Category { get; set; }
        public string Name { get; set; }
        public string EvidenceDetail { get; set; }
        public bool Passed { get; set; }
    }

    public class SelfTestReport
    {
        public DateTime ExecutedAtUtc { get; set; }
        public int TotalCount { get; set; }
        public int PassedCount { get; set; }
        public int FailedCount { get; set; }
        public List<SelfTestItem> Items { get; set; }

        public SelfTestReport()
        {
            Items = new List<SelfTestItem>();
        }
    }

    public static class WaimaiContractSelfTests
    {
        public static Task<SelfTestReport> RunAllAsync(string repoRootPath)
        {
            // 文件、JSON 和断言统一在后台运行，存储调用使用 await 等待真实结果。
            return Task.Run(async delegate
            {
                SelfTestReport report = new SelfTestReport { ExecutedAtUtc = DateTime.UtcNow };
                List<SelfTestItem> results = report.Items;
                RunAssetTests(results, repoRootPath);
                RunCartTests(results);
                RunNotificationTests(results);
                RunOrderTests(results);
                RunSeedTests(results);
                await RunStorageTestsAsync(results).ConfigureAwait(false);
                report.TotalCount = results.Count;
                report.PassedCount = results.Count(delegate(SelfTestItem item) { return item.Passed; });
                report.FailedCount = report.TotalCount - report.PassedCount;
                return report;
            });
        }

        private static void RunAssetTests(List<SelfTestItem> results, string repoRootPath)
        {
            Run(results, "ASSET-01", "APK 资产", "EasyGo 客户端及主从页面契约", "读取实际 easygo.json，核对版本、包名、magicwindow 及两个 Activity。", delegate
            {
                Dictionary<string, object> root = ReadAsset(repoRootPath, "easygo.json");
                EqualText("1.0", ValueText(root, "easyGoVersion"), "EasyGo 版本");
                EqualText("com.sankuai.meituan.takeoutnew", ValueText(root, "client"), "客户端包名");
                object[] entities = ArrayValue(root, "logicEntities");
                Expect(entities.Length == 1, "logicEntities 应包含一个平行视界实体。");
                Dictionary<string, object> entity = ObjectValue(entities[0]);
                EqualText("magicwindow", ValueText(ObjectValue(entity["head"]), "function"), "功能名");
                Dictionary<string, object> body = ObjectValue(entity["body"]);
                Dictionary<string, object> pages = ObjectValue(body["defaultDualActivities"]);
                EqualText("com.sankuai.waimai.business.page.homepage.MainActivity", ValueText(pages, "mainPages"), "主页面");
                EqualText("com.sankuai.waimai.business.page.kingkong.KingKongActivity", ValueText(pages, "relatedPage"), "关联页面");
                EasyGoMagicWindowContract parsed = EasyGoMagicWindowContract.ParseFromJson(File.ReadAllText(Path.Combine(repoRootPath, "research", "apk-assets", "easygo.json"), Encoding.UTF8));
                EqualText("1.0", parsed.EasyGoVersion, "解析器输出版本");
                EqualText("com.sankuai.meituan.takeoutnew", parsed.Client, "解析器输出包名");
                EqualText("magicwindow", parsed.FunctionName, "解析器输出功能");
                EqualText("com.sankuai.waimai.business.page.homepage.MainActivity", parsed.MainPages, "解析器输出主页面");
                EqualText("com.sankuai.waimai.business.page.kingkong.KingKongActivity", parsed.RelatedPage, "解析器输出关联页面");
                Expect(parsed.SupportRotationUxCompat && parsed.IsDraggable, "解析器应识别字符串布尔值。");
            });
            Run(results, "ASSET-02", "APK 资产", "EasyGo 中转与交互配置", "读取真实中转 Activity，验证旋转兼容与可拖拽配置。", delegate
            {
                Dictionary<string, object> root = ReadAsset(repoRootPath, "easygo.json");
                Dictionary<string, object> entity = ObjectValue(ArrayValue(root, "logicEntities")[0]);
                Dictionary<string, object> body = ObjectValue(entity["body"]);
                object[] transfers = ArrayValue(body, "transActivities");
                Expect(transfers.Length == 1, "中转 Activity 数量应为 1。");
                EqualText("com.sankuai.waimai.foundation.core.base.activity.transfer.TransferActivity", Convert.ToString(transfers[0]), "中转页面");
                Dictionary<string, object> ux = ObjectValue(body["UX"]);
                EqualText("true", ValueText(ux, "supportRotationUxCompat"), "旋转兼容");
                EqualText("true", ValueText(ux, "isDraggable"), "拖拽支持");
            });
            Run(results, "ASSET-03", "APK 资产", "金刚区页面和推荐模块", "解析实际 future_king_kong.json，核对成功码、页面布局和 market 主模块。", delegate
            {
                Dictionary<string, object> root = ReadAsset(repoRootPath, "future_king_kong.json");
                EqualText("0", ValueText(root, "code"), "响应码");
                EqualText("success", ValueText(root, "msg"), "响应状态");
                Dictionary<string, object> data = ObjectValue(root["data"]);
                EqualText("list", ValueText(data, "layout_type"), "页面布局");
                EqualText("native", ValueText(data, "render_mode"), "渲染方式");
                EqualText("page", ValueText(data, "view_type"), "页面类型");
                object[] modules = ArrayValue(data, "module_list");
                Expect(modules.Length == 2, "实际金刚区应包含两个模块。");
                Dictionary<string, object> module = ObjectValue(modules[0]);
                EqualText("future_kingkong_rcmd", ValueText(module, "module_id"), "推荐模块 ID");
                EqualText("mtmall/v1/home/market", ValueText(module, "url"), "推荐接口");
                EqualText("list", ValueText(module, "layout_type"), "推荐布局");
                Expect(Convert.ToInt32(module["is_Main_url"]) == 1, "推荐模块必须为主接口。");
            });
            Run(results, "ASSET-04", "APK 资产", "金刚区标签流模块", "验证真实第二模块的 tabs、feed、tab_list 和非主接口标记。", delegate
            {
                Dictionary<string, object> data = ObjectValue(ReadAsset(repoRootPath, "future_king_kong.json")["data"]);
                Dictionary<string, object> module = ObjectValue(ArrayValue(data, "module_list")[1]);
                EqualText("future_kingkong_tabs", ValueText(module, "module_id"), "标签模块 ID");
                EqualText("mtmall/v1/home/feed", ValueText(module, "url"), "标签流接口");
                EqualText("tab_list", ValueText(module, "layout_type"), "标签布局");
                EqualText("frame", ValueText(module, "view_type"), "模块视图");
                EqualText("native", ValueText(module, "render_mode"), "模块渲染方式");
                Expect(Convert.ToInt32(module["is_Main_url"]) == 0 && Convert.ToInt32(module["is_loading"]) == 1, "标签流主接口和加载标记错误。");
            });
        }

        private static void RunCartTests(List<SelfTestItem> results)
        {
            Run(results, "CART-14", "购物车", "显式原价不被现价覆盖", "现价20、原价18、起送19，应按18判断，还差1元，节省金额不能为负数。", delegate
            {
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(19), Cart(Item(20, 18, 0, 1, 1)), null);
                EqualAmount(18, price.TotalAndBoxPrice, "显式原价金额");
                Expect(!price.CanCheckout, "显式原价不足时不能按较高现价放行。");
                EqualText("差¥1起送", price.CheckoutButtonText, "显式原价起送差额");
                EqualAmount(0, price.TotalSavedAmount, "没有优惠时节省金额下限");
            });
            Run(results, "CART-01", "购物车", "原价与打包费达到起送边界", "2 × 原价18 + 2 × 打包费2 = 起送价40；现价商品金额20。", delegate
            {
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(40), Cart(Item(10, 18, 2, 2, 1)), null);
                Expect(price.CanCheckout, "原价加打包费达到起送金额时应可结算。");
                EqualAmount(40, price.ThresholdPrice, "起送门槛");
                EqualText("去结算", price.CheckoutButtonText, "达到门槛后的结算按钮");
                EqualAmount(20, price.TotalDiscountedPrice, "商品现价");
                EqualAmount(24, price.TotalDiscountedAndBoxPrice, "商品加打包费");
            });
            Run(results, "CART-02", "购物车", "起送不足与未填原价回退", "原价金额40距45差5；缺省原价按现价24加打包费2，距30差4。", delegate
            {
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(45), Cart(Item(10, 18, 2, 2, 1)), null);
                Expect(!price.CanCheckout, "未达起送价不能结算。");
                EqualAmount(45, price.ThresholdPrice, "商家起送门槛");
                EqualText("差¥5起送", price.CheckoutButtonText, "原价起送差额提示");
                price = ShopCartCalculatorEngine.Calculate(TestPoi(30), Cart(Item(12, 0, 1, 2, 1)), null);
                Expect(!price.CanCheckout, "原价回退后仍不足起送价。");
                EqualAmount(30, price.ThresholdPrice, "原价回退不改变门槛");
                EqualText("差¥4起送", price.CheckoutButtonText, "原价回退后的差额提示");
            });
            Run(results, "CART-03", "购物车", "满减选择符合门槛的最大优惠", "商品40匹配10减8、30减5、20减9，实际取9，不按最高门槛取5。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 10, ReduceAmount = 8 });
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 30, ReduceAmount = 5 });
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 20, ReduceAmount = 9 });
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(40, 40, 0, 1, 1)), null);
                EqualAmount(9, price.AppliedFullReductionAmount, "最大满减");
                EqualAmount(31, price.TotalDiscountedPrice, "满减后商品金额");
            });
            Run(results, "CART-04", "购物车", "满减门槛只使用商品现价", "商品现价19、原价100、打包费50，不能触发满20减5。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 20, ReduceAmount = 5 });
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(19, 100, 50, 1, 1)), null);
                EqualAmount(0, price.AppliedFullReductionAmount, "未达现价门槛的满减");
                EqualAmount(69, price.TotalDiscountedAndBoxPrice, "不参与满减门槛的打包费");
            });
            Run(results, "CART-05", "购物车", "下一档满减凑单提示", "商品25、满20减6及满40减12，下一档还差15。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 40, ReduceAmount = 12 });
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 20, ReduceAmount = 6 });
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(25, 25, 0, 1, 1)), null);
                EqualAmount(6, price.AppliedFullReductionAmount, "当前档位优惠");
                Expect(price.PreviewSubTip != null && price.PreviewSubTip.Contains("15") && price.PreviewSubTip.Contains("12"), "下一档提示必须包含凑单差额15与优惠12，实际为：" + price.PreviewSubTip);
            });
            Run(results, "CART-06", "购物车", "券门槛使用满减前的商品现价", "商品30先满减10，满30减8券仍可用，最终商品12。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 20, ReduceAmount = 10 });
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(30, 30, 2, 1, 1)), Coupon(8, 30, 0));
                EqualAmount(8, price.AppliedCouponAmount, "券抵扣");
                EqualAmount(12, price.TotalDiscountedPrice, "满减和券后的商品金额");
                EqualAmount(14, price.TotalDiscountedAndBoxPrice, "结算打包费");
            });
            Run(results, "CART-07", "购物车", "膨胀券金额参与抵扣且重复膨胀幂等", "基础6加膨胀4，两次膨胀后仍为10；商品30抵扣后20。", delegate
            {
                ShenQuanCouponContract coupon = Coupon(6, 20, 1001);
                coupon.InflatedBonus = 4;
                coupon.Inflate();
                coupon.Inflate();
                Expect(coupon.IsInflated, "膨胀状态未设置。");
                EqualAmount(10, coupon.Amount, "重复膨胀后的券额");
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(0), Cart(Item(30, 30, 0, 1, 1)), coupon);
                EqualAmount(10, price.AppliedCouponAmount, "膨胀后抵扣");
                EqualAmount(20, price.TotalDiscountedPrice, "使用膨胀券后金额");
            });
            Run(results, "CART-08", "购物车", "不适用商家、已使用和未达门槛的券不抵扣", "同一商品30分别使用异店券、已用券和满31券，三种抵扣均为0。", delegate
            {
                ShenQuanCouponContract[] coupons = { Coupon(8, 0, 2002), Coupon(8, 0, 0), Coupon(8, 31, 0) };
                coupons[1].IsUsed = true;
                foreach (ShenQuanCouponContract coupon in coupons)
                {
                    ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(0), Cart(Item(30, 50, 8, 1, 1)), coupon);
                    EqualAmount(0, price.AppliedCouponAmount, "不可用券抵扣");
                    EqualAmount(30, price.TotalDiscountedPrice, "不可用券不改变商品金额");
                }
            });
            Run(results, "CART-09", "购物车", "券抵扣上限保护商品余额和打包费", "商品10满减8后仅剩2，面额9的券最多抵2，打包费3仍应支付。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.FullReductionRules.Add(new FullReductionTier { Threshold = 10, ReduceAmount = 8 });
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(10, 10, 3, 1, 1)), Coupon(9, 10, 0));
                EqualAmount(2, price.AppliedCouponAmount, "封顶后的券抵扣");
                EqualAmount(0, price.TotalDiscountedPrice, "商品金额下限");
                EqualAmount(3, price.TotalDiscountedAndBoxPrice, "不能被券抵扣的打包费");
            });
            Run(results, "CART-10", "购物车", "多口袋按商品数量收取打包费", "口袋1内2件20元、口袋2内3件10元，商品70，打包费2×2+1×3=7。", delegate
            {
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(TestPoi(0), Cart(Item(20, 20, 2, 2, 1), Item(10, 10, 1, 3, 2)), null);
                Expect(price.TotalItemCount == 5, "多口袋商品数量应为5。");
                EqualAmount(7, price.TotalBoxPrice, "多口袋打包费");
                EqualAmount(70, price.TotalDiscountedPrice, "多口袋商品金额");
                EqualAmount(77, price.TotalDiscountedAndBoxPrice, "多口袋含打包费金额");
            });
            Run(results, "CART-11", "购物车", "配送费优惠独立于商品和打包费", "配送原价6现价2，减免4；商品20加打包费3的金额仍为23。", delegate
            {
                PoiContract poi = TestPoi(0);
                poi.OriginShippingFee = 6;
                poi.ShippingFee = 2;
                ShopCartPriceContract price = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(20, 20, 3, 1, 1)), null);
                EqualAmount(6, price.OriginalShippingFee, "配送原价");
                EqualAmount(4, price.ShippingFeeReduce, "配送减免");
                EqualAmount(2, price.FinalShippingFee, "配送实付");
                EqualAmount(23, price.TotalDiscountedAndBoxPrice, "商品与打包金额不含配送");
            });
            Run(results, "CART-12", "购物车", "空车与零数量商品不能结算", "零起送价也不能结算空车；零数量商品不计件数、费用或优惠。", delegate
            {
                PoiContract poi = TestPoi(0);
                ShopCartPriceContract empty = ShopCartCalculatorEngine.Calculate(poi, new List<ShopCartItemContract>(), Coupon(9, 0, 0));
                ShopCartPriceContract zero = ShopCartCalculatorEngine.Calculate(poi, Cart(Item(10, 10, 3, 0, 1)), null);
                foreach (ShopCartPriceContract price in new[] { empty, zero })
                {
                    Expect(!price.CanCheckout && price.TotalItemCount == 0, "空车应禁用结算且数量为0。");
                    EqualAmount(0, price.TotalDiscountedAndBoxPrice, "空车金额");
                    EqualAmount(0, price.TotalBoxPrice, "空车打包费");
                    EqualAmount(0, price.AppliedCouponAmount, "空车券抵扣");
                }
            });
            Run(results, "CART-13", "购物车", "拒绝负数、非有限金额和混店商品", "负数量、NaN 单价、其他商家的有效商品必须报参数错误。", delegate
            {
                ShopCartItemContract[] invalid = { Item(10, 10, 0, -1, 1), Item(double.NaN, 10, 0, 1, 1), Item(10, 10, 0, 1, 1) };
                invalid[2].PoiId = 2002;
                foreach (ShopCartItemContract item in invalid)
                {
                    bool rejected = false;
                    try { ShopCartCalculatorEngine.Calculate(TestPoi(0), Cart(item), null); }
                    catch (ArgumentException) { rejected = true; }
                    Expect(rejected, "非法金额、数量或混店商品未被拒绝。");
                }
            });
        }

        private static void RunNotificationTests(List<SelfTestItem> results)
        {
            Run(results, "NOTIFY-01", "属性通知", "选购数量变化同步通知数量与可见状态", "从0到2再到0，每次通知 SelectedCount 和 HasSelectedCount；状态随数量变化。", delegate
            {
                GoodsSpuContract goods = new GoodsSpuContract();
                List<string> names = new List<string>();
                goods.PropertyChanged += delegate(object sender, PropertyChangedEventArgs args) { names.Add(args.PropertyName); };
                goods.SelectedCount = 2;
                Expect(goods.HasSelectedCount && goods.SelectedCount == 2, "选购后数量或可见状态错误。");
                Expect(names.Count == 2 && names.Contains("SelectedCount") && names.Contains("HasSelectedCount"), "数量增加必须通知两个绑定属性。");
                names.Clear();
                goods.SelectedCount = 0;
                Expect(!goods.HasSelectedCount && names.Count == 2 && names.Contains("SelectedCount") && names.Contains("HasSelectedCount"), "数量清零必须更新状态并通知两个绑定属性。");
            });
            Run(results, "NOTIFY-02", "属性通知", "重复赋值不发通知且拒绝负数量", "同值赋值保持静默，负数量抛异常且原值和状态不变。", delegate
            {
                GoodsSpuContract goods = new GoodsSpuContract { SelectedCount = 2 };
                int count = 0;
                goods.PropertyChanged += delegate { count++; };
                goods.SelectedCount = 2;
                Expect(count == 0, "同值赋值不应重复通知。");
                bool rejected = false;
                try { goods.SelectedCount = -1; }
                catch (ArgumentOutOfRangeException) { rejected = true; }
                Expect(rejected && goods.SelectedCount == 2 && goods.HasSelectedCount && count == 0, "非法数量必须被拒绝且不改变已有状态。");
            });
        }

        private static void RunOrderTests(List<SelfTestItem> results)
        {
            Run(results, "JSON-01", "订单 JSON", "官方响应键映射及数字字符串兼容", "实际调用 FromJson，验证官方响应键及非零 unread 标记不被反转。", delegate
            {
                OrderListResponseContract response = OrderListResponseContract.FromJson(OfficialOrderJson);
                EqualText("history", response.Type, "type");
                EqualText("cursor-7", response.Cursor, "cursor");
                EqualText("multi-8", response.MultiCursor, "multi_cursor");
                Expect(response.HasMore == 1 && response.UnCommentCount == 2 && response.WaitTime == 30 && response.RefundCount == 3 && response.ShareType == 4, "响应整数或数字字符串映射错误。");
                Expect(response.HasReadUnCommentOrder && response.HasReadUnRefundOrder, "官方两个非零 unread 标记必须直接映射为 true。");
                EqualText("待评价提醒", response.UnreadUncommentTips, "未评价提示");
                EqualText("没有更多订单", response.BottomTips, "底部提示");
                EqualText("搜索订单", response.SearchDefaultTips, "搜索提示");
                EqualText("trace-9", response.TrackingInfo, "跟踪信息");
                Expect(response.OrderList.Count == 1, "digestlist 必须解析一笔订单。");
            });
            Run(results, "JSON-02", "订单 JSON", "订单和嵌套明细字段完整映射", "手写 snake_case JSON 含64位ID、商品、轨迹、IM和快捷回复，逐项核对关键值。", delegate
            {
                OrderContract order = OrderListResponseContract.FromJson(OfficialOrderJson).OrderList[0];
                Expect(order.OrderId == 9876543210L && order.PoiId == 1001 && order.Status == 30 && order.DeliveryStatus == 2 && order.PayStatus == 1, "订单 ID、商家或状态映射错误。");
                EqualText("hash-17", order.HashId, "订单哈希");
                EqualText("1001", order.PoiIdStr, "商家字符串 ID");
                EqualText("测试门店", order.PoiName, "商家名");
                EqualText("配送中", order.StatusDescription, "状态描述");
                EqualText("2026-10-22T12:00:00+08:00", order.OrderTime, "下单时间");
                EqualText("今天 12:00", order.FormatOrderTime, "格式化时间");
                EqualText("12:30送达", order.ArrivalTimeText, "预计送达");
                EqualText("测试骑手", order.RiderName, "骑手姓名");
                EqualText("13800000000", order.RiderPhone, "骑手电话");
                EqualText("自检用户", order.RecipientName, "收货人");
                EqualText("13900000000", order.RecipientPhone, "收货电话");
                EqualText("测试路1号", order.RecipientAddress, "收货地址");
                Expect(order.Latitude == 31230400 && order.Longitude == 121473700 && order.ProductCount == 2 && !order.IsComment && order.CanRefund, "坐标、商品件数或布尔值映射错误。");
                EqualAmount(25.5, order.Total, "订单总额");
                EqualAmount(2, order.BoxFeeTotal, "订单打包费");
                EqualAmount(3, order.ShippingFee, "订单配送费");
                EqualAmount(5, order.DiscountTotal, "订单优惠");
                EqualAmount(0.5, order.RiderProgressRatio, "骑手进度");
                Expect(order.FoodItems.Count == 1 && order.FoodItems[0].Count == 2, "商品明细和数量未保留。");
                EqualText("鸡肉饭", order.FoodItems[0].FoodName, "商品名");
                EqualText("大份", order.FoodItems[0].SpecText, "商品规格");
                EqualAmount(12.75, order.FoodItems[0].Price, "商品单价");
                Expect(order.TrackingSteps.Count == 1 && order.TrackingSteps[0].IsCompleted && order.TrackingSteps[0].IsCurrent, "轨迹及布尔标记未保留。");
                EqualText("12:10", order.TrackingSteps[0].TimeText, "轨迹时间");
                EqualText("骑手已取餐", order.TrackingSteps[0].Title, "轨迹标题");
                EqualText("正在送达", order.TrackingSteps[0].Subtitle, "轨迹说明");
                Expect(order.ImMessages.Count == 1 && !order.ImMessages[0].IsSelf, "IM 消息与发送方标记未保留。");
                EqualText("rider", order.ImMessages[0].SenderRole, "IM 角色");
                EqualText("测试骑手", order.ImMessages[0].SenderName, "IM 姓名");
                EqualText("12:11", order.ImMessages[0].TimeText, "IM 时间");
                EqualText("马上送达", order.ImMessages[0].Content, "IM 内容");
                Expect(order.SmartReplySuggestions.Count == 2 && order.SmartReplySuggestions[0] == "谢谢" && order.SmartReplySuggestions[1] == "请放门口", "快捷回复未保留。");
            });
            Run(results, "JSON-03", "订单 JSON", "订单导出使用官方键且完整往返", "ToJson 输出 digestlist/hasmore/has_unread_* 等真实键，重新解析保留嵌套与转义文本。", delegate
            {
                OrderListResponseContract source = OrderListResponseContract.FromJson(OfficialOrderJson);
                source.OrderList[0].Caution = "少辣\n备注：\"不要香菜\"";
                source.OrderList[0].FoodNames = "鸡肉饭×2";
                string json = source.ToJson();
                Dictionary<string, object> root = ObjectValue(new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(json));
                string[] keys = { "type", "cursor", "multi_cursor", "hasmore", "uncomment_count", "has_unread_comment_order", "unread_uncomment_tips", "digestlist", "wait_time", "list_types", "refund_count", "bottom_tips", "search_default_tips", "has_unread_refund_order", "share_type", "tracking_info", "envelope_share_tip", "activity_share_tip", "ugc_pop_ups_info" };
                Expect(root.Count == keys.Length && keys.All(delegate(string key) { return root.ContainsKey(key); }), "导出响应未完整使用官方键集合。");
                Dictionary<string, object> exportedOrder = ObjectValue(ArrayValue(root, "digestlist")[0]);
                Expect(exportedOrder.ContainsKey("order_id") && exportedOrder.ContainsKey("food_items") && exportedOrder.ContainsKey("tracking_steps") && exportedOrder.ContainsKey("im_messages") && !exportedOrder.ContainsKey("OrderId"), "订单导出未使用 snake_case 字段。");
                OrderListResponseContract restored = OrderListResponseContract.FromJson(json);
                Expect(restored.HasReadUnCommentOrder && restored.HasReadUnRefundOrder && restored.OrderList.Count == 1, "响应标记或订单数往返丢失。");
                Expect(restored.ListTypes != null && restored.ListTypes.Count == 2 && restored.EnvelopeShareTip != null && restored.ActivityShareTip != null && restored.UgcPopUpsInfo == "ugc-popup-1", "list_types / share_tip / ugc_pop_ups_info 往返丢失。");
                OrderContract order = restored.OrderList[0];
                Expect(order.OrderId == 9876543210L && order.PoiId == 1001 && order.ProductCount == 2, "订单 ID 或数量往返丢失。");
                EqualAmount(25.5, order.Total, "订单总额往返");
                EqualText("少辣\n备注：\"不要香菜\"", order.Caution, "中文及转义文本往返");
                EqualText("鸡肉饭×2", order.FoodNames, "商品名称汇总往返");
                Expect(order.FoodItems.Count == 1 && order.TrackingSteps.Count == 1 && order.ImMessages.Count == 1 && order.SmartReplySuggestions.Count == 2, "嵌套集合往返丢失。");
                EqualText("马上送达", order.ImMessages[0].Content, "消息往返");
                EqualText("骑手已取餐", order.TrackingSteps[0].Title, "轨迹往返");
            });
            Run(results, "JSON-04", "订单 JSON", "缺省字段与空订单列表", "缺失和 null digestlist 均为空集，缺失 wait_time 为-1，0 unread 标记为 false。", delegate
            {
                foreach (string json in new[] { "{}", "{\"digestlist\":null,\"has_unread_comment_order\":0,\"has_unread_refund_order\":\"0\"}" })
                {
                    OrderListResponseContract response = OrderListResponseContract.FromJson(json);
                    Expect(response.OrderList != null && response.OrderList.Count == 0 && response.WaitTime == -1, "空列表或缺省等待时间错误。");
                    Expect(!response.HasReadUnCommentOrder && !response.HasReadUnRefundOrder, "零或缺省 unread 标记应为 false。");
                }
            });
            Run(results, "JSON-05", "订单 JSON", "兼容驼峰键并优先使用官方键", "camelCase 输入可读；官方 hasmore 与 digestlist 覆盖冲突的兼容字段。", delegate
            {
                OrderListResponseContract response = OrderListResponseContract.FromJson("{\"multiCursor\":\"camel\",\"hasMore\":1,\"waitTime\":7,\"hasReadUnCommentOrder\":true,\"orderList\":[{\"orderId\":17,\"poiId\":1001,\"foodItems\":[{\"foodName\":\"饭\",\"count\":1,\"price\":8}]}]}");
                EqualText("camel", response.MultiCursor, "驼峰游标");
                Expect(response.HasMore == 1 && response.WaitTime == 7 && response.HasReadUnCommentOrder && response.OrderList[0].OrderId == 17 && response.OrderList[0].FoodItems[0].Price == 8, "驼峰兼容读取错误。");
                response = OrderListResponseContract.FromJson("{\"hasmore\":0,\"hasMore\":1,\"digestlist\":[],\"orderList\":[{\"orderId\":17}]}");
                Expect(response.HasMore == 0 && response.OrderList.Count == 0, "官方键必须优先于冲突的兼容键。");
            });
            Run(results, "JSON-06", "订单 JSON", "拒绝非法 JSON 和错误列表结构", "不完整JSON、数组根、对象digestlist和空订单元素均抛出 FormatException。", delegate
            {
                string[] malformed = { "{", "[]", "{\"digestlist\":{}}", "{\"digestlist\":[null]}" };
                foreach (string json in malformed)
                {
                    bool rejected = false;
                    try { OrderListResponseContract.FromJson(json); }
                    catch (FormatException) { rejected = true; }
                    Expect(rejected, "错误 JSON 或订单列表结构未被拒绝：" + json);
                }
            });
        }

        private const string OfficialOrderJson = @"{
            ""type"":""history"",""cursor"":""cursor-7"",""multi_cursor"":""multi-8"",""hasmore"":""1"",
            ""uncomment_count"":2,""has_unread_comment_order"":-2,""unread_uncomment_tips"":""待评价提醒"",
            ""wait_time"":""30"",""list_types"":[1,2],""refund_count"":3,""bottom_tips"":""没有更多订单"",""search_default_tips"":""搜索订单"",
            ""has_unread_refund_order"":""1"",""share_type"":4,""tracking_info"":""trace-9"",
            ""envelope_share_tip"":{""title"":""发红包得津贴""},""activity_share_tip"":{""title"":""拼好饭活动""},""ugc_pop_ups_info"":""ugc-popup-1"",
            ""digestlist"":[{
                ""order_id"":9876543210,""poi_id"":""1001"",""hash_id"":""hash-17"",""poi_id_str"":""1001"",""poi_name"":""测试门店"",
                ""status_description"":""配送中"",""order_time"":""2026-10-22T12:00:00+08:00"",""format_order_time"":""今天 12:00"",
                ""arrival_time_text"":""12:30送达"",""rider_name"":""测试骑手"",""rider_phone"":""13800000000"",
                ""recipient_name"":""自检用户"",""recipient_phone"":""13900000000"",""recipient_address"":""测试路1号"",
                ""caution"":""少辣"",""food_names"":""鸡肉饭×2"",""status"":30,""delivery_status"":2,""pay_status"":1,
                ""latitude"":31230400,""longitude"":121473700,""product_count"":2,""total"":""25.5"",
                ""box_fee_total"":2,""shipping_fee"":3,""discount_total"":5,""rider_progress_ratio"":""0.5"",""is_comment"":0,""can_refund"":""true"",
                ""food_items"":[{""food_name"":""鸡肉饭"",""spec_text"":""大份"",""count"":2,""price"":12.75}],
                ""tracking_steps"":[{""time_text"":""12:10"",""title"":""骑手已取餐"",""subtitle"":""正在送达"",""is_completed"":1,""is_current"":""true""}],
                ""im_messages"":[{""sender_role"":""rider"",""sender_name"":""测试骑手"",""time_text"":""12:11"",""content"":""马上送达"",""is_self"":""false""}],
                ""smart_reply_suggestions"":[""谢谢"",""请放门口""]
            }]
        }";

        private static void RunSeedTests(List<SelfTestItem> results)
        {
            Run(results, "SEED-01", "演示种子", "五个导航业务及初始选中状态", "五个业务依次为1/9/10/2/3，索引连续且仅首页选中。", delegate
            {
                List<DynamicTabContract> tabs = WaimaiSeedDataFactory.CreateDefaultTabs();
                int[] ids = { 1, 9, 10, 2, 3 };
                Expect(tabs.Count == 5 && tabs.Count(delegate(DynamicTabContract tab) { return tab.IsSelected; }) == 1, "导航数量或选中数量错误。");
                for (int i = 0; i < ids.Length; i++)
                    Expect(tabs[i].BusinessId == ids[i] && tabs[i].TabIndex == i && tabs[i].IsSelected == (i == 0), "导航业务、顺序或初始选中状态错误。");
                Expect(tabs[2].IsProminentArcTab, "大促导航必须保留突出显示标记。");
            });
            Run(results, "SEED-02", "演示种子", "十个金刚区频道指向真实模块", "频道ID唯一，首频道映射rcmd/market，其余映射tabs/feed。", delegate
            {
                List<KingKongChannelContract> channels = WaimaiSeedDataFactory.CreateKingKongChannels();
                Expect(channels.Count == 10 && channels.Select(delegate(KingKongChannelContract channel) { return channel.ChannelId; }).Distinct().Count() == 10, "十个频道必须具有唯一 ID。");
                for (int i = 0; i < channels.Count; i++)
                {
                    EqualText(i == 0 ? "future_kingkong_rcmd" : "future_kingkong_tabs", channels[i].ModuleId, "频道模块映射");
                    EqualText(i == 0 ? "mtmall/v1/home/market" : "mtmall/v1/home/feed", channels[i].FeedUrl, "频道接口映射");
                }
            });
            Run(results, "SEED-03", "演示种子", "商家、商品、规格和属性可用于下单", "六家店各至少六菜、每菜至少两规格两属性组，ID唯一且商品分类归属正确。", delegate
            {
                List<PoiContract> pois = WaimaiSeedDataFactory.CreateDefaultPois();
                Expect(pois.Count >= 6, "商家数量不足6。");
                HashSet<long> poiIds = new HashSet<long>();
                HashSet<long> spuIds = new HashSet<long>();
                HashSet<long> skuIds = new HashSet<long>();
                foreach (PoiContract poi in pois)
                {
                    Expect(poi.Id > 0 && poiIds.Add(poi.Id) && poi.Foods.Count >= 6 && poi.FullReductionRules.Count >= 2, "商家ID重复或菜品/满减档位不完整。");
                    EqualText(poi.Id.ToString(System.Globalization.CultureInfo.InvariantCulture), poi.PoiIdStr, "商家字符串 ID 一致性");
                    foreach (GoodsSpuContract food in poi.Foods)
                    {
                        Expect(food.PoiId == poi.Id && food.Id > 0 && spuIds.Add(food.Id), "商品必须归属当前商家且全局ID唯一。");
                        Expect(poi.MenuCategories.Contains(food.CategoryTag) && food.Skus.Count >= 2 && food.AttrGroups.Count >= 2, "菜品分类、规格或属性组不完整。");
                        foreach (GoodsSkuContract sku in food.Skus)
                            Expect(sku.SkuId > 0 && skuIds.Add(sku.SkuId) && sku.Price > 0 && sku.OriginPrice >= sku.Price && sku.BoxFee >= 0 && sku.Stock > 0, "SKU必须唯一且具有可售库存和有效价格。");
                        foreach (GoodsAttrGroupContract group in food.AttrGroups)
                            Expect(group.Options.Count >= 2 && group.Options.Contains(group.SelectedOption), "属性组选项不足或默认选项不在可选列表内。");
                    }
                }
                Expect(spuIds.Count >= 36 && skuIds.Count >= 72, "商品或规格总数未达到完整菜单规模。");
            });
            Run(results, "SEED-04", "演示种子", "券钱包与会员资产保持一致", "四张券ID唯一、金额与膨胀状态一致，适用商家存在，会员券数等于未用券数量。", delegate
            {
                List<ShenQuanCouponContract> coupons = WaimaiSeedDataFactory.CreateDefaultCoupons();
                HashSet<long> poiIds = new HashSet<long>(WaimaiSeedDataFactory.CreateDefaultPois().Select(delegate(PoiContract poi) { return poi.Id; }));
                Expect(coupons.Count == 4 && coupons.Select(delegate(ShenQuanCouponContract coupon) { return coupon.CouponId; }).Distinct().Count() == 4, "券数量或唯一ID不正确。");
                foreach (ShenQuanCouponContract coupon in coupons)
                {
                    Expect(coupon.BaseAmount > 0 && coupon.Threshold >= 0 && coupon.InflatedBonus >= 0, "券金额或门槛无效。");
                    EqualAmount(coupon.BaseAmount + (coupon.IsInflated ? coupon.InflatedBonus : 0), coupon.Amount, "券状态对应的面额");
                    Expect(coupon.ApplicablePoiId == 0 || poiIds.Contains(coupon.ApplicablePoiId), "店铺券引用不存在的门店。");
                }
                MineProfileContract profile = WaimaiSeedDataFactory.CreateDefaultMineProfile();
                Expect(profile.ShenQuanCount == coupons.Count(delegate(ShenQuanCouponContract coupon) { return !coupon.IsUsed; }), "会员神券数量必须与钱包保持一致。");
            });
            Run(results, "SEED-05", "演示种子", "订单与商家商品关联且金额守恒", "五笔订单覆盖五状态，商品属于订单门店，件数与商品/打包/配送/优惠金额守恒。", delegate
            {
                List<OrderContract> orders = WaimaiSeedDataFactory.CreateDefaultOrders();
                List<PoiContract> pois = WaimaiSeedDataFactory.CreateDefaultPois();
                Expect(orders.Count >= 5 && orders.Select(delegate(OrderContract order) { return order.OrderId; }).Distinct().Count() == orders.Count && orders.Select(delegate(OrderContract order) { return order.Status; }).Distinct().Count() >= 5, "订单数量、ID或状态覆盖不足。");
                foreach (OrderContract order in orders)
                {
                    PoiContract poi = pois.Single(delegate(PoiContract candidate) { return candidate.Id == order.PoiId; });
                    EqualText(poi.Name, order.PoiName, "订单商家名称");
                    Expect(order.FoodItems.Count >= 2 && order.ProductCount == order.FoodItems.Sum(delegate(OrderFoodItemContract food) { return food.Count; }), "订单商品明细或总件数不一致。");
                    foreach (OrderFoodItemContract food in order.FoodItems)
                        Expect(food.Count > 0 && poi.Foods.Any(delegate(GoodsSpuContract candidate) { return candidate.Name == food.FoodName && candidate.Skus.Any(delegate(GoodsSkuContract sku) { return food.SpecText == sku.SpecName || (food.SpecText != null && food.SpecText.StartsWith(sku.SpecName + " · ", StringComparison.Ordinal) && candidate.AttrGroups.Any(delegate(GoodsAttrGroupContract group) { return group.Options.Contains(food.SpecText.Substring(sku.SpecName.Length + 3)); })); }); }), "订单商品或规格不属于当前商家。");
                    double goods = order.FoodItems.Sum(delegate(OrderFoodItemContract food) { return food.Price * food.Count; });
                    EqualAmount(Math.Round(goods + order.BoxFeeTotal + order.ShippingFee - order.DiscountTotal, 2), order.Total, "订单金额守恒");
                    Expect(order.TrackingSteps.Count >= 4 && order.ImMessages.Count >= 3 && order.SmartReplySuggestions.Count >= 3, "订单轨迹、IM或快捷回复不完整。");
                    Expect(order.ImMessages.Select(delegate(ImChatMessageContract message) { return message.SenderRole; }).Distinct().Count() >= 3, "订单应具备三方聊天角色。");
                }
            });
            Run(results, "SEED-06", "演示种子", "地址簿唯一默认地址和有效坐标", "三条地址ID唯一且仅一条默认，坐标落在地理有效范围，订单收货地址可关联。", delegate
            {
                List<WmAddressContract> addresses = WaimaiSeedDataFactory.CreateDefaultAddresses();
                Expect(addresses.Count >= 3 && addresses.Select(delegate(WmAddressContract address) { return address.AddressId; }).Distinct().Count() == addresses.Count && addresses.Count(delegate(WmAddressContract address) { return address.IsDefault; }) == 1, "地址数量、唯一ID或默认地址数量错误。");
                foreach (WmAddressContract address in addresses)
                    Expect(address.Latitude != 0 && Math.Abs((long)address.Latitude) <= 90000000 && address.Longitude != 0 && Math.Abs((long)address.Longitude) <= 180000000, "地址坐标无效。");
                foreach (OrderContract order in WaimaiSeedDataFactory.CreateDefaultOrders())
                    Expect(addresses.Any(delegate(WmAddressContract address) { return order.RecipientName == address.ContactName && order.RecipientPhone == address.Phone && order.RecipientAddress.Contains(address.AddressDetail); }), "订单收货信息未关联到演示地址簿。");
            });
            Run(results, "SEED-07", "演示种子", "推荐和秒杀活动引用有效商家商品", "四条推荐与六条秒杀均指向存在且归属一致的SPU，秒杀价格有效。", delegate
            {
                List<PoiContract> pois = WaimaiSeedDataFactory.CreateDefaultPois();
                List<DeepEatRecommendationContract> recommendations = WaimaiSeedDataFactory.CreateDeepEatRecommendations();
                List<PromoFlashDealContract> deals = WaimaiSeedDataFactory.CreatePromoFlashDeals();
                Expect(recommendations.Count >= 4 && deals.Count >= 6, "推荐或秒杀数量不足。");
                Expect(recommendations.Select(delegate(DeepEatRecommendationContract recommendation) { return recommendation.SceneId; }).Distinct().Count() == recommendations.Count, "推荐场景ID必须唯一。");
                Expect(deals.Select(delegate(PromoFlashDealContract deal) { return deal.DealId; }).Distinct().Count() == deals.Count, "秒杀活动ID必须唯一。");
                foreach (DeepEatRecommendationContract recommendation in recommendations)
                    Expect(pois.Any(delegate(PoiContract poi) { return poi.Id == recommendation.TargetPoiId && poi.Foods.Any(delegate(GoodsSpuContract food) { return food.Id == recommendation.TargetSpuId; }); }), "推荐引用不存在或不属于目标门店的商品。");
                foreach (PromoFlashDealContract deal in deals)
                {
                    PoiContract poi = pois.Single(delegate(PoiContract candidate) { return candidate.Id == deal.TargetPoiId; });
                    Expect(poi.Foods.Any(delegate(GoodsSpuContract food) { return food.Id == deal.TargetSpuId; }) && deal.PoiName == poi.Name, "秒杀门店、名称或商品ID不一致。");
                    Expect(deal.FlashPrice > 0 && deal.OriginPrice >= deal.FlashPrice && deal.SoldPercent >= 0 && deal.SoldPercent <= 100, "秒杀价格或已售比例无效。");
                }
            });
            Run(results, "PASS-01", "通行证契约", "美团通行证 User 契约字段与手机号脱敏", "验证 com.meituan.passport.pojo.User 核心字段及 MaskMobile 脱敏算法。", delegate
            {
                PassportUserContract user = WaimaiSeedDataFactory.CreateDefaultPassportUser();
                List<PassportUserContract> presets = WaimaiSeedDataFactory.CreatePresetPassportUsers();
                Expect(user.IsLoggedIn && user.Id > 0 && !string.IsNullOrEmpty(user.Token) && !string.IsNullOrEmpty(user.Ticket), "默认通行证用户令牌或登录状态无效。");
                EqualText("138****0001", PassportUserContract.MaskMobile("13800130001"), "11位手机号脱敏");
                Expect(presets.Count >= 3 && presets.Select(delegate(PassportUserContract u) { return u.Id; }).Distinct().Count() == presets.Count, "预设快捷登录账号数量或唯一ID无效。");
            });
            Run(results, "POI-01", "门店详情契约", "门店评价列表与食品安全商家档案", "验证每家门店包含至少4条真实评论、商家回复及A级食安档案。", delegate
            {
                List<PoiContract> pois = WaimaiSeedDataFactory.CreateDefaultPois();
                foreach (PoiContract poi in pois)
                {
                    Expect(poi.MerchantArchive != null && !string.IsNullOrEmpty(poi.MerchantArchive.Address) && !string.IsNullOrEmpty(poi.MerchantArchive.LicenseNumber), "门店商家档案或执照编号缺失。");
                    Expect(poi.Reviews != null && poi.Reviews.Count >= 4 && poi.Reviews.Any(delegate(PoiReviewItemContract r) { return r.HasPictures; }), "门店评价数量不足或缺少带图评价。");
                }
            });
            Run(results, "CASH-01", "收银台契约", "美团收银台支付渠道与月付随机立减", "验证 MTCashierActivity 四大支付方式及美团月付立减金额。", delegate
            {
                List<CashierPaymentMethodContract> methods = WaimaiSeedDataFactory.CreateDefaultPaymentMethods();
                Expect(methods.Count >= 4 && methods.Count(delegate(CashierPaymentMethodContract m) { return m.IsSelected; }) == 1, "收银台支付方式数量或默认选中状态错误。");
                Expect(methods[0].InstantDiscount > 0, "美团月付应包含随机立减优惠金额。");
            });
        }

        private static async Task RunStorageTestsAsync(List<SelfTestItem> results)
        {
            await RunAsync(results, "STORE-01", "DPAPI 存储", "真实快照加密保存与读取", "await 保存及读取随机临时文件，核对购物车、订单嵌套、券和地址全部恢复。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    WaimaiPersistedSnapshot source = StorageSnapshot();
                    await WaimaiStorageService.SaveSnapshotAsync(source, path).ConfigureAwait(false);
                    Expect(File.Exists(path), "保存操作没有生成磁盘文件。");
                    WaimaiPersistedSnapshot restored = await WaimaiStorageService.LoadSnapshotAsync(path).ConfigureAwait(false);
                    Expect(restored != null && restored.IsEasyGoDualPane && restored.CurrentAddressId == "selftest-address", "快照主状态未恢复。");
                    EqualText("2026-10-22T04:00:00Z", restored.SavedAtUtc, "快照时间往返");
                    Expect(restored.CartItems.Count == 1 && restored.CartItems[0].Count == 2 && restored.CartItems[0].PocketId == 2, "购物车数量或口袋未恢复。");
                    EqualAmount(12.5, restored.CartItems[0].UnitPrice, "购物车单价往返");
                    Expect(restored.Orders.Count == 1 && restored.Orders[0].OrderId == 9876543210L && restored.Orders[0].FoodItems.Count == 1, "订单及商品明细未恢复。");
                    EqualText("马上送达", restored.Orders[0].ImMessages[0].Content, "订单消息存储往返");
                    Expect(restored.Coupons.Count == 1 && restored.Coupons[0].Amount == 8 && restored.Addresses.Count == 1 && restored.Addresses[0].IsDefault, "券钱包或地址簿未恢复。");
                    EqualText("磁盘加密自检收货人", restored.Addresses[0].ContactName, "收货人往返");
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
            await RunAsync(results, "STORE-02", "DPAPI 存储", "磁盘仅包含当前用户DPAPI密文", "磁盘字节不含敏感明文，CurrentUser Unprotect 可解出真实快照内容。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    await WaimaiStorageService.SaveSnapshotAsync(StorageSnapshot(), path).ConfigureAwait(false);
                    byte[] ciphertext = File.ReadAllBytes(path);
                    string diskText = Encoding.UTF8.GetString(ciphertext);
                    Expect(ciphertext.Length > 32 && !diskText.Contains("selftest-address") && !diskText.Contains("磁盘加密自检收货人") && !diskText.Contains("CurrentAddressId"), "磁盘不应写入敏感明文或原始JSON。");
                    byte[] decrypted = System.Security.Cryptography.ProtectedData.Unprotect(ciphertext, null, System.Security.Cryptography.DataProtectionScope.CurrentUser);
                    try
                    {
                        Dictionary<string, object> json = ObjectValue(new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(Encoding.UTF8.GetString(decrypted)));
                        EqualText("selftest-address", ValueText(json, "CurrentAddressId"), "CurrentUser DPAPI解密内容");
                        Expect(ArrayValue(json, "CartItems").Length == 1 && ArrayValue(json, "Orders").Length == 1, "解密后必须是包含购物车与订单的真实快照。");
                    }
                    finally { Array.Clear(decrypted, 0, decrypted.Length); }
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
            await RunAsync(results, "STORE-03", "DPAPI 存储", "覆盖保存后读取新状态", "同一路径先保存2件再保存4件，新读取结果为4件且新地址ID生效。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    WaimaiPersistedSnapshot source = StorageSnapshot();
                    await WaimaiStorageService.SaveSnapshotAsync(source, path).ConfigureAwait(false);
                    source.CartItems[0].Count = 4;
                    source.CurrentAddressId = "updated-address";
                    source.Addresses[0].AddressId = "updated-address";
                    source.IsEasyGoDualPane = false;
                    await WaimaiStorageService.SaveSnapshotAsync(source, path).ConfigureAwait(false);
                    WaimaiPersistedSnapshot restored = await WaimaiStorageService.LoadSnapshotAsync(path).ConfigureAwait(false);
                    Expect(restored.CartItems.Count == 1 && restored.CartItems[0].Count == 4 && restored.CurrentAddressId == "updated-address" && restored.Addresses[0].AddressId == "updated-address" && !restored.IsEasyGoDualPane, "覆盖保存仍读取到旧状态或丢失更新字段。");
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
            await RunAsync(results, "STORE-04", "DPAPI 存储", "不存在的快照返回null且不创建文件", "读取唯一未使用路径得到null，磁盘不新增空快照。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    Expect(!File.Exists(path), "隔离测试路径意外存在。");
                    WaimaiPersistedSnapshot restored = await WaimaiStorageService.LoadSnapshotAsync(path).ConfigureAwait(false);
                    Expect(restored == null && !File.Exists(path), "不存在的快照必须返回null且不创建文件。");
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
            await RunAsync(results, "STORE-05", "DPAPI 存储", "损坏密文必须向调用方报告错误", "写入明确损坏的字节，await LoadSnapshotAsync 必须抛 CryptographicException，不能冒充空数据。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    File.WriteAllBytes(path, new byte[] { 7, 19, 31, 43, 59 });
                    bool rejected = false;
                    try { await WaimaiStorageService.LoadSnapshotAsync(path).ConfigureAwait(false); }
                    catch (System.Security.Cryptography.CryptographicException) { rejected = true; }
                    Expect(rejected, "损坏快照没有抛出加密错误。");
                    Expect(File.ReadAllBytes(path).SequenceEqual(new byte[] { 7, 19, 31, 43, 59 }), "读取失败不应覆盖损坏文件。");
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
            await RunAsync(results, "STORE-06", "DPAPI 存储", "保存不修改调用方状态且拒绝null", "保存后原快照时间/列表/数量保持原值，null保存抛参数异常且不产生文件。", async delegate
            {
                string path = NewStoragePath();
                try
                {
                    WaimaiPersistedSnapshot snapshot = StorageSnapshot();
                    List<ShopCartItemContract> originalItems = snapshot.CartItems;
                    await WaimaiStorageService.SaveSnapshotAsync(snapshot, path).ConfigureAwait(false);
                    Expect(object.ReferenceEquals(originalItems, snapshot.CartItems) && snapshot.CartItems[0].Count == 2 && snapshot.SavedAtUtc == "2026-10-22T04:00:00Z", "保存不能修改调用方快照。");
                    DeleteOwnStorageFile(path);
                    bool rejected = false;
                    try { await WaimaiStorageService.SaveSnapshotAsync(null, path).ConfigureAwait(false); }
                    catch (ArgumentNullException) { rejected = true; }
                    Expect(rejected && !File.Exists(path), "null快照必须被拒绝且不能创建文件。");
                }
                finally { DeleteOwnStorageFile(path); }
            }).ConfigureAwait(false);
        }

        private static WaimaiPersistedSnapshot StorageSnapshot()
        {
            WaimaiPersistedSnapshot snapshot = new WaimaiPersistedSnapshot
            {
                SavedAtUtc = "2026-10-22T04:00:00Z", CurrentAddressId = "selftest-address", IsEasyGoDualPane = true
            };
            snapshot.CartItems.Add(Item(12.5, 15, 1, 2, 2));
            snapshot.Orders.Add(OrderListResponseContract.FromJson(OfficialOrderJson).OrderList[0]);
            snapshot.Coupons.Add(Coupon(8, 20, 1001));
            snapshot.Addresses.Add(new WmAddressContract { AddressId = "selftest-address", ContactName = "磁盘加密自检收货人", Phone = "13900000000", AddressDetail = "自检路1号", HouseNumber = "101", IsDefault = true, Latitude = 31230400, Longitude = 121473700 });
            return snapshot;
        }

        private static string NewStoragePath()
        {
            return Path.Combine(Path.GetTempPath(), "waimai-contract-selftest-" + Guid.NewGuid().ToString("N") + ".dpapi");
        }

        private static void DeleteOwnStorageFile(string path)
        {
            // 路径仅由本自检的 GUID 工厂生成；只清理自己创建的单个文件。
            if (File.Exists(path)) File.Delete(path);
        }

        private static PoiContract TestPoi(double minimum)
        {
            return new PoiContract { Id = 1001, MinPrice = minimum, ShippingFee = 0, OriginShippingFee = 0 };
        }

        private static ShopCartItemContract Item(double unit, double original, double box, int count, int pocket)
        {
            return new ShopCartItemContract { PoiId = 1001, SpuId = 100101, SkuId = 1001011, FoodName = "自检商品", UnitPrice = unit, OriginUnitPrice = original, BoxFeePerUnit = box, Count = count, PocketId = pocket };
        }

        private static List<ShopCartItemContract> Cart(params ShopCartItemContract[] items)
        {
            return new List<ShopCartItemContract>(items);
        }

        private static ShenQuanCouponContract Coupon(double amount, double threshold, long poiId)
        {
            return new ShenQuanCouponContract { CouponId = "selftest-coupon", BaseAmount = amount, Amount = amount, Threshold = threshold, ApplicablePoiId = poiId };
        }

        private static void Run(List<SelfTestItem> results, string id, string category, string name, string evidence, Action body)
        {
            SelfTestItem item = new SelfTestItem { Id = id, Category = category, Name = name };
            try
            {
                body();
                item.Passed = true;
                item.EvidenceDetail = evidence;
            }
            catch (Exception error)
            {
                item.Passed = false;
                item.EvidenceDetail = error.GetType().FullName + ": " + error.Message;
            }
            results.Add(item);
        }

        private static async Task RunAsync(List<SelfTestItem> results, string id, string category, string name, string evidence, Func<Task> body)
        {
            SelfTestItem item = new SelfTestItem { Id = id, Category = category, Name = name };
            try
            {
                await body().ConfigureAwait(false);
                item.Passed = true;
                item.EvidenceDetail = evidence;
            }
            catch (Exception error)
            {
                item.Passed = false;
                item.EvidenceDetail = error.GetType().FullName + ": " + error.Message;
            }
            results.Add(item);
        }

        private static void Expect(bool condition, string detail)
        {
            if (!condition) throw new InvalidOperationException(detail);
        }

        private static void EqualAmount(double expected, double actual, string name)
        {
            if (double.IsNaN(actual) || double.IsInfinity(actual) || Math.Abs(expected - actual) > 0.000001)
                throw new InvalidOperationException(name + "：期望 " + expected.ToString(System.Globalization.CultureInfo.InvariantCulture) + "，实际 " + actual.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        private static void EqualText(string expected, string actual, string name)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException(name + "：期望「" + expected + "」，实际「" + actual + "」。");
        }

        private static Dictionary<string, object> ReadAsset(string repoRootPath, string fileName)
        {
            if (string.IsNullOrWhiteSpace(repoRootPath)) throw new ArgumentException("必须提供仓库根目录。", "repoRootPath");
            string path = Path.Combine(repoRootPath, "research", "apk-assets", fileName);
            string json = File.ReadAllText(path, Encoding.UTF8);
            return ObjectValue(new System.Web.Script.Serialization.JavaScriptSerializer().DeserializeObject(json));
        }

        private static Dictionary<string, object> ObjectValue(object value)
        {
            Dictionary<string, object> result = value as Dictionary<string, object>;
            if (result == null) throw new FormatException("预期 JSON 对象，实际结构不匹配。");
            return result;
        }

        private static object[] ArrayValue(Dictionary<string, object> source, string key)
        {
            object[] result = source[key] as object[];
            if (result == null) throw new FormatException(key + " 必须为 JSON 数组。");
            return result;
        }

        private static string ValueText(Dictionary<string, object> source, string key)
        {
            return Convert.ToString(source[key], System.Globalization.CultureInfo.InvariantCulture);
        }
    }

}
