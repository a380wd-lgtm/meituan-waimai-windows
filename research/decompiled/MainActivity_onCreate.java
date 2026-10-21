// REA Evidence ID: ev_0063a0c6f104e543d38943d632c596951f7e026ba44437b8c64e936f02eda476
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.homepage.MainActivity
// Method: onCreate(android.os.Bundle): void

    @Override // com.sankuai.waimai.business.page.common.arch.PageActivity, com.sankuai.waimai.foundation.core.base.activity.BaseActivity, androidx.fragment.app.FragmentActivity, androidx.activity.ComponentActivity, androidx.core.app.ComponentActivity, android.app.Activity
    public final void onCreate(Bundle bundle) {
        SparseArray<TabInfo> sparseArray;
        Object[] objArr = {bundle};
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 14010386)) {
            PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 14010386);
            return;
        }
        if (com.sankuai.waimai.foundation.core.a.h() && com.sankuai.waimai.platform.utils.sharedpreference.b.b0() && !L0) {
            setTheme(R.style.HomePageTheme);
            ((IWelcomeProvider) com.sankuai.waimai.router.a.d(IWelcomeProvider.class, "welcome")).onCreate(this, getIntent(), bundle);
        }
        if (TimeUtil.elapsedTimeMillis() - TimeUtil.processStartElapsedTimeMillis() > 30000) {
            com.sankuai.waimai.business.page.home.utils.l.a("main_page_start_delayed");
        } else {
            com.sankuai.waimai.business.page.home.utils.l.c("Home+");
            com.sankuai.waimai.business.page.home.utils.j.b().c("Home+", new boolean[0]);
        }
        r.b().d("Home+");
        if (bundle != null) {
            this.x = bundle.getBoolean("mHasPostSkyFallMark", false);
            this.y = bundle.getBoolean("mNeedPostSkyFallRequest", false);
            this.z = bundle.getBoolean("pre_loading_discard_sky_fall_mark");
            this.C = bundle.getBoolean("splash_ad_intent", false);
            this.B = bundle.getBoolean("check_popups_after_ad", false);
        }
        if (!this.C) {
            this.A = (Intent) getIntent().getParcelableExtra("splash_ad_intent");
        }
        if (bundle == null && this.A != null) {
            ABStrategy strategy = ABTestManager.getInstance().getStrategy("tianjiang_loading_android", null);
            if (strategy == null || !"preloading".equalsIgnoreCase(strategy.expName)) {
                this.x = true;
                this.y = true;
                this.z = false;
            } else {
                this.z = true;
                this.x = false;
                this.y = false;
            }
        }
        com.sankuai.waimai.business.page.homepage.manager.b.a().b(this);
        if (this.A == null) {
            M4();
        }
        HomePageProvider homePageProvider = (HomePageProvider) com.sankuai.waimai.router.a.d(HomePageProvider.class, "wm_main");
        if (homePageProvider != null) {
            homePageProvider.setIsExit(false);
        }
        super.onCreate(bundle);
        com.sankuai.waimai.business.page.home.preload.g.c(this.g);
        if (bundle != null) {
            this.U = bundle.getInt("mCurrentTab", 0);
            Object[] objArr2 = {bundle};
            ChangeQuickRedirect changeQuickRedirect3 = changeQuickRedirect;
            if (PatchProxy.isSupport(objArr2, this, changeQuickRedirect3, 13695365)) {
                PatchProxy.accessDispatch(objArr2, this, changeQuickRedirect3, 13695365);
            } else {
                String string = bundle.getString("vipCardTabName", "");
                String string2 = bundle.getString("vipCardTabLink", "");
                String string3 = bundle.getString("normalPicUrl", "");
                String string4 = bundle.getString("selectedPicUrl", "");
                this.v0 = bundle.getBoolean("isShowVipCardTab", false);
                int i2 = bundle.getInt("vipCardTabBusinessId", 9);
                Class<MarketingTabKNBFragment> clsB = Q4().b();
                if (i2 == 11) {
                    clsB = MarketingTabKNBFragment.class;
                }
                TabInfo tabInfo = new TabInfo(i2, string, 1, clsB);
                tabInfo.f50922c = string2;
                tabInfo.f50923d = string3;
                tabInfo.f50924e = string4;
                this.y0 = tabInfo;
            }
        } else if (!b5()) {
            a5(getIntent());
        }
        getWindow().setBackgroundDrawableResource(R.color.wm_restaurant_common_background_page);
        I4(true, true);
        com.sankuai.waimai.platform.popup.f.d().g(this);
        com.sankuai.waimai.platform.popup.f.d().a(this);
        try {
            setContentView(com.meituan.android.paladin.b.d(R.layout.takeout_activity_main));
        } catch (Resources.NotFoundException e2) {
            try {
                setContentView(com.meituan.android.paladin.b.d(R.layout.takeout_activity_main));
            } catch (Exception unused) {
                com.sankuai.waimai.foundation.utils.log.a.c(getClass().getSimpleName(), e2.getMessage(), new Object[0]);
                finish();
                return;
            }
        }
        N0 = new WeakReference<>(this);
        this.c0 = (FrameLayout) findViewById(R.id.indicator_bg_container);
        this.Z = new com.sankuai.waimai.business.page.homepage.view.tab.i(this.c0, this);
        BottomTabView bottomTabView = (BottomTabView) findViewById(R.id.pagerindicator);
        this.Y = bottomTabView;
        bottomTabView.setActivity(this);
        this.e0 = (FrameLayout) findViewById(R.id.pager_container);
        this.d0 = new com.sankuai.waimai.business.page.homepage.view.tab.e(this.Y);
        ViewStub viewStub = (ViewStub) findViewById(R.id.layout_order_status);
        ViewStub viewStub2 = (ViewStub) findViewById(R.id.layout_weather_status);
        this.a0 = (ViewStub) findViewById(R.id.feedback_container);
        this.b0 = (ViewStub) findViewById(R.id.feedback_toast_container);
        this.j0 = (LinearLayout) findViewById(R.id.floating_container);
        Object[] objArr3 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect4 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr3, this, changeQuickRedirect4, 14994874)) {
            PatchProxy.accessDispatch(objArr3, this, changeQuickRedirect4, 14994874);
        } else {
            HomeGrayManager.e().b(this.Y, 2, 4, "", 0);
            HomeGrayManager.e().b(this.j0, 2, 5, "", 0);
            if (getWindow() != null) {
                HomeGrayManager.e().b(getWindow().getDecorView(), 1, 0, "", 0);
            }
        }
        this.V = new ResourceHelper(this);
        this.X = new com.sankuai.waimai.business.page.homepage.controller.e(this, this.o0, viewStub, viewStub2);
        this.W = new com.sankuai.waimai.business.page.homepage.controller.f(this, this.o0, this.Y, v4(), this.X);
        com.sankuai.waimai.business.page.homepage.bubble.e.a().b(this);
        com.sankuai.waimai.platform.domain.manager.user.a.A().q(this);
        TabLoadManager.a().b(this);
        Object[] objArr4 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect5 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr4, this, changeQuickRedirect5, 8833246)) {
            PatchProxy.accessDispatch(objArr4, this, changeQuickRedirect5, 8833246);
        } else {
            Object[] objArr5 = new Object[0];
            ChangeQuickRedirect changeQuickRedirect6 = changeQuickRedirect;
            if (PatchProxy.isSupport(objArr5, this, changeQuickRedirect6, 1186606)) {
                sparseArray = (SparseArray) PatchProxy.accessDispatch(objArr5, this, changeQuickRedirect6, 1186606);
            } else {
                sparseArray = new SparseArray<>();
                Class<Fragment> orderListFragmentClass = ((IOrderListProvider) com.sankuai.waimai.router.a.d(IOrderListProvider.class, "wm_order")).getOrderListFragmentClass();
                sparseArray.put(0, new TabInfo(1, getString(R.string.title_section0), 0, WMHomePageFragment.class));
                TabInfo tabInfo2 = this.y0;
                if (tabInfo2 == null) {
                    tabInfo2 = new TabInfo(9, getString(R.string.title_section2), 1, Q4().b());
                }
                sparseArray.put(1, tabInfo2);
                sparseArray.put(2, new TabInfo(10, "大促", 2, TabKnbFragment.class));
                String string5 = getString(R.string.title_section3);
                if (orderListFragmentClass == null) {
                    orderListFragmentClass = Fragment.class;
                }
                sparseArray.put(3, new TabInfo(2, string5, 3, orderListFragmentClass));
                sparseArray.put(4, new TabInfo(3, getString(R.string.title_section4), 4, MineMPFragment.class));
            }
            this.Y.i(this.U, sparseArray);
            new ResourceHelper(this).getShowDrawable("bg_bottom_tab");
            this.Y.setOnTabEventListener(new com.sankuai.waimai.business.page.homepage.d(this));
            com.sankuai.waimai.business.page.homepage.controller.d dVar = new com.sankuai.waimai.business.page.homepage.controller.d();
            this.i0 = dVar;
            dVar.b(this.Y, this);
            TabInfo tabInfoC = this.Y.c(1);
            if (tabInfoC != null) {
                this.Y.t(tabInfoC.h, this.v0 ? 0 : 8);
            }
            TabInfo tabInfoC2 = this.Y.c(2);
            if (tabInfoC2 != null) {
                this.Y.t(tabInfoC2.h, 8);
            }
            e5();
        }
        Object[] objArr6 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect7 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr6, this, changeQuickRedirect7, 2352243)) {
            PatchProxy.accessDispatch(objArr6, this, changeQuickRedirect7, 2352243);
        } else {
            z4(new o(this.V));
        }
        Z4(getIntent());
        Object[] objArr7 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect8 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr7, this, changeQuickRedirect8, 6615563)) {
            PatchProxy.accessDispatch(objArr7, this, changeQuickRedirect8, 6615563);
        } else {
            IntentFilter intentFilter = new IntentFilter();
            this.u0 = intentFilter;
            intentFilter.addAction("android.intent.action.SCREEN_OFF");
            this.u0.addAction("android.intent.action.SCREEN_ON");
        }
        this.k0 = new h();
        com.sankuai.waimai.business.page.homepage.bubble.g.h().a(this.k0);
        this.l0 = new i();
        HomePageListStrategyHelper.c().a(this.l0);
        androidx.localbroadcastmanager.content.a.b(this).c(this.J0, new IntentFilter("waimai:waimai_vip_status_bar_style"));
        n nVar = new n(this.j0);
        this.u = nVar;
        com.sankuai.waimai.business.page.homepage.manager.a.a(nVar);
        com.sankuai.waimai.popup.b.g();
        Weaver.getExtension().addCustomTags(this.n, this, "wm_home_second_opening_rate_expname", com.sankuai.waimai.business.page.common.abtest.a.v());
        Object[] objArr8 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect9 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr8, this, changeQuickRedirect9, 16221260)) {
            PatchProxy.accessDispatch(objArr8, this, changeQuickRedirect9, 16221260);
        } else {
            com.sankuai.waimai.business.page.common.util.c.b(this);
            com.sankuai.waimai.business.page.common.util.c.d(findViewById(R.id.navigation_bar_margin_layout_container));
            com.sankuai.waimai.business.page.common.util.c.c(this.e0);
        }
        DeepEatStateManager.f().w(this);
        WMHomePageFragment wMHomePageFragmentP4 = P4();
        if (wMHomePageFragmentP4 != null) {
            wMHomePageFragmentP4.G0 = (ViewStub) findViewById(R.id.deep_eat_ai_container);
            wMHomePageFragmentP4.C4(this.a0, this.b0);
        }
        AsyncViewUtils.e().j(this);
    }
