using System;
using HttpUtil;
using System.IO;
using Newtonsoft.Json;
using System.Xml.Linq;
using System.Data;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace anfang
{
    public class Regions //区域信息
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class OrgList //组织机构
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class PersonList //人员信息
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class Resources //资源信息
    {
        public string resourceType { get; set; }
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class Cameras //监控点位
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class PreviewURLs //实时视频
    {
        public string cameraIndexCode { get; set; }
        public string protocol { get; set; }
    }

    public class PlaybackURLs //视频回放
    {
        public string cameraIndexCode { get; set; }
        public string protocol { get; set; }
        public string beginTime { get; set; }
        public string endTime { get; set; }
    }

    public class Doorsearch //门禁信息
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
    }

    public class Doorevents //门禁事件
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public string beginTime { get; set; }
        public string endTime { get; set; }
        public int[] eventTypes { get; set; }
        public string sort { get; set; }
        public string order { get; set; }
    }

    public class CrossRecords //过车记录
    {
        public int pageNo { get; set; }
        public int pageSize { get; set; }
        public string beginTime { get; set; }
        public string endTime { get; set; }
        public int vehicleOut { get; set; }
    }

    public class EventSubscription //事件订阅
    {
        public int[] eventTypes { get; set; }
        //public List<int> eventTypeList { get; set; }
        public string eventDest { get; set; }
    }

    public class EventUnSubscription //取消订阅
    {
        public int[] eventTypes { get; set; }
    }

    class Program
    {
        static void Main()
        {
            // 读取各个子元素的值
            string appKey = "";
            string appSecret = "";
            string ip = "";
            int socket = 0;
            bool https = false;
            string connectionString = "";
            string eventDest = "";

            DataTable dataTable;

            // 获取 XML 文件的路径
            string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inc.xml");
            Console.WriteLine($"XML文件路径: {xmlFilePath}");

            // 检查文件是否存在
            if (File.Exists(xmlFilePath))
            {
                //Console.WriteLine("XML文件存在，开始加载...");

                try
                {
                    // 加载 XML 文件
                    XDocument xdoc = XDocument.Load(xmlFilePath);

                    // 打印整个 XML 文档内容
                    //Console.WriteLine("XML 文件内容:");
                    //Console.WriteLine(xdoc);

                    // 获取根元素 <option>
                    XElement optionElement = xdoc.Element("option");
                    if (optionElement != null)
                    {
                        //Console.WriteLine("根元素 <option> 已找到，读取各个子元素的值...");

                        // 读取各个子元素的值并输出
                        appKey = optionElement.Element("APPKey")?.Value;
                        appSecret = optionElement.Element("APPSecret")?.Value;
                        ip = optionElement.Element("IP")?.Value;
                        socket = int.Parse(optionElement.Element("Socket")?.Value ?? "0");
                        https = bool.Parse(optionElement.Element("Https")?.Value ?? "false");
                        connectionString = optionElement.Element("connectionString")?.Value;
                        eventDest = optionElement.Element("eventDest")?.Value;

                        //Console.WriteLine($"APPKey: {appKey}");
                        //Console.WriteLine($"APPSecret: {appSecret}");
                        //Console.WriteLine($"IP: {ip}");
                        //Console.WriteLine($"Socket: {socket}");
                        //Console.WriteLine($"Https: {https}");
                        //Console.WriteLine($"ConnectionString: {connectionString}");
                        //Console.WriteLine($"EventDest: {eventDest}");
                    }
                    else
                    {
                        Console.WriteLine("根元素 <option> 不存在。");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("加载 XML 文件时发生错误: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine($"文件 {xmlFilePath} 不存在。");
            }

            string body = "";
            string uri = "";
            byte[] result = null;

            // 设置平台信息参数：合作方APPKey、合作方APPSecret、平台IP、平台端口、以及是否适用HTTPS协议
            HttpUtillib.SetPlatformInfo(appKey, appSecret, ip, socket, https);
            MySQLHelper mySQLHelper = new MySQLHelper(connectionString);

            while (true)
            {
                /*
                //1.获取区域信息###############################################################################
                Console.Write("1.获取区域信息列表\n");
                debug_log("1.获取区域信息列表:");
                try
                {
                    Regions regions = new Regions { pageNo = 1, pageSize = 1000 };
                    body = JsonConvert.SerializeObject(regions);
                    uri = "/artemis/api/resource/v1/regions";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //2.获取组织机构信息###############################################################################
                Console.Write("2.获取组织机构信息列表\n");
                debug_log("2.获取组织机构信息列表:");
                try
                {
                    OrgList orgList = new OrgList { pageNo = 1, pageSize = 1000 };
                    body = JsonConvert.SerializeObject(orgList);
                    uri = "/artemis/api/resource/v1/org/orgList";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //3.获取人员信息###############################################################################
                Console.Write("3.获取人员信息列表\n");
                debug_log("3.获取人员信息列表:");
                try
                {
                    PersonList personList = new PersonList { pageNo = 1, pageSize = 1000 }; //值从第1个接口获取
                    body = JsonConvert.SerializeObject(personList);
                    uri = "/artemis/api/resource/v2/person/personList";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //4.获取资源信息###############################################################################
                Console.Write("4.获取资源信息列表\n");
                debug_log("4.获取资源信息列表:");
                try
                {
                    Resources resources = new Resources { resourceType = "EncodeDevice", pageNo = 1, pageSize = 1000 }; //resourceType:编码设备EncodeDevice,监控点Camera,IO通道IoChannel,门禁设备AcsDevice,门禁点Door,报警主机IasDevice,子系统通道SubSys,防区通道Defence,可视对讲VisDevice,梯控-控制器EcsDevice,梯控-读卡器Reader,梯控-楼层Floor
                    body = JsonConvert.SerializeObject(resources);
                    uri = "/artemis/api/irds/v2/deviceResource/resources";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                */

                //5.获取监控点列表###############################################################################
                Console.Write("5.获取监控点列表\n");
                debug_log("5.获取监控点列表:");
                try
                {
                    /*
                    Cameras cameras = new Cameras { pageNo = 1, pageSize = 200 };
                    body = JsonConvert.SerializeObject(cameras);
                    uri = "/artemis/api/resource/v1/cameras";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));

                        // 解析 JSON 数据
                        JObject jsonObj = JObject.Parse(System.Text.Encoding.UTF8.GetString(result));
                        JArray list = (JArray)jsonObj["data"]["list"];

                        foreach (var item in list)
                        {
                            if (!mySQLHelper.IsExist("select id from anfang_point where cameraIndexCode='" + item["cameraIndexCode"]?.ToString() + "'"))
                            {
                                // 插入数据
                                Dictionary<string, object> data = new Dictionary<string, object>();
                                data.Add("cameraIndexCode", item["cameraIndexCode"]?.ToString());
                                data.Add("cameraName", item["cameraName"]?.ToString());
                                mySQLHelper.Insert("anfang_point", data);
                            }
                            else
                            {
                                // 更新数据
                                Dictionary<string, object> data = new Dictionary<string, object>();
                                data.Add("cameraName", item["cameraName"]?.ToString());
                                mySQLHelper.Update("anfang_point", data, "cameraIndexCode='" + item["cameraIndexCode"]?.ToString() + "'");
                            }
                        }
                    }
                    */

                    // 假设你已经有接口返回数据保存为本地 JSON 文件 test_cameras.json
                    string jsonFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "test_cameras.json");

                    if (!File.Exists(jsonFilePath))
                    {
                        Console.WriteLine("JSON 文件不存在: " + jsonFilePath);
                        return;
                    }

                    string jsonContent = File.ReadAllText(jsonFilePath, Encoding.UTF8);
                    debug_log(jsonContent);
                    Console.WriteLine(jsonContent);

                    // 解析 JSON 数据
                    JObject jsonObj = JObject.Parse(jsonContent);
                    JArray list = (JArray)jsonObj["data"]?["list"];

                    if (list == null)
                    {
                        Console.WriteLine("JSON 数据格式错误，未找到 data.list");
                        return;
                    }

                    foreach (var item in list)
                    {
                        string indexCode = item["cameraIndexCode"]?.ToString();
                        string name = item["cameraName"]?.ToString();

                        if (string.IsNullOrEmpty(indexCode))
                        {
                            Console.WriteLine("跳过空 cameraIndexCode");
                            continue;
                        }

                        if (!mySQLHelper.IsExist("SELECT id FROM anfang_point WHERE cameraIndexCode='" + indexCode + "'"))
                        {
                            // 插入数据
                            //Dictionary<string, object> data = new Dictionary<string, object>();
                            //data.Add("cameraIndexCode", indexCode);
                            //data.Add("cameraName", name);
                            //mySQLHelper.Insert("anfang_point", data);
                        }
                        else
                        {
                            // 更新数据
                            //Dictionary<string, object> data = new Dictionary<string, object>();
                            //data.Add("cameraName", name);
                            //mySQLHelper.Update("anfang_point", data, "cameraIndexCode='" + indexCode + "'");
                        }
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                /*
                //6.获取点位实时视频地址###############################################################################
                Console.Write("6.获取点位实时视频地址\n");
                debug_log("6.获取点位实时视频地址:");
                dataTable = mySQLHelper.ExecuteQuery("SELECT * FROM anfang_point");
                foreach (DataRow row in dataTable.Rows)
                {
                    try
                    {
                        PreviewURLs previewURLs = new PreviewURLs { cameraIndexCode = row["cameraIndexCode"].ToString(), protocol = "hls" }; //值从第1个接口获取
                        body = JsonConvert.SerializeObject(previewURLs);
                        uri = "/artemis/api/video/v2/cameras/previewURLs";
                        result = HttpUtillib.HttpPost(uri, body, 15);

                        if (null == result)
                        {
                            Console.WriteLine("POST fail");
                        }
                        else
                        {
                            debug_log(System.Text.Encoding.UTF8.GetString(result));
                            Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));

                            // 解析 JSON 数据
                            JObject jsonObj = JObject.Parse(System.Text.Encoding.UTF8.GetString(result));
                            jsonObj["data"]["url"].ToString();

                            // 更新数据
                            Dictionary<string, object> data = new Dictionary<string, object>();
                            data.Add("url", jsonObj["data"]["url"].ToString());
                            data.Add("up_time", DateTime.Now.ToString());
                            mySQLHelper.Update("anfang_point", data, "cameraIndexCode='" + row["cameraIndexCode"].ToString() + "'");
                        }
                    }
                    catch (ArgumentNullException ex)
                    {
                        Console.WriteLine("请求失败：接口地址无效或无法访问。");
                        Console.WriteLine("错误信息: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("请求失败：发生未知错误。");
                        Console.WriteLine("错误信息: " + ex.Message);
                    }
                }

                //7.获取点位视频回放地址###############################################################################
                Console.Write("7.获取点位视频回放地址\n");
                debug_log("7.获取点位视频回放地址:");
                try
                {
                    PlaybackURLs playbackURLs = new PlaybackURLs { cameraIndexCode = "748d84750e3a4a5bbad3cd4af9ed5101", protocol = "hls", beginTime = "2025-06-14T00:00:00.000+08:00", endTime = "2025-06-15T00:00:00.000+08:00" }; //值从第1个接口获取
                    body = JsonConvert.SerializeObject(playbackURLs);
                    uri = "/artemis/api/video/v2/cameras/playbackURLs";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //8.获取门禁点信息###############################################################################
                Console.Write("8.获取门禁点信息\n");
                debug_log("8.获取门禁点信息:");
                try
                {
                    Doorsearch doorsearch = new Doorsearch { pageNo = 1, pageSize = 100 };
                    body = JsonConvert.SerializeObject(doorsearch);
                    uri = "/artemis/api/resource/v2/door/search";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //9.获取获取门禁事件(统计入场人数)##########################################################################先要订阅事件,入场事件类型值可联系海康现场技术
                Console.Write("9.获取门禁事件\n");
                debug_log("9.获取门禁事件:");
                try
                {
                    Doorevents doorevents = new Doorevents { pageNo = 1, pageSize = 1000, beginTime = "2025-06-14T00:00:00.000+08:00", endTime = "2025-06-15T00:00:00.000+08:00", eventTypes = new int[] { 10, 20 }, order = "eventTime", sort = "desc" };
                    body = JsonConvert.SerializeObject(doorevents);
                    uri = "/artemis/api/acs/v2/door/events";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //10.获取过车记录事件(统计车辆数量)##########################################################################
                Console.Write("10.获取过车记录事件\n");
                debug_log("10.获取过车记录事件:");
                try
                {
                    CrossRecords crossRecords = new CrossRecords { pageNo = 1, pageSize = 1000, beginTime = "2025-06-14T00:00:00.000+08:00", endTime = "2025-06-15T00:00:00.000+08:00", vehicleOut = 0 }; //vehicleOut:0进场,1出场
                    body = JsonConvert.SerializeObject(crossRecords);
                    uri = "/artemis/api/pms/v1/crossRecords/page";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //11.按事件类型订阅事件##########################################################################
                Console.Write("11.按事件类型订阅事件\n");
                debug_log("11.按事件类型订阅事件:");
                try
                {
                    List<int> eventTypesList = new List<int>(); // 使用List来存储动态数量的事件类型
                    dataTable = mySQLHelper.ExecuteQuery("SELECT event_code FROM anfang_event where flag=1");
                    foreach (DataRow row in dataTable.Rows)
                    {
                        int eventCode = int.Parse(row["event_code"].ToString());
                        eventTypesList.Add(eventCode);
                    }
                    int[] eventTypes = eventTypesList.ToArray(); // 将List转换为数组
                    EventSubscription eventSubscription = new EventSubscription { eventTypes = eventTypes, eventDest = eventDest }; //eventDest:推送地址
                    body = JsonConvert.SerializeObject(eventSubscription);
                    uri = "/artemis/api/eventService/v1/eventSubscriptionByEventTypes";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //12.查询订阅事件信息##########################################################################
                Console.Write("12.查询订阅事件信息\n");
                debug_log("12.查询订阅事件信息:");
                try
                {
                    body = "";
                    uri = "/artemis/api/eventService/v1/eventSubscriptionByEventTypes";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                //13.按事件类型取消订阅##########################################################################
                Console.Write("13.按事件类型取消订阅\n");
                debug_log("13.按事件类型取消订阅:");
                try
                {
                    EventUnSubscription eventUnSubscription = new EventUnSubscription { eventTypes = new int[] { 100, 101 } };
                    body = JsonConvert.SerializeObject(eventUnSubscription);
                    uri = "/artemis/api/eventService/v1/eventSubscriptionByEventTypes";
                    result = HttpUtillib.HttpPost(uri, body, 15);

                    if (null == result)
                    {
                        Console.WriteLine("POST fail");
                    }
                    else
                    {
                        debug_log(System.Text.Encoding.UTF8.GetString(result));
                        Console.WriteLine(System.Text.Encoding.UTF8.GetString(result));
                    }
                }
                catch (ArgumentNullException ex)
                {
                    Console.WriteLine("请求失败：接口地址无效或无法访问。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("请求失败：发生未知错误。");
                    Console.WriteLine("错误信息: " + ex.Message);
                }

                Console.WriteLine("执行完成，按任意键重新执行...");
                Console.ReadKey(); // 等待用户按任意键

                Console.Clear(); // 清空控制台内容以便重新执行时看起来干净

                */
            }
        }

        /// <summary>
        /// 写日志
        /// </summary>
        /// <param name="message"></param>
        static void debug_log(string message)
        {
            string filePath = "debug_log.txt";
            string logMessage = $"{DateTime.Now}: {message}";

            // 使用StreamWriter向文件中追加内容
            using (StreamWriter writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine(logMessage);
            }
        }

        /*事件报文格式
        {
	        "method": "OnEventNotify", //方法名,用于标识报文用途,事件固定为:OnEventNotify
	        "params": {
		        "sendTime": "2018-06-28 17:04:31", //发出时间
		        "ability": "event_pms", //事件类别,如视频事件、门禁事件?
		        "uids": "", //指定的事件接收用户列表
		        "events": [{  //事件内容:最多50条
			        "eventId": "6DF7CA9A-7000-4AD5-8E73-17A83038BB4C", //事件Id,如同一事件若上报多次，则上报事件的eventId相同
			        "eventType": 771764226, //事件类型?
			        "happenTime": "2018-06-28 17:04:31", //发生时间
			        "srcIndex": "d0de2c6a62ee428498a9a42c2db39ebc", //事件源编号，物理设备是资源编号
			        "srcName": "", //事件源名称
			        "srcParentIdex": "", //事件发生的事件源父设备，无-空字符串
			        "srcType": "parkspace", //事件源类型?
			        "status": 0, //事件状态, 0-瞬时 1-开始 2-停止 3-事件脉冲 4-事件联动结果更新 5-异步图片上传
			        "eventLvl": 0, //事件等级:0-未配置,1-低,2-中,3-高,注意,此处事件等级是指在事件联动中配置的等级,需要配置了事件联动,才返回这个字段
			        "timeout": 30, //脉冲超时时间，一个持续性的事件，上报的间隔
			        "data": {} //事件其它扩展信息,不同类型的事件，扩展信息不同，具体信息可查看具体事件的报文
		        }]
	        }
        }
        */
    }
}
