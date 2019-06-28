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
        public const UInt32 UV_CMD_RESPONSE = 0x3000;

        public Byte[] sockSendBuffer = new byte[100];
        SocketFlags flags;

        public static int curDataTypeIndex = 0;
        public static int[] dataTypeSize = new int[8] { 1, 2, 4, 1, 2, 4, 4, 8 };

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

        [StructLayout(LayoutKind.Explicit)]
        public struct MemReadResponseStruct
        {
            [FieldOffset(0)]
            public UInt32 m_nTotalLen;
            [FieldOffset(4)]
            public UInt32 m_eCmd; //UV_CMD_RESPONSE
            [FieldOffset(8)]
            public UInt32 m_nBufLen;
            [FieldOffset(12)]
            public UInt64 cycles;
            [FieldOffset(20)]
            public UInt64 tStamp;
            [FieldOffset(28)]
            public UInt32 m_Id;

            [FieldOffset(32)]
            public UInt32 dataCmd; //UV_DBG_MEM_READ
            [FieldOffset(36)]
            public UInt32 notKnow; //0

            [FieldOffset(40)]
            public UInt64 address;
            [FieldOffset(48)]
            public UInt32 dataLength;
            [FieldOffset(52)]
            public UInt64 errAddr;
            [FieldOffset(60)]
            public UInt32 nErr;
        } //64Bytes

        public void MemRead(UInt64 addr, UInt32 size,int dataTypeIndex)
        {
            curDataTypeIndex = dataTypeIndex;

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
        public static void rxProcess(Byte[] data,int length)
        {
            UInt32 totalLen = BitConverter.ToUInt32(data, 0);
            if(data.Length < totalLen)
            {
                return;
            }
            UInt32 cmd = BitConverter.ToUInt32(data, 4);
            UInt32 dataLength = BitConverter.ToUInt32(data, 8);
            UInt64 cycles = BitConverter.ToUInt64(data, 12);
            UInt64 tStamp = BitConverter.ToUInt64(data, 20);
            UInt32 idwhat = BitConverter.ToUInt32(data, 28);
            UInt32 cmdData = BitConverter.ToUInt32(data, 32);
            UInt32 whatTheFuck = BitConverter.ToUInt32(data, 36);
            UInt64 address = BitConverter.ToUInt64(data, 40);
            UInt32 dataSize = BitConverter.ToUInt32(data, 48);
            UInt64 whatTheFuck2 = BitConverter.ToUInt64(data, 52);
            UInt32 whatTheFuck3 = BitConverter.ToUInt32(data, 60);
            //mem data
            Byte[] MemData = data.Skip(64).Take((int)dataSize).ToArray();

            //unsigned short
            for(int i=0;i< (int)dataSize/ dataTypeSize[curDataTypeIndex]; i++)
            {
                switch (curDataTypeIndex)
                {
                    case 0://unsigned char
                        Byte var0 = MemData[i * dataTypeSize[curDataTypeIndex]];
                        Console.WriteLine("rx:{0}", var0.ToString());
                        break;
                    case 1://unsigned short
                        UInt16 var1 = BitConverter.ToUInt16(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}", String.Format("0x{0:X}", var1));
                        break;
                    case 2://unsigned int
                        UInt32 var2 = BitConverter.ToUInt32(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}", String.Format("0x{0:X}", var2));
                        break;
                    case 3://char
                        char var3 = (char)MemData[i * dataTypeSize[curDataTypeIndex]];
                        Console.WriteLine("rx:{0}", var3);
                        break;
                    case 4://short
                        Int16 var4 = BitConverter.ToInt16(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}",  var4);
                        break;
                    case 5://int
                        Int32 var5 = BitConverter.ToInt32(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}", var5);
                        break;
                    case 6://float
                        float var6 = BitConverter.ToSingle(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}", var6);
                        break;
                    case 7://double
                        double var7 = BitConverter.ToDouble(MemData, i * dataTypeSize[curDataTypeIndex]);
                        Console.WriteLine("rx:{0}", var7);
                        break;
                }
            }
        }
        //MemReadResponseStruct ByteArrayToMemReadResponse(byte[] bytes)
        //{
        //    GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        //    MemReadResponseStruct stuff;
        //    try
        //    {
        //        stuff = (MemReadResponseStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MemReadResponseStruct));
        //    }
        //    finally
        //    {
        //        handle.Free();
        //    }
        //    return stuff;
        //}
    }
}
