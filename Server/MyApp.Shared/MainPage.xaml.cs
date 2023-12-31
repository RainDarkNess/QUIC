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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MyApp
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class MainPage : Page
	{
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
		async void Button1_Click(object sender, RoutedEventArgs e){
		 
     
			TextBox1.Text = "Connect starting...";
            String IpStr = IpText.Text;
            String PortStr = PortText.Text;
            String AllIpPort = IpStr+PortStr;
if (!QuicListener.IsSupported)
{
Console.WriteLine("QUIC is not supported, check for presence of libmsquic and support of TLS 1.3.");
return;
}
    
bool IsWorking = true;

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
     
TextBoxStatus.Text = "Connect started...";
Console.WriteLine(listener.LocalEndPoint);
int k = 0;
    String EndString = null;
    await using var connection = await listener.AcceptConnectionAsync();
    await using var stream = await connection.AcceptInboundStreamAsync();

    Console.WriteLine("Begin connection");
	TextBoxConnect.Text = "Client connected with " + listener.LocalEndPoint;
    // Read
    String masaggeToString = null;
    var buffer = new byte[4060];
    while(await stream.ReadAsync(buffer) > 1){
        Console.WriteLine("readed: " + buffer.Length+ " bytes");
        ConsoleText.Text = ConsoleText.Text + "\r readed: " + buffer.Length+ " bytes";
        masaggeToString  = masaggeToString + Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        //Console.WriteLine(masaggeToString);
        //break;
    }
    Console.WriteLine("readed buffer");
	Console.WriteLine(masaggeToString.Length);
	Console.WriteLine("readed local file");
    EndString = EndString + masaggeToString;
    Console.WriteLine(EndString.Length);
    String EndStringNew = null;
    // Console.WriteLine(EndString);
    String nameOFFile = null;
    int is_percent = 0;
	int removeOne = 0;
	bool isChecked = false;
	int removeTwo = 0;
    try{
        for(int i = 0; i < EndString.Length; i++){
            if(EndString[i] == '%'){
                is_percent++;
                if(!isChecked){
                	removeOne = i;
                	isChecked = true;
                }
                
                //i = EndString.Length;
                //break;
            }
            else if(is_percent == 1 & EndString[i] != '%'){
                nameOFFile = nameOFFile + EndString[i];
		removeTwo = i;
                Console.WriteLine(nameOFFile);
            }else{
		removeTwo = i;
		Console.WriteLine("substring");Console.WriteLine(removeOne);Console.WriteLine(removeTwo);
		EndString = EndString.Remove(removeOne, removeTwo);
        	i = EndString.Length;
                break;
            }

        }
    }catch(Exception ee){}
        try
    {
        if(nameOFFile != null & k == 0){
            StreamWriter sw = new StreamWriter(nameOFFile);
            //sw.WriteLine(EndStringNew);
            sw.WriteLine(EndString);
            sw.Close();
            ReadedFiles.Text = ReadedFiles.Text + "\r"+nameOFFile;
        }else{
		StreamWriter sw = new StreamWriter("test.txt");
            //sw.WriteLine(EndStringNew);
            sw.WriteLine(EndString);
            sw.Close();
            ReadedFiles.Text = ReadedFiles.Text + "\r"+"test.txt";
        }
		FileReaderText.Text = "Client sended this: "+EndStringNew;
        ReadedText.Text = ReadedText.Text+" \r"+EndStringNew;
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
    if(k == 1){
        IsWorking = false;
    }
}

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
Console.WriteLine("Exit");

		}
        async void Button3_Click(object sender, RoutedEventArgs e){
            ConsoleText.Text = " ";
            ReadedText.Text = " ";
        }
	}
}

