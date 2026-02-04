using HttpUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace anfang
{
    public class Regions { public int pageNo { get; set; } public int pageSize { get; set; } }
    public class OrgList { public int pageNo { get; set; } public int pageSize { get; set; } }
    public class PersonList { public int pageNo { get; set; } public int pageSize { get; set; } }
    public class Resources { public string resourceType { get; set; } public int pageNo { get; set; } public int pageSize { get; set; } }
    public class Cameras { public int pageNo { get; set; } public int pageSize { get; set; } }
    public class PreviewURLs { public string cameraIndexCode { get; set; } public int streamType { get; set; } public string protocol { get; set; } }
    public class PlaybackURLs { internal int streamType; public string cameraIndexCode { get; set; } public int transmode { get; set; } public string protocol { get; set; } public string beginTime { get; set; } public string endTime { get; set; } public string expand { get; set; } public int lockType { get; set; } }
    public class Doorsearch { public int pageNo { get; set; } public int pageSize { get; set; } }
    public class Doorevents { public int pageNo { get; set; } public int pageSize { get; set; } public string beginTime { get; set; } public string endTime { get; set; } public int[] eventTypes { get; set; } public string sort { get; set; } public string order { get; set; } }
    public class CrossRecords { public int pageNo { get; set; } public int pageSize { get; set; } public string beginTime { get; set; } public string endTime { get; set; } public int vehicleOut { get; set; } }
    public class EventSubscription { public int[] eventTypes { get; set; } public string eventDest { get; set; } }
    public class EventUnSubscription { public int[] eventTypes { get; set; } }

    class Program
    {
        static System.Timers.Timer _timer;
        static MySQLHelper mySQLHelper;
        static string body = "";
        static string uri = "";
        static byte[] result = null;

        static void Main()
        {
            // ========= 全局兜底（记录日志用，不是保命用） =========
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                debug_log("【致命未捕获异常】" + e.ExceptionObject);
            };

            TaskScheduler.UnobservedTaskException += (sender, e) =>
            {
                debug_log("【未观察到的Task异常】" + e.Exception);
                e.SetObserved();
            };

            try
            {
                // 启动时执行一次
                RunMainLogic();

                // Main() 里，RunMainLogic() 后面
                Task.Run(() =>
                {
                    Thread.Sleep(3000); // 给程序 3 秒“站稳”
                    SyncCameras();
                });

                //-----------------------------------
                // 每 4 分钟执行：获取视频地址
                //-----------------------------------
                _timer = new System.Timers.Timer(240000);
                _timer.Elapsed += (sender, e) =>
                {
                    try
                    {
                        ExecutePreviewUpdate();
                    }
                    catch (Exception ex)
                    {
                        debug_log("ExecutePreviewUpdate 异常：" + ex);
                    }
                };
                _timer.AutoReset = true;
                _timer.Start();

                //-----------------------------------
                // 每天 12:00 / 18:00 黑白名单同步（每分钟检查）
                //-----------------------------------
                System.Timers.Timer timerNameList = new System.Timers.Timer(60000);
                timerNameList.Elapsed += (sender, e) =>
                {
                    try
                    {
                        CheckAndRunNameListJob();
                    }
                    catch (Exception ex)
                    {
                        debug_log("CheckAndRunNameListJob 异常：" + ex);
                    }
                };
                timerNameList.AutoReset = true;
                timerNameList.Start();

                //-----------------------------------
                // 每 5 分钟：车辆通行记录
                //-----------------------------------
                System.Timers.Timer timerPassRecord = new System.Timers.Timer(300000);
                timerPassRecord.Elapsed += (sender, e) =>
                {
                    try
                    {
                        FetchPassRecord();
                    }
                    catch (Exception ex)
                    {
                        debug_log("FetchPassRecord 异常：" + ex);
                    }
                };
                timerPassRecord.AutoReset = true;
                timerPassRecord.Start();

                //-----------------------------------
                // HTTP 接口服务
                //-----------------------------------
                Task.Run(() =>
                {
                    try
                    {
                        StartHttpServer();
                    }
                    catch (Exception ex)
                    {
                        debug_log("HTTP 服务异常退出：" + ex);
                    }
                });

                Console.WriteLine("程序已启动，所有定时任务与 HTTP 服务已运行");

                // 不要用 ReadKey，否则误触即死
                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception ex)
            {
                // 这里只能兜 Main 线程同步异常
                debug_log("Main 线程异常：" + ex);
            }
        }


        // ✅ 新增：HTTP接口服务
        static void StartHttpServer()
        {
            //如果拒绝访问,请用管理员命令行执行:netsh http add urlacl url=http://+:8088/ user=Everyone
            string url = "http://+:8088/"; // 监听所有地址
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(url);
            try
            {
                listener.Start();
                Console.WriteLine($"HTTP服务已启动：{url}");
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine($"HTTP服务启动失败：{ex.Message}");
                debug_log("HTTP服务启动异常：" + ex.ToString());
                return;
            }

            while (true)
            {
                try
                {
                    var context = listener.GetContext(); // 阻塞等待请求
                    Task.Run(() => HandleRequest(context));
                }
                catch (Exception ex)
                {
                    debug_log("HTTP监听异常：" + ex.ToString());
                }
            }
        }

        // ✅ 新增：处理请求
        static void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var resp = context.Response;

            string act = req.QueryString["act"];
            string responseString = "";

            /*
            视频回放(起止时间不超过3天)
            http://127.0.0.1:8088/?act=playbackURLs&cameraIndexCode=c836794d6cd543c7a69314866458969b&beginTime=2026-1-21%2009:00:00&endTime=2026-1-21%2009:15:00&protocol=hls
            实时抓拍
            http://127.0.0.1:8088/?act=manualCapture&cameraIndexCode=***
            视频图片
            http://127.0.0.1:8088/?act=pictureInfos&pageNo=1&pageSize=100&cameraIndexCode=***&startTime=2025-11-22 09:00:00&endTime=2025-11-22 10:00:00
             */

            try
            {
                if (string.IsNullOrEmpty(act))
                {
                    WriteJson(resp, new { status = "error", msg = "缺少参数 act" });
                    return;
                }

                string uri = "";
                string body = "";

                switch (act)
                {
                    // =========================
                    // 1. 回放接口
                    // =========================
                    case "playbackURLs":
                        {
                            string cameraIndexCode = req.QueryString["cameraIndexCode"];
                            string beginTimeRaw = req.QueryString["beginTime"];
                            string endTimeRaw = req.QueryString["endTime"];
                            string beginTime = ToHikIsoTime(beginTimeRaw);
                            string endTime = ToHikIsoTime(endTimeRaw);

                            string protocol = req.QueryString["protocol"] ?? "hls";

                            if (string.IsNullOrEmpty(cameraIndexCode) ||
                                string.IsNullOrEmpty(beginTime) ||
                                string.IsNullOrEmpty(endTime))
                            {
                                WriteJson(resp, new { status = "error", msg = "缺少必要参数 cameraIndexCode / beginTime / endTime" });
                                return;
                            }

                            /*var obj = new PlaybackURLs
                            {
                                cameraIndexCode = cameraIndexCode,
                                streamType = 1,
                                beginTime = beginTime,
                                endTime = endTime,
                                protocol = "rtsp",
                                expand = "streamform=rtp",
                                transmode = 1,
                                lockType = 0
                            };*/

                            var obj = new PlaybackURLs
                            {
                                cameraIndexCode = cameraIndexCode,
                                streamType = 1,
                                beginTime = beginTime,
                                endTime = endTime,
                                protocol = "hls",
                                transmode = 1,
                                expand = "streamform=ps",
                                lockType = 0
                            };

                            body = JsonConvert.SerializeObject(obj);
                            uri = "/artemis/api/video/v2/cameras/playbackURLs";
                        }
                        break;

                    // =========================
                    // 2. 抓拍接口
                    // =========================
                    case "manualCapture":
                        {
                            string cameraIndexCode = req.QueryString["cameraIndexCode"];
                            if (string.IsNullOrEmpty(cameraIndexCode))
                            {
                                WriteJson(resp, new { status = "error", msg = "缺少参数 cameraIndexCode" });
                                return;
                            }

                            var obj = new { cameraIndexCode = cameraIndexCode };
                            body = JsonConvert.SerializeObject(obj);
                            uri = "/artemis/api/video/v1/manualCapture";
                        }
                        break;

                    // =========================
                    // 3. 图片列表接口
                    // =========================
                    case "pictureInfos":
                        {
                            int pageNo = int.Parse(req.QueryString["pageNo"] ?? "1");
                            int pageSize = int.Parse(req.QueryString["pageSize"] ?? "20");
                            string cameraIndexCode = req.QueryString["cameraIndexCode"];

                            string startTimeRaw = req.QueryString["startTime"];
                            string endTimeRaw = req.QueryString["endTime"];
                            string startTime = ToHikIsoTime(startTimeRaw);
                            string endTime = ToHikIsoTime(endTimeRaw);

                            if (string.IsNullOrEmpty(cameraIndexCode) ||
                                string.IsNullOrEmpty(startTime) ||
                                string.IsNullOrEmpty(endTime))
                            {
                                WriteJson(resp, new { status = "error", msg = "缺少必要参数 cameraIndexCode / startTime / endTime" });
                                return;
                            }

                            var obj = new
                            {
                                pageNo = pageNo,
                                pageSize = pageSize,
                                cameraIndexCode = cameraIndexCode,
                                startTime = startTime,
                                endTime = endTime
                            };

                            body = JsonConvert.SerializeObject(obj);
                            uri = "/artemis/api/video/v1/pictureInfos";
                        }
                        break;

                    // =========================
                    // 未知 act
                    // =========================
                    default:
                        WriteJson(resp, new { status = "error", msg = $"未知接口 act={act}" });
                        return;
                }

                // ================================
                // 统一向海康发送 POST
                // ================================
                byte[] rs = HttpUtillib.HttpPost(uri, body, 15);
                if (rs == null)
                {
                    WriteJson(resp, new { status = "error", msg = "海康接口请求失败" });
                    return;
                }

                string hkJson = Encoding.UTF8.GetString(rs);
                debug_log($"接口 [{act}] 返回：{hkJson}");

                // 原样输出海康返回
                byte[] buffer = Encoding.UTF8.GetBytes(hkJson);
                resp.ContentType = "application/json";
                resp.ContentLength64 = buffer.Length;
                resp.OutputStream.Write(buffer, 0, buffer.Length);
                resp.OutputStream.Close();
                return;
            }
            catch (Exception ex)
            {
                debug_log("接口处理异常：" + ex);
                WriteJson(resp, new { status = "error", msg = ex.Message });
            }
        }

        static void WriteJson(HttpListenerResponse resp, object obj)
        {
            string json = JsonConvert.SerializeObject(obj);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            resp.ContentType = "application/json";
            resp.ContentLength64 = buffer.Length;
            resp.OutputStream.Write(buffer, 0, buffer.Length);
            resp.OutputStream.Close();
        }


        // 原有逻辑保持不变 ↓
        static void RunMainLogic()
        {
            string appKey = "";
            string appSecret = "";
            string ip = "";
            int socket = 0;
            bool https = false;
            string connectionString = "";
            string eventDest = "";

            string xmlFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "inc.xml");
            Console.WriteLine($"XML文件路径: {xmlFilePath}");

            try
            {
                if (!File.Exists(xmlFilePath))
                {
                    Console.WriteLine($"配置文件不存在：{xmlFilePath}");
                    debug_log("配置文件不存在：" + xmlFilePath);
                    return;
                }

                XDocument xdoc = XDocument.Load(xmlFilePath);
                XElement optionElement = xdoc.Element("option");
                if (optionElement == null)
                {
                    debug_log("XML 中缺少 <option> 根节点");
                    return;
                }

                appKey = optionElement.Element("APPKey")?.Value;
                appSecret = optionElement.Element("APPSecret")?.Value;
                ip = optionElement.Element("IP")?.Value;
                socket = int.Parse(optionElement.Element("Socket")?.Value ?? "0");
                https = bool.Parse(optionElement.Element("Https")?.Value ?? "false");
                connectionString = optionElement.Element("connectionString")?.Value;
                eventDest = optionElement.Element("eventDest")?.Value;
            }
            catch (Exception ex)
            {
                debug_log("加载配置异常：" + ex);
                return; //启动准备失败，直接返回，但不抛异常
            }

            // 初始化平台参数（不访问网络）
            HttpUtillib.SetPlatformInfo(appKey, appSecret, ip, socket, https);

            // 初始化数据库对象（不主动连库）
            mySQLHelper = new MySQLHelper(connectionString);

            debug_log("RunMainLogic 初始化完成");
        }

        static void SyncCameras()
        {
            Console.WriteLine("同步监控点列表开始");
            debug_log("同步监控点列表开始");

            try
            {
                Cameras cameras = new Cameras
                {
                    pageNo = 1,
                    pageSize = 200
                };

                string body = JsonConvert.SerializeObject(cameras);
                string uri = "/artemis/api/resource/v1/cameras";

                byte[] result = HttpUtillib.HttpPost(uri, body, 15);
                if (result == null)
                {
                    debug_log("获取监控点失败：HTTP 返回 null");
                    return;
                }

                string jsonStr = Encoding.UTF8.GetString(result);
                debug_log(jsonStr);

                JObject jsonObj = JObject.Parse(jsonStr);
                JArray list = (JArray)jsonObj["data"]?["list"];
                if (list == null)
                {
                    debug_log("监控点列表为空");
                    return;
                }

                foreach (var item in list)
                {
                    string indexCode = item["cameraIndexCode"]?.ToString();
                    string name = item["cameraName"]?.ToString();

                    if (string.IsNullOrEmpty(indexCode))
                        continue;

                    try
                    {
                        if (!mySQLHelper.IsExist(
                            "SELECT id FROM anfang_point WHERE cameraIndex_Code='" + indexCode + "'"))
                        {
                            var data = new Dictionary<string, object>
                    {
                        { "cameraIndex_Code", indexCode },
                        { "camera_Name", name }
                    };
                            mySQLHelper.Insert("anfang_point", data);
                        }
                        else
                        {
                            var data = new Dictionary<string, object>
                    {
                        { "camera_Name", name },
                        { "up_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    };
                            mySQLHelper.Update(
                                "anfang_point",
                                data,
                                "cameraIndex_Code='" + indexCode + "'");
                        }
                    }
                    catch (SqlException ex)
                    {
                        debug_log("单条监控点数据库异常：" + ex.Message);
                        // ❗ 不中断整个同步
                    }
                }
            }
            catch (Exception ex)
            {
                debug_log("SyncCameras 异常：" + ex);
            }
        }


        static void ExecutePreviewUpdate()
        {
            Console.Write("2.获取点位实时视频地址\n");
            debug_log("2.获取点位实时视频地址:");
            try
            {
                DataTable dataTable = mySQLHelper.ExecuteQuery("SELECT * FROM anfang_point");

                foreach (DataRow row in dataTable.Rows)
                {
                    string cameraCode = row["cameraIndex_Code"].ToString();
                    string[] protocols = { "hls", "ws" };

                    foreach (string protocol in protocols)
                    {
                        try
                        {
                            PreviewURLs previewURLs = new PreviewURLs
                            {
                                cameraIndexCode = cameraCode, //监控点唯一标识
                                streamType = 1, //码流类型，0:主码流,1:子码流,2:第三码流,参数不填，默认为主码流
                                protocol = protocol //取流协议,hik,rtsp,rtmp,hls,ws
                            };

                            body = JsonConvert.SerializeObject(previewURLs);
                            uri = "/artemis/api/video/v2/cameras/previewURLs"; ///api/video/v2/cameras/previewURLs
                            result = HttpUtillib.HttpPost(uri, body, 15);

                            if (result == null)
                            {
                                Console.WriteLine($"[{protocol}] POST fail for camera {cameraCode}");
                                continue;
                            }

                            string jsonStr = Encoding.UTF8.GetString(result);
                            debug_log(jsonStr);
                            Console.WriteLine(jsonStr);

                            JObject jsonObj = JObject.Parse(jsonStr);
                            string url = jsonObj["data"]?["url"]?.ToString();

                            if (!string.IsNullOrEmpty(url))
                            {
                                var data = new Dictionary<string, object>
                            {
                                { protocol == "hls" ? "url" : "web_url", url },
                                { "up_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                            };

                                mySQLHelper.Update("anfang_point", data, "cameraIndex_Code='" + cameraCode + "'");
                            }
                            else
                            {
                                Console.WriteLine($"[{protocol}] 获取 url 失败，data 字段为空");
                            }
                        }
                        catch (SqlException ex)
                        {
                            debug_log("数据库异常，稍后重试：" + ex.Message);
                            return; //直接返回，等下一个周期
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("子请求失败：" + ex.Message);
                            debug_log("子请求异常：" + ex.ToString());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("执行查询时出错：" + ex.Message);
                debug_log("主查询异常：" + ex.ToString());
            }
        }

        static bool hasRun_12 = false;
        static bool hasRun_18 = false;

        static void CheckAndRunNameListJob()
        {
            var now = DateTime.Now;

            if (now.Hour == 12 && !hasRun_12)
            {
                FetchNameLists();
                hasRun_12 = true;
                hasRun_18 = false;
            }
            else if (now.Hour == 18 && !hasRun_18)
            {
                FetchNameLists();
                hasRun_18 = true;
                hasRun_12 = false;
            }
        }

        //----------------------
        // 获取黑名单 + 白名单
        //----------------------
        static void FetchNameLists()
        {
            try
            {
                Console.WriteLine("获取黑白名单数据...");
                debug_log("获取黑白名单数据...");

                FetchBlackOrWhite("black", "/artemis/api/resource/v1/park/vehicleBlacklist");
                FetchBlackOrWhite("white", "/artemis/api/resource/v1/park/vehicleWhiteList");
            }
            catch (Exception ex)
            {
                debug_log("黑白名单任务异常：" + ex.ToString());
            }
        }

        static void FetchBlackOrWhite(string category, string api)
        {
            byte[] rs = HttpUtillib.HttpPost(api, "{}", 20);
            if (rs == null) return;

            string json = Encoding.UTF8.GetString(rs);
            JObject jobj = JObject.Parse(json);
            JArray list = (JArray)jobj["data"]?["list"];
            if (list == null) return;

            foreach (var item in list)
            {
                string plate = item["plateNumber"]?.ToString();
                string effective = item["effectiveTime"]?.ToString();
                string expire = item["expireTime"]?.ToString();

                // 判断 long / short
                string type = "long";
                int dayCount = 0;

                if (!string.IsNullOrEmpty(effective) && !string.IsNullOrEmpty(expire))
                {
                    DateTime t1 = DateTime.Parse(effective);
                    DateTime t2 = DateTime.Parse(expire);
                    var days = (t2 - t1).TotalDays;

                    if (days <= 7)
                    {
                        type = "short";
                        dayCount = (int)days;
                    }
                }

                var data = new Dictionary<string, object>
                {
                    { "category", category },
                    { "car_num", plate },
                    { "type", type },
                    { "day_count", dayCount },
                    { "update_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                };

                bool exists = mySQLHelper.IsExist($"SELECT id FROM anfang_namelist WHERE car_num='{plate}' AND category='{category}'");

                if (!exists)
                {
                    data.Add("create_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    mySQLHelper.Insert("anfang_namelist", data);
                }
                else
                {
                    mySQLHelper.Update("anfang_namelist", data, $"car_num='{plate}' AND category='{category}'");
                }
            }
        }

        static void FetchPassRecord()
        {
            try
            {
                Console.WriteLine("获取车辆通行记录...");
                debug_log("获取车辆通行记录...");

                string api = "/artemis/api/resource/v1/park/vehiclePassRecord?pageNo=1&pageSize=200";
                byte[] rs = HttpUtillib.HttpPost(api, "{}", 20);
                if (rs == null) return;

                string json = Encoding.UTF8.GetString(rs);
                JObject jobj = JObject.Parse(json);
                JArray list = (JArray)jobj["data"]?["list"];
                if (list == null) return;

                foreach (var item in list)
                {
                    string id = item["id"]?.ToString();
                    string carNo = item["plateNumber"]?.ToString();
                    string passTime = item["passTime"]?.ToString();
                    int vehicleOut = int.Parse(item["vehicleOut"]?.ToString() ?? "0");

                    string direction = vehicleOut == 0 ? "in" : "out";
                    string lane = item["laneName"]?.ToString();

                    bool exists = mySQLHelper.IsExist($"SELECT id FROM anfang_gatepass WHERE id='{id}'");
                    if (exists) continue;
                    var data = new Dictionary<string, object>
                    {
                        { "id", id },
                        { "car_num", carNo },
                        { "gate_name", lane },
                        { "direction", direction },
                        { "pass_time", passTime },
                        { "create_time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
                    };

                    mySQLHelper.Insert("anfang_gatepass", data);
                }
            }
            catch (SqlException ex)
            {
                debug_log("数据库异常，稍后重试：" + ex.Message);
                return; //直接返回，等下一个周期
            }
            catch (Exception ex)
            {
                debug_log("车辆通过记录异常：" + ex.ToString());
            }
        }

        private static string ToHikIsoTime(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            // 允许：2026-1-21 09:00:00 / 2026-01-21 09:00:00
            if (!DateTime.TryParse(raw, out DateTime dt))
                throw new Exception("时间格式错误：" + raw);

            // 明确指定为北京时间
            var dto = new DateTimeOffset(
                dt,
                TimeSpan.FromHours(8)
            );

            // 海康严格要求的格式
            return dto.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz");
        }

        static void debug_log(string message)
        {
            string filePath = "debug_log.txt";
            string logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    writer.WriteLine(logMessage);
                }
            }
            catch { }
        }
    }
}
