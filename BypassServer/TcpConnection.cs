using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace TcpGenericServerNET
{
    /// <summary>
    /// Clase que contiene informacion relacionada a la conexion, el TcpClient, el Reader el Writer, el ID
    /// Y datos customizables que pueden agregar las clases heredadas
    /// </summary>
    public class TcpConnection : IDisposable
    {
        protected static Object lastIdLock = new Object();
        protected static Int32 lastId = 0;
        public readonly int id;
        public readonly TcpClient client = null;
        public StreamReader reader = null;
        protected StreamWriter writer = null;
        protected Stream stream = null;
        public byte[] buff = null;
        public int buffPos = 0;
        public int buffTop = 0;
        public string delimiter { get; protected set; }
        protected byte[] delimiterArr = null;
        public Thread workerThread { get; protected set; }
        public Encoding encoding { get; protected set; }
        public volatile bool abort = false;
        public volatile bool aborted = false;

        static TcpConnection()
        {
        }

        public TcpConnection(TcpClient client)
            : this(client, "\r\n", Encoding.UTF8)
        {
        }

        public TcpConnection(TcpClient client, Encoding encoding)
            : this(client, "\r\n", encoding)
        {
        }

        public TcpConnection(TcpClient client, string delimiter) : this(client, delimiter, Encoding.UTF8)
        {
        }

        public TcpConnection(TcpClient client, string delimiter, Encoding encoding)
        {
            lock (lastIdLock)
            {
                lastId++;
                this.id = lastId;
            }
            this.encoding = encoding ?? Encoding.UTF8;
            this.delimiter = string.IsNullOrEmpty(delimiter) ? "\r\n" : delimiter;
            this.delimiterArr = encoding.GetBytes(this.delimiter);
            this.workerThread = workerThread;
            this.client = client;
            this.stream = client.GetStream();
            this.reader = new StreamReader(client.GetStream());
            this.writer = new StreamWriter(client.GetStream());
            this.writer.AutoFlush = true;
            if (delimiter == null) delimiter = "\r\n";
            buff = new byte[client.ReceiveBufferSize];
            buffPos = 0;
            buffTop = 0;
        }



        public int Available
        {
            get
            {
                if (client == null)
                    return 0;
                else
                    return client.Available;
            }
        }



        public virtual void doBeginReceive(AsyncCallback callback)
        {
            if (client.Connected && !abort)
                client.Client.BeginReceive(buff, buffTop, buff.Length - buffTop, SocketFlags.None, callback, this);
        }


        public virtual string ReadFromBuffer(int bytesReceived)
        {
            if (buffPos == buffTop) return null;
            int pos = buff.LocateFirst(delimiterArr,buffPos,buffTop-buffPos);
            if (pos < 0)
            {
                return null;
            }
            else
            {
                Debug.Assert(pos >= buffPos);
                string s = encoding.GetString(buff, buffPos, pos - buffPos);
                Buffer.BlockCopy(buff, pos + delimiterArr.Length, buff, 0, buff.Length - pos - delimiterArr.Length);
                buffPos = 0;
                buffTop -= pos + delimiterArr.Length;
                Debug.Assert(buffTop >= 0);
                return s;
            }
        }

        public virtual string ReadLine()
        {
            lock (reader)
            {
                try
                {
                    int bytesread = this.stream.Read(buff, buffPos, buff.Length - buffPos);
                    if (bytesread == 0)
                    {
                        return null;
                    }
                    else
                    {
                        buffPos += bytesread;
                        int pos = buff.LocateFirst(delimiterArr);
                        if (pos < 0)
                        {
                            return null;
                        }
                        else
                        {
                            string ret = encoding.GetString(buff, 0, pos);
                            Buffer.BlockCopy(buff, pos + delimiterArr.Length, buff, 0, buff.Length - pos - delimiterArr.Length);
                            pos = 0;
                            return ret;
                        }
                    }
                }
                catch (IOException)
                {
                    this.Dispose();
                    return null;
                }
                catch (Exception ex)
                {
                    ConnectionError("Error leyendo línea", ex);
                    this.Dispose();
                    return null;
                }
            }
        }

        public virtual void ConnectionError(string msg, Exception ex)
        {

        }


        public virtual void WriteLine(string line)
        {
            lock (writer)
            {
                try
                {
                    if (this.delimiter == null)
                        this.writer.WriteLine(line);
                    else
                        this.writer.Write(line + delimiter);
                }
                catch (Exception ex)
                {
                    ConnectionError("Error escribiendo línea", ex);
                    this.Dispose();
                    //throw;
                }
            }
        }

        public virtual void Write(string line)
        {
            lock (writer)
            {
                try
                {
                    this.writer.Write(line);
                }
                catch (Exception ex)
                {
                    ConnectionError("Error escribiendo datos", ex);
                    this.Dispose();
                    //throw;
                }
            }
        }


        public virtual void Write(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
        }

        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        public virtual void WriteFile(string fullPath)
        {
            FileStream file = new FileStream(fullPath,FileMode.Open,FileAccess.Read);
            byte[] buff = new byte[65536];
            try
            {
                while (true)
                {
                    int bytesRead = file.Read(buff, 0, buff.Length);
                    if (bytesRead == 0)
                    {
                        stream.Flush();
                        break;
                    }
                    else
                    {
                        stream.Write(buff, 0, bytesRead);
                    }
                }
            }
            finally
            {
                file.Close();
            }
        }


        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                abort = true;
                if (!aborted && this.workerThread != null)
                {
                    Thread.Sleep(100);
                    if (!aborted && this.workerThread != null)
                    {
                        Thread.Sleep(1000);
                        if (!aborted && this.workerThread != null)
                        {
                            try { this.workerThread.Abort(); }
                            catch { }
                            aborted = true;
                        }
                    }
                }
                if (this.client != null) { try { this.client.Close(); } catch { } }
            }
        }


        ~TcpConnection()
        {
            Dispose(false);
        }

    }

}
