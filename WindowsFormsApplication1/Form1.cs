using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using System.IO;
using System.Web.Script.Serialization;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;


namespace WorkspaceLoginManagement
{
    public partial class Form1 : Form
    {
        private static string loginAddress;
        private static string baseURI;
        private static string tokenId;
        private static string userName;
        private static string passWord;
        private static Dictionary<string, string> loginTokens = new Dictionary<string, string>();
        private static readonly string authURI = "auth/login";
        private static readonly string vmsURI = "desktop/getVmList";
        private static readonly string loginURI = "desktop/loginVm";

        public Form1()
        {
            InitializeComponent();
        }

        public void WriteLog(string documentName, string msg)
        {
            string errorLogFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Log");
            if (!System.IO.Directory.Exists(errorLogFilePath))
            {
                System.IO.Directory.CreateDirectory(errorLogFilePath);
            }
            string logFile = System.IO.Path.Combine(errorLogFilePath, documentName + "@" + DateTime.Today.ToString("yyyy-MM-dd") + ".txt");
            bool writeBaseInfo = System.IO.File.Exists(logFile);
            StreamWriter swLogFile = new StreamWriter(logFile, true, Encoding.Unicode);
            swLogFile.WriteLine(DateTime.Now.ToString("HH:mm:ss") + "\t" + msg);
            swLogFile.Close();
            swLogFile.Dispose();
        }

        private static bool CheckValidatioinResult(object sender, X509Certificate certificate, X509Chain chane, SslPolicyErrors errors)
        {
            return true;
        }

        public string GetTokenStr(string jsonStr, string jsonKey)
        {
            string result = "";
            JObject jResult = JObject.Parse(jsonStr);
            JToken jToken = jResult[jsonKey];
            result = jToken.ToString();

            return result;
        }

        public string HTTPSPost(string URL, Dictionary<string, object> values)
        {
            //TODO 
            string result = "";
            var request = (HttpWebRequest)WebRequest.Create(URL);
            //for https requests
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidatioinResult);
            ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;// SecurityProtocolType.Tls1.2; 
            request.KeepAlive = false;
            ServicePointManager.CheckCertificateRevocationList = true;
            ServicePointManager.DefaultConnectionLimit = 100;
            ServicePointManager.Expect100Continue = false;
            //reference: https://www.cnblogs.com/Supperlitt/p/5395338.html

            request.ContentType = "application/json";
            request.Method = "Post";

            
            try
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    string json = JsonConvert.SerializeObject(values, Formatting.Indented);

                    streamWriter.Write(json);
                }
                var response = (HttpWebResponse)request.GetResponse();
                var streamReader = new StreamReader(response.GetResponseStream());

