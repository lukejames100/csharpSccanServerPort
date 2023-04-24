using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Collections;
using System.Net.Sockets;
//ip和端口，做成一个整体数组，然后分配到30个线程里面，数组有三部分组成，ip，端口，状态（未开始扫描0，扫面关闭1，扫描打开2）
namespace csharpScanPort
{
    public partial class Form1 : Form
    {
        int sumscan = 0;
        int threadnum = 0;
        int threadsum = 30;//总线程数
        private BackgroundWorker backgroudWorker = new BackgroundWorker();
        //创建全局数组，前台计算填充，后台根据这个数组操作；
        List<string[]> globalArray = new List<string[]>();
        public Form1()
        {
            InitializeComponent();
            SetUpListView();
            SetUpBackground();
        }

        private void SetUpBackground()
        {
            backgroudWorker.WorkerReportsProgress = true;
            backgroudWorker.DoWork += backgroundWorker_DoWork;
            backgroudWorker.ProgressChanged+=backgroudWorker_ProgressChanged;
            backgroudWorker.RunWorkerCompleted+=backgroudWorker_RunWorkerCompleted;
        }
        private void SetUpListView()
        {
            listView1.View = View.Details;
            listView1.Columns.Add("IP地址", listView1.Width * 60 / 100, HorizontalAlignment.Center);
            listView1.Columns.Add("端口", listView1.Width * 20 / 100, HorizontalAlignment.Center);
            listView1.Columns.Add("状态", listView1.Width * 20 / 100, HorizontalAlignment.Center);
        }
        private void button2_Click(object sender, EventArgs e)
        {
            string dnsname = textBox1.Text;
            if (dnsname.Length < 1)
            {
                MessageBox.Show("请输入域名");
                return;
            }
            string ip = Dns.GetHostAddresses(dnsname)[0].ToString();
            textBox2.Text = ip;
        }
        //参数，总共需要扫描的总数，返回值，数组第一位是所有线程都基本需要扫描的数目，第二个，是前面re个线程需要增加多1个来扫描
        private int[] calarray(int sum)
        {
            int[] ret=new int[2]{0,0};
            int av = (int)sum / threadsum;
            int re = sum % threadsum;
            ret[0] = av;
            ret[1] = re;
            return ret;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string myfilepath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
            //MessageBox.Show(myfilepath);
            //反向找\，获取完整目录名，不包括文件
            int pot = myfilepath.LastIndexOf('\\');
            string dirnamepath = myfilepath.Substring(0, pot);//没有包括最后的斜杠
            string singleip;
            string ipfile=null;
            string portfile=null;
            bool allport = true;
            //获取ip地址单选按钮，如果是文件，就弹出文件选择框选择文件
            if (radioButton1.Checked)
            {
                //选择在文本框里的单ip
                singleip = textBox3.Text;
            }
            else
            {
                //需要选择ip文件，每行一个ip
                OpenFileDialog ofd1 = new OpenFileDialog();
                ofd1.InitialDirectory = dirnamepath;
                ofd1.Filter = "文本文件|*.txt";
                ofd1.RestoreDirectory = true;
                ofd1.FilterIndex = 1;
                ofd1.Title = "请选择IP地址文件";
                if (ofd1.ShowDialog() == DialogResult.OK)
                {
                    ipfile = ofd1.FileName;
                    textBox3.Text = ipfile;
                }
                else
                {
                    MessageBox.Show("请选择ip地址文件");
                    return;
                }
            }
            //端口选择处理
            if (radioButton3.Checked)//所有端口0-65535
            {
                allport = true;
            }
            else
            {
                allport = false;
                OpenFileDialog ofd1 = new OpenFileDialog();
                ofd1.InitialDirectory = dirnamepath;
                ofd1.Filter = "文本文件|*.txt";
                ofd1.RestoreDirectory = true;
                ofd1.FilterIndex = 1;
                ofd1.Title = "请选择端口文件";
                if (ofd1.ShowDialog() == DialogResult.OK)
                {
                    portfile = ofd1.FileName;
                    textBox4.Text = portfile;
                }
                else
                {
                    MessageBox.Show("请选择端口文件");
                    return;
                }

            }

            string[] lines = System.IO.File.ReadAllLines(@portfile);
            if (lines.Length < 1)//沒有端口數，退出
            {
                MessageBox.Show("沒有數據，推出");
                return;
            }
            int sum = lines.Length;

            string[,] sumarray = new string[sum, 3];
            int[] layoutthread = calarray(sum);
            //int[0],除數，int[1],餘數
            //定義一個數組，每個對應綫程需要跑的數目
            int[] threadrunnum=new int[threadsum];
            for(int i=0;i<30;i++)
            {
                if (i < layoutthread[1])
                {
                    threadrunnum[i] = layoutthread[0] + 1;
                }
                else
                {
                    threadrunnum[i] = layoutthread[0];
                }
            }

            //开始扫描操作，一个ip就一个线程，然后下面继续产生多个线程扫描
            //第一种情况，是一个ip
            //使用多线程，传递参数，第一个ip，第二个，端口数组，第三个是数组长度，最多30个线程
            int ipnum = 0;
            if (radioButton1.Checked)
            {
                //输入框一个ip
                ipnum = 1;

            }
            else
            {
                ipnum = System.IO.File.ReadAllBytes(@ipfile).Length;
            }
            string[] iparray = new string[ipnum];
            if(ipnum==1)
                iparray[0]=textBox3.Text;
            else{
                using(StreamReader ReaderOject=new StreamReader(@ipfile))
                {
                    string line;
                    int k = 0;
                    while ((line = ReaderOject.ReadLine()) != null)
                    {
                        iparray[k] = line;
                        k++;
                    }
                    ReaderOject.Close();
                    ipnum = k;
                }
            }
            //读port文件
            string[] portarry = new string[sum];
            using (StreamReader ro = new StreamReader(@portfile))
            {
                string line1;
                int k = 0;
                while ((line1 = ro.ReadLine()) != null)
                {
                    portarry[k] = line1;
                    k++;
                }
            }

            int sumnum = ipnum * sum;//總共的行數
            sumscan = sumnum;
            //生成一个总的数组，第一个字符串 string[n,3] {{str1,str2,str3},{str4,str5,str6}...};
            string[,] ipportarray = new string[sumnum, 3];
            for (int i = 0; i < ipnum; i++)//ip数量
            {
                for (int j = 0; j < lines.Length; j++)
                {
                    //ipportarray[j+i*lines.Length, 0] = iparray[i];
                    //ipportarray[j+i*lines.Length, 1] = portarry[j];
                    //ipportarray[j + i * lines.Length, 2] = "0";
                    globalArray.Add(new string[] { iparray[i].ToString(), portarry[j].ToString(), "0" });
                }
            }
            //分成30个数组，并赋值
            /*
            ArrayList al0 = new ArrayList();
            ArrayList al1 = new ArrayList();
            ArrayList al2 = new ArrayList();
            ArrayList al3 = new ArrayList();
            ArrayList al4 = new ArrayList();
            ArrayList al5 = new ArrayList();
            ArrayList al6 = new ArrayList();
            ArrayList al7 = new ArrayList();
            ArrayList al8 = new ArrayList();
            ArrayList al9 = new ArrayList();
            ArrayList al10 = new ArrayList();
            ArrayList al11 = new ArrayList();
            ArrayList al12 = new ArrayList();
            ArrayList al13 = new ArrayList();
            ArrayList al14 = new ArrayList();
            ArrayList al15 = new ArrayList();
            ArrayList al16 = new ArrayList();
            ArrayList al17 = new ArrayList();
            ArrayList al18 = new ArrayList();
            ArrayList al19 = new ArrayList();
            ArrayList al20 = new ArrayList();
            ArrayList al21 = new ArrayList();
            ArrayList al22 = new ArrayList();
            ArrayList al23 = new ArrayList();
            ArrayList al24 = new ArrayList();
            ArrayList al25 = new ArrayList();
            ArrayList al26 = new ArrayList();
            ArrayList al27 = new ArrayList();
            ArrayList al28 = new ArrayList();
            ArrayList al29 = new ArrayList();*/

            backgroudWorker.RunWorkerAsync();
            button1.Enabled = false;
            
        }

        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            //后台开始工作
            int totle = sumscan;
            
