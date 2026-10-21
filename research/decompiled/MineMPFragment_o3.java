// REA Evidence ID: ev_1efe4797dec6daa8e1939e1e190961e6817d0e07071ef8dc8291841947ed3d96
// Subject SHA-256: 0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82
// Class: com.sankuai.waimai.business.page.mine.MineMPFragment
// Method: o3(): com.sankuai.waimai.machpro.base.MachMap

    @Override // com.sankuai.waimai.machpro.container.MPBaseFragment
    public final MachMap o3() {
        Object[] objArr = new Object[0];
        ChangeQuickRedirect changeQuickRedirect2 = changeQuickRedirect;
        if (PatchProxy.isSupport(objArr, this, changeQuickRedirect2, 11369532)) {
            return (MachMap) PatchProxy.accessDispatch(objArr, this, changeQuickRedirect2, 11369532);
        }
        MachMap machMap = new MachMap();
        boolean z = getContext() instanceof MainActivity;
        return machMap;
    }
