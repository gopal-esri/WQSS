using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;
using WQSS.Models;
using System.Data.Entity;
using System.Data.Entity.Validation;

namespace WQSS.Controllers
{
    public class HomeController : Controller
    {   
        public ActionResult Index()
        {
            return View();

        }

        public ActionResult Label(string objID, string sch, string matrix, string loc, string prg, string postal, 
                                    string bfaf, string isds, string login, string sampleID, string spm, string ros,string type)
        {
            // Default data passed to label
            ViewBag.logo = Generate_Logo();
            //ViewBag.logo2 = Generate_Logo_2();
            //ViewBag.logo3 = Generate_Logo_3();
            ViewBag.collectedDate = System.DateTime.Now;
            ViewBag.location = loc;
            ViewBag.sample_pt_name = spm;
            ViewBag.matrix = matrix;
            ViewBag.reas_sampling = ros;


            // For Routine-Routine : Print label
            if (type == "Routine-Routine") // Need to change the condition to check for routine routine
            {
                ViewBag.data_matrix = Generate_Code(sampleID);
                ViewBag.bottleID = sampleID;
            }

            // For Routine-Adhoc / Adhoc-Adhoc : Login_routine/adhoc -> Requery_request -> print label
            if ((type == "Routine-Adhoc") || (type == "Adhoc-Adhoc"))
            {
                Dictionary<string, string> login_type = new Dictionary<string, string>
                {
                    {"Routine-Adhoc","LOGIN_ROUTINE"},
                    {"Adhoc-Adhoc","LOGIN_ADHOC"}
                };

                // Check if sample ID is already obtained. If so, fetch from DB and print.
                try
                {
                    using (var context = new PUBWQSSEntities())
                    {
                        var query = context.dbo_Request.Where(s => s.ObjectID == objID).FirstOrDefault<dbo_Request>();

                        if (query.Result != null)
                        {
                            ViewBag.Error_1_message = "Login Successful";
                            ViewBag.Error_2_message = "Retrieve Successful";
                            ViewBag.data_matrix = Generate_Code(query.Result);
                            ViewBag.bottleID = query.Result;
                            return View();
                        }
                    }
                }
                catch { }


                // Sample ID is not in DB, Generating parameters to query REST
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add("WQSS_REQ_ID", $"LOGIN_{objID}");
                parameters.Add("WQSS_REQ_NAME", login_type[type]); 

                parameters.Add("WQSS_SCHEDULE", sch ?? "");
                parameters.Add("WQSS_MATRIX", matrix ?? "");
                parameters.Add("WQSS_LOCATION", loc ?? "");
                parameters.Add("WQSS_POINT", spm ?? "");
                parameters.Add("WQSS_PROGRAM", prg ?? "");

                // Login_Routine Query
                var result_from_lims = "";
                var lims_req_id = "";
                try
                {
                    Dictionary<string, string> result = Request_Login(parameters);
                    /*
                    Dictionary<string, string> result = new Dictionary<string, string>
                    {
                        {"Response","PENDING"},
                        {"LIMS_REQ_ID","123-234-345"}
                    };
                    */

                    result_from_lims = result["Response"];
                    lims_req_id = result["LIMS_REQ_ID"];


                    if (result_from_lims != "PENDING")
                    {
                        ViewBag.Error_1_message = "Failed to Login Request";
                        ViewBag.Error_2_message = "-";
                        ViewBag.bottleID = "";
                        return View();
                    }
                }
                catch
                {
                    ViewBag.Error_1_message = "Unable to reach LIMS";
                    ViewBag.Error_2_message = "-";
                    ViewBag.bottleID = "";
                    return View();
                }
                ViewBag.Error_1_message = "Login Successful";


                // Write to DB 
                try
                {
                    using (var context = new PUBWQSSEntities())
                    {
                        var query = context.dbo_Request.Where(s => s.ObjectID == objID).FirstOrDefault<dbo_Request>();

                        if (query == null)
                        {
                            var data = new dbo_Request()
                            {
                                ObjectID = objID,
                                WQSS_REQ_ID = objID,
                                LIMS_REQS_ID = lims_req_id,
                                Response = result_from_lims
                            };
                            context.dbo_Request.Add(data);
                            context.SaveChanges();
                        }
                    }
                }
                catch { }


                var retry = 0;
                while (result_from_lims == "PENDING")
                {
                    if (retry < 5)
                    {
                        System.Threading.Thread.Sleep(1000);
                        result_from_lims = Get_Sample_ID($"LOGIN_{objID}", lims_req_id);
                        //result_from_lims = "PENDING";

                        retry++;
                    }
                    else
                    {
                        ViewBag.Error_2_message = "Failed to retrieve Sample ID";
                        ViewBag.bottleID = "";
                        return View();
                    }
                }
                ViewBag.Error_2_message = "Retrieve Successful";


                try
                {
                    // Update DB
                    using (var context = new PUBWQSSEntities())
                    {
                        var data = context.dbo_Request.SingleOrDefault(b => b.ObjectID == objID);
                        if (data != null)
                        {
                            data.Result = result_from_lims;
                            data.Response = "RECEIVED";
                            context.SaveChanges();
                        }
                    }
                }
                catch { }

                //Pass data to view
                ViewBag.data_matrix = Generate_Code(result_from_lims);
                ViewBag.bottleID = result_from_lims;

            }
            
            return View();
        }
        public ActionResult Create()
        {
            ViewBag.status = "0";
            //ViewBag.logo2 = Generate_Logo_2();
            //ViewBag.logo3 = Generate_Logo_3();
            return View();
        }

