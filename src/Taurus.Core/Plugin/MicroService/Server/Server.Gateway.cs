﻿using CYQ.Data.Tool;
using System;
using System.Collections.Generic;
using Taurus.Mvc;

namespace Taurus.Plugin.MicroService
{
    /// <summary>
    /// 网关或注册中心端编码
    /// </summary>
    internal partial class Server
    {
        /// <summary>
        /// 服务端网关 -（从注册中心 与 网关 使用）
        /// </summary>
        public partial class Gateway
        {
            public static MDictionary<string, List<HostInfo>> _HostList;
            /// <summary>
            /// 作为微服务主程序时，存档的微服务列表【和注册中心列表各自独立】
            /// </summary>
            public static MDictionary<string, List<HostInfo>> HostList
            {
                get { return _HostList; }
                set
                {
                    _HostList = value;
                    kvHosts.Clear();//清空缓存。
                }
            }
            /// <summary>
            /// 获取模块所在的对应主机网址【若存在多个：每次获取都会循环下一个】。
            /// </summary>
            /// <param name="name">服务模块名称</param>
            /// <returns></returns>
            public static string GetHost(string name)
            {
                List<HostInfo> infoList = GetHostList(name);//微服务程序。
                if (infoList != null && infoList.Count > 0)
                {
                    // bool isRegCenter = MsConfig.IsRegCenterOfMaster;
                    //分拆出网关：网关列表，不具备注册时间（即不更新注册时间），所以取消该时间判断。
                    HostInfo firstInfo = infoList[0];
                    for (int i = 0; i < infoList.Count; i++)
                    {
                        int callIndex = firstInfo.CallIndex + i;
                        if (callIndex >= infoList.Count)
                        {
                            callIndex = 0;
                        }
                        HostInfo info = infoList[callIndex];

                        if (info.Version < 0 || (info.CallTime > DateTime.Now && infoList.Count > 0))//正常5-10秒注册1次: || (isRegCenter && info.RegTime < DateTime.Now.AddSeconds(-10))
                        {
                            continue;//已经断开服务的。
                        }
                        firstInfo.CallIndex = callIndex + 1;//指向下一个。
                        return infoList[callIndex].Host;
                    }
                }
                return string.Empty;
            }
            /// <summary>
            /// 获取模块的所有Host列表。
            /// </summary>
            /// <param name="name">服务模块名称</param>
            /// <returns></returns>
            public static List<HostInfo> GetHostList(string name)
            {
                return GetHostList(name, true);
            }
            /// <summary>
            /// 获取模块的所有Host列表。
            /// </summary>
            /// <param name="name">服务模块名称</param>
            /// <param name="withStar">是否包含星号通配符（默认true）</param>
            /// <returns></returns>
            public static List<HostInfo> GetHostList(string name, bool withStar)
            {
                //System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                //sw.Start();
                var hostList = HostList;//先获取引用【避免执行过程，因线程更换了引用的对象】
                if (hostList != null)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        name = "/";
                    }
                    List<HostInfo> list = new List<HostInfo>();
                    if (hostList.ContainsKey(name))//微服务程序。
                    {
                        list.AddRange(hostList[name]);
                    }
                    if (withStar)
                    {
                        if (name == "localhost" || name.Contains("."))//域名
                        {
                            if (list.Count == 0 && name.Split('.').Length > 2)//2级泛域名检测
                            {
                                string seName = "*" + name.Substring(name.IndexOf("."));
                                if (hostList.ContainsKey(seName))
                                {
                                    list.AddRange(hostList[seName]);
                                }
                            }
                            if (name != "*.*" && hostList.ContainsKey("*.*"))
                            {
                                List<HostInfo> commList = hostList["*.*"];
                                if (commList.Count > 0)
                                {
                                    if (list.Count == 0 || commList[0].Version >= list[0].Version)//版本号比较处理
                                    {
                                        list.AddRange(commList);//增加“*.*”模块的通用符号处理。
                                    }
                                }
                            }
                        }
                        else //普通模块
                        {
                            switch (name)
                            {
                                case "*":
                                case "RegCenter":
                                case "RegCenterOfSlave":
                                case "Gateway":
                                    return list;
                            }
                            if (hostList.ContainsKey("*"))
                            {
                                List<HostInfo> commList = hostList["*"];
                                if (commList.Count > 0)
                                {
                                    if (list.Count == 0 || commList[0].Version >= list[0].Version)//版本号比较处理
                                    {
                                        list.AddRange(commList);//增加“*”模块的通用符号处理。
                                    }
                                }
                            }
                        }
                    }
                    //sw.Stop();
                    //if (sw.ElapsedMilliseconds > 1000)
                    //{
                    //    Log.WriteLogToTxt("GetHostList : " + sw.ElapsedMilliseconds, "DebugMS");
                    //}
                    return list;
                }
                return null;
            }
        }
        public partial class Gateway
        {
            /// <summary>
            /// 缓存
            /// </summary>
            private static MDictionary<string, List<HostInfo>> kvHosts = new MDictionary<string, List<HostInfo>>(StringComparer.OrdinalIgnoreCase);
            /// <summary>
            /// 获取主机列表
            /// </summary>
            /// <returns></returns>
            internal static List<HostInfo> GetHostListWithCache(Uri uri, string host, string module)
            {
                string key = host + module;
                if (kvHosts.ContainsKey(key))
                {
                    return kvHosts[key];
                }
                var hostList = GetHostList(uri, host, module);
                if (hostList != null)
                {
                    kvHosts.Add(key, hostList);
                }
                return hostList;
            }

            private static List<HostInfo> GetHostList(Uri uri, string host, string module)
            {
                List<HostInfo> domainList = GetHostList(host);
                if (domainList == null || domainList.Count == 0)
                {
                    return null;
                }
                List<HostInfo> moduleList = Server.Gateway.GetHostList(module);
                if (moduleList == null || moduleList.Count == 0)
                {
                    return null;
                }
                List<HostInfo> infoList = new List<HostInfo>();
                //存在域名，也存在模块，过滤出满足：域名+模块
                foreach (var domainItem in domainList)//过滤掉不在域名下的主机
                {
                    foreach (var moduleItem in moduleList)
                    {
                        if (domainItem.Host == moduleItem.Host)
                        {
                            if (uri.AbsoluteUri.StartsWith(domainItem.Host))
                            {
                                return null;//请求自身，直接返回，避免死循环。
                            }
                            infoList.Add(moduleItem);// 用模块，模块里有包含IsVirtual属性，而域名则没有。
                            break;
                        }
                    }
                }
                if (infoList.Count == 0)
                {
                    return null;
                }
                return infoList;
            }
        }
    }
}
