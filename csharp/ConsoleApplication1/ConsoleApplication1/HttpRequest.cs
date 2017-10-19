using System;
using System.IO;
using System.Net;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;


namespace MyHttp
{
    public class HttpRequest
    {

        public String Get(string url)
        {
            HttpWebRequest httpRequest = (HttpWebRequest)WebRequest.Create(url);
            httpRequest.Timeout = 2000;
            httpRequest.Method = "GET";
            HttpWebResponse httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            StreamReader sr = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
            string result = sr.ReadToEnd();
            int status = (int)httpResponse.StatusCode;
            sr.Close();
            return result;
            
        }

        public bool Download(string downUrl, string target)
        {
            //File.Copy(downUrl, target, true); return true;
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(downUrl);
            WebResponse response = request.GetResponse();
            Stream stream = response.GetResponseStream();

            byte[] buffer = new byte[1024];
            Stream outStream = System.IO.File.Create(target);
            Stream inStream = response.GetResponseStream();

            int l;
            do
            {
                l = inStream.Read(buffer, 0, buffer.Length);
                if (l > 0)
                    outStream.Write(buffer, 0, l);
            }
            while (l > 0);

            outStream.Close();
            inStream.Close();
            return true;
        }

        public string uploadFile(string uploadUrl, string fieldName, string filePath, NameValueCollection extraFields) {
            string boundary = DateTime.Now.Ticks.ToString("x");
            HttpWebRequest uploadRequest = (HttpWebRequest)WebRequest.Create(uploadUrl);
            uploadRequest.ContentType = "multipart/form-data; boundary=" + boundary;
            uploadRequest.Method = "POST";
            uploadRequest.Accept = "*/*";
            uploadRequest.KeepAlive = true;
            uploadRequest.Headers.Add("Accept-Language", "zh-cn");
            uploadRequest.Headers.Add("Accept-Encoding", "gzip, deflate");
            uploadRequest.Credentials = System.Net.CredentialCache.DefaultCredentials;
            //创建一个内存流
            Stream memStream = new MemoryStream();
            //确定上传的文件路径
            if (!String.IsNullOrEmpty(filePath))
            {
                boundary = "--" + boundary;

                //添加上传文件参数格式边界
                string paramFormat = boundary + "\r\nContent-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}\r\n";
                NameValueCollection param = new NameValueCollection();
                //param.Add("mypdf", Guid.NewGuid().ToString() + Path.GetExtension(filePath));
                if (extraFields.Count > 0)
                {
                    foreach(String key in extraFields.Keys)
                    {
                        param.Add(key, extraFields[key]);
                    }
                }

                //写上参数
                foreach (string key in param.Keys)
                {
                    string formItem = string.Format(paramFormat, key, param[key]);
                    byte[] formItemBytes = System.Text.Encoding.UTF8.GetBytes(formItem);
                    memStream.Write(formItemBytes, 0, formItemBytes.Length);
                }

                //添加上传文件数据格式边界
                string dataFormat = boundary + "\r\nContent-Disposition: form-data; name=\"{0}\";filename=\"{1}\"\r\nContent-Type:application/octet-stream\r\n\r\n";
                string header = string.Format(dataFormat, fieldName, Path.GetFileName(filePath));
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                memStream.Write(headerbytes, 0, headerbytes.Length);

                //获取文件内容
                FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[1024];
                int bytesRead = 0;

                //将文件内容写进内存流
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    memStream.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();

                //添加文件结束边界
                byte[] boundaryBytes = System.Text.Encoding.UTF8.GetBytes(boundary + "--");
                memStream.Write(boundaryBytes, 0, boundaryBytes.Length);

                //设置请求长度
                uploadRequest.ContentLength = memStream.Length;
                //获取请求写入流
                Stream requestStream = uploadRequest.GetRequestStream();


                //将内存流数据读取位置归零
                memStream.Position = 0;
                byte[] tempBuffer = new byte[memStream.Length];
                memStream.Read(tempBuffer, 0, tempBuffer.Length);
                memStream.Close();

                //将内存流中的buffer写入到请求写入流
                requestStream.Write(tempBuffer, 0, tempBuffer.Length);
                requestStream.Close();
            }

            //获取到上传请求的响应
            HttpWebResponse httpResponse = (HttpWebResponse)uploadRequest.GetResponse();
            StreamReader sr = new StreamReader(httpResponse.GetResponseStream(), System.Text.Encoding.GetEncoding("utf-8"));
            string result = sr.ReadToEnd();
            int status = (int)httpResponse.StatusCode;
            sr.Close();
            return result;
        }

        public string HttpUploadFile(string url, string[] files, string[] fieldNames, NameValueCollection data, Encoding encoding)
        {
            string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
            byte[] boundarybytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
            byte[] endbytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            //1.HttpWebRequest
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            request.Method = "POST";
            request.KeepAlive = true;
            request.Credentials = CredentialCache.DefaultCredentials;

            using (Stream stream = request.GetRequestStream())
            {
                //1.1 key/value
                string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
                if (data != null)
                {
                    foreach (string key in data.Keys)
                    {
                        stream.Write(boundarybytes, 0, boundarybytes.Length);
                        string formitem = string.Format(formdataTemplate, key, data[key]);
                        byte[] formitembytes = encoding.GetBytes(formitem);
                        stream.Write(formitembytes, 0, formitembytes.Length);
                    }
                }

                //1.2 file
                if (files != null)
                {
                    string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: application/octet-stream\r\n\r\n";
                    int fileCount = files.Length;
                    for (int i = 0; i < fileCount; i++ )
                    {
                        byte[] buffer = new byte[4096];
                        int bytesRead = 0;

                        stream.Write(boundarybytes, 0, boundarybytes.Length);
                        string header = string.Format(headerTemplate, fieldNames[i], Path.GetFileName(files[i]));
                        byte[] headerbytes = encoding.GetBytes(header);
                        stream.Write(headerbytes, 0, headerbytes.Length);
                        using (FileStream fileStream = new FileStream(files[i], FileMode.Open, FileAccess.Read))
                        {
                            while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                            {
                                stream.Write(buffer, 0, bytesRead);
                            }
                        }
                    }
                }
                
                //1.3 form end
                stream.Write(endbytes, 0, endbytes.Length);
            }
            //2.WebResponse
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using (StreamReader stream = new StreamReader(response.GetResponseStream()))
            {
                return stream.ReadToEnd();
            }
        }
    }

   
}