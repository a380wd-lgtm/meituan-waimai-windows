// REA Evidence ID: ev_85cbd0da4f81234469001e25c484a966098bd41bafe88fa5e8d9f76304cb4668
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.order.api.model.OrderListResponse
// Method: fromJson(org.json.JSONObject): com.sankuai.waimai.business.order.api.model.OrderListResponse

    public static OrderListResponse fromJson(JSONObject jSONObject) throws JSONException {
        Object[] objArr = {jSONObject};
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr, null, changeQuickRedirect2, 10386284)) {
            return (OrderListResponse) PatchProxy.accessDispatch(objArr, null, changeQuickRedirect2, 10386284);
        }
        if (jSONObject == null) {
            return null;
        }
        OrderListResponse orderListResponse = new OrderListResponse();
        orderListResponse.type = jSONObject.optString("type");
        orderListResponse.cursor = jSONObject.optString("cursor");
        orderListResponse.multiCursor = jSONObject.optString("multi_cursor");
        orderListResponse.hasMore = jSONObject.optInt("hasmore");
        orderListResponse.unCommentCount = jSONObject.optInt("uncomment_count");
        orderListResponse.hasReadUnCommentOrder = jSONObject.optInt("has_unread_comment_order") != 0;
        orderListResponse.unreadUncommentTips = jSONObject.optString("unread_uncomment_tips");
        orderListResponse.orderList = f.a(jSONObject.optJSONArray("digestlist"));
        orderListResponse.setWaitTime(jSONObject.optInt("wait_time", -1));
        orderListResponse.parseTypeList(jSONObject.optJSONArray("list_types"));
        orderListResponse.refundCount = jSONObject.optInt("refund_count");
        orderListResponse.bottomTips = jSONObject.optString("bottom_tips");
        orderListResponse.searchDefaultTips = jSONObject.optString("search_default_tips");
        orderListResponse.hasReadUnRefundOrder = jSONObject.optInt("has_unread_refund_order") != 0;
        orderListResponse.shareType = jSONObject.optInt("share_type");
        orderListResponse.trackingInfo = jSONObject.optString("tracking_info");
        JSONObject jSONObjectOptJSONObject = jSONObject.optJSONObject("envelope_share_tip");
        if (jSONObjectOptJSONObject != null) {
            EnvelopeShareTip envelopeShareTip = new EnvelopeShareTip();
            orderListResponse.mShareTip = envelopeShareTip;
            envelopeShareTip.parseJson(jSONObjectOptJSONObject);
        }
        JSONObject jSONObjectOptJSONObject2 = jSONObject.optJSONObject("activity_share_tip");
        if (jSONObjectOptJSONObject2 != null) {
            ActivityDialogInfo activityDialogInfo = new ActivityDialogInfo();
            orderListResponse.mDialogInfo = activityDialogInfo;
            activityDialogInfo.parseJson(jSONObjectOptJSONObject2);
        }
        String strOptString = jSONObject.optString("ugc_pop_ups_info");
        if (!TextUtils.isEmpty(strOptString)) {
            com.sankuai.waimai.business.order.api.detail.network.response.a aVar = new com.sankuai.waimai.business.order.api.detail.network.response.a();
            orderListResponse.mPopupsInfo = aVar;
            aVar.b(strOptString);
        }
        return orderListResponse;
    }
