using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace yomdem_test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            //初始化调用
            EnumComportfromReg(comboBox1);
           // comboBox1.SelectedIndex = 2;
            cmb_baud.SelectedIndex = 1;
        }

        private delegate void FlushClient(); //代理
        int packetNumber = 0;
        string path;
       
        int fsLen;
        private void EnumComportfromReg(ComboBox Combobox)
        {
            Combobox.Items.Clear();
            ///定义注册表子Path
            string strRegPath = @"Hardware\\DeviceMap\\SerialComm";
            ///创建两个RegistryKey类，一个将指向Root Path，另一个将指向子Path
            RegistryKey regRootKey;
            RegistryKey regSubKey;
            ///定义Root指向注册表HKEY_LOCAL_MACHINE节点
            regRootKey = Registry.LocalMachine;
            regSubKey = regRootKey.OpenSubKey(strRegPath);
            if (regSubKey.GetValueNames() == null) return;
            string[] strCommList = regSubKey.GetValueNames();
            foreach (string VName in strCommList)
            {
                //向listbox1中添加字符串的名称和数据，数据是从rk对象中的GetValue(it)方法中得来的
                Combobox.Items.Add(regSubKey.GetValue(VName));
            }
            // if (Combobox.Items.Count > 0)
            //    Combobox.SelectedIndex = 0;
            if (Combobox.Items.Count <= 0)
            { MessageBox.Show("Error Device Type!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); Application.Exit(); return; }
            else
            { Combobox.SelectedIndex = 0; }
            regSubKey.Close();
            regRootKey.Close();
        }

        /*
         * Upload file via Ymodem protocol to the device
         * ret: is the transfer succeeded? true is if yes
         */
   
          private void ThreadFunction()
          {
              if (this.txb_section.InvokeRequired)//等待异步
              {
                  FlushClient fc = new FlushClient(ThreadFunction);
                  this.Invoke(fc); //通过代理调用刷新方法
              }
              else
              {
                  this.txb_section.Text = packetNumber.ToString();
                  pgb_download.Value = (int)(((float)packetNumber / (float)fsLen) * 100);
              }
          }
     
        public bool YmodemUploadFile()
        {
            /* control signals */
            const byte STX = 2;  // Start of TeXt 
            const byte EOT = 4;  // End Of Transmission
            const byte ACK = 6;  // Positive ACknowledgement
            const byte C = 67;   // capital letter C

            /* sizes */
            const int dataSize = 1024;
            const int crcSize = 2;

            /* THE PACKET: 1029 bytes */
            /* header: 3 bytes */
            // STX
        
            int invertedPacketNumber = 255;
            /* data: 1024 bytes */
            byte[] data = new byte[dataSize];
            /* footer: 2 bytes */
            byte[] CRC = new byte[crcSize];

        
            /* get the file */
            FileStream fileStream = new FileStream(@path, FileMode.Open, FileAccess.Read);
             
            
          DateTime dt= DateTime.Now; 

            byte[] ack;
            ack = new byte[] { 0x31 };
            try
            {
                serialPort1.Write(ack, 0, 1);
            }
            catch
            {
                MessageBox.Show("Exception");
            }

            Thread.Sleep(300);

            try
            {
                /* send the initial packet with filename and filesize */
                //if (serialPort1.ReadByte() != C)
                //{
                //    Console.WriteLine("Can't begin the transfer.");
                //    return false;
                //}

                sendYmodemInitialPacket(STX, packetNumber, invertedPacketNumber, data, dataSize, path, fileStream, CRC, crcSize);
                if (serialPort1.ReadByte() != ACK)
                {
                    MessageBox.Show("Can't send the initial packet.");
                    return false;
                }
                

                if (serialPort1.ReadByte() != C)
                    return false;

                /* send packets with a cycle until we send the last byte */
                int fileReadCount;
                do
                {
                    /* if this is the last packet fill the remaining bytes with 0 */
                    fileReadCount = fileStream.Read(data, 0, dataSize);
                    if (fileReadCount == 0) break;
                    if (fileReadCount != dataSize)
                        for (int i = fileReadCount; i < dataSize; i++)
                            data[i] = 0;

                    /* calculate packetNumber */
                    packetNumber++;
                    if (packetNumber > 255)
                        packetNumber -= 256;

                    ThreadFunction();
 

                    Console.WriteLine(packetNumber);

                    /* calculate invertedPacketNumber */
                    invertedPacketNumber = 255 - packetNumber;

                    /* calculate CRC */
                    Crc16Ccitt crc16Ccitt = new Crc16Ccitt(InitialCrcValue.Zeros);
                    CRC = crc16Ccitt.ComputeChecksumBytes(data);

                    /* send the packet */
                    sendYmodemPacket(STX, packetNumber, invertedPacketNumber, data, dataSize, CRC, crcSize);

                    /* wait for ACK */
                    if (serialPort1.ReadByte() != ACK)
                    {
                        Console.WriteLine("Couldn't send a packet.");
                        return false;
                    }
                } while (dataSize == fileReadCount);

                /* send EOT (tell the downloader we are finished) */
                serialPort1.Write(new byte[] { EOT }, 0, 1);
                /* send closing packet */
                packetNumber = 0;
                invertedPacketNumber = 255;
                data = new byte[dataSize];
                CRC = new byte[crcSize];
                sendYmodemClosingPacket(STX, packetNumber, invertedPacketNumber, data, dataSize, CRC, crcSize);
                /* get ACK (downloader acknowledge the EOT) */
                if (serialPort1.ReadByte() != ACK)
                {
                    Console.WriteLine("Can't complete the transfer.");
                    return false;
                }
            }
            catch (TimeoutException)
            {
                throw new Exception("Eductor does not answering");
            }
            finally
            {
                fileStream.Close();
            }

            Console.WriteLine("File transfer is succesful");
            TimeSpan span = DateTime.Now-dt;

            btn_download.Invoke
           (
                    //委托，托管无参数的任何方法
               new MethodInvoker
               (
                   delegate
                   {
                       btn_download.Text = "烧录完成";
                   }
               )
            );
             
            MessageBox.Show("烧录耗时:" + span.ToString(), "下载成功", MessageBoxButtons.OK, MessageBoxIcon.None);


            btn_download.Invoke
            (
                        //委托，托管无参数的任何方法
                new MethodInvoker
                (
                    delegate
                    {
                        btn_download.Text = "开始烧录";
                    }
                )
             );

         
            return true;
        }

        private void sendYmodemInitialPacket(byte STX, int packetNumber, int invertedPacketNumber, byte[] data, int dataSize, string path, FileStream fileStream, byte[] CRC, int crcSize)
        {
            string fileName = System.IO.Path.GetFileName(path);
            string fileSize = fileStream.Length.ToString();

            /* add filename to data */
            int i;
            for (i = 0; i < fileName.Length && (fileName.ToCharArray()[i] != 0); i++)
            {
                data[i] = (byte)fileName.ToCharArray()[i];
            }
            data[i] = 0;

            /* add filesize to data */
            int j;
            for (j = 0; j < fileSize.Length && (fileSize.ToCharArray()[j] != 0); j++)
            {
                data[(i + 1) + j] = (byte)fileSize.ToCharArray()[j];
            }
            data[(i + 1) + j] = 0;

            /* fill the remaining data bytes with 0 */
            for (int k = ((i + 1) + j) + 1; k < dataSize; k++)
            {
                data[k] = 0;
            }

            /* calculate CRC */
            Crc16Ccitt crc16Ccitt = new Crc16Ccitt(InitialCrcValue.Zeros);
            CRC = crc16Ccitt.ComputeChecksumBytes(data);

            /* send the packet */
            sendYmodemPacket(STX, packetNumber, invertedPacketNumber, data, dataSize, CRC, crcSize);
        }

        private void sendYmodemClosingPacket(byte STX, int packetNumber, int invertedPacketNumber, byte[] data, int dataSize, byte[] CRC, int crcSize)
        {
            /* calculate CRC */
            Crc16Ccitt crc16Ccitt = new Crc16Ccitt(InitialCrcValue.Zeros);
            CRC = crc16Ccitt.ComputeChecksumBytes(data);

            /* send the packet */
            sendYmodemPacket(STX, packetNumber, invertedPacketNumber, data, dataSize, CRC, crcSize);
        }

        private void sendYmodemPacket(byte STX, int packetNumber, int invertedPacketNumber, byte[] data, int dataSize, byte[] CRC, int crcSize)
        {
            serialPort1.Write(new byte[] { STX }, 0, 1);
            serialPort1.Write(new byte[] { (byte)packetNumber }, 0, 1);
            serialPort1.Write(new byte[] { (byte)invertedPacketNumber }, 0, 1);
            serialPort1.Write(data, 0, dataSize);
            serialPort1.Write(CRC, 0, crcSize);
        }

        private void btn_select_hex_Click(object sender, EventArgs e)
        {      
            openFileDialog1.ShowDialog();
            btn_download.Enabled = true;
        }

     
      
        private void hexfileloadthread()
        {
            YmodemUploadFile();
        }
        private void openFileDialog1_FileOk(object sender, CancelEventArgs e)
        {
            path = openFileDialog1.FileName;
            FileStream fileStream = new FileStream(@path, FileMode.Open, FileAccess.Read);
            fsLen = (int)fileStream.Length/1000;                    //计算总段长
            txb_totalsection.Text = fsLen.ToString();
        }

        private void btn_port_Click(object sender, EventArgs e)
        {
            if(serialPort1.IsOpen)
            {
                btn_port.Text = "Open";

                try
                {
                    serialPort1.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("关闭异常");
                    return;
                }
             
            }
            else
            {
                btn_port.Text = "Close";
                int baud = int.Parse(cmb_baud.Text);
                serialPort1.PortName = comboBox1.Text;
                serialPort1.BaudRate = baud;

                try
                {
                    serialPort1.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("打开错误"); 
                    return;
                }
              
            }
        }
        

        private void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            serialPort1.ReadExisting(); 
        }

        private void btn_download_Click(object sender, EventArgs e)
        {
            Thread th = new Thread(hexfileloadthread);
            th.Start();
            btn_download.Text = "正在烧录";
            this.txb_section.Text = "0";
        }

        private void 联系我ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            About ab_dialog = new About();
            ab_dialog.ShowDialog();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }


    }
}
