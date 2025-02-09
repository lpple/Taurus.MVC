﻿using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using CYQ.Data;
using CYQ.Data.Cache;

namespace Taurus.Plugin.MicroService
{
    /// <summary>
    /// WebClient 池 For .NET
    /// 知识补充：
    /// 1、Net6 及以上，默认才有端口复用。
    /// 2、异步操作：都无端口利用。
    /// </summary>
    internal class RpcClientPool
    {
        static CacheManage cache = CacheManage.LocalInstance;
        static MDictionary<string, Queue<RpcClient>> rpcClientPool = new MDictionary<string, Queue<RpcClient>>();
        /// <summary>
        /// NET 6 及以上自带池。
        /// </summary>
        private static bool isNeedPool = !AppConfig.IsNetCore;//  Environment.Version.Major < 6;

        /// <summary>
        /// 是否包含指定的Uri
        /// </summary>
        /// <returns></returns>
        public static bool Contains(Uri uri)
        {
            return rpcClientPool.ContainsKey(uri.Authority);
        }

        public static RpcClient Create(Uri uri)
        {
            if (cache.Get("RpcClientPool_" + uri.Authority) != null)
            {
                return null;
            }
            if (!isNeedPool)
            {
                return new RpcClient();
            }
            RpcClient client = null;
            if (rpcClientPool.ContainsKey(uri.Authority))
            {
                Queue<RpcClient> queue = rpcClientPool[uri.Authority];
                lock (queue)
                {
                    if (queue.Count > 0)
                    {
                        client = queue.Dequeue();
                    }
                }
            }
            if (client == null)
            {
                AddToPool(uri, new RpcClient());//预存一个
                AddToPool(uri, new RpcClient());//预存一个
                AddToPool(uri, new RpcClient());//预存一个
                client = new RpcClient();
            }
            return client;
        }
        public static void AddToPool(Uri uri, RpcClient wc)
        {
            if (cache.Get("RpcClientPool_" + uri.Authority) != null)
            {
                return;
            }
            if (!isNeedPool)
            {
                return;
            }
            wc.Timeout = 0;//重置为默认超时时间。
            wc.Headers.Clear();
            if (rpcClientPool.ContainsKey(uri.Authority))
            {
                Queue<RpcClient> queue = rpcClientPool[uri.Authority];
                lock (queue)
                {
                    queue.Enqueue(wc);
                }
            }
            else
            {

                Queue<RpcClient> queue = new Queue<RpcClient>(64);
                queue.Enqueue(wc);
                rpcClientPool.Add(uri.Authority, queue);
            }
        }
        public static void RemoveFromPool(Uri uri)
        {
            cache.Set("RpcClientPool_" + uri.Authority, true, 0.15);
            rpcClientPool.Remove(uri.Authority);
        }
    }
}
