// REA Evidence ID: ev_b83d2da5bff9337004ff614bc26afffc1d30211ea6b8dabf80ba9423d46c2814
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.home.HomePageFragment
// Method: onCreateView(android.view.LayoutInflater, android.view.ViewGroup, android.os.Bundle): android.view.View

    @Override // androidx.fragment.app.Fragment
    @Nullable
    public View onCreateView(LayoutInflater layoutInflater, @Nullable ViewGroup viewGroup, @Nullable Bundle bundle) {
        com.sankuai.waimai.business.page.home.layer.f fVar;
        Object[] objArr = {layoutInflater, viewGroup, bundle};
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 15012764)) {
            return (View) PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 15012764);
        }
        com.sankuai.waimai.business.page.home.utils.r.b().d("HomeViewInflate+");
        ViewGroup viewGroup2 = (ViewGroup) AsyncViewUtils.e().i(com.meituan.android.paladin.b.d(R.layout.wm_page_home_fragment_layout), layoutInflater, viewGroup);
        AsyncViewUtils.e().b(viewGroup2.getContext(), com.sankuai.waimai.foundation.core.a.h() ? 20 : 10);
        com.sankuai.waimai.business.page.home.utils.r.b().d("HomeViewInflate-");
        Object[] objArr2 = {viewGroup2};
        ChangeQuickRedirect changeQuickRedirect3 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr2, this, changeQuickRedirect3, 12202927)) {
            PatchProxy.accessDispatch(objArr2, this, changeQuickRedirect3, 12202927);
        } else {
            this.q.s(viewGroup2);
            WmHomeRooView wmHomeRooView = (WmHomeRooView) viewGroup2.findViewById(R.id.wm_page_main_home);
            this.x = wmHomeRooView;
            wmHomeRooView.setReFreshHeaderHelper(this.d0);
            this.I0 = (ViewStub) this.x.findViewById(R.id.food_guide_info_layer_layout);
            this.y = (ViewGroup) viewGroup2.findViewById(R.id.floating_container);
            HomePageNestedScrollRecyclerView homePageNestedScrollRecyclerView = (HomePageNestedScrollRecyclerView) viewGroup2.findViewById(R.id.nested_scroll_recycler_view);
            this.h = homePageNestedScrollRecyclerView;
            this.y0.r0.f49816e = homePageNestedScrollRecyclerView;
            homePageNestedScrollRecyclerView.setFocusable(true);
            this.h.setFocusableInTouchMode(true);
            this.h.requestFocus();
            PreLoadLinearLayoutManager preLoadLinearLayoutManager = new PreLoadLinearLayoutManager(getContext());
            this.W = preLoadLinearLayoutManager;
            this.h.setLayoutManager(preLoadLinearLayoutManager);
            this.X = new com.sankuai.waimai.business.page.home.basal.c(this, this.h, this.y0, this.W, this.o);
            this.h.setOnNestedScrollListener(new h0());
            this.h.setAdapter(this.X);
            com.sankuai.waimai.business.page.home.basal.c cVar = this.X;
            cVar.u = this.I0;
            cVar.t = this.d0;
            this.X.r = (ViewStub) this.x.findViewById(R.id.wm_page_home_mask_layer);
            this.d0.s = this.h;
            HomePullToRefreshView homePullToRefreshView = (HomePullToRefreshView) viewGroup2.findViewById(R.id.pull_to_refresh_view);
            this.k = homePullToRefreshView;
            l0 l0Var = new l0();
            this.l = l0Var;
            homePullToRefreshView.a(l0Var);
            this.V0 = I3();
            this.C = this.x.findViewById(R.id.fl_actionbar_bg_foreground);
            this.j = new com.sankuai.waimai.business.page.home.locate.c((ViewStub) this.x.findViewById(R.id.address_suggestion_container_vs), this, this.d0);
            this.y0.r0.f = (ViewStub) this.x.findViewById(R.id.anchor_history_position_vs);
            Object[] objArr3 = new Object[0];
            ChangeQuickRedirect changeQuickRedirect4 = changeQuickRedirect;
            if (PatchProxy.isSupport(objArr3, this, changeQuickRedirect4, 14646081)) {
                PatchProxy.accessDispatch(objArr3, this, changeQuickRedirect4, 14646081);
            } else {
                RecyclerView.g adapter = this.h.getAdapter();
                if (adapter instanceof com.sankuai.waimai.business.page.home.basal.c) {
                    com.sankuai.waimai.business.page.home.basal.c cVar2 = (com.sankuai.waimai.business.page.home.basal.c) adapter;
                    this.u = new com.sankuai.waimai.business.page.home.q(this);
                    int iJ = com.sankuai.waimai.platform.capacity.immersed.a.c(j3()) ? com.sankuai.waimai.foundation.utils.h.j(j3()) : 0;
                    int i2 = this.t.i();
                    com.sankuai.waimai.business.page.home.q qVar = this.u;
                    int i3 = iJ + i2;
                    qVar.k = i3;
                    this.s.b0 = qVar;
                    qVar.k(this.h);
                    this.u.j = new com.sankuai.waimai.business.page.home.r(this, cVar2);
                    if (!com.sankuai.waimai.business.page.common.abtest.a.K() && (fVar = this.v) != null) {
                        fVar.n = this.u;
                    }
                    com.sankuai.waimai.pouch.viewmodel.a aVar = this.t0;
                    if (aVar != null) {
                        aVar.k = i3;
                        if (j3() != null && j3().getResources() != null) {
                            this.t0.l = j3().getResources().getDimensionPixelSize(R.dimen.wm_page_home_future_filter_bar_layout_height);
                        }
                    }
                }
            }
            this.X.p();
            PreloadDataModel.get().mTabsResponse.a(new com.sankuai.waimai.business.page.home.o(this));
            HomeGrayManager.e().b(this.C, 4, 1, "", 0);
            HomeGrayManager.e().b(this.y, 2, 5, "", 0);
            HomeGrayManager.e().b(viewGroup2.findViewById(R.id.remind_container), 2, 5, "", 0);
            this.J0 = new com.sankuai.waimai.business.page.home.list.future.refresh.l(this, this.X, this.m);
            Z3(this.m0);
            if (!com.sankuai.waimai.business.page.common.abtest.a.K()) {
                com.sankuai.waimai.business.page.home.layer.f fVar2 = new com.sankuai.waimai.business.page.home.layer.f(this, this.s);
                this.v = fVar2;
                fVar2.c(viewGroup2);
                com.sankuai.waimai.business.page.home.q qVar2 = this.u;
                if (qVar2 != null) {
                    this.v.n = qVar2;
                }
            }
            this.m.i = new j0();
            this.m.k = new i0();
            this.x.getViewTreeObserver().addOnGlobalLayoutListener(this.P0);
            this.x.getViewTreeObserver().addOnGlobalLayoutListener(new com.sankuai.waimai.business.page.home.p(this));
            com.sankuai.waimai.business.page.home.head.promotion.c cVar3 = new com.sankuai.waimai.business.page.home.head.promotion.c(this);
            this.w = cVar3;
            cVar3.I1(this.d0);
            this.w.j1(viewGroup2);
            this.w.Y = viewGroup2.findViewById(R.id.student_promotion_layer);
        }
        this.m.j = new g0();
        this.m.l = new n0();
        HomeGrayManager.e().b(viewGroup2, 2, 0, "", 0);
        this.n.e();
        this.M = new k();
        HomePageListStrategyHelper.c().a(this.M);
        this.T = new l();
        com.sankuai.waimai.business.page.homepage.bubble.g.h().a(this.T);
        Object[] objArr4 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect5 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr4, this, changeQuickRedirect5, 16468274)) {
            PatchProxy.accessDispatch(objArr4, this, changeQuickRedirect5, 16468274);
        } else {
            City cityR = com.sankuai.waimai.foundation.location.v2.p.l().r();
            this.c0 = cityR == null ? "" : cityR.getCityCode();
        }
        if (!com.sankuai.waimai.business.page.common.abtest.a.A0() && !com.sankuai.waimai.business.page.common.abtest.a.K()) {
            w4();
        }
        FuTiaoMatrixView fuTiaoMatrixView = (FuTiaoMatrixView) viewGroup2.findViewById(R.id.matrix_mach_container);
        fuTiaoMatrixView.setIsHomePage(true);
        this.a0 = new com.sankuai.waimai.business.page.home.factory.a(fuTiaoMatrixView);
        this.b0 = new com.sankuai.waimai.business.page.home.layer.i(this, viewGroup2, new m());
        G4();
        Object[] objArr5 = new Object[0];
        ChangeQuickRedirect changeQuickRedirect6 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr5, this, changeQuickRedirect6, 4242479)) {
            PatchProxy.accessDispatch(objArr5, this, changeQuickRedirect6, 4242479);
        } else if (getActivity() != null) {
            IntentFilter intentFilter = new IntentFilter("wm_student_authentication");
            this.v0 = new com.sankuai.waimai.business.page.home.l(this);
            getActivity().registerReceiver(this.v0, intentFilter);
            IntentFilter intentFilter2 = new IntentFilter("WMRefreshFirstPageWindowData");
            this.w0 = new com.sankuai.waimai.business.page.home.m();
            getActivity().registerReceiver(this.w0, intentFilter2);
        }
        return viewGroup2;
    }
