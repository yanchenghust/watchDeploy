using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MyHttp;
using System.Collections.Specialized;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text.RegularExpressions;
using MyLog;
using System.Threading;

namespace ConsoleApplication1
{
    class Program
    {
        
        private string workspace;
        private Log logger;
        public List<DeployConfig> depConf = new List<DeployConfig>();
        public List<FileWatcher> watcher = new List<FileWatcher>();

        public Program()
        {
            this.workspace = @"D:\\watchdir";
            this.logger = new Log(this.workspace);
        }
        static void Main(string[] args)
        {
            Program program = new Program();
            program.startWatch();
        }

        public void startWatch()
        {
            bool initRet = initConf();
            if (false == initRet)
            {
                this.logger.WriteLog("init conf failed.");
                return ;
            }
            initWatcher();
            Console.WriteLine("watching...");
            while (true)
            {
                Thread.Sleep(1);
            }
        }
        

        public bool initWatcher()
        {
            for (int idx = 0; idx < this.depConf.Count; idx++)
            {
                FileWatcher fwatcher = new FileWatcher(this.depConf[idx]);
                this.watcher.Add(fwatcher);
            }

            return true;
        }

        

        private bool initConf()
        {
            string confPath = @"deploy.conf";
            if (!File.Exists(confPath))
            {
                this.logger.WriteLog("conf file " + confPath + " not found");
                return false;
            }
            StreamReader sr = new StreamReader(confPath, Encoding.Default);
            string line;
            string mode = "";
            int lno = 0;
            int depIndex = -1;
            int mapIndex = -1;
            
            while ((line = sr.ReadLine()) != null)
            {
                lno++;
                line = line.Trim();
                if (line.IndexOf("#") == 0 || line.Length == 0)
                {
                    continue;
                }
                if (line.IndexOf("[deploy]") >= 0)
                {
                    depIndex++;
                    this.depConf.Add(new DeployConfig());
                    this.depConf[depIndex].map = new List<DeployMap>();
                    mode = "dep.conf";
                    mapIndex = -1;
                    continue;
                }
                else if (line.IndexOf("[.map]") >= 0)
                {
                    mapIndex++;
                    mode = "dep.map";
                    this.depConf[depIndex].map.Add(new DeployMap());
                    continue;
                }
                else
                {
                    string []kv = line.Split(new char[] {':'}, 2);
                    if(kv.Length != 2){
                        this.logger.WriteLog("conf format error at line " + lno + ".");
                        return false;
                    }
                    for(int i=0; i<kv.Length; i++){
                        kv[i] = kv[i].Trim();
                    }
                    if(mode == "dep.conf"){
                        if("watchdir" == kv[0]){
                            this.depConf[depIndex].watchDir = kv[1];
                        }
                        if ("receiver" == kv[0])
                        {
                            this.depConf[depIndex].receiver = kv[1];
                        }
                        if ("filter" == kv[0])
                        {
                            this.depConf[depIndex].filter = kv[1];
                        }
                        if ("ison" == kv[0])
                        {
                            this.depConf[depIndex].ison = kv[1];
                        }
                    }
                    if (mode == "dep.map")
                    {
                        if("local" == kv[0]){
                            this.depConf[depIndex].map[mapIndex].local = kv[1];
                        }
                        if ("remote" == kv[0])
                        {
                            this.depConf[depIndex].map[mapIndex].remote = kv[1];
                        }
                    }
                }
            }

            return true;
        }

    }

  

    public static class JsonHelper
    {
        /// <summary>
        /// Json序列化,用于发送到客户端
        /// </summary>
        public static string ToJson(this object item)
        {

            DataContractJsonSerializer serializer = new DataContractJsonSerializer(item.GetType());

            using (MemoryStream ms = new MemoryStream())
            {
                serializer.WriteObject(ms, item);
                StringBuilder sb = new StringBuilder();
                sb.Append(Encoding.UTF8.GetString(ms.ToArray()));
                return sb.ToString();

            }

        }

