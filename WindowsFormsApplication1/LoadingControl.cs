using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WorkspaceLoginManagement
{
    //reference: https://www.cnblogs.com/lijuanfei/p/6080594.html
    public partial class LoadingControl : Form
    {

        public delegate void mydelegate();
        public mydelegate eventMethod;

        private static LoadingControl pLoading;
        Thread t;
        delegate void CloseFormCallback();
        public LoadingControl()
        {
            InitializeComponent();
            this.ControlBox = false;   // 设置不出现关闭按钮
            this.StartPosition = FormStartPosition.CenterParent;


        }

        public static LoadingControl getLoading()
        {
            if (pLoading == null || pLoading.IsDisposed)
            {
                pLoading = new LoadingControl();

            }
            return pLoading;
        }

        private void LoadingControl_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!this.IsDisposed)
            {
                this.Dispose(true);
            }
        }


        private void delegateEventMethod()
        {
            eventMethod();
        }



        //这种方法演示如何在线程安全的模式下调用Windows窗体上的控件。  
        /// <summary>
        /// 设置Loading 窗体的 标题title,标签 caption 和描述 description
        /// </summary>
        /// <param name="caption">标签（例如:please wait）[为空时，取默认值]</param>
        /// <param name="description">描述(例如：正在加载资源...)[为空时，取默认值]</param>
        public void SetCaptionAndDescription(string title, string caption, string description)
        {
            Action<string, string> action = (caption_data, description_data) =>
            {
                if (caption_data != "") { pLoading.lbl_caption.Text = caption_data.ToString(); }
                if (description_data != "") { pLoading.lbl_description.Text = description_data.ToString(); }
            };
            if (pLoading.IsHandleCreated)
            {
                Invoke(action, caption, description);
            }
        }



        public void CloseLoadingForm()
        {
            if (this.InvokeRequired)
            {
                CloseFormCallback d = new CloseFormCallback(CloseLoadingForm);
                this.Invoke(d, new object[] { });
            }
            else
            {
                if (!this.IsDisposed)
                {
                    this.Dispose(true);
                }
            }
        }

        public void SetExecuteMethod(mydelegate method)
        {
            this.eventMethod += method;
            if (t != null && t.IsAlive) { t.Abort(); MessageBox.Show("error"); }
            t = new Thread(new ThreadStart(delegateEventMethod));
            t.IsBackground = true;
            t.Start();
        }
    }
}
