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
        List<string> pathOfFiles = new List<string>();

		public MainPage()
		{
			this.InitializeComponent();
		}
		async void Button1_Click(object sender, RoutedEventArgs e){
            String ipTxt = IpText.Text;
            String port = PortText.Text;
        String choose = TextType.Text;

        if(choose != "W" && choose != "w" && choose != "F" && choose != "f"){
            TextBoxStatus.Text = "Repet plize";
        }
        else if(choose == "F" || choose == "f"){
// foreach(string pathString in pathOfFiles){
            	Console.WriteLine("picked file");
		// Console.WriteLine(pathString);
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

    //var connection = await QuicConnection.ConnectAsync(clientConnectionOptions);
    //Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
    //TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";

    //Pass the file path and file name to the StreamReader constructor
    Console.WriteLine("Choose, File - [F] or Write to file - [W]");
        // Console.WriteLine(pathString);
    try
    {
        // string[] name_arr = pathString.Split('/');
        // string name = name_arr[name_arr.Length-1];
        Console.WriteLine("Entering...");
        // String path = pathString;
        String path = TextPath.Text;

      //  String allTextLocal = File.ReadAllText(path);
      	byte[] AllStr = File.ReadAllBytes(path);  
      	//AllStr.Join("", AllStr);
        //String AllStr = "%"+name+"%";

        //AllStr = AllStr + allTextLocal;
        Console.WriteLine(AllStr.Length);
    	var connection = await QuicConnection.ConnectAsync(clientConnectionOptions);  
    	Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
    	TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";
        await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        char[] myChars = new char[] {'s', 't', 'o', 'p', '$', '$'};
        var AllBytes = new byte[AllStr.Length+6];
	for(int t = 0; t < AllStr.Length; t++){
		AllBytes[t] = AllStr[t];
		//Console.WriteLine("new bytes "+AllBytes[t]);
	}
	//Console.WriteLine("new len "+AllBytes.Length);
	for(int l = 0; l < Encoding.UTF8.GetBytes(myChars).Length; l++){
	//Console.WriteLine(Encoding.UTF8.GetBytes(myChars)[l]);
	
	AllBytes[AllStr.Length+l] = Encoding.UTF8.GetBytes(myChars)[l];
	}
	
	
        await stream.WriteAsync(AllBytes);
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
// }
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
        await stream.WriteAsync(Encoding.UTF8.GetBytes(WriteString+'`'));
        // Read
        var buffer = new byte[4096];
        try{
            int BlockSizeInt = Int32.Parse(BlockSize.Text);
            buffer = new byte[BlockSizeInt];
        }catch{
        }
        await stream.ReadAsync(buffer);
        // Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
        StatusOfSend.Text = StatusOfSend.Text + " done...";

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
        


        }



    }
		async void Button2_Click(object sender, RoutedEventArgs e){
            var filePicker = new Windows.Storage.Pickers.FileOpenPicker();
            // folderPicker.FileTypeFilter.Add("*");
            // var folder = await folderPicker.PickSingleFolderAsync();
            // Console.WriteLine(folder);
            filePicker.FileTypeFilter.Add("*");
            var file = await filePicker.PickSingleFileAsync();
            Console.WriteLine(file.Name);
            // TextPath.Text = TextPath.Text+file.Path+file.Name+'~';
            //listwiew1.Items.Add(file.Name);
            pathOfFiles.Add(file.Path);
            Console.WriteLine(file.Path);
            foreach(string a in pathOfFiles){
                Console.WriteLine(a);
            }

        }
		async void Button3_Click(object sender, RoutedEventArgs e){
            
        }
	}
}
