// REA Evidence ID: ev_4529da019a3a6438fd6d90b50b3906319ed532bdb6d6ab2fd3bcc7f21779b85c
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.homepage.MainActivity
// Method: O4(int, java.lang.Class<T extends androidx.fragment.app.Fragment>): T extends androidx.fragment.app.Fragment

    @Nullable
    public final <T extends Fragment> T O4(int i2, Class<T> cls) {
        TabInfo tabInfoC;
        Object[] objArr = {new Integer(i2), cls};
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 15861473)) {
            return (T) PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 15861473);
        }
        BottomTabView bottomTabView = this.Y;
        if (bottomTabView == null || (tabInfoC = bottomTabView.c(i2)) == null || !cls.isInstance(tabInfoC.f)) {
            return null;
        }
        return (T) tabInfoC.f;
    }
