// REA Evidence ID: ev_13ebf920d821533bfc3f4637c1ec73fbe46200362d214e4bf97edd86544d6f87
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.home.WMHomePageFragment
// Method: onCreateView(android.view.LayoutInflater, android.view.ViewGroup, android.os.Bundle): android.view.View

    @Override // com.sankuai.waimai.business.page.home.HomePageFragment, androidx.fragment.app.Fragment
    @Nullable
    public final View onCreateView(LayoutInflater layoutInflater, @Nullable ViewGroup viewGroup, @Nullable Bundle bundle) {
        Object[] objArr = {layoutInflater, viewGroup, bundle};
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        return PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 13508837) ? (View) PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 13508837) : super.onCreateView(layoutInflater, viewGroup, bundle);
    }
