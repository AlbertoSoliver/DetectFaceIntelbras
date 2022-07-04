using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp_cameras
{
    class Program
    {
        static void Main(string[] args)
        {

            monitorar("IP:PORTA");
            //Task.Run(() => monitorar(""));

            Console.ReadKey();
        }

        static void monitorar(string cameraIp)
        {
            string responseString = string.Empty;
            var httpWebRequestCameraSnapManager = (HttpWebRequest)WebRequest.Create($"http://{cameraIp}/cgi-bin/snapManager.cgi?action=attachFileProc&Flags[0]=Event&Events=[FaceDetection]&heartbeat=1");
            httpWebRequestCameraSnapManager.Credentials = new NetworkCredential("__USER__", "__SENHA___");
            //httpWebRequestCameraSnapManager.UserAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 72.0.3626.121 Safari / 537.36";
            //httpWebRequestCameraSnapManager.AllowReadStreamBuffering = true;
            //httpWebRequestCameraSnapManager.Method = "GET";
            //httpWebRequestCameraSnapManager.ContentType = "image/jpeg";

            HttpWebResponse httpResponse = (HttpWebResponse)httpWebRequestCameraSnapManager.GetResponse();


            if (httpResponse.StatusCode == HttpStatusCode.OK)
            {
                Console.WriteLine($"status code {httpResponse.StatusCode}, Content-Type : {httpResponse.ContentType}");

                var contentType = httpResponse.ContentType;

                if (contentType.IndexOf("boundary=") < 0)
                    return; // "not multipart"

                var streamDados = httpResponse.GetResponseStream();

                //<1> read part header
                while (true)
                {
                    var buffer = new byte[8];
                    var data = new List<byte>();

                    var idx = 0;
                    var seq = 1;

                    while (true)
                    {
                        var lenRead = 0;
                        while (lenRead < buffer.Length)
                            lenRead += streamDados.Read(buffer, lenRead, buffer.Length - lenRead);

                        for (int i = 0; i < buffer.Length; i++)
                            data.Add(buffer[i]);

                        idx = Encoding.Latin1.GetString(data.ToArray()).IndexOf("\r\n\r\n");

                        var dataNew = new byte[data.ToArray().Length - 4];
                        if (idx == 0)
                        {
                            for (int i = 4; i < data.ToArray().Length; i++)
                                dataNew[i - 4] = data[i];
                            data.Clear();
                            data.AddRange(dataNew);
                            continue;
                        }

                        if (idx > 0)
                        {

                            //<2> parse part header, get Content-Length

                            var headerAll = "";
                            for (int i = 0; i < idx + 4; i++)
                                headerAll += Encoding.Latin1.GetString(new byte[] { data[i] });
                            var idx_contetType = headerAll.IndexOf("Content-Type: image/jpeg");
                            var idx_contetLen = headerAll.IndexOf("Content-Length", idx_contetType + 1);
                            if (idx_contetType >= 0)
                            {
                                if (idx_contetLen >= 0)
                                {
                                    var cend = headerAll.IndexOf("\r\n", idx_contetLen);
                                    var clen = Convert.ToInt32(headerAll.Substring(idx_contetLen, cend - idx_contetLen).Split(":")[1].Trim());

                                    if (clen > 0)
                                    {
                                        dataNew = new byte[data.Count - (idx + 4)];
                                        for (int i = idx + 4; i < data.ToArray().Length; i++)
                                            dataNew[i - (idx + 4)] = data[i];
                                        data.Clear();
                                        data.AddRange(dataNew);

                                        var needlen = clen - data.Count;

                                        if (needlen > 0)
                                        {

                                            dataNew = new byte[needlen];
                                            lenRead = 0;
                                            while (lenRead < needlen)
                                                lenRead += streamDados.Read(dataNew, lenRead, needlen - lenRead);
                                            data.AddRange(dataNew);

                                            File.WriteAllBytes(@$"C:\temp\fotosfaces\fotos-teste\foto_Entrada_" + DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString() + "_" + seq.ToString() + "_.jpg", data.ToArray());
                                            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} face detectada.");

                                            data.Clear();
                                        }
                                    }
                                }
                            }

                            Console.WriteLine($"{DateTime.Now.ToLongTimeString()} recv Heartbeat - { data.ToArray().Length }");

                            break;

                        }
                        if (data.ToArray().Length > 72 && idx <= 0)
                            break;

                        seq++;
                    }
                }
            }
        }

        static void GetPhoto(string camera)
        {
            var httpWebRequestCameraSnapShot = (HttpWebRequest)WebRequest.Create($"http://{camera}/cgi-bin/snapshot.cgi?channel=1");
            httpWebRequestCameraSnapShot.Credentials = new NetworkCredential("admin", "SIRIUS@1234");
            httpWebRequestCameraSnapShot.Method = "GET";

            HttpWebResponse httpResponseCamera = (HttpWebResponse)httpWebRequestCameraSnapShot.GetResponse();

            if (httpResponseCamera.StatusCode == HttpStatusCode.OK)
            {
                byte[] data = new byte[0];
                using (MemoryStream ms = new MemoryStream())
                {
                    httpResponseCamera.GetResponseStream().CopyTo(ms);
                    data = ms.ToArray();
                }

                Task.Delay(1000);
                File.WriteAllBytes(@$"C:\temp\fotosfaces\fotos-teste\foto" + DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString() + ".Jpeg", data);
            }
        }
        static void GetPhoto1(byte[] imageBytes)
        {
            File.WriteAllBytes(@$"C:\temp\fotosfaces\fotos-teste\foto" + DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString() + ".Jpg", imageBytes);
        }
        static void GetPhotoBin(char[] imagePhoto)
        {
            var buff = new byte[imagePhoto.Length];
            var ii = 0;
            for (int i = 0; ii < imagePhoto.Length; i++)
            {
                if (imagePhoto[i] <= 255)
                {
                    buff[ii] = (byte)imagePhoto[i];
                    ii++;
                }
                else
                {
                    buff[ii] = (byte)(imagePhoto[i] >> 8); // delocar 8 bits para direita para pagar o byte mais alto ou da esquerda (sistema little endian)
                    buff[ii + 1] = (byte)(imagePhoto[i] & (2 ^ 8 - 1)); // mascara o 8 bits da esquerda para pegar o byte mais baixo ou da direita (sistema little endian)

                    ii += 2;

                }

            }

            File.WriteAllBytes(@$"C:\temp\fotosfaces\fotos-teste\foto" + DateTime.Now.Second.ToString() + DateTime.Now.Millisecond.ToString() + "-buff.Jpeg", buff);

        }


    }
}
