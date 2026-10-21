// REA Evidence ID: ev_05148e135ba41d4a9f18fb39d2c3ea5134ebf9a6b5480c2a354da5510b9c1617
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.homepage.MainActivity
// Method: v0(com.sankuai.waimai.business.page.home.model.DynamicTabListInfoResponse): void

    /* JADX WARN: Code duplicated, block: B:211:0x044b  */
    /* JADX WARN: Code duplicated, block: B:212:0x044f  */
    /* JADX WARN: Code duplicated, block: B:214:0x0458  */
    /* JADX WARN: Code duplicated, block: B:259:0x045a A[SYNTHETIC] */
    /* JADX WARN: Multi-variable type inference failed */
    /* JADX WARN: Type inference failed for: r2v0 */
    /* JADX WARN: Type inference failed for: r2v47 */
    /* JADX WARN: Type inference failed for: r4v0 */
    /* JADX WARN: Type inference failed for: r4v36 */
    /* JADX WARN: Type inference failed for: r6v1 */
    /* JADX WARN: Type inference failed for: r6v14 */
    /* JADX WARN: Type inference failed for: r6v2, types: [com.sankuai.waimai.business.page.home.model.MarketingTabExtInfo, com.sankuai.waimai.business.page.homepage.view.tab.TabInfo, java.lang.Object] */
    @Override // com.sankuai.waimai.business.page.home.interfacer.c
    public final void v0(DynamicTabListInfoResponse dynamicTabListInfoResponse) {
        boolean z;
        int i2;
        TabInfo tabInfoC;
        boolean z2;
        String str;
        String str2;
        int i3;
        int i4;
        Object[] objArr;
        ChangeQuickRedirect changeQuickRedirect2;
        int iC;
        View childAt;
        int i5 = 1;
        int i6 = 0;
        Object[] objArr2 = {dynamicTabListInfoResponse};
        ChangeQuickRedirect changeQuickRedirect3 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr2, this, changeQuickRedirect3, 5368816)) {
            PatchProxy.accessDispatch(objArr2, this, changeQuickRedirect3, 5368816);
            return;
        }
        if (dynamicTabListInfoResponse == null) {
            return;
        }
        List<DynamicTabInfo> listT3 = MarketingTabKNBFragment.t3(dynamicTabListInfoResponse.dynamicTabList);
        this.z0 = false;
        this.A0 = 0;
        this.B0 = "";
        this.x0 = false;
        this.w0 = false;
        ?? r6 = 0;
        this.g0 = null;
        this.h0 = null;
        this.C0 = "";
        this.D0 = false;
        PreloadDataModel.get().setResetPromotionTab(Boolean.TRUE);
        int i7 = 11;
        int i8 = 2;
        int i9 = 8;
        if (com.sankuai.waimai.foundation.utils.b.f(listT3)) {
            z = false;
            int i10 = 0;
            while (i10 < listT3.size()) {
                DynamicTabInfo dynamicTabInfo = listT3.get(i10);
                int i11 = dynamicTabInfo.id;
                if (i11 == i7) {
                    Object[] objArr3 = new Object[i5];
                    objArr3[i6] = dynamicTabInfo;
                    ChangeQuickRedirect changeQuickRedirect4 = changeQuickRedirect;
                    if (PatchProxy.isSupport(objArr3, this, changeQuickRedirect4, 10545196)) {
                        PatchProxy.accessDispatch(objArr3, this, changeQuickRedirect4, 10545196);
                    } else if (dynamicTabInfo.id == i7 && dynamicTabInfo.isShow && !TextUtils.isEmpty(dynamicTabInfo.name) && this.Y != null) {
                        MarketingTabExtInfo marketingTabExtInfo = MarketingTabExtInfo.parse(dynamicTabInfo.ext_info);
                        this.h0 = marketingTabExtInfo;
                        if (MarketingTabKNBFragment.z3(dynamicTabInfo.link, dynamicTabInfo.normalPicUrl, marketingTabExtInfo)) {
                            TabInfo tabInfoC2 = this.Y.c(i5);
                            if (tabInfoC2 != null && (childAt = this.Y.getChildAt(tabInfoC2.h)) != null) {
                                if ((tabInfoC2.i == MarketingTabKNBFragment.class && this.f0.get(i5) == null) ? false : true) {
                                    Fragment fragment = this.f0.get(i5);
                                    if (fragment != null) {
                                        l0 l0VarG = getSupportFragmentManager().g();
                                        l0VarG.q(fragment);
                                        l0VarG.j();
                                    }
                                    this.f0.put(i5, (Fragment) r6);
                                    tabInfoC2.c(MarketingTabKNBFragment.class);
                                }
                                tabInfoC2.f50920a = dynamicTabInfo.id;
                                String str3 = dynamicTabInfo.name;
                                tabInfoC2.f50921b = str3;
                                tabInfoC2.f50922c = dynamicTabInfo.link;
                                String str4 = dynamicTabInfo.normalPicUrl;
                                if (str4 == null) {
                                    str4 = "";
                                }
                                tabInfoC2.f50923d = str4;
                                String str5 = dynamicTabInfo.selectedPicUrl;
                                if (str5 == null) {
                                    str5 = "";
                                }
                                tabInfoC2.f50924e = str5;
                                this.Y.A(str3);
                                this.v0 = i5;
                                this.y0 = tabInfoC2;
                                childAt.setVisibility(i6);
                                com.sankuai.waimai.business.page.homepage.view.tab.h.g(this, dynamicTabInfo.name, this.h0);
                            }
                        } else {
                            this.h0 = r6;
                        }
                    }
                    MarketingTabExtInfo marketingTabExtInfo2 = MarketingTabExtInfo.parse(dynamicTabInfo.ext_info);
                    if (marketingTabExtInfo2 == null || !com.sankuai.waimai.business.page.homepage.view.tab.i.g(marketingTabExtInfo2.jumpType)) {
                        try {
                            com.sankuai.waimai.business.knb.k.c(dynamicTabInfo.link, "FusionTab");
                        } catch (Exception e2) {
                            com.sankuai.waimai.foundation.utils.log.a.n(e2);
                        }
                    }
                    int i12 = this.U;
                    if (i12 == i5) {
                        f5(i12);
                    }
                } else {
                    if (i11 == 9) {
                        this.x0 = i5;
                        Object[] objArr4 = new Object[i5];
                        objArr4[i6] = dynamicTabInfo;
                        ChangeQuickRedirect changeQuickRedirect5 = changeQuickRedirect;
                        if (PatchProxy.isSupport(objArr4, this, changeQuickRedirect5, 4970920)) {
                            PatchProxy.accessDispatch(objArr4, this, changeQuickRedirect5, 4970920);
                        } else {
                            BottomTabView bottomTabView = this.Y;
                            TabInfo tabInfoC3 = bottomTabView != null ? bottomTabView.c(i5) : r6;
                            if (dynamicTabInfo.id == 9 && tabInfoC3 != 0 && (this.f0.get(i5) != null || tabInfoC3.f50920a == i7)) {
                                Fragment fragment2 = this.f0.get(i5);
                                if (fragment2 != null) {
                                    l0 l0VarG2 = getSupportFragmentManager().g();
                                    l0VarG2.q(fragment2);
                                    l0VarG2.j();
                                }
                                this.f0.put(i5, (Fragment) r6);
                                if (com.sankuai.waimai.business.page.homepage.knb.a.c()) {
                                    tabInfoC3.c(Q4().b());
                                } else {
                                    tabInfoC3.c(WMVIPCardKNBFragment.class);
                                }
                            }
                            BottomTabView bottomTabView2 = this.Y;
                            if (bottomTabView2 != null && tabInfoC3 != 0 && tabInfoC3.h < bottomTabView2.getTabCount()) {
                                View childAt2 = this.Y.getChildAt(tabInfoC3.h);
                                String str6 = dynamicTabInfo.link;
                                String str7 = dynamicTabInfo.name;
                                boolean z3 = dynamicTabInfo.isShow;
                                if (childAt2 != null) {
                                    if (!z3 || TextUtils.isEmpty(str6) || TextUtils.isEmpty(str7)) {
                                        this.v0 = i6;
                                        this.y0 = r6;
                                        childAt2.setVisibility(i9);
                                    } else {
                                        this.Y.A(str7);
                                        tabInfoC3.f50920a = 9;
                                        tabInfoC3.f50922c = str6;
                                        tabInfoC3.f50921b = str7;
                                        String str8 = dynamicTabInfo.normalPicUrl;
                                        if (str8 == null) {
                                            str8 = "";
                                        }
                                        tabInfoC3.f50923d = str8;
                                        String str9 = dynamicTabInfo.selectedPicUrl;
                                        if (str9 == null) {
                                            str9 = "";
                                        }
                                        tabInfoC3.f50924e = str9;
                                        this.v0 = i5;
                                        this.y0 = tabInfoC3;
                                        childAt2.setVisibility(i6);
                                        JudasManualManager.a aVarM = JudasManualManager.m("b_qnjzwaav");
                                        aVarM.i("c_m84bv26");
                                        aVarM.l(this.Y).a();
                                        Object[] objArr5 = new Object[i6];
                                        ChangeQuickRedirect changeQuickRedirect6 = changeQuickRedirect;
                                        if (PatchProxy.isSupport(objArr5, this, changeQuickRedirect6, 6665142)) {
                                            PatchProxy.accessDispatch(objArr5, this, changeQuickRedirect6, 6665142);
                                        } else {
                                            JudasManualManager.o("b_waimai_23w4c3m0_mv", "c_vt3zp1ef", AppUtil.generatePageInfoKey(this)).f("vip_tab_exp", this.x0 ? "sq" : KiteFlyConstants.VIP_NAME).a();
                                        }
                                    }
                                }
                            }
                        }
                        if (dynamicTabInfo.id == 9 && (i3 = this.U) == i5) {
                            f5(i3);
                            TabInfo tabInfoC4 = this.Y.c(i5);
                            if (tabInfoC4 != null) {
                                T4(this.U, tabInfoC4.f, i6);
                            }
                        }
                        PreloadWebViewHelper.b(dynamicTabInfo.link);
                    } else {
                        if (i11 == 10) {
                            BottomTabView bottomTabView3 = this.Y;
                            if (bottomTabView3 != null) {
                                TabInfo tabInfoC5 = bottomTabView3.c(i8);
                                Object[] objArr6 = new Object[i8];
                                objArr6[i6] = dynamicTabInfo;
                                objArr6[i5] = tabInfoC5;
                                ChangeQuickRedirect changeQuickRedirect7 = changeQuickRedirect;
                                if (PatchProxy.isSupport(objArr6, this, changeQuickRedirect7, 5708160)) {
                                    PatchProxy.accessDispatch(objArr6, this, changeQuickRedirect7, 5708160);
                                } else if (tabInfoC5 != null) {
                                    if (dynamicTabInfo.id == 10) {
                                        l0 l0VarG3 = getSupportFragmentManager().g();
                                        if (this.f0.get(i8) != null) {
                                            l0VarG3.q(this.f0.get(i8));
                                            l0VarG3.j();
                                        }
                                        this.f0.put(i8, (Fragment) r6);
                                        tabInfoC5.f = new TabKnbFragment();
                                    }
                                    com.sankuai.waimai.business.page.homepage.bubble.d dVar = new com.sankuai.waimai.business.page.homepage.bubble.d();
                                    this.g0 = dVar;
                                    dVar.d(dynamicTabInfo.ext_info);
                                    com.sankuai.waimai.business.page.homepage.bubble.d dVar2 = this.g0;
                                    JSONObject jSONObject = dVar2.f50774a;
                                    JSONObject jSONObject2 = dVar2.f50775b;
                                    boolean zC = com.sankuai.waimai.business.page.homepage.bubble.d.c(dVar2.f50778e);
                                    BottomTabView bottomTabView4 = this.Y;
                                    View childAt3 = (bottomTabView4 == null || tabInfoC5.h >= bottomTabView4.getTabCount()) ? null : this.Y.getChildAt(tabInfoC5.h);
                                    if (childAt3 != null) {
                                        childAt3.setVisibility(i9);
                                    }
                                    if (jSONObject != null) {
                                        String strOptString = jSONObject.optString("commonUrlV2");
                                        String strOptString2 = jSONObject.optString("tab_icon_pic");
                                        String strOptString3 = jSONObject.optString("tab_icon_second_pic");
                                        int iOptInt = jSONObject.optInt("jumpType");
                                        this.A0 = iOptInt;
                                        if (com.sankuai.waimai.business.page.homepage.view.tab.i.g(iOptInt)) {
                                            this.B0 = strOptString;
                                        }
                                        if (!TextUtils.isEmpty(strOptString) && !TextUtils.isEmpty(strOptString2)) {
                                            tabInfoC5.f50922c = strOptString;
                                            tabInfoC5.f50924e = strOptString3;
                                            tabInfoC5.f50923d = strOptString2;
                                            dynamicTabInfo.link = strOptString;
                                            dynamicTabInfo.normalPicUrl = strOptString2;
                                            dynamicTabInfo.selectedPicUrl = strOptString3;
                                            this.z0 = i5;
                                            if (zC) {
                                                this.w0 = i5;
                                            }
                                            this.o = jSONObject.optString("yyActivityType");
                                            if (jSONObject2 == null) {
                                                str = "";
                                            } else {
                                                str = jSONObject2.optInt("resourceId") + "";
                                            }
                                            this.p = str;
                                            if (jSONObject2 == null) {
                                                str2 = "";
                                            } else {
                                                str2 = jSONObject2.optString("templateCode") + "";
                                            }
                                            this.q = str2;
                                            com.sankuai.waimai.business.page.homepage.view.tab.h.i(this, this.p, this.o, str2);
                                        }
                                    }
                                    if (this.z0) {
                                        View viewFindViewById = childAt3.findViewById(R.id.iv_mt_cardview);
                                        View viewFindViewById2 = childAt3.findViewById(R.id.iv_top_arc);
                                        ImageView imageView = (ImageView) childAt3.findViewById(R.id.iv_promotion);
                                        CardView cardView = (CardView) viewFindViewById;
                                        Object[] objArr7 = new Object[2];
                                        objArr7[0] = cardView;
                                        objArr7[i5] = imageView;
                                        ChangeQuickRedirect changeQuickRedirect8 = changeQuickRedirect;
                                        if (PatchProxy.isSupport(objArr7, this, changeQuickRedirect8, 4498107)) {
                                            PatchProxy.accessDispatch(objArr7, this, changeQuickRedirect8, 4498107);
                                        } else {
                                            BottomTabView bottomTabView5 = this.Y;
                                            if (bottomTabView5 != null) {
                                                bottomTabView5.q(this.w0, cardView, imageView);
                                            }
                                        }
                                        if (zC) {
                                            cardView.setCardBackgroundColor(0);
                                        }
                                        childAt3.setVisibility(0);
                                        if (viewFindViewById != null) {
                                            viewFindViewById.setVisibility(0);
                                        }
                                        if (viewFindViewById2 != null) {
                                            if (zC) {
                                                viewFindViewById2.setVisibility(8);
                                            } else {
                                                viewFindViewById2.setVisibility(0);
                                            }
                                        }
                                    } else {
                                        com.sankuai.waimai.foundation.utils.log.a.a("WMPopup", "notifyDynamicTabsChanged7: no hasPromotionTab", new Object[0]);
                                    }
                                }
                                if (this.U == 2) {
                                    if (com.sankuai.waimai.business.page.homepage.view.tab.i.g(this.A0)) {
                                        this.Z.c(this.B0);
                                    } else {
                                        f5(this.U);
                                        TabInfo tabInfoC6 = this.Y.c(2);
                                        if (tabInfoC5 != null && tabInfoC6 != null) {
                                            S4(tabInfoC5.h, tabInfoC6.f, false);
                                        }
                                    }
                                }
                            }
                        } else if (i11 == i5) {
                            HomePageListStrategyHelper.c().F(dynamicTabInfo.ext_info);
                            if (this.U != 0) {
                                z2 = true;
                            } else if (!HomePageListStrategyHelper.c().j() || this.s) {
                                z2 = true;
                                this.Y.setCurrentIconStatus(1);
                            } else {
                                this.Y.setCurrentIconStatus(4);
                                z2 = true;
                            }
                            this.C0 = dynamicTabInfo.ext_info;
                            DeepEatStateManager.f().S(this.C0);
                            this.D0 = z2;
                            i4 = z2;
                        }
                        i4 = 1;
                    }
                    objArr = new Object[i4];
                    objArr[0] = dynamicTabInfo;
                    changeQuickRedirect2 = changeQuickRedirect;
                    if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 8775566)) {
                        PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 8775566);
                    } else {
                        iC = com.sankuai.waimai.business.page.homepage.view.tab.g.c(dynamicTabInfo.id);
                        if (iC != -1) {
                            dynamicTabInfo.index = iC;
                        }
                    }
                    this.Y.z(dynamicTabInfo, this.U, this.w0);
                    i10++;
                    i5 = 1;
                    i6 = 0;
                    r6 = 0;
                    i7 = 11;
                    i8 = 2;
                    i9 = 8;
                }
                z = true;
                i4 = 1;
                objArr = new Object[i4];
                objArr[0] = dynamicTabInfo;
                changeQuickRedirect2 = changeQuickRedirect;
                if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 8775566)) {
                    PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 8775566);
                } else {
                    iC = com.sankuai.waimai.business.page.homepage.view.tab.g.c(dynamicTabInfo.id);
                    if (iC != -1) {
                        dynamicTabInfo.index = iC;
                    }
                }
                this.Y.z(dynamicTabInfo, this.U, this.w0);
                i10++;
                i5 = 1;
                i6 = 0;
                r6 = 0;
                i7 = 11;
                i8 = 2;
                i9 = 8;
            }
        } else {
            z = false;
        }
        HomePageListStrategyHelper.c().n(this.z0, this.w0);
        if (z) {
            i2 = 8;
        } else {
            this.v0 = false;
            Object[] objArr8 = {new Integer(1)};
            ChangeQuickRedirect changeQuickRedirect9 = changeQuickRedirect;
            if (PatchProxy.isSupport(objArr8, this, changeQuickRedirect9, 9379731)) {
                PatchProxy.accessDispatch(objArr8, this, changeQuickRedirect9, 9379731);
            } else {
                TabInfo tabInfoC7 = this.Y.c(1);
                if (tabInfoC7 != null) {
                    i2 = 8;
                    this.Y.t(tabInfoC7.h, 8);
                }
            }
            i2 = 8;
        }
        if (!this.z0 && (tabInfoC = this.Y.c(2)) != null) {
            this.Y.t(tabInfoC.h, i2);
        }
        Object[] objArr9 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect10 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr9, this, changeQuickRedirect10, 13820458)) {
            PatchProxy.accessDispatch(objArr9, this, changeQuickRedirect10, 13820458);
        } else if (this.Y != null) {
            if (com.sankuai.waimai.business.page.homepage.bubble.g.h().m()) {
                this.Y.post(new l());
            } else if (com.sankuai.waimai.business.page.homepage.bubble.g.h().j()) {
                this.Y.postDelayed(new m(), 1000L);
            }
        }
        if (!this.D0) {
            this.D0 = true;
            this.Y.post(new k());
        }
        this.Z.k(this.g0, this.h0);
        com.sankuai.waimai.business.page.homepage.controller.d dVar3 = this.i0;
        if (dVar3 != null) {
            dVar3.h(com.sankuai.waimai.business.page.homepage.view.tab.g.c(11), this.h0);
        }
        Object[] objArr10 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect11 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr10, this, changeQuickRedirect11, 863246)) {
            PatchProxy.accessDispatch(objArr10, this, changeQuickRedirect11, 863246);
        } else {
            TabBubbleInfo tabBubbleInfo = com.sankuai.waimai.business.page.homepage.bubble.g.h().f50783c;
            this.Y.y(com.sankuai.waimai.business.page.homepage.bubble.g.h().i(tabBubbleInfo), tabBubbleInfo);
        }
    }
