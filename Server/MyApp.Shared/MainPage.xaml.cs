using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Net;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Globalization;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Text;  

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
	
	
X509Certificate2 CreateSelfSignedCertificate()
    {
    var rsa = RSA.Create();
    var certificateRequest = new CertificateRequest("CN= localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    certificateRequest.CertificateExtensions.Add(
    new X509BasicConstraintsExtension(
    certificateAuthority: false,
    hasPathLengthConstraint: false,
    pathLengthConstraint: 0,
    critical: true
    )
    );
    certificateRequest.CertificateExtensions.Add(
    new X509KeyUsageExtension(
    keyUsages:
    X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment |
    X509KeyUsageFlags.CrlSign | X509KeyUsageFlags.KeyCertSign,
    critical: false
    )
    );
    certificateRequest.CertificateExtensions.Add(
    new X509EnhancedKeyUsageExtension(
    new OidCollection {
    new Oid("1.3.6.1.5.5.7.3.2"), // TLS Client auth
    new Oid("1.3.6.1.5.5.7.3.1") // TLS Server auth
    },
    false));
    certificateRequest.CertificateExtensions.Add(
    new X509SubjectKeyIdentifierExtension(
    key: certificateRequest.PublicKey,
    critical: false
    )
    );
    var sanBuilder = new SubjectAlternativeNameBuilder();
    sanBuilder.AddDnsName("localhost");
    certificateRequest.CertificateExtensions.Add(sanBuilder.Build());
    return certificateRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(5));
    }
    
		public MainPage()
		{
			this.InitializeComponent();
			string hostName = Dns.GetHostName();  
    			Console.WriteLine(hostName);   
      
    			// Get the IP from GetHostByName method of dns class. 
    			string IP = Dns.GetHostByName(hostName).AddressList[1].ToString(); 
     			Console.WriteLine("IP Address is : " + IP);  
                IpText.Text = IP+':';
                PortText.Text = "8081";
		}
		async void StartQuic_Click(object sender, RoutedEventArgs e){
		
                bool IsWorking = true;


                        String IpStr = IpText.Text;
                        String PortStr = PortText.Text;
                        String AllIpPort = IpStr+PortStr;
            if (!QuicListener.IsSupported)
            {
            Console.WriteLine("QUIC is not supported, check for presence of libmsquic and support of TLS 1.3.");
            return;
            }
            while(IsWorking){
                var cert2 = CreateSelfSignedCertificate();
                var serverConnectionOptions = new QuicServerConnectionOptions
                {
                DefaultStreamErrorCode = 0x0A,
                DefaultCloseErrorCode = 0x0B,
                ServerAuthenticationOptions = new SslServerAuthenticationOptions
                {
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("test") },
                ServerCertificate = cert2,
                ClientCertificateRequired = false,
                RemoteCertificateValidationCallback = (sender, chain, certificate, errors) => true,
                }
                };
                var listener = await QuicListener.ListenAsync(new QuicListenerOptions
                {
                ListenEndPoint = IPEndPoint.Parse(AllIpPort),
                ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("test") },
                ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(serverConnectionOptions)
                }); 
                
            TextBoxConnect.Text = "Запуск QUIC...";
            Console.WriteLine(listener.LocalEndPoint);
                int k = 0;
                String EndString = null;
                await using var connection = await listener.AcceptConnectionAsync();
                await using var stream = await connection.AcceptInboundStreamAsync();

                Console.WriteLine("Begin connection");
                TextBoxConnect.Text = "Client connected with " + listener.LocalEndPoint;

                // Read
                String masaggeToString = null;
                var buffer = new byte[1024];

                
                try{
                    int BufferSizeCons = Int32.Parse(BufferSize.Text); 
                    buffer = new byte[BufferSizeCons];
                }catch{}

                    FileText.Text = " ";
                    int bufferCount = 0;
                    int memorySize = 0;
                    int indexOfConsoleLog = 0;
                    DateTime localDate_start = DateTime.Now;
                    ConsoleText.Text = " ";
                    byte[] tmpBuffer = new byte[0];
                    byte[] byteName = new byte[0];
                    List<string> name = new List<string>();
                    String result_name = "";
                    string cleanName = "";
                    bool isNotNamed = true;
                        int b = 0;
                        int bb = 0;
                    FileStream sw = new FileStream("text.txt", FileMode.Create);

                while(await stream.ReadAsync(buffer) > 0){

                    DateTime localDate_buffer = DateTime.Now;
                    bufferCount++;
                    Console.WriteLine("Buffer #"+bufferCount);
                    int tmpIntVal = 0;

                    if(isNotNamed){

                        while( b < buffer.Length){
                            
                                    if(buffer[b] == 36 && buffer[b+1] == 36){
                                        bb = b + 2;
                                        Console.WriteLine(bb);
                                        b = buffer.Length;
                                    }else{
                                        byteName = new byte[1];
                                        byteName[0] = buffer[b];
                                        Console.WriteLine(Encoding.UTF8.GetString(byteName, 0, byteName.Length));
                                        result_name += Encoding.UTF8.GetString(byteName, 0, byteName.Length);
                                    }
                                    b++;
                        }
                        sw = new FileStream(result_name, FileMode.Create);
                        isNotNamed = false;
                        Console.WriteLine("named");
                    }
                    for(int l = +bb; l < buffer.Length; l++){
                        try{
                            if((buffer[l] == 115) && (buffer[l+1] == 116) && (buffer[l+2] == 111) && (buffer[l+3] == 112) && (buffer[l+4] == 36) && (buffer[l+5] == 36)){
                            
                                break;
                            }
                            else{
                                tmpIntVal++;
                                tmpBuffer = new byte[tmpIntVal];
                            }
                        }catch(Exception jk){
                            tmpIntVal++;
                            tmpBuffer = new byte[tmpIntVal];
                            Console.WriteLine(jk);
                        }
                    }
                    
                    DateTime localDate_buufer_end = DateTime.Now;
                    TimeSpan localDate_buffer_value = localDate_buffer.Subtract(localDate_buufer_end);
                    Console.WriteLine("new buffer length "+tmpBuffer.Length + " time end: " + localDate_buffer_value);
                    if(indexOfConsoleLog < 5){
                        ConsoleText.Text += "Новая длина буффера: "+tmpBuffer.Length+'\n';
                    }else{
                        ConsoleText.Text = " ";
                        indexOfConsoleLog = 0;
                    }
                    indexOfConsoleLog += 1;
                    for(int y = 0; y < tmpBuffer.Length; y++){
                        tmpBuffer[y] = buffer[y+bb];
                    }

                    memorySize+=tmpBuffer.Length;
                    ReadedText.Text = Encoding.UTF8.GetString(tmpBuffer, 0, tmpBuffer.Length);

                    char[] charsToTrim = {' '};
                    cleanName = result_name.Trim(charsToTrim);
                    
                    sw.Write(tmpBuffer, 0, tmpBuffer.Length);

                    bb = 0;
                }
                    sw.Close();

                Console.WriteLine("readed buffer");
                Console.WriteLine(memorySize);
                DateTime localDate_end = DateTime.Now;
                TimeSpan localDate = localDate_start.Subtract(localDate_end);

                FileText.Text = "Размер файла: "+ memorySize + " байтов" + ", отправка заняла: " + localDate.Minutes + " Минут " + localDate.Seconds + " Секунд " + localDate.Milliseconds + " Милисекунд\n " + " файл с названием "+ cleanName +" сохранен в эту деррикторию " + ". Количество принятых блоков: " + bufferCount;
                try
                {
                    await listener.DisposeAsync();

                }
                catch(Exception ee)
                {
                    Console.WriteLine("Exception: " + ee.Message);
                }
                finally
                {
                    Console.WriteLine("Executing finally block.");
                }
                k++;
            }
            Console.WriteLine("Exit");
		}
        async void DelLogs_Click(object sender, RoutedEventArgs e){
            ConsoleText.Text = " ";
            ReadedText.Text = " ";
        }

        async void StartUDP_Click(object sender, RoutedEventArgs e){
            Console.WriteLine("UDP is upped");    
            TextBoxConnect.Text = "Подключено с UDP...";
            var receiveClient = new UdpClient();
            receiveClient.EnableBroadcast = true;
            receiveClient.Client.Bind(new IPEndPoint(IPAddress.Parse(IpText.Text.Remove(IpText.Text.Length-1, 1)), Int32.Parse(PortText.Text)));

                    FileText.Text = " ";
                    int bufferCount = 0;
                    int memorySize = 0;
                    int indexOfConsoleLog = 0;
                    DateTime localDate_start = DateTime.Now;
                    ConsoleText.Text = " ";
                    byte[] tmpBuffer = new byte[0];
                    byte[] byteName = new byte[0];
                    List<string> name = new List<string>();
                    String result_name = "";
                    string cleanName = "";
                    bool isNotNamed = true;
                        int b = 0;
                        int bb = 0;
                    FileStream sw = new FileStream("text.txt", FileMode.Create);
                    bool time_checked = false;
                    DateTime localDate_end = DateTime.Now;


            while (true)
            {
                if(!time_checked){
                    localDate_start = DateTime.Now;
                    time_checked = true;
                }

                var result = await receiveClient.ReceiveAsync();
                var buffer = result.Buffer;

                    DateTime localDate_buffer = DateTime.Now;
                    bufferCount++;
                    Console.WriteLine("Buffer #"+bufferCount);
                    int tmpIntVal = 0;

                    if(isNotNamed){

                        while( b < buffer.Length){
                            
                                    if(buffer[b] == 36 && buffer[b+1] == 36){
                                        bb = b + 2;
                                        Console.WriteLine(bb);
                                        b = buffer.Length;
                                    }else{
                                        byteName = new byte[1];
                                        byteName[0] = buffer[b];
                                        Console.WriteLine(Encoding.UTF8.GetString(byteName, 0, byteName.Length));
                                        result_name += Encoding.UTF8.GetString(byteName, 0, byteName.Length);
                                    }
                                    b++;
                        }
                        sw = new FileStream(result_name, FileMode.Create);
                        isNotNamed = false;
                        Console.WriteLine("named");
                    }
                    for(int l = +bb; l < buffer.Length; l++){
                        try{
                            if((buffer[l] == 115) && (buffer[l+1] == 116) && (buffer[l+2] == 111) && (buffer[l+3] == 112) && (buffer[l+4] == 36) && (buffer[l+5] == 36)){
                                
                                break;
                            }
                            else{
                                tmpIntVal++;
                                tmpBuffer = new byte[tmpIntVal];
                            }
                        }catch(Exception jk){
                            tmpIntVal++;
                            tmpBuffer = new byte[tmpIntVal];
                            Console.WriteLine(jk);
                        }
                    }
                    
                    DateTime localDate_buufer_end = DateTime.Now;
                    TimeSpan localDate_buffer_value = localDate_buffer.Subtract(localDate_buufer_end);
                    Console.WriteLine("new buffer length "+tmpBuffer.Length + " time end: " + localDate_buffer_value);
                    if(indexOfConsoleLog < 5){
                        ConsoleText.Text += "Новая длина буффера: "+tmpBuffer.Length+'\n';
                    }else{
                        ConsoleText.Text = " ";
                        indexOfConsoleLog = 0;
                    }
                    indexOfConsoleLog += 1;
                    for(int y = 0; y < tmpBuffer.Length; y++){
                        
                        tmpBuffer[y] = buffer[y+bb];
                    }

                    memorySize+=tmpBuffer.Length;
                    ReadedText.Text = Encoding.UTF8.GetString(tmpBuffer, 0, tmpBuffer.Length);

                    char[] charsToTrim = {' '};
                    cleanName = result_name.Trim(charsToTrim);
                    
                    sw.Write(tmpBuffer, 0, tmpBuffer.Length);

                    bb = 0;
                    buffer = null;

                    time_checked = false;
                    TimeSpan localDate = localDate_start.Subtract(localDate_end);
                    FileText.Text = "Размер файла: "+ memorySize + " байтов" + ", отправка заняла: " + localDate.Minutes + " Минут " + localDate.Seconds + " Секунд " + localDate.Milliseconds + " Милисекунд\n " + " файл с названием "+ cleanName +" сохранен в эту деррикторию " + ". Количество принятых блоков: " + bufferCount;
            }
            sw.Close();
            Console.WriteLine("Writed   ");
        }
	}
}

