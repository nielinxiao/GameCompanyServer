using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using ProtoBuf;
namespace IOCP
{
    public enum SendState
    {
        None,
        Sending
    }
    public class IOCPToken<T>
    {
        public delegate void OnDisConnect(IOCPToken<T> token);

        public delegate void ReceiveCallBack(IOCPToken<T> token, T msgcallback);

        public delegate void LogAction(string logstr);

        private OnDisConnect onDisConnect;

        private ReceiveCallBack receiveCallBack;

        private LogAction logaction;

        private Socket sck;

        private SocketAsyncEventArgs eventReceive;

        private SocketAsyncEventArgs eventSend;

        private Queue<byte[]> SendTemp = new Queue<byte[]>();
        private SendState state = SendState.None;

        public IOCPToken()
        {
        }
        
        int Length = 0;
        public IOCPToken(Socket sck, ReceiveCallBack receiveCallBack, int Length, OnDisConnect onDisConnect, LogAction logAction)
        {
            eventReceive = new SocketAsyncEventArgs();
            isClose=false;
            eventSend = new SocketAsyncEventArgs();
            this.Length = Length;
            this.sck = sck;
            eventReceive.Completed += ReceiveClient;
            eventSend.Completed += DequeMsg;
            eventReceive.SetBuffer(new byte[Length], 0, Length);
            tempArry = new byte[Length];
            this.onDisConnect = onDisConnect;
            this.receiveCallBack = receiveCallBack;
            logaction = logAction;
            if (!this.sck.ReceiveAsync(eventReceive))
            {
                ReceiveClient(null, eventReceive);
            }
        }

        public void Reset(Socket sck, ReceiveCallBack receiveCallBack, int Length, OnDisConnect onDisConnect, LogAction logAction)
        {
            eventReceive = new SocketAsyncEventArgs();
            eventSend = new SocketAsyncEventArgs();
            isClose=false;
            this.Length = Length;
            tempArry = new byte[Length];
            this.sck = sck;
            eventReceive.Completed += ReceiveClient;
            eventSend.Completed += DequeMsg;
            eventReceive.SetBuffer(new byte[Length], 0, Length);
            this.onDisConnect = onDisConnect;
            this.receiveCallBack = receiveCallBack;
            logaction = logAction;
            if (!this.sck.ReceiveAsync(eventReceive))
            {
                ReceiveClient(null, eventReceive);
            }
        }

