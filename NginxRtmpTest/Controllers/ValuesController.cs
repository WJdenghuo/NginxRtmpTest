using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Web.Http;

namespace NginxRtmpTest.Controllers
{
    public class ValuesController : ApiController
    {
        static FileInfo logCfg = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + "log4net.config");       
        
       
        [HttpGet]
        public IHttpActionResult Test(String user,String password,String endTime)
        {
            XmlConfigurator.ConfigureAndWatch(logCfg);
            ILog _log = LogManager.GetLogger(typeof(ValuesController));
            DateTime dt = new DateTime();
            _log.Info($"rtmp推流访问时间{DateTime.Now}");
            if (String.IsNullOrEmpty(user)||String.IsNullOrEmpty(password)||String.IsNullOrEmpty(endTime))
            {
                return NotFound();
            }
            else if (DateTime.TryParse(endTime, out dt))
            {
                if (DateTime.Now < dt&&user=="test"&&password=="123456")
                {
                    return Ok();
                }
                return NotFound();
            }           
            return NotFound();
        }
          
        /// <summary>
        /// 设置ffmpeg.exe的路径
        /// </summary>
        static string FFmpegPath = AppDomain.CurrentDomain.BaseDirectory+"ffmpeg.exe";
        static Queue<Task<String> > queueTask = new Queue<Task<String>>();
        static Queue<String> queueString = new Queue<String>();
        private static object lockObj = new object();
        static Int32 pID = 0;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoUrl"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [Route("api/QueueTestAsnyc")]
        public async Task<IHttpActionResult> QueueTestAsnyc(String ffmpegPath,string videoUrl, string startTime, string endTime)
        {
            if (!String.IsNullOrEmpty(ffmpegPath))
            {
                FFmpegPath = ffmpegPath;
            }           
            await FfmpegTestAsnyc(videoUrl, startTime, endTime);
            return Ok();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ffmpegPath"></param>
        /// <param name="videoUrl"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        /// <returns></returns>
        [Route("api/QueueTest")]
        public IHttpActionResult QueueTest(String ffmpegPath, string videoUrl, string startTime, string endTime)
        {
            if (!String.IsNullOrEmpty(ffmpegPath))
            {
                FFmpegPath = ffmpegPath;
            }
            FfmpegTest(videoUrl, startTime, endTime);
            return Ok();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="videoUrl"></param>
        /// <param name="startTime"></param>
        /// <param name="endTime"></param>
        static async Task FfmpegTestAsnyc(string videoUrl, string startTime, string endTime)
        {
            string newVideoUrl = "";
            //videoUrl = "t24.mp4";

            //处理路径
            videoUrl = AppDomain.CurrentDomain.BaseDirectory + videoUrl;
            var extention= Path.GetExtension(videoUrl);
            var pathVIdeo = Path.GetDirectoryName(videoUrl);
            var fileName = Path.GetFileNameWithoutExtension(videoUrl);
            newVideoUrl = pathVIdeo+$"/{fileName}-new"+ extention;
            newVideoUrl= AppDomain.CurrentDomain.BaseDirectory+ $"{fileName}-new" + extention;

            //视频转码
            string para = $@" -ss {startTime} -t {endTime} -y -i {videoUrl} -vcodec copy -acodec copy {newVideoUrl}";
            //string para = "ffmpeg -version";

            queueTask.Enqueue(Task.FromResult(para));

            await RunMyProcessAsnyc(queueTask.Dequeue());
        }
        static  void FfmpegTest(string videoUrl, string startTime, string endTime)
        {
            string newVideoUrl = "";
            //videoUrl = "t24.mp4";

            //处理路径
            videoUrl = AppDomain.CurrentDomain.BaseDirectory + videoUrl;
            var extention = Path.GetExtension(videoUrl);
            var pathVIdeo = Path.GetDirectoryName(videoUrl);
            var fileName = Path.GetFileNameWithoutExtension(videoUrl);
            newVideoUrl = pathVIdeo + $"/{fileName}-new" + extention;
            newVideoUrl = AppDomain.CurrentDomain.BaseDirectory + $"{fileName}-new" + extention;

            //视频转码
            string para = $@" -ss {startTime} -t {endTime} -y -i {videoUrl} -vcodec copy -acodec copy {newVideoUrl}";
            //string para = "ffmpeg -version";

            queueString.Enqueue(para);
            lock (lockObj)
            {
                RunMyProcess(queueString.Dequeue());
            }
        }

        static async Task RunMyProcessAsnyc(Task<String> Parameters)
        {
            XmlConfigurator.ConfigureAndWatch(logCfg);
            ILog _log = LogManager.GetLogger(typeof(ValuesController));
            //
           
            var p = new Process();                 
            p.StartInfo.FileName = FFmpegPath;
            p.StartInfo.Arguments =await Parameters;
            _log.Info($"ffmpeg命令为：{p.StartInfo.Arguments}");
            //是否使用操作系统shell启动
            p.StartInfo.UseShellExecute = false;
            //不显示程序窗口
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;
            
            p.StartInfo.RedirectStandardOutput = true;            
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中           
            p.ErrorDataReceived += new DataReceivedEventHandler(Output);            
            p.OutputDataReceived += new DataReceivedEventHandler(Output);
            p.Start();
            //记录日志
            _log.Info($"当前异步ffmpeg进程ID：{p.Id}");

            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.BeginErrorReadLine();//开始异步读取
            
            p.WaitForExit();
            p.Close();
            p.Dispose();
        }
        static void  RunMyProcess(String parameters)
        {
            XmlConfigurator.ConfigureAndWatch(logCfg);
            ILog _log = LogManager.GetLogger(typeof(ValuesController));
            //

            var p = new Process();
            if (pID != 0)
            {
                p = Process.GetProcessById(pID);
                pID = p.Id;
            }

            p.StartInfo.FileName = FFmpegPath;
            p.StartInfo.Arguments = parameters;
            _log.Info($"ffmpeg命令为：{p.StartInfo.Arguments}");
            //是否使用操作系统shell启动
            p.StartInfo.UseShellExecute = false;
            //不显示程序窗口
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardInput = true;

            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;//把外部程序错误输出写到StandardError流中           
            p.ErrorDataReceived += new DataReceivedEventHandler(Output);
            p.OutputDataReceived += new DataReceivedEventHandler(Output);
            p.Start();
            //记录日志
            _log.Info($"当前同步ffmpeg进程ID：{p.Id}");

            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.BeginErrorReadLine();//开始异步读取

            p.WaitForExit();

            //
            if (queueString.Count == 0)
            {
                pID = 0;
                p.Close();
                p.Dispose();
            }           
        }
        static void Output(object sendProcess, DataReceivedEventArgs output)
        {
            XmlConfigurator.ConfigureAndWatch(logCfg);
            ILog _log = LogManager.GetLogger(typeof(ValuesController));
            if (!String.IsNullOrEmpty(output.Data))
            {
                //处理方法...
                //Console.WriteLine(output.Data);
                _log.Info(output.Data);
            }
        }
    }
}
