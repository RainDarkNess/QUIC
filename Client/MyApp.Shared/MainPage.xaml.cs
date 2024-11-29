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
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;

namespace MyApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
	

        List<string> pathOfFiles = new List<string>();

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
        }

        async void Button1_Click(object sender, RoutedEventArgs e){
            	String choose = TextType.Text;

                String ipTxt = IpText.Text;
                String port = PortText.Text;
               

            if(choose != "W" && choose != "w" && choose != "F" && choose != "f"){
                TextBoxStatus.Text = "Repet plize";
            }
            else if(choose == "F" || choose == "f"){
                    Console.WriteLine("picked file");
            if (!QuicConnection.IsSupported)
            {
                Console.WriteLine("QUIC is not supported, check for presence of libmsquic and support of TLS 1.3.");
                return;
            }

            var cert2 = CreateSelfSignedCertificate();
            var endPoint = IPEndPoint.Parse(ipTxt+port);
            Console.WriteLine(endPoint);

            var clientConnectionOptions = new QuicClientConnectionOptions{
                    RemoteEndPoint = endPoint,
                    DefaultStreamErrorCode = 0x0A,
                    DefaultCloseErrorCode = 0x0B,
                    MaxInboundUnidirectionalStreams = 10,
                    MaxInboundBidirectionalStreams = 100,
                    ClientAuthenticationOptions = new SslClientAuthenticationOptions
                    {
                    ClientCertificates = new X509CertificateCollection { cert2 },
                    ApplicationProtocols = new List<SslApplicationProtocol>
                    {
                    new SslApplicationProtocol("test")
                    },
                    //192.168.0.107
                    TargetHost =  ipTxt,
                    RemoteCertificateValidationCallback = (sender, chain, certificate, errors) => true
                    }
                };

            Console.WriteLine("Choose, File - [F] or Write to file - [W]");
            try
            {
                Console.WriteLine("Entering...");
                String path = TextPath.Text;

                byte[] AllStr = File.ReadAllBytes(path);  
                
                

                Console.WriteLine(AllStr.Length);
                var connection = await QuicConnection.ConnectAsync(clientConnectionOptions);  
                Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
                TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";
                await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                char[] myChars_command = new char[] {'s', 't', 'o', 'p', '$', '$'};
                
            String FileName = Path.GetFileName(path); 
            Console.WriteLine(FileName);
            char[] FileNameChars = new char[FileName.Length+2];  
            
                for (int i = 0; i < FileName.Length; i++) {  
                    FileNameChars[i] = FileName[i];  
                }  
                
                FileNameChars[FileNameChars.Length-1] = '$';
                FileNameChars[FileNameChars.Length-2] = '$';
                
                // char[] myChars = myChars_command.Concat(FileNameChars).ToArray();
            
                var AllBytes = new byte[FileNameChars.Length + AllStr.Length + myChars_command.Length];
                
            for(int l = 0; l < FileNameChars.Length; l++){
                Console.WriteLine(FileNameChars[l]);

                AllBytes[l] = Encoding.UTF8.GetBytes(FileNameChars)[l];
            }

            for(int t = 0; t < AllStr.Length; t++){
                AllBytes[FileNameChars.Length + t] = AllStr[t];
            }

            for(int l = 0; l < myChars_command.Length; l++){
                AllBytes[FileNameChars.Length + AllStr.Length + l] = Encoding.UTF8.GetBytes(myChars_command)[l];
                // Console.WriteLine("end");
            }

                if(MethodSend.Text == "New"){
                    int jj = 0;
                    int bbbb = 0;
                    try{
                        while(jj+Int32.Parse(BlockSize.Text) < AllBytes.Length){
                            await stream.WriteAsync(AllBytes[jj..(jj+Int32.Parse(BlockSize.Text))]);
                            jj+=Int32.Parse(BlockSize.Text);
                            Console.WriteLine("Packet# {0} sended", bbbb);
                            bbbb++;
                            Thread.Sleep(Int32.Parse(Period.Text));
                        }
                    }catch{
                        await stream.WriteAsync(AllBytes[jj..AllBytes.Length]);
                    }
                }else{
                    await stream.WriteAsync(AllBytes);
                }
            }
            catch(Exception ee)
            {
                Console.WriteLine("Exception: " + ee.Message);
            }
            finally
            {
                Console.WriteLine("Executing finally block.");
            }

                StatusOfSend.Text = StatusOfSend.Text + " done...";

            }
                
            else if(choose == "W" || choose == "w"){


            if (!QuicConnection.IsSupported)
            {
                Console.WriteLine("QUIC is not supported, check for presence of libmsquic and support of TLS 1.3.");
                return;
            }

            var cert2 = CreateSelfSignedCertificate();
            var endPoint = IPEndPoint.Parse(ipTxt+port);
            Console.WriteLine(endPoint);

            var clientConnectionOptions = new QuicClientConnectionOptions{
                RemoteEndPoint = endPoint,
                DefaultStreamErrorCode = 0x0A,
                DefaultCloseErrorCode = 0x0B,
                MaxInboundUnidirectionalStreams = 10,
                MaxInboundBidirectionalStreams = 100,
                ClientAuthenticationOptions = new SslClientAuthenticationOptions
                {
                ClientCertificates = new X509CertificateCollection { cert2 },
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                new SslApplicationProtocol("test")
                },
                //192.168.0.107
                TargetHost = ipTxt,
                RemoteCertificateValidationCallback = (sender, chain, certificate, errors) => true
                }
            };
            var connection = await QuicConnection.ConnectAsync(clientConnectionOptions);
            Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
            TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";

            //Pass the file path and file name to the StreamReader constructor
            Console.WriteLine("Choose, File - [F] or Write to file - [W]");

                Console.WriteLine("Writing");
                String WriteString = TextPath.Text;
            
                await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
                // Write
                byte[] AllStr = Encoding.UTF8.GetBytes(WriteString);
                char[] myChars = new char[] {'s', 't', 'o', 'p', '$', '$'};
                
                var AllBytes = new byte[AllStr.Length+6];

            for(int t = 0; t < AllStr.Length; t++){
                AllBytes[t] = AllStr[t];
            }
            for(int l = 0; l < Encoding.UTF8.GetBytes(myChars).Length; l++){
                AllBytes[AllStr.Length+l] = Encoding.UTF8.GetBytes(myChars)[l];
            }
            
            
                await stream.WriteAsync(AllBytes);
                
                await stream.WriteAsync(Encoding.UTF8.GetBytes(WriteString+'`'));
                // Read
                var buffer = new byte[4096];
                try{
                    int BlockSizeInt = Int32.Parse(BlockSize.Text);
                    buffer = new byte[BlockSizeInt];
                }catch{
                }
                await stream.ReadAsync(buffer);
                StatusOfSend.Text = StatusOfSend.Text + " done...";

            }

        }

        async void Button2_Click(object sender, RoutedEventArgs e){

            String WriteString = TextPath.Text;
            String choose = TextType.Text;

            var brodcastAddress = IPAddress.Parse(IpText.Text.Remove(IpText.Text.Length-1, 1)); 
            using var udpSender = new UdpClient();
            Console.WriteLine("Начало отправки пакетов...");
            
            if(choose != "W" && choose != "w" && choose != "F" && choose != "f"){
                TextBoxStatus.Text = "Repet plize";
            }
            else if(choose == "F" || choose == "f"){
                    Console.WriteLine("Entering...");
                    String path = TextPath.Text;

                    byte[] AllStr = File.ReadAllBytes(path);  
                    
                    

                    // Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
                    // TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";
                    char[] myChars_command = new char[] {'s', 't', 'o', 'p', '$', '$'};
                
                    String FileName = Path.GetFileName(path); 
                    Console.WriteLine(FileName);
                    char[] FileNameChars = new char[FileName.Length+2];  
                    
                        for (int i = 0; i < FileName.Length; i++) {  
                            FileNameChars[i] = FileName[i];  
                        }  
                        
                        FileNameChars[FileNameChars.Length-1] = '$';
                        FileNameChars[FileNameChars.Length-2] = '$';
                        
                        // char[] myChars = myChars_command.Concat(FileNameChars).ToArray();
                    
                        var AllBytes = new byte[FileNameChars.Length + AllStr.Length + myChars_command.Length];
                        
                    for(int l = 0; l < FileNameChars.Length; l++){
                        Console.WriteLine(FileNameChars[l]);

                        AllBytes[l] = Encoding.UTF8.GetBytes(FileNameChars)[l];
                    }

                    for(int t = 0; t < AllStr.Length; t++){
                        AllBytes[FileNameChars.Length + t] = AllStr[t];
                    }

                    for(int l = 0; l < myChars_command.Length; l++){
                        AllBytes[FileNameChars.Length + AllStr.Length + l] = Encoding.UTF8.GetBytes(myChars_command)[l];
                        // Console.WriteLine("end");
                    }

                
                    int jj = 0;
                    int bbbb = 0;
                    try{
                        while(jj+Int32.Parse(BlockSize.Text) < AllBytes.Length){
                            await udpSender.SendAsync(AllBytes[jj..(jj+Int32.Parse(BlockSize.Text))], new IPEndPoint(brodcastAddress, Int32.Parse(PortText.Text)));
                            jj+=Int32.Parse(BlockSize.Text);
                            Console.WriteLine("Packet# {0} sended", bbbb);
                            bbbb++;
                            Thread.Sleep(Int32.Parse(Period.Text));
                        }
                    }catch{
                        await udpSender.SendAsync(AllBytes[jj..AllBytes.Length], new IPEndPoint(brodcastAddress, Int32.Parse(PortText.Text)));
                    }


            }
            else if(choose == "W" || choose == "w"){

                byte[] AllStr = Encoding.UTF8.GetBytes(WriteString);
                char[] myChars = new char[] {'s', 't', 'o', 'p', '$', '$'};
                    
                var AllBytes = new byte[AllStr.Length+6];

                for(int t = 0; t < AllStr.Length; t++){
                    AllBytes[t] = AllStr[t];
                }
                for(int l = 0; l < Encoding.UTF8.GetBytes(myChars).Length; l++){
                    AllBytes[AllStr.Length+l] = Encoding.UTF8.GetBytes(myChars)[l];
                }
                
                    byte[] data = AllBytes;
                    await udpSender.SendAsync(data, new IPEndPoint(brodcastAddress, Int32.Parse(PortText.Text)));

                    // Read
                    var buffer = new byte[4096];
                    try{
                        int BlockSizeInt = Int32.Parse(BlockSize.Text);
                        buffer = new byte[BlockSizeInt];
                    }catch{
                    }
                    StatusOfSend.Text = StatusOfSend.Text + " done...";

            }
        }
	}
}
