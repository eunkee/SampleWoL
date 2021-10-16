using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SampleWoL
{
    public partial class Form1 : Form
    {
        //https://www.sysnet.pe.kr/2/0/12336
        public Form1()
        {
            InitializeComponent();
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            string macAddress = textBox1.Text;
            if (macAddress.Length >= 12)
            {
                byte[] wolBuffer = GetWolPacket(macAddress);

                UdpClient udp = new UdpClient();
                udp.EnableBroadcast = true;

                //Nic 모두 전송
                foreach (IPAddress ipAddress in GetDirectedBroadcastAddresses())
                {
                    //port 7과 9 모두 전송
                    udp.Send(wolBuffer, wolBuffer.Length, ipAddress.ToString(), 7);
                    udp.Send(wolBuffer, wolBuffer.Length, ipAddress.ToString(), 9);
                }
            }
        }

        //매직 패킷 만들기
        private static byte[] GetWolPacket(string macAddress)
        {
            byte[] datagram = new byte[102];

            byte[] macBuffer = StringToBytes(macAddress);

            MemoryStream ms = new MemoryStream(datagram);
            BinaryWriter bw = new BinaryWriter(ms);

            // 6바이트의 0xff를 선두에 채우고,
            for (int i = 0; i < 6; i++)
            {
                bw.Write((byte)0xff);
            }

            // 이후 WoL로 깨울 PC가 소유한 Network Adapter의 MAC 주소를 16번 반복
            for (int i = 0; i < 16; i++)
            {
                bw.Write(macBuffer, 0, macBuffer.Length);
            }

            return datagram;
        }

        private static byte[] StringToBytes(string macAddress)
        {
            // Remove any semicolons or minus characters present in our MAC address
            macAddress = Regex.Replace(macAddress, "[-|:]", ""); 
            byte[] buffer = new byte[macAddress.Length / 2];

            for (int i = 0; i < macAddress.Length; i += 2)
            {
                string digit = macAddress.Substring(i, 2);
                buffer[i / 2] = byte.Parse(digit, NumberStyles.HexNumber);
            }

            return buffer;
        }

        private static IPAddress[] GetDirectedBroadcastAddresses()
        {
            List<IPAddress> list = new List<IPAddress>();

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                if (item.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                UnicastIPAddressInformationCollection unicasts = item.GetIPProperties().UnicastAddresses;

                foreach (UnicastIPAddressInformation unicast in unicasts)
                {
                    IPAddress ipAddress = unicast.Address;

                    if (ipAddress.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    byte[] addressBytes = ipAddress.GetAddressBytes();
                    byte[] subnetBytes = unicast.IPv4Mask.GetAddressBytes();

                    if (addressBytes.Length != subnetBytes.Length)
                    {
                        continue;
                    }

                    byte[] broadcastAddress = new byte[addressBytes.Length];
                    for (int i = 0; i < broadcastAddress.Length; i++)
                    {
                        broadcastAddress[i] = (byte)(addressBytes[i] | (subnetBytes[i] ^ 255));
                    }

                    list.Add(new IPAddress(broadcastAddress));
                }
            }

            return list.ToArray();
        }
    }
}