        [HttpPost]
        public ActionResult Create(LabelForm LF)
        {
            ViewBag.logo = Generate_Logo();
            //ViewBag.logo2 = Generate_Logo_2();
            //ViewBag.logo3 = Generate_Logo_3();

            ViewBag.status = "1";
            ViewBag.bottleID = LF.SampleID;
            ViewBag.collectedDate = LF.CollectedDate + " " + LF.CollectedTime;
            ViewBag.location = LF.Location;
            ViewBag.sample_pt_name = LF.Sample_Pt;
            ViewBag.matrix = LF.Matrix;
            ViewBag.reas_sampling = LF.Reason;
            if (LF.SampleID != null)
            {
                ViewBag.data_matrix = Generate_Code(LF.SampleID);
            }

            return View(LF);
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
        public string Generate_Code(string sampleID)
        {
            DataMatrix.net.DmtxImageEncoder encoder = new DataMatrix.net.DmtxImageEncoder();
            Bitmap bmp = encoder.EncodeImage(sampleID);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            using (var ms = new MemoryStream())
            {
                using (var bitmap = new Bitmap(bmp))
                {
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    return Convert.ToBase64String(ms.GetBuffer()); 
                }
            }
        }

        public string Generate_Logo()
        {
            //string url = WebConfigurationManager.AppSettings["LocalPath"]; // Enter "QAPath"/"LocalPath"
            //byte[] logo = System.IO.File.ReadAllBytes($@"{url}\Views\Home\Resource\pub_logo.png");
            var item = "data:image/png;base64,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";
            //return Convert.ToBase64String(logo);
            return item;
        }
        /*
        public string Generate_Logo_2()
        {
            string url = WebConfigurationManager.AppSettings["LocalPath"]; // Enter "QAPath"
            byte[] logo = System.IO.File.ReadAllBytes($@"{url}\Views\Home\Resource\pub_logo2.png");
            return Convert.ToBase64String(logo);
        }
        public string Generate_Logo_3()
        {
            string url = WebConfigurationManager.AppSettings["LocalPath"]; // Enter "QAPath"
            byte[] logo = System.IO.File.ReadAllBytes($@"{url}\Views\Home\Resource\qr.png");
            return Convert.ToBase64String(logo);
        }*/

        public Dictionary<string, string> Request_Login(Dictionary<string,string> input)
        {
            //  HttpWebRequest 
            Uri url = new Uri("https://limsstgweb01.pub.gov.sg/starlims11.uat/REST_API.WQSS.LIMS");
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            //webrequest.Method = "POST";
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";

            webrequest.Headers.Add("STARLIMSUser", "WQSS");
            webrequest.Headers.Add("STARLIMSPass", "WQSS");
            webrequest.Headers.Add("WQSS_REQ_ID", input["WQSS_REQ_ID"]);
            webrequest.Headers.Add("WQSS_REQ_NAME", input["WQSS_REQ_NAME"]);


            webrequest.Headers.Add("WQSS_SCHEDULE", input["WQSS_SCHEDULE"]);
            webrequest.Headers.Add("WQSS_MATRIX", input["WQSS_MATRIX"]);
            webrequest.Headers.Add("WQSS_LOCATION", input["WQSS_LOCATION"]);
            webrequest.Headers.Add("WQSS_POINT", input["WQSS_POINT"]);
            webrequest.Headers.Add("WQSS_PROGRAM", input["WQSS_PROGRAM"]);

            // Optional Parameter
            if (input["WQSS_REQ_NAME"] == "Adhoc-Adhoc")
            {
                //input["sth"] ?? webrequest.Headers.Add("sth", input["sth"]);
            }


            //  Declare & read the response from service
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();

            // Fetch the response from the POST web service
            Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader loResponseStream = new StreamReader(webresponse.GetResponseStream(), enc);

            Dictionary<string, string> output = new Dictionary<string, string>
            {
                {"Response",loResponseStream.ReadToEnd() },
                {"LIMS_REQ_ID",webresponse.GetResponseHeader("LIMS_REQ_ID") }
            };

            loResponseStream.Close();
            webresponse.Close();

            return output;
        }

        public string Get_Sample_ID(string req_id, string lims_id)
        {
            Uri url = new Uri("https://limsstgweb01.pub.gov.sg/starlims11.uat/REST_API.WQSS.LIMS");
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            //webrequest.Method = "POST";
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";

            webrequest.Headers.Add("STARLIMSUser", "WQSS");
            webrequest.Headers.Add("STARLIMSPass", "WQSS");

            webrequest.Headers.Add("WQSS_REQ_NAME", "REQUERY_REQUEST");
            webrequest.Headers.Add("WQSS_REQ_ID", req_id);
            webrequest.Headers.Add("PENDING_LIMS_REQUEST", lims_id);

            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();

            // Fetch the response from the POST web service
            Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader loResponseStream = new StreamReader(webresponse.GetResponseStream(), enc);

            string output = loResponseStream.ReadToEnd();

            loResponseStream.Close();
            webresponse.Close();

            return output;
        }
    }
}   