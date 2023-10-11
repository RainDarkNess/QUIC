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
		}
		async void Button1_Click(object sender, RoutedEventArgs e){
			TextBox1.Text = "Connect starting...";

if (!QuicListener.IsSupported)
{
Console.WriteLine("QUIC is not supported, check for presence of libmsquic and support of TLS 1.3.");
return;
}
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
ListenEndPoint = IPEndPoint.Parse("127.0.0.1:8081"),
ApplicationProtocols = new List<SslApplicationProtocol>() { new SslApplicationProtocol("test") },
ConnectionOptionsCallback = (_, _, _) => ValueTask.FromResult(serverConnectionOptions)
});
TextBoxStatus.Text = "Connect started...";
Console.WriteLine(listener.LocalEndPoint);
bool IsWorking = true;
int k = 0;


while(IsWorking){
    
    String EndString = null;
    await using var connection = await listener.AcceptConnectionAsync();
    await using var stream = await connection.AcceptInboundStreamAsync();
    Console.WriteLine("Begin connection");
	TextBoxConnect.Text = "Client connected with " + listener.LocalEndPoint;
    // Read
    var buffer = new byte[100];
    await stream.ReadAsync(buffer);
    Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, buffer.Length));
    String masaggeToString = Encoding.UTF8.GetString(buffer, 0, buffer.Length);

    EndString = EndString + masaggeToString;
    String EndStringNew = null;
    Console.WriteLine(EndString);
    String nameOFFile = null;
    int is_percent = 0;

    try{
        for(int i = 0; i < EndString.Length; i++){
            if(EndString[i] == '%'){
                is_percent++;
            }
            if(is_percent == 1 & EndString[i] != '%'){
                nameOFFile = nameOFFile + EndString[i];
            }else if(is_percent == 2){
                i = EndString.Length;
            }
        }
        for(int i = 0; i < EndString.Length; i++){
            Console.WriteLine(EndString[i]);
            EndStringNew = EndStringNew + EndString[i];
            if(EndString[i+1] == '`'){
                i = EndString.Length;
            }

        }
    }catch(Exception ee){}
        try
    {
        StreamWriter sw = new StreamWriter(nameOFFile);
        sw.WriteLine(EndStringNew);
		FileReaderText.Text = "Client sended this: "+EndStringNew;
        sw.Close();
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
    if(k == 10){
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
/*await listener.DisposeAsync();*/


		}
	}
}