            for (int i = 0; i < totle; i++)
            {
                for (int j = 0; j < 3; j++)//尝试3次，如果异常
                {
                    Socket sk = null ;
                    try
                    {
                        IPAddress ip = IPAddress.Parse(globalArray[i][0].ToString());
                        int port = int.Parse(globalArray[i][1].ToString());
                        sk = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        sk.Connect(ip, port);
                        if (sk.Connected)
                        {
                            globalArray[i][2] = "1";
                        }
                        else
                            globalArray[i][2] = "2";
                        if (sk != null)
                        {
                            sk.Close();
                            sk = null;
                        }
                        break;
                    }
                    catch (Exception ee)
                    {
                        Console.WriteLine(ee.Message);
                    }
                    if (sk != null)
                    {
                        sk.Close();
                        sk = null;
                    }
                }
                int process = (int)(((float)i / totle) * 100);
                backgroudWorker.ReportProgress(process);


            }
            e.Result = globalArray;
        }
        private void backgroudWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int num = (int)(e.ProgressPercentage * sumscan/100);

            progressBar1.Value = e.ProgressPercentage;
            textBox5.Text = num.ToString() + "/" + sumscan.ToString();
        }
        private void backgroudWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //后台工作完成做的时候
            List<string[]>result=(List<string[]>)e.Result;
            foreach (string[] ipport in result)
            {
                ListViewItem it = new ListViewItem();
                it.SubItems[0].Text = ipport[0].ToString();
                it.SubItems.Add(ipport[1].ToString());
                it.SubItems.Add(ipport[2].ToString());
                if (ipport[2].ToString() == "1")
                {
                    it.ForeColor = Color.Green;
                }
                listView1.Items.Add(it);
            }
            button1.Enabled = true;
            //更新界面
            progressBar1.Value = 0;
        }
    }
}
