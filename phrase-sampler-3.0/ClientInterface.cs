namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Net.Sockets;

    class ClientInterface
    {
        private String server;
        private int port;
        private NetworkStream stream;
        private TcpClient client;
        private Boolean connected = false;

        //--------------------------------------------------
        private static ClientInterface clientInstance = null;
        private static int instanceCreationCount = 0;

        public static ClientInterface getClientInstance(){
            if(clientInstance==null){
                clientInstance = new ClientInterface();
                instanceCreationCount++;
            }
            return clientInstance;
        }
        //--------------------------------------------------

        private ClientInterface(){
            //server = "143.215.199.231";
            server = "localhost";
            port = 5005;
            clientInstance = this;
            
        }

        public String connect(){
            Console.WriteLine("Inside client interface! connect function!\n");
            String status = "not attempted";
            String message = "connection attempt";
            try
            {
                client = new TcpClient(server, port);
                // Translate the passed message into ASCII and store it as a Byte array.
                Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);         

                // Get a client stream for reading and writing.
                //  Stream stream = client.GetStream();
                stream = client.GetStream();

                // Send the message to the connected TcpServer. 
                stream.Write(data, 0, data.Length);

                                                                            //Console.WriteLine("Sent: {0}", message);         

                // Receive the TcpServer.response.
                // Buffer to store the response bytes.
                data = new Byte[256];
                // String to store the response ASCII representation.
                String responseData = String.Empty;

                // Read the first batch of the TcpServer response bytes.
                Int32 bytes = stream.Read(data, 0, data.Length);
                responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                                                                            //Console.WriteLine("Received: {0}", responseData);         
                connected = true;
        
            } 
            catch (ArgumentNullException e) 
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            } 
            catch (SocketException e) 
            {
                Console.WriteLine("SocketException: {0}", e);
            }

            Console.WriteLine("\n Press Enter to continue...");
            Console.Read();
            
            return status;
        }


        public void disconnect()
        {
            // Close everything.
            stream.Close();
            client.Close();
            connected = false;
        }


        public void sendData(String message)
        {
            if (connected)
            {
                try
                {
                    Byte[] data = System.Text.Encoding.ASCII.GetBytes(message);

                    // Get a client stream for reading and writing.
                    //  Stream stream = client.GetStream();
                    stream = client.GetStream();

                    // Send the message to the connected TcpServer. 
                    stream.Write(data, 0, data.Length);
                    //Console.WriteLine("Sent: {0}", message);

                    // Receive the TcpServer.response.
                    // Buffer to store the response bytes.
                    data = new Byte[256];
                    // String to store the response ASCII representation.
                    String responseData = String.Empty;

                    // Read the first batch of the TcpServer response bytes.
                    Int32 bytes = stream.Read(data, 0, data.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
                    char[] c = responseData.ToCharArray();
                    if (c[0].Equals('@'))
                    {
                        Console.WriteLine("Received: {0}", responseData);
                    }
                    
 
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                }
                catch (SocketException e)
                {
                    Console.WriteLine("SocketException: {0}", e);
                }
            }
            else
            {
                Console.WriteLine("Error! Disconnected from server!");
            }
        }

        public Boolean isConnected()
        {
            return this.connected;
        }



    }
}
