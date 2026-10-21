# -*- coding: utf-8 -*-
"""
从美团外卖官方最新版 APK (com.sankuai.meituan.takeoutnew v8.84.2) 与 REA 证据链中
提取完整逆向证据、反编译核心源码、Manifest XML、APK 内置 JSON 契约配置及官方原生 PNG/WebP 视觉资产。
"""

import hashlib
import io
import json
import os
import shutil
import zipfile
from pathlib import Path
from PIL import Image

PROJECT_ROOT = Path(r"C:\Users\a380w\Documents\Codex\2026-10-15\github\work\meituan-waimai-windows")
STEPS_DIR = Path(r"C:\Users\a380w\.gemini\antigravity\brain\88979bbf-2491-4dd5-ae47-6f04f8e82a88\.system_generated\steps")
APK_PATH = PROJECT_ROOT / "package" / "meituan-waimai-latest.apk"

RESEARCH_DIR = PROJECT_ROOT / "research"
CONTRACTS_DIR = RESEARCH_DIR / "contracts"
DECOMPILED_DIR = RESEARCH_DIR / "decompiled"
APK_ASSETS_DIR = RESEARCH_DIR / "apk-assets"
ASSETS_DIR = PROJECT_ROOT / "ui-layout" / "assets"
DELIVERABLES_DIR = PROJECT_ROOT / "deliverables"


def ensure_dirs() -> None:
    for d in [RESEARCH_DIR, CONTRACTS_DIR, DECOMPILED_DIR, APK_ASSETS_DIR, ASSETS_DIR, DELIVERABLES_DIR]:
        d.mkdir(parents=True, exist_ok=True)


def load_step_json(step_num: int) -> dict:
    p = STEPS_DIR / str(step_num) / "output.txt"
    with p.open("r", encoding="utf-8") as f:
        return json.load(f)


def reconstruct_manifest() -> str:
    """合并 steps 129, 140, 141 中的完整 AndroidManifest.xml 文本。"""
    parts = []
    for step_num in [129, 140, 141]:
        data = load_step_json(step_num)
        text = data["normalized_result"]["item"]["text"]
        parts.append(text)
    full_xml = "".join(parts)
    out_path = RESEARCH_DIR / "AndroidManifest.decoded.xml"
    out_path.write_text(full_xml, encoding="utf-8")
    return full_xml


