using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MeituanWaimaiClient.Services
{
    /// <summary>
    /// 美团外卖 APK 原始图标与资源高性能加载器。
    /// 严格遵循 wpf-dotnet-performance 与 exhaustive-code-audit 规范：
    /// 1. 绝不在 UI 线程执行同步磁盘 I/O（通过后台线程预热为内存字节流或冻结的 BitmapSource）。
    /// 2. 强制设置 DecodePixelWidth 硬件级缩放，防止高分辨率原图撑爆非托管内存。
    /// 3. 强制设置 BitmapCacheOption.OnLoad 并在流关闭后立即调用 Freeze()，确保跨线程安全共享与零句柄泄漏。
    /// </summary>
    public sealed class ApkAssetLoader
    {
        private readonly string _assetsDirectory;
        private readonly ConcurrentDictionary<string, BitmapSource> _frozenCache;
        private readonly ConcurrentDictionary<string, byte[]> _rawBytesCache;
        private volatile bool _isPreloaded;

        public ApkAssetLoader(string assetsDirectory)
        {
            _assetsDirectory = assetsDirectory ?? string.Empty;
            _frozenCache = new ConcurrentDictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
            _rawBytesCache = new ConcurrentDictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
            _isPreloaded = false;
        }

        /// <summary>
        /// 资源目录路径
        /// </summary>
        public string AssetsDirectory
        {
            get { return _assetsDirectory; }
        }

        /// <summary>
        /// 是否已完成后台预热
        /// </summary>
        public bool IsPreloaded
        {
            get { return _isPreloaded; }
        }

        /// <summary>
        /// 已缓存的冻结位图数量
        /// </summary>
        public int CachedImageCount
        {
            get { return _frozenCache.Count; }
        }

        /// <summary>
        /// 在后台线程池异步预加载 APK 解包出的 PNG 图标字节并构建冻结的 BitmapSource。
        /// 保证 UI 线程零同步文件 I/O。
        /// </summary>
        public Task PreloadAssetsAsync(int defaultDecodeWidth)
        {
            return Task.Run(delegate
            {
                PreloadAssetsOnBackground(defaultDecodeWidth);
            });
        }

        /// <summary>
        /// 供后台线程或启动引导阶段预加载图标资源。
        /// </summary>
        public void PreloadAssetsOnBackground(int defaultDecodeWidth)
        {
            try
            {
                if (string.IsNullOrEmpty(_assetsDirectory) || !Directory.Exists(_assetsDirectory))
                {
                    _isPreloaded = true;
                    return;
                }

                string[] pngFiles = Directory.GetFiles(_assetsDirectory, "*.png", SearchOption.TopDirectoryOnly);
                int decodeWidth = defaultDecodeWidth > 0 ? defaultDecodeWidth : 96;

                for (int i = 0; i < pngFiles.Length; i++)
                {
                    string filePath = pngFiles[i];
                    string fileName = Path.GetFileName(filePath);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        continue;
                    }

                    try
                    {
                        byte[] bytes = File.ReadAllBytes(filePath);
                        _rawBytesCache[fileName] = bytes;

                        int targetWidth = decodeWidth;
                        if (string.Equals(fileName, "wm_ic_launcher.png", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(fileName, "wm_kangaroo_mascot.png", StringComparison.OrdinalIgnoreCase))
                        {
                            targetWidth = 160;
                        }

                        BitmapSource frozenBitmap = DecodeFrozenBitmapFromBytes(bytes, targetWidth);
                        if (frozenBitmap != null)
                        {
                            _frozenCache[BuildCacheKey(fileName, targetWidth)] = frozenBitmap;
                            _frozenCache[fileName] = frozenBitmap;
                        }
                    }
                    catch
                    {
                        // 忽略单个损坏资源，保证整体稳定
                    }
                }
            }
            catch
            {
                // 忽略目录访问异常
            }
            finally
            {
                _isPreloaded = true;
            }
        }

        /// <summary>
        /// 从内存缓存获取已冻结的 BitmapSource（非阻塞、零磁盘 I/O）。
        /// </summary>
        public BitmapSource TryGetFrozenIcon(string fileName, int decodePixelWidth)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            string normalized = Path.GetFileName(fileName);
            string sizedKey = BuildCacheKey(normalized, decodePixelWidth);

            BitmapSource cached;
            if (_frozenCache.TryGetValue(sizedKey, out cached))
            {
                return cached;
            }

            if (_frozenCache.TryGetValue(normalized, out cached))
            {
                return cached;
            }

            byte[] rawBytes;
            if (_rawBytesCache.TryGetValue(normalized, out rawBytes))
            {
                BitmapSource decoded = DecodeFrozenBitmapFromBytes(rawBytes, decodePixelWidth);
                if (decoded != null)
                {
                    _frozenCache[sizedKey] = decoded;
                    return decoded;
                }
            }

            return null;
        }

        /// <summary>
        /// 使用纯内存流解码并冻结 BitmapImage，彻底杜绝文件锁与非托管内存泄漏。
        /// </summary>
        public static BitmapSource DecodeFrozenBitmapFromBytes(byte[] imageBytes, int decodePixelWidth)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes, false))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    if (decodePixelWidth > 0)
                    {
                        bitmap.DecodePixelWidth = decodePixelWidth;
                    }
                    bitmap.StreamSource = ms;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    return bitmap;
                }
            }
            catch
            {
                return null;
            }
        }

        private static string BuildCacheKey(string fileName, int decodePixelWidth)
        {
            return string.Format("{0}@{1}", fileName, decodePixelWidth > 0 ? decodePixelWidth : 0);
        }
    }
}
