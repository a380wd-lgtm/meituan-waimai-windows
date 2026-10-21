# -*- coding: utf-8 -*-
"""
从美团外卖官网、腾讯应用宝、豌豆荚等官方渠道探测并下载最新版美团外卖官方正版 APK
目标包名：com.sankuai.meituan.takeoutnew
"""
import hashlib
import json
import os
import re
import sys
import urllib.request

HEADERS = {
    "User-Agent": "Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Mobile Safari/537.36"
}
PC_HEADERS = {
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
}

def probe_sources():
    candidates = []

    # 1. 美团外卖官方网站下载入口探测
    meituan_urls = [
        "https://waimai.meituan.com/mobile/download/default",
        "https://i.meituan.com/awp/hfe/block/a945391288b790d558b7/78716/index.html",
        "https://waimai.meituan.com",
    ]
    for u in meituan_urls:
        try:
            req = urllib.request.Request(u, headers=HEADERS)
            with urllib.request.urlopen(req, timeout=15) as r:
                final_u = r.geturl()
                ct = r.headers.get("Content-Type", "")
                print(f"[Meituan Probe] {u} -> {final_u} ({ct})")
                if ".apk" in final_u.lower() or "application/vnd.android" in ct.lower() or "octet-stream" in ct.lower():
                    candidates.append(("meituan_official", final_u))
                else:
                    body = r.read(200_000).decode("utf-8", "ignore")
                    apks = re.findall(r'https?://[^"\'\s<>]+\.apk[^"\'\s<>]*', body)
                    for a in apks:
                        print(f"  [Meituan HTML APK] {a}")
                        candidates.append(("meituan_official_html", a))
        except Exception as e:
            print(f"[Meituan Probe Err] {u}: {e}")

    # 2. 豌豆荚包名详情页解析
    try:
        wdj_page = "https://www.wandoujia.com/apps/com.sankuai.meituan.takeoutnew"
        req = urllib.request.Request(wdj_page, headers=PC_HEADERS)
        with urllib.request.urlopen(req, timeout=15) as r:
            html = r.read().decode("utf-8", "ignore")
            print(f"[WDJ Page] status={r.status}, len={len(html)}")
            dl_links = re.findall(r'href="(https?://[^"]*wandoujia\.com/apps/[^"]*/download/[^"]+)"', html)
            for d in dl_links:
                print(f"  [WDJ DL] {d}")
                candidates.append(("wandoujia", d))
            if "/apps/com.sankuai.meituan.takeoutnew" in r.geturl():
                candidates.append(("wandoujia_direct", "https://www.wandoujia.com/apps/com.sankuai.meituan.takeoutnew/download/dot?ch=detail_normal_dl"))
    except Exception as e:
        print(f"[WDJ Err] {e}")

    # 3. 腾讯应用宝 HTML 全文扫描
    try:
        yyb_page = "https://sj.qq.com/appdetail/com.sankuai.meituan.takeoutnew"
        req = urllib.request.Request(yyb_page, headers=PC_HEADERS)
        with urllib.request.urlopen(req, timeout=15) as r:
            html = r.read().decode("utf-8", "ignore")
            print(f"[YYB Page] len={len(html)}")
            urls = re.findall(r'https?:\\?/\\?/[^"\'\s<>]+?(?:imtt\.dd\.qq\.com|\.apk)[^"\'\s<>]*', html)
            for u in urls:
                u_clean = u.replace("\\u002F", "/").replace("\\/", "/")
                print(f"  [YYB APK] {u_clean}")
                candidates.append(("yyb", u_clean))
    except Exception as e:
        print(f"[YYB Err] {e}")

    return candidates

def download_apk(url, dest_path):
    print(f"Downloading from {url} -> {dest_path} ...")
    req = urllib.request.Request(url, headers=HEADERS)
    with urllib.request.urlopen(req, timeout=120) as resp:
        final_url = resp.geturl()
        content_type = resp.headers.get("Content-Type", "")
        content_len = resp.headers.get("Content-Length", "0")
        print(f"Final URL: {final_url}")
        print(f"Content-Type: {content_type}, Content-Length: {content_len}")
        sha256 = hashlib.sha256()
        total = 0
        with open(dest_path, "wb") as f:
            while True:
                chunk = resp.read(1024 * 256)
                if not chunk:
                    break
                f.write(chunk)
                sha256.update(chunk)
                total += len(chunk)
        with open(dest_path, "rb") as f:
            magic = f.read(4)
        if magic != b"PK\x03\x04" or total < 5_000_000:
            print(f"Invalid APK file (magic={magic}, size={total})")
            return None
        digest = sha256.hexdigest()
        print(f"Downloaded valid APK! Size={total} bytes, SHA256={digest}")
        return {
            "source_url": url,
            "final_url": final_url,
            "size_bytes": total,
            "sha256": digest,
            "path": dest_path
        }

if __name__ == "__main__":
    out_dir = r"C:\Users\a380w\Documents\Codex\2026-10-15\github\work\meituan-waimai-windows\package"
    os.makedirs(out_dir, exist_ok=True)
    dest = os.path.join(out_dir, "meituan-waimai-latest.apk")
    cands = probe_sources()
    seen = set()
    for src, url in cands:
        if url in seen:
            continue
        seen.add(url)
        try:
            res = download_apk(url, dest)
            if res:
                res["provider"] = src
                meta_path = os.path.join(out_dir, "apk-download-metadata.json")
                with open(meta_path, "w", encoding="utf-8") as mf:
                    json.dump(res, mf, ensure_ascii=False, indent=2)
                print("Saved metadata to", meta_path)
                sys.exit(0)
        except Exception as e:
            print(f"Download failed for {src} ({url}): {e}")
    sys.exit(1)
