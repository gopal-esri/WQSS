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
            try
            {
                // Convert our JSON in into bytes using ascii encoding
                ASCIIEncoding encoding = new ASCIIEncoding();
                //byte[] data = encoding.GetBytes(tbJSONdata.Text);

                //  HttpWebRequest 
                Uri url = new Uri(WebConfigurationManager.AppSettings["LIMS_URL"]);
                HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
                //webrequest.Method = "POST";
                webrequest.Method = "GET";
                webrequest.ContentType = "application/x-www-form-urlencoded";
                webrequest.Headers.Add("STARLIMSUser", "WQSS");
                webrequest.Headers.Add("STARLIMSPass", "WQSS");
                webrequest.Headers.Add("WQSS_REQ_ID", "chrom12345wqss1912");
                webrequest.Headers.Add("WQSS_REQ_NAME", "ROUTINE_FILE");


                //  Declare & read the response from service
                HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();

                // Fetch the response from the POST web service
                Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
                StreamReader loResponseStream = new StreamReader(webresponse.GetResponseStream(), enc);
                string result = loResponseStream.ReadToEnd();
                loResponseStream.Close();

                webresponse.Close();
                ViewBag.output = result;

                return View();
            }
            catch (Exception ex)
            {
                ViewBag.output = ex.ToString();
                return View();
                //throw;
            }


        }

        public ActionResult Label(string objID, string sch, string matrix, string loc, string prg, string postal, 
                                    string bfaf, string isds, string login, string sampleID, string spm, string ros,string type)
        {
            // Offline Test Mode
            bool test_mode = false;

            // Default data passed to label
            ViewBag.logo = Generate_Logo();
            ViewBag.logo2 = Generate_Logo_2();
            ViewBag.logo3 = Generate_Logo_3();
            ViewBag.collectedDate = System.DateTime.Now;
            ViewBag.location = loc;
            ViewBag.sample_pt_name = spm;
            ViewBag.matrix = matrix;
            ViewBag.reas_sampling = ros;


            // For Routine-Routine : Print label
            if (type == "Routine-Routine") 
            {
                ViewBag.data_matrix = Generate_Code(sampleID);
                ViewBag.bottleID = sampleID;
            }

            // For Routine-Adhoc / Adhoc-Adhoc : Login_routine/adhoc -> Requery_request -> print label
            if ((type == "Adhoc-Routine") || (type == "Adhoc-Adhoc"))
            {
                Dictionary<string, string> login_type = new Dictionary<string, string>
                {
                    {"Adhoc-Routine","LOGIN_ROUTINE"},
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
                            if (query.Result != "PENDING" && query.Result != "Processing")
                            {
                                ViewBag.Error_1_message = "Login Successful";
                                ViewBag.Error_2_message = "Retrieve Successful";
                                ViewBag.data_matrix = Generate_Code(query.Result);
                                ViewBag.bottleID = query.Result;
                                return View();
                            }
                            else
                            {
                                var result = Get_Sample_ID($"LOGIN_{objID}_ID", query.LIMS_REQS_ID).Replace("\"", "");
                                if (result != "Processing" || result != "PENDING")
                                {
                                    ViewBag.Error_1_message = "Login Successful";
                                    ViewBag.Error_2_message = "Retrieve Successful";
                                    ViewBag.data_matrix = Generate_Code(result);
                                    ViewBag.bottleID = result;

                                    try
                                    {
                                        // Update DB
                                        var data = context.dbo_Request.SingleOrDefault(b => b.ObjectID == objID);
                                        data.Result = result;
                                        data.Response = "RECEIVED";
                                        context.SaveChanges();
                                    }
                                    catch (Exception ex)
                                    {
                                        ViewBag.output = ex.ToString();
                                        return View();
                                    }

                                    return View();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.output = ex.ToString();
                }


                // Sample ID is not in DB, Generating parameters to query REST
                Dictionary<string, string> parameters = new Dictionary<string, string>();

                parameters.Add("WQSS_REQ_ID", $"LOGIN_{objID}");
                parameters.Add("WQSS_REQ_NAME", login_type[type]); 

                parameters.Add("WQSS_SCHEDULE", sch ?? "");
                parameters.Add("WQSS_MATRIX", matrix ?? "");
                parameters.Add("WQSS_LOCATION", loc ?? "");
                parameters.Add("WQSS_POINT", spm ?? "");
                parameters.Add("WQSS_PROGRAM", prg ?? "");

                parameters.Add("WQSS_POSTAL_CODE", postal ?? "nil");
                parameters.Add("WQSS_BF_AF", bfaf ?? "nil");
                parameters.Add("WQSS_IS_DS", isds ?? "nil");

                // Login_Routine Query
                var result_from_lims = "";
                var lims_req_id = "";

                try
                {
                    Dictionary<string, string> result = new Dictionary<string, string>();
                    if (test_mode == false)
                    {
                        result = Request_Login(parameters);
                    }
                    else
                    {
                        result = new Dictionary<string, string>
                        {
                            {"Response","PENDING"},
                            {"LIMS_REQ_ID","123-234-345"}
                        };
                    }
                    
                    

                    result_from_lims = result["Response"].Replace("\"", "");
                    lims_req_id = result["LIMS_REQ_ID"].Replace("\"","");


                    if (result_from_lims != "PENDING")
                    {
                        ViewBag.Error_1_message = "Failed to Login Request" + result_from_lims;
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
                                WQSS_REQ_ID = "LOGIN_" + objID,
                                LIMS_REQS_ID = lims_req_id,
                                Response = result_from_lims,
                                Uploaded = "no"
                            };
                            context.dbo_Request.Add(data);
                            context.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    ViewBag.output = ex.ToString();
                    return View();
                    //throw;
                }


                var retry = 0;
                while (result_from_lims == "PENDING" || result_from_lims == "Processing")
                {
                    if (retry < 3)
                    {
                        System.Threading.Thread.Sleep(3000);
                        if (test_mode == false)
                        {
                            result_from_lims = Get_Sample_ID($"LOGIN_{objID}_ID", lims_req_id).Replace("\"", "");
                        }
                        else
                        {
                            result_from_lims = test();
                        }

                        retry++;
                    }
                    else
                    {
                        ViewBag.Error_2_message = "Failed to retrieve Sample ID - Please try again in 2 mins";
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
                catch (Exception ex)
                {
                    ViewBag.output = ex.ToString();
                }

                //Pass data to view
                ViewBag.data_matrix = Generate_Code(result_from_lims);
                ViewBag.bottleID = result_from_lims;

            }
            
            return View();
        }
        public ActionResult Create()
        {
            ViewBag.status = "0";
            ViewBag.logo2 = Generate_Logo_2();
            ViewBag.logo3 = Generate_Logo_3();
            return View();
        }

        [HttpPost]
        public ActionResult Create(LabelForm LF)
        {
            ViewBag.logo = Generate_Logo();
            ViewBag.logo2 = Generate_Logo_2();
            ViewBag.logo3 = Generate_Logo_3();

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
            try
            {
                using (var context = new PUBWQSSEntities())
                {
                    var query = context.dbo_Request.Where(s => s.ObjectID == "test").FirstOrDefault<dbo_Request>();

                    if (query != null)
                    {
                        ViewBag.output1 = query.ObjectID;
                        ViewBag.output2 = query.Response;
                        ViewBag.output3 = query.Result;
                        ViewBag.output4 = query.LIMS_REQS_ID;
                    }
                }
            }
            catch (Exception ex)
            {
                ViewBag.output = ex.ToString();
                //throw;
            }

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
            var item = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAJkAAAFeCAYAAACWxr0CAAAgAElEQVR4Xu3dCdg1dVk/8HnbtU2pNC0FlRIhrRTXZDFcMcFEBTOVShRFLAExUTBLEStMElMULNEIwTQhJTHIBbRcyg1LUdEKLVOWtMU2/9fnp/fz/73D/GbmrM+cc+a+rud63+c558yZueeee/ne246vfe1rX6tGGjmwQA7sGIVsgdwdD504MFch+9d//dfq2muvrS6++OLqiCOOGFk8cmC+QvbFL36x+p3f+Z3qb//2b6uPf/zj1Xvf+97qO7/zO0c2jxyYXpN97nOfq/7qr/6qevWrX1194AMfqAjZf/7nf26x9Etf+lK1yy67jCweOTCZkL32ta+tXvGKV1QE7B/+4R+q//qv/9qJhd/8zd9c3fa2t63uc5/7VGecccbI3pED7ebyv//7v6uLLrqouvTSS6s/+qM/qq655prqf/7nf7bYtmPHjupbvuVbqu/6ru9KQvWUpzylutWtblXd5ja3qQjbSCMHggNFx/8nf/Inqw9+8IM7cYpg3fzmN6/23nvv6oADDqiOPvro9PooVKNAtXGgKGSE6Qtf+MLWZ7/pm76puvvd7169+93vHjk6cmAiDhSF7PnPf371spe9LDn0fK/AbL/t274tRY1M5F3vetfqvve9b9JsI40cKHGgEycTMb7yla+s3vzmN1ef+cxnqk9/+tMVfy3oO77jO5Kz/4M/+IPVQQcdVD30oQ+tdt1115HjIwf+v/8+KeL/0Y9+tHrf+96Xfi688MIUaf7f//3f16OIbwQDt771ratPfvKTI5tHDrRHl334c8IJJ1QvfelLqy9/+ctb5jSE7d///d8rWm6kkQOd5hKLmEwCQ6Be//rXV5///Oerq6666gY4GU3GZ7vLXe5SXX755SN3Rw60azJC9I53vKM666yzEqLP+f/f//3fndhGoL792789wRpHHXVUdfjhh1c3vvGNE6QxwhqjhAUHiprsh3/4h6urr756J05967d+a/UjP/Ij6ecOd7hD9aQnPanif400cqCNA72FjCkkUH/+539e7b777iNXRw705kBRyF7zmtdUf/iHf1h9+MMfTlhZwBaEjYn80R/90QRZ3Pve964e8IAH9P7C8Y2bx4Fejj/t9Sd/8ifVhz70oQRdfPWrX93iFN/rB37gBxI29qxnPat6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";
            //return Convert.ToBase64String(logo);
            return item;
        }
        
        public string Generate_Logo_2()
        {
            var logo2 = "data:image/png;base64,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rvKZz3wmOcuaKIxez3AahxKWxeLtquVyXdFAfw1MFfA6KQF3KewpALfJugXATSB86KGHJtCW5TUL4l2UuuzFxzouiJNtEO71sssuS2exFeCdhRFS3mGhamBqgPeBBx5IRcg//elPJws3froAL2BaZ511qvPPP7/zYZQLocOOO+646g1veEPfI4uk8zr9uKsAaovbL3/5y663pPPnisXbWV3lwqKBVg1MDfBypp1wwglLeN2otxB1GJq43vibo9ZZhuJbZ0k4F9VX+NWvftX4WhanY489tnrta1/b+bWvuuqqlHzRBXiD4inA21m95cKigU4amArgBQYy0xQxJ8DWpM/r6UZSRNAMAbp+77ffftVb3vKWgbjOTtqZ54vUZaCXfo4w1usFF1zQuaWDAG88tABvZ/WWC4sGOmlg3oGXE+31r399oglITjM0FTLPgde1a665ZiqGPqlz0jppdUQXcay97GUv6wu8kihkonWVK6+8MlENsuO6ipOPr7jiipGegNz1u8t1RQOzqIF5B16nATsdwXY6EiX85qnPEyei5m4Ar884lWyzFcCZRekCvOJ477rrrs6vjxNWLKirc42+VXa76KKLSk3ezlouFxYN9NfAvAKv5AAn2cqK+t3vfreUxRt0gz8GrQB8498+d/T4ueeeWzkiZxZFVTA0SiSRNL2jI+nVs3BKcheJcDL0RVv2WuheeN6ZZ55ZqWlcpGigaGDuGpg34AW0gEX5w3oUQ/C79VThHHjxjs4D22qrreauhSl9guy7N73pTUsWpaZmAlwRB094whM6vQVaRmnJrkcAAWcOPgktTsIoUjRQNDB3Dcwb8KrFsM0226SU2EgAyKmGJosXWAcoA9zTTz99qLq0c1fbZJ4A8FRX6ydSh4WICafrIhYr8cG/+c1vulyerGIRJ+igIkUDRQOj0cC8AK9jaFheJrOwJpM7HGm5tZsXanFN8LzOHUMxoBpmWXCxIhv6ie0/3najjTbqpIrDDjssHSHUVeidrmXRjUukiYvf1r8PechDep6hN8z3G0NOIPHj3LhROWEZAepE48rrhkPezojGMebXWmutzpRQ27vasUj/bisCZV7xn6hbosoc/XYRz7/66qvTQbK+owst1eW5cY1+MY8ZDnw1drCLSeYFeCVL2O7qWAOS5AkTYe3mVEMAr0EgYUDM76x3lnhbKcFtwCtE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QdWRRx45dLMBrrH81re+Nc37ScgS4HX6gu3pxz/+8WrVVVdNFlIOrD73dyuQYuDONLMK5eIFfH733Xens8kGEVv7W2+9Na3W66677iC3jvRa7V9xxRXTKRXzaeHmL8WaVKSdOP2YddK0qwDIhxxySPrMScwAl8Xr93333Vc973nPS1b8MOI5TmB2igexK9p3332rLbfcsvrNb39bffnGG6uzzz47nXu3//77z2kiDNO+WbzH1v8zn/lMOtXkBz/4QU/LF2g4iFWfb7LJJtXVV189kDrMaye1OEHFPJ8m2XnnnasPf/jDaUFxykuI/8fC/5a3vKWy8AwrzkxkSFi4XvSiFw37mIHuWwK8gPRtb3tbsqYArgnEwg058cQTEzDHselOasi3lJ/61Keq7bffPpnrrKw6KLe1KkDD/Y7pmS8B/oBfp37hC18YKSc6zDs5Pw5wxnlvVnjgyXqtSxPw6leHcs4VeB11/7nPfS5NAFa/BbrIeDWALkID0DmKCFXUJIDWAgg8UGEMhkEkgNcC74zCaZIA3rbFZ5ra3KUtS3G8999/f7XyyiunDrzqqquqpz71qUuesfnmm6e/hZxwwgnJsgnZaKON0hHrW2yxRXXFFVcs9d1f/vKXk3MHoG6wwQbpSB08ZAA7C5mZj+4AvLbJLGCHR9b5QgPDdtnnKA/bsLo08cxAA39tgDbxpIBWO1AKe+21Vxrs7llppZXSv/GluZWpDUDaArP11ltXG264YRd9p2sAJz5t7bXXbr3HYmZRi90HzvnZz352Ota+/h6A99BDD03OrrB4c+A1iM8777zG77z33nvT1rZJLrvssmqbbbZJixEr7JJLLmltd34BvQNqi4jveMlLXpLa2Cbuu+iii5LFB/jr4jRpTiHn51mcnvOc56TdWJP4fta665oWraZ7WJksTg5VW/c252HTCda28L7bwomCGYT/tutac801K33j8NSbb7658d1Ye6ecckq13HLLJcog99EAb/f5m75r4jHNR30ziMXb67TuaKADZvkB9IeFeli/z/Of//w0X1il+tvhsoPKPffck/oRRjmIti6hV7uGJmy49NJLq+uuuy59ZkcB40Jg5le/+tXUT4985CN7Ns2cMeddYyw9KKoB8AIgk/tjH/vYkgdpMP4WCBlEQFNnhaAWDNSLL744TVJi0u+4446JDzaJiMYDK4OE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";
            return logo2;
        }
        public string Generate_Logo_3()
        {
            var logo3 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAJwAAACcAQMAAACwdlAOAAAABlBMVEX///8AAABVwtN+AAAAmklEQVRIic2VUQ6AMAhDl+z+NyZBpVTUC/jQLPPNj1IB1/o1dnwvLMzrPne1FCXC13NJh0JpN+iXgLC/f47HSDjWSvkOJszMUbzsNA9WAtVjddxe86BHQMjidIXQoB1+LkRYtSDpuz1mwp5V+g1YPA+G5C9noObiwQyPp+quOyMgVD3kqCfC8DCdHRKqt3a6cu9ioMFvBBL+Ggf/3TsMvqISzwAAAABJRU5ErkJggg==";
            return logo3;
        }

        public Dictionary<string, string> Request_Login(Dictionary<string,string> input)
        {
            //  HttpWebRequest 
            Uri url = new Uri(WebConfigurationManager.AppSettings["LIMS_URL"]);
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            //webrequest.Method = "POST";
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";

            webrequest.Headers.Add("STARLIMSUser", "WQSS");
            webrequest.Headers.Add("STARLIMSPass", "WQSS");
            webrequest.Headers.Add("WQSS_REQ_ID", input["WQSS_REQ_ID"]);
            webrequest.Headers.Add("WQSS_REQ_NAME", input["WQSS_REQ_NAME"]);


            webrequest.Headers.Add("WQSS_SCHEDULE", input ["WQSS_SCHEDULE"]);
            webrequest.Headers.Add("WQSS_MATRIX", input["WQSS_MATRIX"]);
            webrequest.Headers.Add("WQSS_LOCATION", input["WQSS_LOCATION"]);
            webrequest.Headers.Add("WQSS_POINT", input["WQSS_POINT"]);
            webrequest.Headers.Add("WQSS_PROGRAM", input["WQSS_PROGRAM"]);

            // Optional Parameter
            if (input["WQSS_REQ_NAME"] == "LOGIN_ADHOC")
            {
                
                //input["sth"] ?? webrequest.Headers.Add("sth", input["sth"]);
                webrequest.Headers.Add("WQSS_SAMPLE_NO", "1");
                if (input["WQSS_POSTAL_CODE"] != "nil") { webrequest.Headers.Add("WQSS_POSTAL_CODE", input["WQSS_POSTAL_CODE"]); }
                if (input["WQSS_BF_AF"] != "nil") { webrequest.Headers.Add("WQSS_BF_AF", input["WQSS_BF_AF"]); }
                if (input["WQSS_IS_DS"] != "nil") { webrequest.Headers.Add("WQSS_IS_DS", input["WQSS_IS_DS"]); }
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
            Uri url = new Uri(WebConfigurationManager.AppSettings["LIMS_URL"]);
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

        public string test()
        {
            return "sasdas\"\"";
        }
    }
}   