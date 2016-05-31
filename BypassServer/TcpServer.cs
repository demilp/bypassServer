using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Diagnostics;
using System.Net;
//using log4net;

namespace TcpGenericServerNET
{
    public class TcpServer : IDisposable
    {
        protected TcpListener listener = null;
        protected AsyncCallback acceptConnectionCallback;
        protected WaitCallback handleCommunication;
        protected AsyncCallback receiveData;

        public string delimiter { get; protected set; }
        public Dictionary<int, TcpConnection> connections { get; protected set; }
        ////private static log4net.ILog logger = null;

        protected static volatile int workerThreadCount = 0;
        protected static object workerThreadCountLock = new object();
        protected int maxConn;

        static TcpServer()
        {
            /*if (!log4net.LogManager.GetRepository().Configured)
                log4net.Config.XmlConfigurator.Configure();
            logger = log4net.LogManager.GetLogger(typeof(TcpServer));*/
        }

        /// <summary>
        /// Iniciar el Server TCP y escuchar en el puerto indicado.
        /// Esta clase implementa IDisposable, llamar al metodo Dispose para detener el servidor y dejar de escuchar
        /// </summary>
        /// <param name="port">Numero de puerto</param>
        /// <param name="maxConn">Cantidad máxima de conexiones</param>
        /// <param name="delimiter">Delimitador (si se omite es CR+LF)</param>
        public TcpServer(int port, int maxConn = 0, string delimiter = null)
        {
            try
            {
                this.maxConn = maxConn;
                this.delimiter = delimiter;
                this.connections = new Dictionary<int, TcpConnection>();
                listener = new TcpListener(IPAddress.Any, port);
                acceptConnectionCallback = new AsyncCallback(acceptConnectionCallback_handler);
                handleCommunication = new WaitCallback(handleCommunication_handler);
                DoInitialization();
                listener.Start();
                receiveData = new AsyncCallback(receiveData_handler);
                listener.BeginAcceptTcpClient(acceptConnectionCallback, null);
            }
            catch (Exception ex)
            {
                //logger.Error("Error inicializando TcpServer", ex);
                throw;
            }
        }

        /// <summary>
        /// Sobrecargar para hacer cualquier inicializacion necesaria antes de que el servidor empiece a escuchar
        /// </summary>
        protected virtual void DoInitialization()
        {
        }


        protected virtual void acceptConnectionCallback_handler(IAsyncResult ar)
        {
            if (listener == null) return;
            try
            {
                TcpClient client;
                try
                {
                    client = listener.EndAcceptTcpClient(ar);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }
                if (maxConn > 0 && connections.Count >= maxConn)
                {
                    // Cerrar inmediatamente la conexion si excedio el maximo
                    try
                    {
                        client.Client.Shutdown(SocketShutdown.Send);
                    }
                    catch (Exception) { }
                    try
                    {
                        client.Client.Close();
                    }
                    catch (Exception) { }
                }
                else
                {
                    ThreadPool.QueueUserWorkItem(handleCommunication, client);
                }
            }
            catch (Exception ex)
            {
                //logger.Error("Error aceptando conexion", ex);
            }
            finally
            {
                try
                {
                    Thread.Sleep(100);
                    listener.BeginAcceptTcpClient(acceptConnectionCallback, null);
                }
                catch (Exception ex)
                {
                    //logger.Error("Error aceptando conexion", ex);
                }
            }
        }



        /// <summary>
        /// Agrega una nueva conexion a la lista de conexiones
        /// </summary>
        /// <param name="connection"></param>
        protected virtual void addConnection(TcpConnection connection)
        {
            lock (connections) { connections.Add(connection.id, connection); }
        }

        /// <summary>
        /// Elimina una conexion de la lista de conexiones
        /// </summary>
        /// <param name="connection"></param>
        protected virtual void removeConnection(TcpConnection connection)
        {
            lock (connections) { connections.Remove(connection.id); }
        }