                result = streamReader.ReadToEnd();
                streamReader.Dispose();
                streamReader.Close();
            }
            catch(WebException e)
            {
                //MessageBox.Show("网络连接失败。错误码：" + response.StatusCode.ToString());
                
                MessageBox.Show(e.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                LoadingControl.getLoading().CloseLoadingForm();
                System.Environment.Exit(0);
            }
            

            return result;
        }

        private void SetLoginTokens(string jsonstr)
        {
            //login need: sid; tokenId; farmId name

            JObject resultDict = JObject.Parse(jsonstr);
            JToken vmDict = resultDict["vms"][0];
            loginTokens.Add("state", vmDict["state"].ToString());
            loginTokens.Add("sid", vmDict["sid"].ToString());
            loginTokens.Add("farmId", vmDict["farmId"].ToString());
            loginTokens.Add("dgId", vmDict["dgId"].ToString());
            loginTokens.Add("type", vmDict["type"].ToString());
            loginTokens.Add("name", userName);
            try
            {
                loginTokens.Add("vmDomain", vmDict["vmDomain"].ToString());
            }catch(NullReferenceException e)
            {
                loginTokens.Add("vmDomain", "null");
            }
            
        }

        private void SetClientTokens(string jsonstr)
        {
            //client need:loginTicket; addressTicket; gwPort; vmName; vmDomain; farmId; gwIps
            JObject jObject = JObject.Parse(jsonstr);
            loginTokens.Add("loginTicket", jObject["loginTicket"].ToString());
            loginTokens.Add("addressTicket", jObject["addressTicket"].ToString());
            //loginTokens.Add("gwPort", jObject["gwPort"].ToString());
            loginTokens.Add("gwIps", jObject["gwIps"].ToString());
        }

        private void RunCloudClient()
        {
            string clientURL = "hwcloud://localhost/useGw=1&loginTicket={0}&addressTicket={1}&gwPort=8443&" +
                "vmName={2}&domain={3}&farmId={4}&gwIps={5}";
            clientURL = string.Format(clientURL, loginTokens["loginTicket"], loginTokens["addressTicket"], 
                loginTokens["name"], loginTokens["vmDomain"], loginTokens["farmId"], loginTokens["gwIps"]);
            WriteLog("log", String.Format("[INFO] client url created: {0}", clientURL));

            try
            {
                System.Diagnostics.Process.Start("iexplore.exe", clientURL);
            }
            catch(Exception e)
            {
                WriteLog("log", String.Format("[ERROR] Start IExplore failed: {0}", e.Message));
            }

            return;
        }

        private static string EncodeBase64(string source)
        {
            string result = "";
            byte[] strBytes = Encoding.Default.GetBytes(source);
            result = Convert.ToBase64String(strBytes);
            return result;
        }

        private static string DecodeBase64(string source)
        {
            string result = "";
            byte[] strBytes = Convert.FromBase64String(source);
            result = Encoding.UTF8.GetString(strBytes);
            return result;
        }

        private string LoginVmApi()
        {
            Dictionary<string, string> userInfo = new Dictionary<string, string>
            {
                {"userName", loginTokens["name"] },
                {"password", EncodeBase64(passWord) }
            };
            Dictionary<string, object> values = new Dictionary<string, object>
            {
                {"id", loginTokens["sid"] },
                {"tokenId", tokenId },
                {"farmId", loginTokens["farmId"] },
                {"type", loginTokens["type"] },
                {"dgId", loginTokens["dgId"] },
                {"userInfo", userInfo }
            };
            //ConvertToString()
            string url = baseURI + loginURI;
            WriteLog("log", "loginVm request body: " + values);
            string result = HTTPSPost(url, values);
            WriteLog("log", "loginVm result"+result);
            return result;
        }

        private void LoginRunningVm(string jsonstr)
        {           
            SetClientTokens(jsonstr);
            RunCloudClient();
        }

        private bool LoginUnregisteredVm()//LoadingControl p, string jsonstr
        {
            string jsonstr = LoginVmApi();
            Dictionary<string, object> unregResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonstr);
            int loginCode = Convert.ToInt32(unregResult["resultCode"]);
            Console.WriteLine(loginCode);
            if(loginCode == 0)
            {
                LoginRunningVm(jsonstr);
                return true;
            }
            return false;
        }
        private void LoginButton_Click(object sender, EventArgs e)
        {
            //Control.CheckForIllegalCrossThreadCalls = false;
            LoadingControl pLoading = LoadingControl.getLoading();
            pLoading.SetExecuteMethod(LoginButton_Work);
            pLoading.ShowDialog();
        }

        private void LoginButton_Work()
        {
            LoadingControl pLoading = LoadingControl.getLoading();
            pLoading.SetCaptionAndDescription("执行进度", "请等待", "正在获取TokenID...");
            Console.WriteLine("button clicked");
            WriteLog("log", "[INFO] Login Button Clicked");
            loginAddress = LoginAddress.Text;
            baseURI = string.Format("{0}/services/api/", loginAddress);
            string authURL = baseURI + authURI;
            WriteLog("log", "[INFO] auth URL created: " + authURL);
            //TODO: Button control
            if(loginTokens.Count != 0)
            {
                DialogResult dialogResult = MessageBox.Show("是否重新登录？", "提示", MessageBoxButtons.YesNo);
                if(dialogResult == DialogResult.Yes)
                {
                    loginTokens.Clear();
                }
                else
                {
                    LoadingControl.getLoading().CloseLoadingForm();
                    return;
                }
            }

            userName = UsernameTextBox.Text;
            passWord = PasswordTextBox.Text;

            if(string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(passWord))
            {
                MessageBox.Show("请输入用户名密码", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                LoadingControl.getLoading().CloseLoadingForm();
                return;
            };


            Dictionary<string, object> values = new Dictionary<string, object>
            {
                {"username", userName.ToLower()},
                {"password", passWord}
            };
            string result = HTTPSPost(authURL, values);
            WriteLog("log", "[INFO] auth API called: "+result);
            
            int resultCode;
            Int32.TryParse(GetTokenStr(result, "resultCode"), out resultCode);
            if(resultCode != 0)
            {
                MessageBox.Show("登录失败，请检查用户名密码。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                LoadingControl.getLoading().CloseLoadingForm();
                return;
            }
            tokenId = GetTokenStr(result, "tokenId");
            pLoading.SetCaptionAndDescription("执行进度","请等待","正在获取虚拟机状态...");
            //Console.WriteLine("token get");
            string VmListURL = baseURI + vmsURI;
            Dictionary<string, object> VmValues = new Dictionary<string, object>
            {
                { "queryType", 0 },
                {"tokenId", tokenId }
            };
            string vmResult = HTTPSPost(VmListURL, VmValues);
            WriteLog("log", "vmList result: " + vmResult);
            //log vmlist api called
            SetLoginTokens(vmResult);
            string state = loginTokens["state"];
            WriteLog("log", "VM state get, state = " + state);

            pLoading.SetCaptionAndDescription("执行进度", "请等待", "正在登陆到云桌面...");
            switch (state)
            {
                case "CONNECTED":
                    DialogResult dialogResult = MessageBox.Show("当前桌面正在使用，是否抢占登陆", "提示", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation); 
                    if(dialogResult == DialogResult.Yes)
                    {
                        LoginRunningVm(LoginVmApi());
                        Console.WriteLine("yes");
                    }
                    else if(dialogResult == DialogResult.No)
                    {
                        LoadingControl.getLoading().CloseLoadingForm();
                        return;
                    }
                    break;
                case "UNREGISTER":
                    int maxAttempt = 1;
                    while (!LoginUnregisteredVm())
                    {
                        pLoading.SetCaptionAndDescription("执行进度", "云桌面正在准备中，请稍等...", "已等待： " + maxAttempt*10 + "秒/最长等待时间5分钟");
                        Thread.Sleep(10000);
                        maxAttempt++;
                        if(maxAttempt == 36)
                        {
                            MessageBox.Show("接入云桌面失败！请联系管理员或者拨打华为云服务热线。", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
                    }
                    break;
                case "REGISTERED": case "DISCONNECTED":
                    LoginRunningVm(LoginVmApi());
                    break;
                default:
                    MessageBox.Show(string.Format("VM state unkown: {0},", state));
                    break;
            }
            LoadingControl.getLoading().CloseLoadingForm();
            RememberInput(EncodeBase64(passWord));
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                LoginAddress.Text = Properties.Settings.Default.AddressTextBox;
                UsernameTextBox.Text = Properties.Settings.Default.UNTextBox;
                chkRememberPw.Checked = Properties.Settings.Default.CheckBox;
                if (chkRememberPw.Checked)
                {
                    PasswordTextBox.Text = DecodeBase64(Properties.Settings.Default.PWTextBox);
                }
            }catch(Exception ex)
            {
                MessageBox.Show("读取用户配置失败，请联系管理员！", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                WriteLog("log", String.Format("[Error] Read User config failed ({0}). Current user: {1};\n " +
                    "Login Address text: {2}; Username text: {3}", ex.Message, Environment.UserName, Properties.Settings.Default.AddressTextBox, Properties.Settings.Default.UNTextBox));
            }
        }

        private void RememberInput(string encodedPw)
        {
            Properties.Settings.Default.AddressTextBox = LoginAddress.Text;
            Properties.Settings.Default.UNTextBox = UsernameTextBox.Text;
            if (chkRememberPw.Checked)
            {
                Properties.Settings.Default.PWTextBox = encodedPw;
            }
            Properties.Settings.Default.CheckBox = chkRememberPw.Checked;
            Properties.Settings.Default.Save();
        }
    }
}