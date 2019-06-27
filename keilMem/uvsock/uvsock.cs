using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;



namespace keilMem.uvsock
{
    public class Uvsock
    {

        //public 
        public const int SOCK_NDATA = 32768;

        public const Byte paddingByte = 0;
        public const UInt32 padding = 0;

        public const UInt32 UV_GEN_GET_VERSION = 0x0001;
        public const UInt32 UV_DBG_CALC_EXPRESSION = 0x200A;
        public const UInt32 UV_DBG_MEM_READ = 0x200B;
        public const UInt32 UV_DBG_MEM_WRITE = 0x200C;

        public Byte[] sockSendBuffer = new byte[100];
        SocketFlags flags;

        [StructLayout(LayoutKind.Explicit)]
        public struct _tag_UVSOCK_CMD
        {
            [FieldOffset(0)]
            public UInt32 m_nTotalLen;    ///< Total message length (4 bytes)
            [FieldOffset(4)]
            public UInt32 m_eCmd;    ///< Command code  (4 bytes)
            [FieldOffset(8)]
            public UInt32 m_nBufLen;    ///< Length of Data Section (4 bytes) Command Length
            [FieldOffset(12)]
            public UInt64 cycles;    ///< Cycle value (Simulation mode only)(8 bytes)
            [FieldOffset(20)]
            public double tStamp;    ///< time-stamp (Simulation mode only)(8 bytes)
            [FieldOffset(28)]
            public UInt32 m_Id;    ///< Reserved (4 bytes)
            //[FieldOffset(32)]
            //public UVSOCK_CMD_DATA data;    ///< Data Section (Command code dependent data)
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct _tag_UVSOCK_CMD_DATA
        {
            [FieldOffset(0)]
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SOCK_NDATA)]
            Byte raw;    ///< Command-dependent raw data
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct Amem
        {
            [FieldOffset(0)]
            public UInt64 nAddr;   ///< Address to read / write
            [FieldOffset(8)]
            public UInt32 nBytes;   ///< Number of bytes read / write
            [FieldOffset(12)]
            public UInt64 ErrAddr;   ///< Unused
            [FieldOffset(20)]
            public UInt32 nErr;   ///< Unused
          //  [FieldOffset(24)]
          //  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
           // public Byte aBytes;   ///< @a nBytes of data read or to be written
        }

        public _tag_UVSOCK_CMD UVSOCK_CMD;

        public byte[] GET_VERSION()
        {
            UVSOCK_CMD.m_eCmd = UV_GEN_GET_VERSION;
            UVSOCK_CMD.m_nBufLen = 0;
            UVSOCK_CMD.m_nTotalLen = 0;
            UVSOCK_CMD.cycles = 0;
            UVSOCK_CMD.tStamp = 0;
            UVSOCK_CMD.m_nTotalLen = 32;

            byte[] re = getBytes(ref UVSOCK_CMD);
            return re;
        }

        //读取指定地址内存
        [StructLayout(LayoutKind.Explicit)]
        public struct MemReadStruct
        {
            [FieldOffset(0)]
            public UInt32 m_nTotalLen;    
            [FieldOffset(4)]
            public UInt32 m_eCmd;    
            [FieldOffset(8)]
            public UInt32 m_nBufLen;   
            [FieldOffset(12)]
            public UInt64 cycles;   
            [FieldOffset(20)]
            public UInt64 tStamp;   
            [FieldOffset(28)]
            public UInt32 m_Id;
            [FieldOffset(32)]
            public UInt64 address;
            [FieldOffset(40)]
            public UInt32 dataLength;
            [FieldOffset(44)]
            public UInt64 errAddr;
            [FieldOffset(52)]
            public UInt32 nErr;
            [FieldOffset(56)]
            public Byte aBytes;
        }

        public void MemRead(UInt64 addr, UInt32 size)
        {
            MemReadStruct memRead = new MemReadStruct();

            memRead.m_nTotalLen = 0x39;
            memRead.m_eCmd = UV_DBG_MEM_READ;
            memRead.m_nBufLen = 0x19;
            memRead.cycles = padding;
            memRead.tStamp = padding;
            memRead.m_Id = padding;

            memRead.address = addr;
            memRead.dataLength = size;
            memRead.errAddr = padding;
            memRead.nErr = padding;
            memRead.aBytes = paddingByte;
  
            byte[] data = getBytesMemRead(memRead);

            data.CopyTo(sockSendBuffer, 0);

            Console.WriteLine("Send:{0}", BitConverter.ToString(data));

            SocketClient.sender.Send(sockSendBuffer, data.Length, flags);
        }

        //public byte[] MemRead(UInt64 addr, UInt32 size)
        //{
        //    Amem ma = new Amem();
        //    ma.nAddr = addr;
        //    ma.nBytes = size;

        //    UVSOCK_CMD.m_eCmd = UV_DBG_MEM_READ;
        //    UVSOCK_CMD.m_nBufLen = 25;
        //    UVSOCK_CMD.m_nTotalLen = (UInt32)(32 + UVSOCK_CMD.m_nBufLen);
        //    UVSOCK_CMD.cycles = 0;
        //    UVSOCK_CMD.tStamp = 0;

        //    byte[] data = getBytes(ref ma);
        //    byte[] padding = new byte[1];
        //    padding[0] = 0;

        //    byte[] head = getBytes(ref UVSOCK_CMD);

        //    byte[] re = new byte[head.Length + data.Length+1];
        //    head.CopyTo(re, 0);
        //    data.CopyTo(re, head.Length);
        //    padding.CopyTo(re, head.Length + data.Length);
        //    return re;
        //}
        public byte[] getBytes<T>(ref T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
        public byte[] getBytesMemRead(MemReadStruct str)
        {
            int size = 0x39;
            //int size = Marshal.SizeOf(str); //有问题   等于 0x40
            byte[] arr = new byte[size];

            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}
