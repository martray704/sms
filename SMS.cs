using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Management;
using System.Data;


namespace Luminance.Core
{
    public class SMS
    {
        public static SerialPort port = new SerialPort();
        public enum GSMBrand { Ostent, Wavecom, None }
        public string OpenPort(string _comPort, bool isTest, GSMBrand gsmBrand)
        {
            string ret = string.Empty;

            if (isTest)
                port.Close();

            if (!port.IsOpen)
                 port.PortName = _comPort;


            if (gsmBrand == GSMBrand.Ostent)
                port.BaudRate = 9600;
            else if (gsmBrand == GSMBrand.Wavecom)
                port.BaudRate = 115200;

      
            port.Parity = Parity.None;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.Handshake = Handshake.RequestToSend;
            port.DtrEnable = true;
            port.RtsEnable = true;
            port.NewLine = System.Environment.NewLine;

            try
            {
                port.Open();
                if (isTest)
                {
                    try
                    {
                        port.WriteLine(@"AT+CNMI=1" + (char)(13));
                        Thread.Sleep(1000);
                        ret = port.ReadExisting();
                    }
                    catch (Exception ex)
                    {
                        ret = "Not a GSM." + ex.Message;
                    }
                }
                //port.Close();
            }
            catch (Exception ex)
            {
                ret = ex.Message;
               // MsgBox.Error(ex.Message);
            }
            return ret;
        }
        /// <summary>
        /// Send a single message to a single mobile number
        /// </summary>
        /// <param name="_mobile_number">Mobile Number</param>
        /// <param name="_message">Your Message</param>
        /// <returns></returns>
        public bool Send(string _mobile_number, string _message)
        {
            if (port.IsOpen)
            {
                port.WriteLine(@"AT" + (char)(13));
                Thread.Sleep(200);
                port.WriteLine("AT+CMGF=1" + (char)(13));
                Thread.Sleep(200);
                port.WriteLine(@"AT+CMGS=""" + _mobile_number + @"""" + (char)(13));
                Thread.Sleep(200);
                port.WriteLine(_message + (char)(26));
                Thread.Sleep(200);
                return true;
            }
            port.Close();
            return false;
        }

        /// <summary>
        /// Send Multiple messages to multiple numbers.
        /// </summary>
        /// <param name="_numbersAndMessages">Database should have 2 columns, first is the phone numbers, 2nd is the messages.</param>
        /// <returns></returns>
        public bool Send(DataTable _numbersAndMessages)
        {
            if (port.IsOpen)
            {
                foreach (DataRow row in _numbersAndMessages.Rows)
                {
                    port.WriteLine(@"AT" + (char)(13));
                    Thread.Sleep(200);
                    port.WriteLine("AT+CMGF=1" + (char)(13));
                    Thread.Sleep(200);
                    port.WriteLine(@"AT+CMGS=""" + row[0].ToString() + @"""" + (char)(13));
                    Thread.Sleep(200);
                    port.WriteLine(row[1].ToString() + (char)(26));
                    Thread.Sleep(200);
                }
                return true;
            }
            port.Close();
            return false;
        }

        /// <summary>
        /// Send a single message to multiple numbers.
        /// Create a List, add all numbers in that list
        /// </summary>
        /// <param name="_numbers">List of numbers</param>
        /// <param name="_message">You Message</param>
        /// <returns></returns>
        public bool Send(List<string> _numbers, string _message)
        {
            if (port.IsOpen)
            {
                foreach (string item in _numbers)
                {
                    port.WriteLine(@"AT" + (char)(13));
                    Thread.Sleep(1000);
                    port.WriteLine("AT+CMGF=1" + (char)(13));
                    Thread.Sleep(1000);
                    port.WriteLine(@"AT+CMGS=""" + item + @"""" + (char)(13));
                    Thread.Sleep(1000);
                    port.WriteLine(_message + (char)(26));
                    Thread.Sleep(5000);
                }
                return true;
            }
            port.Close();
            return false;
        }

        public enum ReadMessagesTypes { Unread, Read, All }

        public string ReadMessages(ReadMessagesTypes readMessagesType)
        {
            string ret = string.Empty;
            if (port.IsOpen)
            {
                string read = string.Empty;
                if (readMessagesType == ReadMessagesTypes.All)
                    read = "ALL";
                else if (readMessagesType == ReadMessagesTypes.Read)
                    read = "REC READ";
                else if (readMessagesType == ReadMessagesTypes.Unread)
                    read = "REC UNREAD";

                port.WriteLine(@"AT" + (char)(13));
                Thread.Sleep(100);
                port.WriteLine("AT+CMGF=1" + (char)(13));
                Thread.Sleep(100);
                port.WriteLine("AT+CPMS=\"SM\"\r" + (char)(13));
                Thread.Sleep(100);
                //serialPort.WriteLine("AT+CMGL=\"ALL\"\r" + (char)(13)); // all messages
                port.WriteLine("AT+CMGL=\"" + read + "\"\r" + (char)(13));
                Thread.Sleep(5000);
                ret = port.ReadExisting();

                if (ret.Contains("ERROR"))
                    ret = "An error occured. Please try again.";

            }
            else
                ret = "No GSM port detected.";

            return ret;
        }


        public string DeleteMessages()
        {
            string ret = string.Empty;

            port.WriteLine(@"AT" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("AT+CMGF=1" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("AT+CPMS=\"SM\"\r" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("AT+CMGD=1,3" + (char)(13));    
            Thread.Sleep(3000);
            ret = port.ReadExisting();

            //if (ret.Contains("ERROR"))
            //    ret = "An error occured. Please try again.";

            return ret;
        }

        public string Listen()
        {
            string ret = string.Empty;
            port.WriteLine(@"AT+CNMI=1");

            ret = port.ReadExisting();

            if (ret.Contains("RING"))
            {
                port.WriteLine("AT+CHUP");
                Thread.Sleep(200);
                port.WriteLine("ATH");
                Thread.Sleep(200);
            }

            if (ret.Contains("SM")) //reply
            {
               // Send("09094483517", "We receive your text");               
            }

            return ret;
        }

        public string ReadCall()
        {
            string ret = string.Empty;
            port.WriteLine(@"AT" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("AT+CLIP=1" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("AT+CHUP" + (char)(13));
            Thread.Sleep(200);
            port.WriteLine("ATH" + (char)(13)); // deny the call
            Thread.Sleep(200);
            ret = port.ReadExisting();

            //if (ret.Contains("ERROR"))
            //    ret = "An error occured. Please try again." + serialPort.ReadExisting();

            return ret;
        }

        public List<string> GetPorts()
        {
            List<string> list = new List<string>();
            string[] portnames = SerialPort.GetPortNames();
            foreach (string i2 in portnames)
                list.Add(i2);

            return list;
        }

        public bool Call(string _mobile_number)
        {
            if (port.IsOpen)
            {
                port.WriteLine(@"AT" + (char)(13));
                Thread.Sleep(200);
                port.WriteLine("ATD" + _mobile_number + (char)(13));
                Thread.Sleep(3000);

                return true;
            }
            port.Close();
            return false;
        }
    }
}
