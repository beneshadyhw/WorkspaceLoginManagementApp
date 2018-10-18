using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace WorkspaceLoginManagement
{
    static class Program
    {
        private static readonly HttpClient client = new HttpClient();
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }  

        private static async Task GetMethod(string URL) 
        {
            string result = "";
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                HttpResponseMessage response = await client.GetAsync(URL);
                response.EnsureSuccessStatusCode();
                result = await response.Content.ReadAsStringAsync();

                Console.WriteLine(result);
            }
            catch(HttpRequestException e)
            {
                Console.WriteLine("Message: {0}", e.Message);
            }
            client.Dispose();
        }

        private static string PostMethod(string URL)
        {
            string result = "";

            return result;
        }
    }
}
