using Newtonsoft.Json;
using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Mvc;


namespace WQSS.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
          
            

            return View();

        }

        public ActionResult Label(string bottleID, string loc, string spm, string matrix, string ros)
        {
            

            //Generate Label
            if (bottleID != null)
            {
                string url = @"D:\Projects\wqss";
                //string url = @"C:\Users\bgopal\source\repos\WQSS";

                DataMatrix.net.DmtxImageEncoder encoder = new DataMatrix.net.DmtxImageEncoder();
                Bitmap bmp = encoder.EncodeImage(bottleID);
                bmp.Save($@"{url}\Views\Home\Resource\{bottleID}.png", System.Drawing.Imaging.ImageFormat.Png);

                
                using (Image img = Image.FromFile($@"{url}\Views\Home\Resource\{bottleID}.png"))
                {
                    //rotate the picture by 90 degrees and re-save the picture as a Jpeg
                    img.RotateFlip(RotateFlipType.RotateNoneFlipY);
                    img.Save($@"{url}\Views\Home\Resource\{bottleID}.png", System.Drawing.Imaging.ImageFormat.Png);
                }
                

                byte[] imga = System.IO.File.ReadAllBytes($@"{url}\Views\Home\Resource\{bottleID}.png");
                string base64String = Convert.ToBase64String(imga);
                ViewBag.base64 = base64String;
                System.IO.File.Delete($@"{url}\Views\Home\Resource\{bottleID}.png");

                

                byte[] logo = System.IO.File.ReadAllBytes($@"{url}\Views\Home\Resource\pub_logo.png");
                string base64Stringlogo = Convert.ToBase64String(logo);
                ViewBag.base641 = base64Stringlogo;

                //ViewBag.base641 = "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";
                

                ViewBag.bottleID = bottleID;
                ViewBag.collectedDate = System.DateTime.Now;
                ViewBag.location = loc;
                ViewBag.sample_pt_name = spm;
                ViewBag.matrix = matrix;
                ViewBag.reas_sampling = ros;
            }
            
            if (bottleID == null)
            {

                
                // Convert our JSON in into bytes using ascii encoding
                ASCIIEncoding encoding = new ASCIIEncoding();
                //byte[] data = encoding.GetBytes(tbJSONdata.Text);

                //  HttpWebRequest 
                Uri url = new Uri("https://limsstgweb01.pub.gov.sg/starlims11.uat/REST_API.WQSS.LIMS");
                HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
                //webrequest.Method = "POST";
                webrequest.Method = "GET";
                webrequest.ContentType = "application/x-www-form-urlencoded";

                webrequest.Headers.Add("STARLIMSUser", "WQSS");
                webrequest.Headers.Add("STARLIMSPass", "WQSS");

                webrequest.Headers.Add("WQSS_REQ_ID", "20191029_RR_22");
                webrequest.Headers.Add("WQSS_REQ_NAME", "ROUTINE_FILE");

                /*
                webrequest.Headers.Add("WQSS_SCHEDULE", "Weekly-SR");
                webrequest.Headers.Add("WQSS_MATRIX", "Service Reservoir");
                webrequest.Headers.Add("WQSS_LOCATION", "BUKIT PANJANG SERVICE RESERVOIR");
                webrequest.Headers.Add("WQSS_POINT", "BP(1)-Inlet");
                webrequest.Headers.Add("WQSS_PROGRAM", "Weekly-SR");
                */

                //  Declare & read the response from service
                HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();

                // Fetch the response from the POST web service
                Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader loResponseStream = new StreamReader(webresponse.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                loResponseStream.Close();

                webresponse.Close();
                System.IO.File.WriteAllText(@"D:\Projects\wqss\WQSS.csv", result);
                //Console.Write(result);
                
                
            }


            return View();
        }

        public ActionResult RoutineFile()
        {
            

            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        

    }
}   