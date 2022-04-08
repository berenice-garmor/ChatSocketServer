using System;
using System.Linq;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace chatSocketServer1
{
    class Program
    {
        public static Hashtable clientsList = new Hashtable();

        static void Main(string[] args)
        {
           
            //Se crear el socket principal con la direccion local del host del servidor
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList[0];
            IPAddress ipAddress = ipHostInfo.AddressList.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            TcpListener serverSocket = new TcpListener(ipAddress, 8888);
            TcpClient clientSocket = default(TcpClient);
            int counter = 0;

            //Se inicializa el socket principal
            serverSocket.Start();
            Console.WriteLine("Bot  ....");
            counter = 0;
            while ((true))
            {
                counter += 1;

                //Llego una solicitud de conexion nueva de un cliente, se acepta la conexion
                clientSocket = serverSocket.AcceptTcpClient();

                //Se reserva memoria para los mensajes del cliente
                byte[] bytesFrom = new byte[clientSocket.ReceiveBufferSize];
                string dataFromClient = null;

                //Leemos el mensaje recibido del cliente
                NetworkStream networkStream = clientSocket.GetStream();
                networkStream.Read(bytesFrom, 0, clientSocket.ReceiveBufferSize);
                dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);
                dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));

                //Si el nombre del cliente ya existe se borra de la lista de clientes
                clientsList.Remove(dataFromClient);
                clientsList.Add(dataFromClient, clientSocket);

                //Se lanza la una nueva conexion para aceptar al cliente nuevo 
                HandleClient client = new HandleClient();
                client.startClient(clientSocket, dataFromClient, clientsList);
                Console.WriteLine(dataFromClient + " hizo una pregunta ");

                //Le reenviamos el mensaje al resto de los clientes conectados
                //broadcast(dataFromClient + " se ha unido ", dataFromClient, false, clientSocket);
            }

            clientSocket.Close();
            serverSocket.Stop();
            Console.WriteLine("exit");
            Console.ReadLine();
        }

        //FUNCIONES DE LA VERSION 2.0 CLIMA Y TIPO DE CAMBIO
        public static void WriteAns(NetworkStream broadcastStream, Byte[] broadcastBytes, Byte[] broadcastAns, String res)
        {
            //escribe la respuesta para el cliente
            broadcastStream.Write(broadcastBytes, 0, broadcastBytes.Length);
            broadcastStream.Flush();
            broadcastAns = Encoding.ASCII.GetBytes(res);
            broadcastStream.Write(broadcastAns, 0, broadcastAns.Length);
            broadcastStream.Flush();
        }
        
        public static String splitCity(String msg) //funcion que separa la ciudad del output del usr
        {
            String[] delimiterChars = { "clima de " };
            String text = msg;
            String[] spearator = { "clima de " };
            Int32 count = 2;
            String[] strlist = text.Split(spearator, count,
            StringSplitOptions.RemoveEmptyEntries);
            foreach (String s in strlist) {
                text = s;
            }
            return text;
        }
      
        class weatherInfo //para la info del json
        {
            weatherInfo wi = new weatherInfo();
            public class main
            {
                public double temp { get; set; }
            }
            public class weather
            {
                public string desc { get; set; }
            }
            public class root
            {
                public main main { get; set; }
                public List<weather> weatherList { get; set; }
            }
        }

        public static String getWeater(String city)
        {
            String temp = "";
            using (WebClient web = new WebClient())
            {
                String url = string.Format("https://api.openweathermap.org/data/2.5/weather?q=" + city + "&lang=es&appid=c31e52a2a3ede84d5620322d0b8e35ac&units=metric&ctn=6");

                var json = web.DownloadString(url);

                var result = JsonConvert.DeserializeObject<weatherInfo.root>(json);
                weatherInfo.root output = result;

                temp = String.Format("{0}"+" C", output.main.temp);
            }
            return temp;
        }

        class RateInfo //para la info del json
        {
            RateInfo ri = new RateInfo();
            public class rates
            {
                public double USD { get; set; }
            }
            public class root
            {
                public rates rates { get; set; }
            }
        }
        public static String getChangeRate() //para obtener tipo de cambio de EURO a USD
        {
            String cambio = "";
            using (WebClient web = new WebClient())
            {
                String url = string.Format("http://api.exchangeratesapi.io/v1/latest?access_key=4703de325b6b76bb9cb9eb5974fa9bd6&symbols=USD,AUD,CAD,PLN,MXN&format=1");

                var json = web.DownloadString(url);

                var result = JsonConvert.DeserializeObject<RateInfo.root>(json);
                RateInfo.root output = result;

                cambio = String.Format("{0}" + "USD", output.rates.USD);
            }
            return cambio;
        }
        
        public static void broadcast(string msg, string uName, bool flag, TcpClient socket) //agregar parametro de a quien
        {
            TcpClient broadcastSocket;
            broadcastSocket = socket;
            if (broadcastSocket.Connected)
            {
                NetworkStream broadcastStream = broadcastSocket.GetStream();
                Byte[] broadcastBytes = null;
                Byte[] broadcastAns = null;
                String res="";

                if (flag == true)
                {
                    broadcastBytes = Encoding.ASCII.GetBytes(msg);
                    string ms = Encoding.UTF8.GetString(broadcastBytes);
                    broadcastAns = Encoding.ASCII.GetBytes(msg);

                    if (ms.Contains("clima de"))
                    {
                        //separar la ciudad del output
                        String city = splitCity(msg);
                        Console.WriteLine("ciity: "+city);

                        try{
                           String result= getWeater(city); //llamar funcion del clima
                           WriteAns(broadcastStream, broadcastBytes, broadcastAns, result);
                        }
                        catch (Exception e){
                        }
                    }//if contains clima
                    else if (ms.Contains("tipo de cambio"))
                    {
                        try{
                            String result= getChangeRate(); //llamar funcion del tipo de cambio
                            WriteAns(broadcastStream, broadcastBytes, broadcastAns, result);
                        }
                        catch (Exception e){
                        }
                    }//if contains Tipocambio
                    else if (ms.Contains("celebra el 9 de mayo")){
                        res = "Bot: El 9 de mayo se celebra el dia de goku";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("hola")){
                        res = "Bot: hola";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("capital de suecia")){
                        res = "Bot: La capital de suecia es Estocolmo";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("signo de kurt cobain")){
                        res = "Bot: Kurt cobain nacio el 20 de febrero por lo tanto es signo Piscis";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("numero de equipos en la nba")){
                        res = "Bot: La NBA tiene 30 equipos divididos en la Conferencia Este y la Conferencia Oeste";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("coseno de 0")){
                        res = "Bot: El coseno de 0 es 1";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("mejor spiderman")){
                        res = "Bot: mmm es complicado, pero diria que Andrew";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("dice perro en aleman")){
                        res = "Bot: Perro en aleman se dice Hund";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("pelicula mas antigua")) {
                        res = "Bot: La escena del jardin de Roundhay es la pelicula mas antigua en existencia: rodada en 1888";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else if (ms.Contains("puente de khazad dum")){
                        res = "Bot: Es el puente dentro de las Grandes Puertas de Moria, donde Gandalf se enfrento al Balrog";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                    else{
                        res = "Bot: mmm... no lo se, intenta de nuevo";
                        WriteAns(broadcastStream, broadcastBytes, broadcastAns, res);
                    }
                }
                else{
                    broadcastBytes = Encoding.ASCII.GetBytes(msg);
                }
            }
            else{
                clientsList.Remove(uName);
            }

            //}
        }  //end broadcast function
    }//end Main class

    public class HandleClient
    {
        TcpClient clientSocket;
        string clNo;
        Hashtable clientsList;

        public void startClient(TcpClient inClientSocket, string clineNo, Hashtable cList)
        {
            //Se inicializa la clase con los datos del cliente nuevo
            this.clientSocket = inClientSocket;
            this.clNo = clineNo;
            this.clientsList = cList;
            //Se lanza un hilo para poder recibir asincronamente mensajes
            Thread ctThread = new Thread(doChat);
            ctThread.Start();
        }

        private void doChat()
        {
            int requestCount = 0;
            //byte[] bytesFrom = new byte[10025];
            string dataFromClient = null;
            //Byte[] sendBytes = null;
            //string serverResponse = null;
            string rCount = null;
            requestCount = 0;

            while ((true))
            {
                try
                {
                    requestCount = requestCount + 1;
                    NetworkStream networkStream = clientSocket.GetStream();
                    byte[] bytesFrom = new byte[clientSocket.ReceiveBufferSize];

                    networkStream.Read(bytesFrom, 0, (int)clientSocket.ReceiveBufferSize);
                    dataFromClient = System.Text.Encoding.ASCII.GetString(bytesFrom);

                    //sc = System.Text.Encoding.ASCII.GetString(bytesFrom);

                    //aqui van los if del bot
                    if (dataFromClient.IndexOf("$") > 0)
                    {
                        dataFromClient = dataFromClient.Substring(0, dataFromClient.IndexOf("$"));
                        Console.WriteLine("Del cliente - " + clNo + " : " + dataFromClient);
                        rCount = Convert.ToString(requestCount);

                        Program.broadcast(dataFromClient, clNo, true, clientSocket); //poner parametro del cliente al que va
                    }
                    else
                    {
                        clientsList.Remove(clNo);
                        Console.WriteLine(clNo + " salió de la app ");
                        return;
                    }

                }
                catch (Exception ex)
                {
                    //clientSocket.Close();
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }//end while
        }//end doChat
    } //end class handleClinet
}//end namespace