        /// <summary>
        /// Crea un nuevo TcpConnection
        /// Si se quieren agregar atributos a TcpConnection heredar una clase de TcpConnection
        /// y reescribir este metodo para que cree la clase heredada
        /// </summary>
        /// <param name="client">TcpClient de la conexion</param>
        /// <returns></returns>
        protected virtual TcpConnection connectionFactory(TcpClient client)
        {
            return new TcpConnection(client, delimiter);
        }



        protected virtual void handleCommunication_handler(object obj)
        {
            TcpClient client = (TcpClient)obj;
            TcpConnection conn = null;
            try
            {
                conn = connectionFactory(client);
                addConnection(conn);
                this.ClientConnected(conn);
                conn.doBeginReceive(receiveData);
            }
            catch (Exception ex)
            {
                //logger.Error("Error iniciando comunicación con el cliente", ex);
                if (conn != null) closeConnection(conn);
            }
        }


        protected void receiveData_handler(IAsyncResult ar)
        {
            try
            {
                TcpConnection conn = (TcpConnection)ar.AsyncState;
                int bytesReceived = conn.client.Client.EndReceive(ar);
                conn.buffTop += bytesReceived;
                //Debug.Assert(conn.buffTop < conn.buff.Length);
                if (conn.buffTop >= conn.buff.Length)
                {
                    Array.Resize(ref conn.buff, conn.buffTop + 1);
                }
                if (conn.abort || !conn.client.Connected || bytesReceived == 0)
                {
                    closeConnection(conn);
                }
                else
                {
                    string s;
                    while (null != (s = conn.ReadFromBuffer(bytesReceived)))
                    {
                        try
                        {
                            this.DataArrived(conn, s);
                        }
                        catch (Exception ex)
                        {
                            //logger.Error("Error procesando datos recibidos", ex);
                        }
                    }
                    conn.doBeginReceive(receiveData);
                }
            }
            catch (SocketException)
            {
                TcpConnection conn = (TcpConnection)ar.AsyncState;
                closeConnection(conn);
            }
            catch (Exception ex)
            {
                //logger.Error("Error recibiendo datos", ex);
                // throw;
            }
        }



        public void closeConnection(TcpConnection conn)
        {
            try
            {
                removeConnection(conn);
                ShutdownConnection(conn);
                this.ClientDisconnected(conn);
                conn.Dispose();
            }
            catch (Exception ex)
            {
                //logger.Error("Error cerrando conexión", ex);
                //throw;
            }
        }


        protected virtual void ShutdownConnection(TcpConnection conn)
        {
            try
            {
                conn.client.Client.Shutdown(SocketShutdown.Send);
                while (conn.ReadLine() != null) { }
            }
            catch (Exception) { }
            try
            {
                conn.client.Client.Close();
            }
            catch (Exception) { }
        }


        /// <summary>
        /// Deja de escuhar, cierra todas las conexiones y libera recursos
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Deja de escuhar, cierra todas las conexiones y libera recursos
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (listener != null)
                    try { listener.Stop(); }
                    catch { }
                // if (threadListener != null) { try { threadListener.Abort(); } catch { }; }
                lock (connections)
                {
                    foreach (TcpConnection conn in connections.Values)
                        conn.Dispose();
                }
            }
        }

        ~TcpServer()
        {
            Dispose(false);
        }

        public TcpConnection[] ConnectedConnections()
        {
            foreach (var item in connections)
            {
                item.Value.WriteLine("");
                if (!item.Value.client.Connected)
                {
                    connections.Remove(item.Key);
                }
            }
            TcpConnection[] c = new TcpConnection[connections.Count];
            connections.Values.CopyTo(c, 0);
            return c;
        }
        /// <summary>
        /// Sobrecargar este metodo para manejar el arribo de datos.
        /// El server maneja streams orientados a texto, y este metodo es llamado por cada linea de texto recibida
        /// </summary>
        public virtual void DataArrived(TcpConnection connection, string data)
        {
        }

        /// <summary>
        /// Sobrecargar para manejar la conexión de un nuevo cliente.
        /// </summary>
        public virtual void ClientConnected(TcpConnection connection)
        {
        }

        /// <summary>
        /// Sobrecargar para manejar la desconexión de un cliente.
        /// </summary>
        public virtual void ClientDisconnected(TcpConnection connection)
        {
        }



    }
}