def archive_rea_contracts() -> dict:
    """整理 REA 逆向出的核心类契约与方法反编译源码。"""
    pkg_summary = load_step_json(61)

    pkg_out = {
        "evidence_id": pkg_summary.get("evidence_id"),
        "complete_evidence_id": (pkg_summary.get("raw_result") or {}).get(
            "complete_evidence_id",
            "ev_a7b3ea6c1c77ae38e76306d98031dbea6b724e20f686da1517ff7312dfd54880",
        ),
        "subject": pkg_summary.get("subject"),
        "provider": pkg_summary.get("provider"),
        "apk_metrics": {
            "package_name": "com.sankuai.meituan.takeoutnew",
            "version_name": "8.84.2",
            "version_code": 800840002,
            "min_sdk": 21,
            "target_sdk": 30,
            "compile_sdk": 34,
            "class_count": 42257,
            "resource_count": 6231,
            "manifest_text_code_units": 147301,
            "artifact_graph_nodes": 6200,
            "artifact_graph_occurrences": 6232,
            "artifact_graph_edges": 6231,
            "integrity_contradictions": 0,
        },
        "security_guards_observed": {
            "mtguard_appkey": "e68c4c18-35ec-4972-84e4-32a0836455a8",
            "mtguard_pic": "ms_com.sankuai.meituan.takeoutnew",
            "mtguard_sec": "ppd_com.sankuai.meituan.takeoutnew.xbt",
            "robust_hotfix": "com.meituan.robust.ChangeQuickRedirect",
            "meituan_map_sdk": "49058660-1de4-4c13-a401-a7f237e85065",
            "kitefly_horn_token": "57060f364cd38b606e0522eb",
            "build_number": "91293",
        },
    }
    (PROJECT_ROOT / "package-analysis.json").write_text(
        json.dumps(pkg_out, ensure_ascii=False, indent=2), encoding="utf-8"
    )

    class_steps = {
        "MainActivity": 148,
        "WelcomeActivity": 149,
        "WMHomePageFragment": 197,
        "HomePageFragment": 227,
        "ShopCartPrice": 198,
        "ShopCartItem": 199,
        "ShopCartApiService": 200,
        "OrderListResponse": 201,
        "MineMPFragment": 202,
        "Order": 213,
        "GoodsSpu": 214,
        "Poi": 215,
    }
    contracts_index = {}
    for name, step_num in class_steps.items():
        data = load_step_json(step_num)
        norm = data.get("normalized_result", {})
        contract_record = {
            "evidence_id": data.get("evidence_id"),
            "class_name": norm.get("class_name"),
            "field_count": len(norm.get("fields", [])),
            "method_count": len(norm.get("methods", [])),
            "inner_class_count": len(norm.get("inner_classes", [])),
            "fields": norm.get("fields", []),
            "methods": norm.get("methods", []),
            "inner_classes": norm.get("inner_classes", []),
        }
        (CONTRACTS_DIR / f"{name}.contract.json").write_text(
            json.dumps(contract_record, ensure_ascii=False, indent=2), encoding="utf-8"
        )
        contracts_index[name] = {
            "evidence_id": data.get("evidence_id"),
            "class_name": norm.get("class_name"),
            "field_count": len(norm.get("fields", [])),
            "method_count": len(norm.get("methods", [])),
        }

    method_steps = {
        "MainActivity_onCreate.java": 157,
        "MainActivity_v0_DynamicTabs.java": 158,
        "MainActivity_O4_Lifecycle.java": 159,
        "MineMPFragment_o3.java": 211,
        "OrderListResponse_fromJson.java": 212,
        "WMHomePageFragment_onCreateView.java": 217,
        "HomePageFragment_onCreateView.java": 233,
    }
    decompiled_index = {}
    for filename, step_num in method_steps.items():
        data = load_step_json(step_num)
        norm = data.get("normalized_result", {})
        source_text = norm.get("source", {}).get("text", "")
        header = (
            f"// REA Evidence ID: {data.get('evidence_id')}\n"
            f"// Subject SHA-256: {data.get('subject', {}).get('digest', {}).get('sha256')}\n"
            f"// Class: {norm.get('class_name')}\n"
            f"// Method: {norm.get('method', {}).get('reported_signature')}\n\n"
        )
        (DECOMPILED_DIR / filename).write_text(header + source_text + "\n", encoding="utf-8")
        decompiled_index[filename] = {
            "evidence_id": data.get("evidence_id"),
            "class_name": norm.get("class_name"),
            "method": norm.get("method", {}).get("reported_signature"),
        }

    bundle_path = PROJECT_ROOT / "rea-evidence-bundle.json"
    bundle_meta = {}
    if bundle_path.exists():
        h = hashlib.sha256()
        with bundle_path.open("rb") as bf:
            while chunk := bf.read(1024 * 1024):
                h.update(chunk)
        bundle_meta = {
            "bundle_file": "rea-evidence-bundle.json",
            "size_bytes": bundle_path.stat().st_size,
            "sha256": h.hexdigest(),
            "record_count": 83,
            "unknowns": 0,
        }

    artifact_summary = {
        "root_artifact": {
            "name": "meituan-waimai-latest.apk",
            "package_name": "com.sankuai.meituan.takeoutnew",
            "version_name": "8.84.2",
            "version_code": 800840002,
            "sha256": "0f50d0a4d65d7d047ad33d80e7de836352f980aabcad4e069b0bb9d70638cc82",
            "manifest_id": "agm_8b3711f4dc7a2ff01d24db57bbee82a0ae4444b87930e98e08c86e10aa7f7db9",
            "root_artifact_id": "art_36308ca9208cb5f0386f7733da515ff5cf4178f0789e859a6dd0886a82845a71",
            "graph_sha256": "74305f73952acc99d6c34809988a5dee4f6a7e58a4787cf87545b740eba0a383",
        },
        "rea_evidence_bundle": bundle_meta,
        "contracts_archived": contracts_index,
        "decompiled_methods_archived": decompiled_index,
    }
    (PROJECT_ROOT / "artifact-summary.json").write_text(
        json.dumps(artifact_summary, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    return artifact_summary


def extract_apk_configs_and_visual_assets() -> dict:
    """从 APK 中提取内置 JSON 配置以及代表性的官方 WebP/PNG 图标资源（统一转为 PNG 供 WPF 使用）。"""
    extracted_images = []
    extracted_configs = []

    config_files_to_extract = [
        "assets/future_king_kong.json",
        "assets/easygo.json",
        "assets/iconfonts/iconfont_name.json",
        "assets/mrn/mrn_arch_preset_config.json",
        "assets/mrn/mrn_url_rewrite_config.json",
        "assets/mrnbundle/mrn_base.json",
        "assets/mrnbundle/mrn_business.json",
        "assets/mrnbundle/mrn_container_plugin.json",
        "assets/mrnbundle/mrn_min_bundle_versions.json",
        "assets/msc/msc_min_versions.json",
        "assets/privacy_preset_config.json",
        "assets/hybrid_cashier_preset_config.json",
        "assets/i18n_default_config.json",
        "assets/support_locale.json",
    ]

    with zipfile.ZipFile(APK_PATH, "r") as zf:
        all_entries = zf.namelist()

        for cfg in config_files_to_extract:
            if cfg in all_entries:
                out_cfg = APK_ASSETS_DIR / os.path.basename(cfg)
                out_cfg.write_bytes(zf.read(cfg))
                extracted_configs.append(cfg)

        img_entries = [
            e for e in all_entries
            if (e.startswith("r/") or e.startswith("assets/"))
            and e.endswith((".webp", ".png"))
            and not e.endswith(".9.png")
        ]

        res_inventory = {
            "total_zip_entries": len(all_entries),
            "total_image_entries": len(img_entries),
            "dex_files": [e for e in all_entries if e.endswith(".dex")],
            "native_libs": [e for e in all_entries if e.startswith("lib/") and e.endswith(".so")],
            "assets_configs_extracted": extracted_configs,
        }
        (RESEARCH_DIR / "apk-resource-inventory.json").write_text(
            json.dumps(res_inventory, ensure_ascii=False, indent=2), encoding="utf-8"
        )

        # 扫描图片尺寸与主色调，挑选美团黄应用图标、底部导航图标、袋鼠形象与高质量 UI 贴图
        candidates = []
        for entry in img_entries:
            info = zf.getinfo(entry)
            if info.file_size < 800 or info.file_size > 180000:
                continue
            try:
                raw = zf.read(entry)
                with Image.open(io.BytesIO(raw)) as im:
                    w, h = im.size
                    if w < 36 or h < 36:
                        continue
                    # 计算中心区域平均 RGB 以识别美团品牌黄 (#FFD100 / #FFC300) 图标
                    rgba = im.convert("RGBA")
                    small = rgba.resize((16, 16))
                    pixels = list(small.getdata())
                    non_trans = [p for p in pixels if p[3] > 128]
                    if not non_trans:
                        continue
                    avg_r = sum(p[0] for p in non_trans) / len(non_trans)
                    avg_g = sum(p[1] for p in non_trans) / len(non_trans)
                    avg_b = sum(p[2] for p in non_trans) / len(non_trans)
                    is_yellowish = (avg_r > 200 and avg_g > 150 and avg_b < 100)
                    is_square = abs(w - h) <= 4
                    candidates.append({
                        "entry": entry,
                        "width": w,
                        "height": h,
                        "size_bytes": info.file_size,
                        "is_square": is_square,
                        "is_yellowish": is_yellowish,
                        "avg_rgb": [round(avg_r, 1), round(avg_g, 1), round(avg_b, 1)],
                    })
            except Exception:
                continue

        # 优先选择正方形黄色品牌图标（如 launcher 图标、袋鼠头像、Tab 图标）及大尺寸高清 UI 资产
        candidates.sort(
            key=lambda c: (
                1 if (c["is_square"] and c["is_yellowish"] and 96 <= c["width"] <= 256) else 0,
                1 if c["is_yellowish"] else 0,
                1 if c["is_square"] else 0,
                c["size_bytes"],
            ),
            reverse=True,
        )

        launcher_saved = False
        for idx, cand in enumerate(candidates[:50]):
            entry = cand["entry"]
            raw = zf.read(entry)
            stem = Path(entry).stem
            out_name = f"apk_{idx:02d}_{stem}_{cand['width']}x{cand['height']}.png"
            out_file = ASSETS_DIR / out_name
            with Image.open(io.BytesIO(raw)) as im:
                im.convert("RGBA").save(out_file, format="PNG")
                if not launcher_saved and cand["is_square"] and cand["is_yellowish"] and cand["width"] >= 96:
                    im.convert("RGBA").save(ASSETS_DIR / "wm_ic_launcher.png", format="PNG")
                    launcher_saved = True
            extracted_images.append({
                "apk_entry": entry,
                "local_file": f"ui-layout/assets/{out_name}",
                "width": cand["width"],
                "height": cand["height"],
                "source_size_bytes": cand["size_bytes"],
                "is_yellowish": cand["is_yellowish"],
                "avg_rgb": cand["avg_rgb"],
            })

    (RESEARCH_DIR / "extracted-visual-assets.json").write_text(
        json.dumps({"count": len(extracted_images), "assets": extracted_images}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    return {
        "extracted_images_count": len(extracted_images),
        "extracted_configs_count": len(extracted_configs),
        "launcher_saved": launcher_saved,
    }


if __name__ == "__main__":
    ensure_dirs()
    xml_text = reconstruct_manifest()
    summary = archive_rea_contracts()
    assets_info = extract_apk_configs_and_visual_assets()
    print(json.dumps({
        "manifest_chars": len(xml_text),
        "contracts_count": len(summary["contracts_archived"]),
        "decompiled_count": len(summary["decompiled_methods_archived"]),
        **assets_info,
    }, ensure_ascii=False))