        byte[] tempArry;
        int byteIndex = 0;
        int receLength = 0;
        int tranlateIndex = 0;
        private void ProcessPck(SocketAsyncEventArgs args, int TransCount)
        {
            if (receLength == 0)
            {
                if (TransCount >= 4)
                {
                    byte[] bytes = new byte[4];
                    if (tranlateIndex != 0)
                        Array.Copy(args.Buffer, tranlateIndex, bytes, 0, 4);
                    else
                        Array.Copy(args.Buffer, 0, bytes, 0, 4);
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(bytes);
                    }
                    receLength = BitConverter.ToInt32(bytes, 0);
                    //logaction.Invoke($"数据包应有长度{receLength}:传输长度：" + TransCount.ToString());
                    tempArry = new byte[receLength];
                    TransCount -= 4;
                    if (TransCount <= receLength)
                    {
                        if (tranlateIndex != 0)
                            Array.Copy(args.Buffer, 4 + tranlateIndex, tempArry, byteIndex, TransCount);
                        else
                            Array.Copy(args.Buffer, 4, tempArry, byteIndex, TransCount);
                        byteIndex += TransCount;
                        if (byteIndex >= receLength)
                        {
                            receiveCallBack(this, Deserialize(tempArry));
                            byteIndex = 0;
                            receLength = 0;
                            tranlateIndex = 0;
                        }
                    }
                    else
                    {
                        if (tranlateIndex != 0)
                            Array.Copy(args.Buffer, 4 + tranlateIndex, tempArry, byteIndex, receLength);
                        else
                            Array.Copy(args.Buffer, 4, tempArry, byteIndex, receLength);
                        receiveCallBack(this, Deserialize(tempArry));
                        int index = TransCount - receLength;
                        tranlateIndex += receLength + 4;
                        byteIndex = 0;
                        receLength = 0;
                        logaction.Invoke($"解包中 剩余数据量:{index}");
                        ProcessPck(args, index);
                    }
                }
            }
            else
            {
                if (byteIndex + TransCount >= receLength)
                {
                    logaction.Invoke("重组完成");
                    Array.Copy(args.Buffer, 0, tempArry, byteIndex, TransCount);
                    receiveCallBack(this, Deserialize(tempArry));
                    byteIndex = 0;
                    receLength = 0;
                }
                else
                {
                    Array.Copy(args.Buffer, 0, tempArry, byteIndex, TransCount);
                    byteIndex += TransCount;
                    logaction.Invoke($"正在重组 目标:{receLength} 当前组装了:{byteIndex}");
                }
            }
        }
        private void ReceiveClient(object obj, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success && args.BytesTransferred != 0)
            {
                ProcessPck(args, args.BytesTransferred);

                if (sck != null && !sck.ReceiveAsync(args))
                {
                    ReceiveClient(null, args);
                }
            }
            else
            {
                OnDestory();
            }
        }
        ManualResetEvent manualReset = new ManualResetEvent(false);
        private void DequeMsg(object obj, SocketAsyncEventArgs args)
        {
            if (sck != null && sck.Connected)
            {
                manualReset.Reset();
                Transount += args.BytesTransferred;
                //logaction.Invoke($"需要发送:{bytes.Length} 当前发送:{Transount}");
                if (Transount < bytes.Length)
                {
                    SendAsync(args);
                }
                else
                {
                    Transount = 0;
                    manualReset.Set();
                }
                manualReset.WaitOne();
                lock (SendTemp)
                {
                    if (SendTemp.Count != 0)
                    {
                        byte[] array = SendTemp.Dequeue();
                        args.SetBuffer(array, 0, array.Length);
                        bytes = array;
                        if (!sck.SendAsync(args))
                        {
                            DequeMsg(obj, args);
                        }
                    }
                    else
                        state = SendState.None;
                }
            }
        }
        int Transount = 0;
        byte[] bytes;
        public void SendAsync(SocketAsyncEventArgs socketAsyncEventArgs)
        {
            socketAsyncEventArgs.SetBuffer(bytes, Transount, bytes.Length - Transount);
            if (sck.SendAsync(socketAsyncEventArgs))
            {
                OnSendComplite(null, socketAsyncEventArgs);
            }
        }
        public void OnSendComplite(object obj, SocketAsyncEventArgs args)
        {
            Transount += args.BytesTransferred;
            if (Transount < bytes.Length)
            {
                SendAsync(args);
            }
            else
            {
                Transount = 0;
                manualReset.Set();
                logaction.Invoke("Complite");
            }
        }

        public void Send(T msg)
        {
            if (sck != null && sck.Connected)
            {
                byte[] bytes = Serialize(msg);
                byte[] leng = BitConverter.GetBytes(bytes.Length);
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(leng);
                }
                byte[] array = new byte[bytes.Length + 4];
                Array.Copy(leng, 0, array, 0, 4);
                Array.Copy(bytes, 0, array, 4, bytes.Length);
                if (state == SendState.None)
                {
                    lock (eventSend)
                    {
                        eventSend.SetBuffer(array, 0, array.Length);
                        state = SendState.Sending;
                        this.bytes = array;
                        if (!sck.SendAsync(eventSend))
                        {
                            DequeMsg(null, eventSend);
                        }
                    }
                }
                else
                {
                    lock (SendTemp)
                        SendTemp.Enqueue(array);
                }
            }
        }
        object obj = new object();
        public void Send(byte[] bytes)
        {
            if (sck != null && sck.Connected)
            {
                byte[] leng = BitConverter.GetBytes(bytes.Length);
                byte[] array = new byte[bytes.Length + 4];
                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(leng);
                }
                Array.Copy(leng, 0, array, 0, 4);
                Array.Copy(bytes, 0, array, 4, bytes.Length);
                lock (obj)
                {
                    if (state == SendState.None)
                    {
                        lock(eventSend)
                        {
                            eventSend.SetBuffer(array, 0, array.Length);
                            state = SendState.Sending;
                            this.bytes = array;
                            if (!sck.SendAsync(eventSend))
                            {
                                DequeMsg(null, eventSend);
                            }
                        }
                    }
                    else
                    {
                        lock (SendTemp)
                            SendTemp.Enqueue(array);
                    }
                }

            }
        }

        public static byte[] Serialize(T msg)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                try
                {
                    Serializer.Serialize(memoryStream, msg);
                }
                catch
                {
                    return null;
                }
                byte[] bytes = new byte[memoryStream.Length];
                Buffer.BlockCopy(memoryStream.GetBuffer(), 0, bytes, 0, (int)memoryStream.Length);
                return bytes;

            }
        }
        public static T Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
                return Serializer.Deserialize<T>(stream);
            }
        }
        bool isClose = false;
        public void OnDestory()
        {
            if(!isClose)
            {
                isClose = true;
                sck.Close();
                sck = null;
                eventSend.Dispose();
                eventReceive.Dispose();
                onDisConnect(this);
            }
        }
    }
    public abstract class IOCPClient<T>
    {
        public delegate void LogAction(string logstr);

        private Socket sck;

        private string ip;

        private int port;

        protected LogAction logaction = DefaultLogAction;

        public string IP => ip;

        public int Port => port;

        private static void DefaultLogAction(string log)
        {
            Console.WriteLine(log);
        }
        int length = 0;
        public IOCPClient(int length)
        {
            this.length = length;
        }
        public async void InitIOCPClient(LogAction log_action, string ip = "127.0.0.1", int port = 45454)
        {
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sck.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            if (log_action != null)
            {
                logaction = log_action;
            }
            if (IPAddress.TryParse(ip, out var address))
            {
                ip = address.ToString();
                IPEndPoint iPEndPoint = new IPEndPoint(address, port);
                this.port = port;
                Task connectTask = SocketTaskExtensions.ConnectAsync(sck, (EndPoint)iPEndPoint);
                try
                {
                    await connectTask;
                    if (sck.Connected)
                    {
                        OnConnected(new IOCPToken<T>(sck, OnReceiveMessage, length, OnCloseConnect, delegate (string str)
                        {
                            logaction(str);
                        }), isConnect: true);
                    }
                    else
                    {
                        logaction("Fail to connect server maybe internet have error!");
                        OnConnected(null, isConnect: false);
                    }
                }
                catch (Exception)
                {
                    logaction("Fail to connect server maybe internet have error!");
                    OnConnected(null, isConnect: false);
                }
            }
            else
            {
                logaction("Fail to connect server becauseof ip is error!");
                OnConnected(null, isConnect: false);
            }
        }
        public void CloseClient()
        {
            sck.Close();
        }
        public abstract void OnCloseConnect(IOCPToken<T> client);

        public abstract void OnReceiveMessage(IOCPToken<T> client, T message);

        public abstract void OnConnected(IOCPToken<T> clientToken, bool isConnect);
    }
    public enum IOCPState
    {
        None,
        Successful,
        Fail
    }
    public class Pool<T>where T:new()
    {
        public Queue<T>values=new Queue<T>();
        public Pool(int maxCount)
        {
            for(int i = 0; i < maxCount; i++)
            {
                T temp = new T();
                values.Enqueue(temp);
            }
        }
        public T Pop()
        {
            if (values.Count != 0)
                return values.Dequeue();
            else
                return new T();
        }
        public void Push(T value)
        {
            values.Enqueue(value);
        }
    }
    public abstract class IOCPServer<T>
    {
        public delegate void LogAction(string logstr);

        private Socket sck;

        private string ip;

        private int port;

        protected LogAction logaction = DefaultLogAction;

        private SocketAsyncEventArgs eventAccept = new SocketAsyncEventArgs();

        public Pool<IOCPToken<T>> tokens;

        private int maxCount;

        private int curcount;

        private bool allowMorePlayerEnter;
        private IOCPState state = IOCPState.None;

        public string IP => ip;

        public int Port => port;

        public int currentCount => curcount;

        private static void DefaultLogAction(string log)
        {
            Console.WriteLine(log);
        }
        int length;
        public IOCPServer(int length) 
        {
            this.length = length;
        }
        public bool IsClose = false;
        List<IOCPToken<T>> tokenss=new List<IOCPToken<T>>();
        public IOCPState InitIOCPServer(LogAction log_action, int maxCount = 10, bool allowMorePlayerEnter = false, string ip = "0.0.0.0", int port = 45454)
        {
            sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            sck.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
            this.maxCount = maxCount;
            this.allowMorePlayerEnter = allowMorePlayerEnter;
            tokens = new Pool<IOCPToken<T>>(maxCount);
            if (log_action != null)
            {
                logaction = log_action;
            }
            if (IPAddress.TryParse(ip, out var address))
            {
                ip = address.ToString();
                IPEndPoint localEP = new IPEndPoint(address, port);
                sck.Bind(localEP);
                sck.Listen(0);
                this.port = port;
                state = IOCPState.Successful;
                eventAccept.Completed += AccpetAsyn;
                if (sck != null && !sck.AcceptAsync(eventAccept))
                {
                    AccpetAsyn(null, eventAccept);
                }
                return IOCPState.Successful;
            }
            logaction("Fail to init becauseof ip is error!");
            return IOCPState.Fail;
        }
        public void RemoveToken(IOCPToken<T> token)
        {
            if(tokenss.Contains(token))
            {
                token.OnDestory();
                tokenss.Remove(token);
                logaction($"[typeName:{typeof(T).Name}] Remove Token Successful");
            }
            else
            {
                logaction("Remove Token faied no found!");
            }
        }
        public void CloseServer()
        {
            if(!IsClose)
            {
                lock (tokenss)
                    foreach (var token in tokenss)
                    {
                        token.OnDestory();
                    }
                tokenss.Clear();
                IsClose = true;
                sck.Close();
                sck = null;
            }
        }
        private void AccpetAsyn(object obj, SocketAsyncEventArgs args)
        {
            if (args.AcceptSocket != null)
            {
                if (!allowMorePlayerEnter && curcount + 1 > maxCount)
                {
                    logaction($"[Warrning] server full CurrentPlayer[{curcount}]");
                    args.AcceptSocket = null;
                }
                else
                {
                    curcount++;
                    //logaction($"[Logic]Enter Player room now PlayerCount : {curcount}");
                    IOCPToken<T> iOCPToken = tokens.Pop();
                    iOCPToken.Reset(args.AcceptSocket, OnReceiveMessage, length,
                    (client)=>
                    {
                        OnCloseAccpet(client);
                        curcount--;
                        tokens.Push(client);
                    },
                    (str)=>
                    {
                        logaction(str);
                    });
                    lock (tokenss)
                        if (!tokenss.Contains(iOCPToken))
                        {
                            tokenss.Add(iOCPToken);
                        }
                    args.AcceptSocket = null;
                    AcceptClient(iOCPToken);
                }
            }
            if (!IsClose&&sck != null&& !sck.AcceptAsync(args))
            {
                AccpetAsyn(null, args);
            }
        }

        public abstract void OnCloseAccpet(IOCPToken<T> client);

        public abstract void OnReceiveMessage(IOCPToken<T> client, T message);

        public abstract void AcceptClient(IOCPToken<T> client);
    }
}