        /// <summary>
        /// Json反序列化,用于接收客户端Json后生成对应的对象
        /// </summary>
        public static T FromJsonTo<T>(this string jsonString)
        {
            DataContractJsonSerializer ser = new DataContractJsonSerializer(typeof(T));
            MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(jsonString));
            T jsonObject = (T)ser.ReadObject(ms);
            ms.Close();
            return jsonObject;
        }
    }

    

    [DataContract]
    public class uploadResponse
    {
        [DataMember(Order = 0, IsRequired = true)]
        public int errNo { get; set; }

        [DataMember(Order = 1)]
        public string errStr { get; set; }
    }

    public class DeployConfig
    {
        public List<DeployMap> map;
        public string watchDir;
        public string receiver;
        public string filter;
        public string ison;
    }

    public class DeployMap{
        public string local;
        public string remote;
    }


    public class FileWatcher
    {
        public FileSystemWatcher fWatcher;
        public DeployConfig depConf;
        private HttpRequest req = new HttpRequest();
        private Log logger;
        private string workspace;

        public FileWatcher(DeployConfig dc)
        {
            this.depConf = dc;
            this.workspace = @"D:\\watchdir";
            this.logger = new Log(this.workspace);
            fWatcher = new FileSystemWatcher(this.depConf.watchDir);
            fWatcher.IncludeSubdirectories = true;
            fWatcher.Filter = this.depConf.filter;
            fWatcher.Created += new FileSystemEventHandler(OnCreated);
            fWatcher.Deleted += new FileSystemEventHandler(OnDeleted);
            fWatcher.Renamed += new RenamedEventHandler(OnRenamed);
            fWatcher.Changed += new FileSystemEventHandler(OnChanged);
            fWatcher.EnableRaisingEvents = true;
        }
        public void OnChanged(object sender, FileSystemEventArgs e)
        {
            checkAndUpload(e.FullPath, e.Name);
        }

        public void OnDeleted(object sender, FileSystemEventArgs e)
        {
            checkAndUpload(e.FullPath, e.Name);
        }
        public void OnCreated(object sender, FileSystemEventArgs e)
        {
            checkAndUpload(e.FullPath, e.Name);
        }
        public void OnRenamed(object sender, RenamedEventArgs e)
        {
            checkAndUpload(e.FullPath, e.Name);
        }

        public bool checkAndUpload(string path, string after)
        {
            Regex rx = new Regex(@"^.*\." + this.depConf.filter + "$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            MatchCollection matches = rx.Matches(after);

            if (matches.Count == 0)
            {
                return false;
            }

            //依次匹配映射规则
            string lastLocal = "";
            string lastRemote = "";
            for (int i = 0; i < this.depConf.map.Count; i++)
            {
                if (path.IndexOf(this.depConf.map[i].local) >= 0)
                {
                    //匹配出路径
                    int baseDirLen = this.depConf.map[i].local.Length;
                    if (baseDirLen < lastLocal.Length) continue; //匹配最长的一条规则

                    string file = path.Substring(baseDirLen);
                    if (file[0] == '\\')
                    {
                        file = file.Substring(1);
                    }
                    string dest = this.depConf.map[i].remote;
                    if (dest[dest.Length - 1] != '/')
                    {
                        dest += '/';
                    }

                    dest += file;
                    dest = dest.Replace('\\', '/');

                    lastLocal = this.depConf.map[i].local;
                    lastRemote = dest;
                }
            }
            if (lastLocal.Length == 0 || lastRemote.Length == 0)
            {
                return true;
            }
            bool upRet = uploadFile(path, lastRemote, this.depConf.receiver);
            string strRet = upRet? "ok": "fail";
            string info = string.Format("{0} => {1} [{2}]", path, lastRemote, strRet);
            this.logger.WriteLog(info);
            return upRet;
        }

        public bool uploadFile(string local, string remote, string uploadUrl)
        {
            int tryTimes = 3;
            IOException lastError = new IOException();
            while ((tryTimes --) > 0)
            {
                try
                {
                    NameValueCollection fields = new NameValueCollection();
                    fields.Add("dest[]", remote);
                    string[] files = new string[1];
                    string[] fieldName = new string[1];
                    files[0] = local;
                    fieldName[0] = @"file";
                    string response = this.req.HttpUploadFile(uploadUrl, files, fieldName, fields, Encoding.UTF8);
                    uploadResponse ur = JsonHelper.FromJsonTo<uploadResponse>(response);
                    if (ur.errNo == 0)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (IOException e)
                {
                    lastError = e;
                    Thread.Sleep(10);
                }
                
                
            }
            this.logger.WriteLog("文件上传失败:" + lastError.Message);
            return false;
        }

    }
}
