using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FileKeeperMAUI
{
    internal readonly struct FileSendingData
    {
        public const int AesKeyByteSize = 64;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        private readonly string fileName;

        public ReadOnlyMemory<byte> AesKey { get; }
        
        public string FileName => fileName;

        public long IPAddress { get; }

        public long IPAddress2 { get; }

        public DateTime Timestamp { get; }

#pragma warning disable CS0618
        public FileSendingData(string fileName, IEnumerable<IPAddress> addresses, DateTime timestamp)
        {
            this.fileName = fileName;
            Timestamp = timestamp;
            var availableAddresses = addresses.Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
            int count = availableAddresses.Count();
            IPAddress = IPAddress2 = -1;
            if (count > 0)
            {
                IPAddress = (uint)System.Net.IPAddress.NetworkToHostOrder((int)availableAddresses.First().Address);
                if (count == 2)
                {
                    IPAddress2 = (uint)System.Net.IPAddress.NetworkToHostOrder((int)availableAddresses.Last().Address);
                }
            }
            AesKey = RandomNumberGenerator.GetBytes(AesKeyByteSize);
        }

        public string SerializeToBinaryString()
        {
            int size = Marshal.SizeOf(this);
            byte[] arr = new byte[size];
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);
                Marshal.StructureToPtr(this, ptr, true);
                Marshal.Copy(ptr, arr, 0, size);
                return Encoding.UTF8.GetString(arr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        public static FileSendingData FromSerializedString(string serializedString)
        {
            byte[] arr = Encoding.UTF8.GetBytes(serializedString);
            FileSendingData data = new FileSendingData();
            int size = Marshal.SizeOf(data);
            IntPtr ptr = IntPtr.Zero;
            try
            {
                ptr = Marshal.AllocHGlobal(size);

                Marshal.Copy(arr, 0, ptr, size);

                data = (FileSendingData)Marshal.PtrToStructure(ptr, data.GetType());
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return data;
        }
    }
}
