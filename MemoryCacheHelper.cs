using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CacheDemo
{
    /// <summary>
    /// 缓存项数据
    /// </summary>
    public class CacheItemData
    {
        /// <summary>
        /// 缓存数据键
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// 缓存数据值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 缓存数据创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 缓存数据过期时间(秒)
        /// </summary>
        public int ExpiredSeconds { get; set; }
    }
    /// <summary>
    /// 缓存仓库接口
    /// </summary>
    public interface ICacheStore
    {
        /// <summary>
        /// 保存缓存内容
        /// </summary>
        void Save(List<CacheItemData> cacheItemDatas);
        /// <summary>
        /// 从仓库里取出缓存内容并
        /// </summary>
        /// <returns></returns>
        List<CacheItemData> Load();
    }
    /// <summary>
    /// 文件缓存仓库
    /// </summary>
    public class CacheFileStore:ICacheStore
    {
        /// <summary>
        /// 缓存文件路径
        /// </summary>
        private string _CacheFilePath = "";
        /// <summary>
        /// 无参数初始化
        /// </summary>
        public CacheFileStore()
        {
            var cachePath = System.IO.Path.GetDirectoryName(Application.ExecutablePath) + "\\caches\\";
            var cacheFile = cachePath + "cachesData.data";
            _CacheFilePath = cacheFile;
        }
        /// <summary>
        /// 有参数初始化
        /// </summary>
        /// <returns></returns>
        public CacheFileStore(string CacheFilePath)
        {
            _CacheFilePath = CacheFilePath;
        }
        /// <summary>
        ///  从仓库里取出缓存内容
        /// </summary>
        public List<CacheItemData> Load()
        {
            using (System.IO.FileStream fs = new FileStream(_CacheFilePath, FileMode.OpenOrCreate))
            {
                byte[] buffer = new byte[fs.Length];
                fs.Read(buffer, 0, buffer.Length);
                string jsonData = System.Text.Encoding.Default.GetString(buffer);
                
                return Newtonsoft.Json.JsonConvert.DeserializeObject<List<CacheItemData>>(jsonData);               
            }
        }
        /// <summary>
        /// 保存缓存内容
        /// </summary>
        /// <param name="cacheItemDatas"></param>
        public void Save(List<CacheItemData> cacheItemDatas)
        {
            IsoDateTimeConverter timeConverter = new IsoDateTimeConverter();
            //这里使用自定义日期格式，如果不使用的话，默认是ISO8601格式     
            timeConverter.DateTimeFormat = "yyyy-MM-dd HH:mm:ss";
            string jsonData = Newtonsoft.Json.JsonConvert.SerializeObject(cacheItemDatas, Formatting.Indented, timeConverter);
            using (System.IO.FileStream fs = new FileStream(_CacheFilePath, FileMode.Truncate))
            {
                byte[] buffer = System.Text.Encoding.Default.GetBytes(jsonData);
                fs.SetLength(0);
                fs.Write(buffer, 0, buffer.Length);
            }
        }
    }
    /// <summary>
    /// 缓存工具类
    /// </summary>
    public class MemoryCacheHelper
    {
        /// <summary>
        /// 缓存对象
        /// </summary>
        private MemoryCache _Cache;
        /// <summary>
        /// 缓存持久化仓库
        /// </summary>
        private ICacheStore _CacheStore;
        /// <summary>
        /// 缓存过期时间
        /// </summary>
        private int _ExpireSeconds;
        /// <summary>
        /// 获取缓存策略
        /// </summary>
        /// <returns></returns>
        private CacheItemPolicy GetPolicy()
        {
            CacheItemPolicy policy = new CacheItemPolicy();
            policy.RemovedCallback += CacheEntryRemovedCallback;
            policy.SlidingExpiration = new TimeSpan(0, 0, _ExpireSeconds);
            return policy;
        }
        /// <summary>
        /// 保存当前缓存数据
        /// </summary>
        private void SaveStore()
        {
            var cachingDatas = GetCachingData();
            _CacheStore.Save(cachingDatas);
        }
        /// <summary>
        /// 用已有数据初始化缓存数据
        /// </summary>
        /// <param name="cacheItemDatas"></param>
        private void InitCacheData(List<CacheItemData> cacheItemDatas)
        {
            if (cacheItemDatas == null) return;
            foreach (var item in cacheItemDatas)
            {
                if (item.CreateTime.AddSeconds(item.ExpiredSeconds) < DateTime.Now)
                {
                    CacheItem cacheItem = new CacheItem(item.Key, item.Value);
                    _Cache.Set(cacheItem, GetPolicy());
                }
            }
        }
        /// <summary>
        /// 缓存数据过期事件
        /// </summary>
        /// <param name="arguments"></param>
        private void CacheEntryRemovedCallback(CacheEntryRemovedArguments arguments)
        {
            SaveStore();
        }
        /// <summary>
        /// 初始化缓存助手类
        /// </summary>
        /// <param name="cacheStore">缓存持久化仓库</param>
        /// <param name="ExpireSeconds"></param>
        public MemoryCacheHelper(ICacheStore cacheStore, int ExpireSeconds)
        {
            _Cache = MemoryCache.Default;
            _CacheStore = cacheStore;
            _ExpireSeconds = ExpireSeconds;
            var cacheDatas = cacheStore.Load();
            InitCacheData(cacheDatas);
        }
        /// <summary>
        /// 获取当前所有的在缓存中的数据
        /// </summary>
        /// <returns></returns>
        public List<CacheItemData> GetCachingData()
        {
            List<CacheItemData> result = new List<CacheItemData>();
            foreach (var item in _Cache)
            {
                var cacheItem = _Cache.GetCacheItem(item.Key);
                result.Add(
                    new CacheItemData() {
                        Key = item.Key,
                        Value = item.Value.ToString(),
                        ExpiredSeconds = _ExpireSeconds,
                        CreateTime = DateTime.Now
                    });
            }
            return result;
        }
        /// <summary>
        /// 设置缓存数据
        /// </summary>
        /// <param name="Key"></param>
        /// <param name="Value"></param>
        public void SetData(string Key, object Value)
        {
            CacheItem cacheItem = new CacheItem(Key, Value);
            _Cache.Set(cacheItem, GetPolicy());
            SaveStore();
        }
        /// <summary>
        /// 获取缓存数据
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public object GetData(string Key)
        {
            return _Cache[Key];
        }       
       
    }
}
