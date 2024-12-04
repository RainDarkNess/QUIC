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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;

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

private async void QUICSend_Click(object sender, RoutedEventArgs e)
{
    string choose = TextType.Text.ToLower();
    string ipTxt = IpText.Text;
    string port = PortText.Text;

    if (choose != "w" && choose != "f")
    {
        TextBoxStatus.Text = "Ошибка: неверный выбор. Повторите.";
        return;
    }

    if (!QuicConnection.IsSupported)
    {
        Console.WriteLine("QUIC не поддерживается. Проверьте наличие libmsquic и поддержку TLS 1.3.");
        return;
    }

    var cert2 = CreateSelfSignedCertificate();
    var endPoint = IPEndPoint.Parse($"{ipTxt}{port}");
    Console.WriteLine(endPoint);

    var clientConnectionOptions = new QuicClientConnectionOptions
    {
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
            TargetHost = ipTxt,
            RemoteCertificateValidationCallback = (sender, chain, certificate, errors) => true
        }
    };

    try
    {
        var connection = await QuicConnection.ConnectAsync(clientConnectionOptions);
        Console.WriteLine($"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}");
        TextBoxStatus.Text = $"Connected {connection.LocalEndPoint} —> {connection.RemoteEndPoint}";

        await using var stream = await connection.OpenOutboundStreamAsync(QuicStreamType.Bidirectional);
        
        if (choose == "f")
        {
    string path = TextPath.Text;
    byte[] fileBytes = await File.ReadAllBytesAsync(path);
    string fileName = Path.GetFileName(path);
    byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName + "$$");
    byte[] stopCommand = Encoding.UTF8.GetBytes("stop$$");
    
    byte[] allBytes = new byte[fileNameBytes.Length + fileBytes.Length + stopCommand.Length];
    Buffer.BlockCopy(fileNameBytes, 0, allBytes, 0, fileNameBytes.Length);
    Buffer.BlockCopy(fileBytes, 0, allBytes, fileNameBytes.Length, fileBytes.Length);
    Buffer.BlockCopy(stopCommand, 0, allBytes, fileNameBytes.Length + fileBytes.Length, stopCommand.Length);

    int blockSize = int.TryParse(BlockSize.Text, out int size) ? size : 4096;
    for (int i = 0; i < allBytes.Length; i += blockSize)
    {
        int bytesToSend = Math.Min(blockSize, allBytes.Length - i);
        await stream.WriteAsync(allBytes.AsMemory(i, bytesToSend));
        Console.WriteLine($"Sent packet #{i / blockSize}");
        await Task.Delay(int.TryParse(Period.Text, out int period) ? period : 100);
    }
        }
        else if (choose == "w")
        {
             string writeString = TextPath.Text;
    		byte[] writeBytes = Encoding.UTF8.GetBytes(writeString + "stop$$");
    		await stream.WriteAsync(writeBytes);
    
    		// Read response (if necessary)
    		var buffer = new byte[4096];
    		int bytesRead = await stream.ReadAsync(buffer);
        }

        StatusOfSend.Text += " done...";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception: {ex.Message}");
        TextBoxStatus.Text = "Ошибка при подключении или отправке данных.";
    }
}

async void StopButton_Click(object sender, RoutedEventArgs e)
{
    
}

async void UDPSend_Click(object sender, RoutedEventArgs e) {
    String WriteString = TextPath.Text;
    String choose = TextType.Text;

    var broadcastAddress = IPAddress.Parse(IpText.Text.Remove(IpText.Text.Length-1, 1)); 
    using var udpSender = new UdpClient();
    Console.WriteLine("Начало отправки пакетов...");

    if (choose != "W" && choose != "w" && choose != "F" && choose != "f" && choose != "V" && choose != "v") {
        TextBoxStatus.Text = "Выберите правильный тип отправки";
    }else if (choose == "V" || choose == "v") {
    string path = TextPath.Text;
    
    
    string udpIp = IpText.Text.Remove(IpText.Text.Length-1, 1); // IP-адрес получателя
    int udpPort = Int32.Parse(PortText.Text); // Порт для передачи видео

    int blockSize =Int32.Parse(BlockSize.Text); // Размер блока (можно изменить)
    int delayBetweenFrames = Int32.Parse(Period.Text); // Задержка между кадрами в миллисекундах

    using (var udpSender1 = new UdpClient())
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-i \"{path}\" -f image2pipe -vcodec mjpeg -",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        byte[] buffer = new byte[blockSize];
        int packetIndex = 0;

        while (true)
        {
            // Читаем кадры из стандартного вывода FFmpeg
            int bytesRead = await process.StandardOutput.BaseStream.ReadAsync(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                Console.WriteLine("Конец видео.");
                break; // Конец видео
            }

            // Конвертируем прочитанные байты в изображение
            using (var ms = new MemoryStream(buffer, 0, bytesRead))
            {
                byte[] frameBytes = ms.ToArray(); // Сохраняем кадр в виде байтов
                await udpSender1.SendAsync(frameBytes, new IPEndPoint(IPAddress.Parse(udpIp), udpPort));
                Console.WriteLine($"Отправлен кадр# {packetIndex}");
                packetIndex++;
                await Task.Delay(delayBetweenFrames); // Задержка между кадрами
            }
        }

        process.WaitForExit(); // Ждем завершения процесса
        Console.WriteLine("Процесс завершен.");
    }
    
    // Неработает
    // string udpIp = IpText.Text.Remove(IpText.Text.Length-1, 1); // IP-адрес получателя
    // int udpPort = Int32.Parse(PortText.Text); // Порт для передачи видео
    // //
    // // // Создание UDP-клиента
    // UdpClient client = new UdpClient();
    // //
    // // // Путь к видеофайлу (замените на свой путь)
    // string videoPath = path; 
    // VideoCapture capture = new VideoCapture(videoPath);
    //
    // if (!capture.IsOpened)
    // {
    //     Console.WriteLine("Error: Could not open video file.");
    //     return;
    // }
    // IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(udpIp), udpPort);
    //
    // while (true)
    // {
    //     Mat frame = new Mat();
    //     capture.Read(frame);
    //     if (frame.IsEmpty)
    //     {
    //         Console.WriteLine("End of video stream or error reading frame.");
    //         break;
    //     }
    //
    //     // Кодирование кадра в формат JPEG
    //     Image<Bgr, byte> image = frame.ToImage<Bgr, byte>();
    //     using (var memoryStream = new System.IO.MemoryStream())
    //     {
    //         image.ToBitmap().Save(memoryStream, System.Drawing.Imaging.ImageFormat.Jpeg);
    //         byte[] buffer = memoryStream.ToArray();
    //
    //         // Отправка размера данных сначала
    //         byte[] dataSize = BitConverter.GetBytes(buffer.Length);
    //         Array.Reverse(dataSize); // Преобразование порядка байт, чтобы соответствовать big-endian
    //         client.Send(dataSize, dataSize.Length, endPoint);
    //
    //         // Отправка данных изображения частями
    //         const int MAX_UDP_SIZE = 1400;
    //         for (int i = 0; i < buffer.Length; i += MAX_UDP_SIZE)
    //         {
    //             int size = Math.Min(MAX_UDP_SIZE, buffer.Length - i);
    //             client.Send(buffer, size, udpIp, udpPort);
    //         }
    //     }
    // }
    //
    // capture.Dispose();
    // client.Close();
    
    
    // просто отправка байтов 
    // try {
    //     byte[] allBytes = File.ReadAllBytes(path);
    //     var totalBytes = allBytes; // Используем только содержимое файла
    //
    //     int blockSize = Int32.Parse(BlockSize.Text);
    //     int offset = 0;
    //     int packetIndex = 0;
    //
    //     // Находим длину totalBytes и проверяем, что она больше 0
    //     if (totalBytes.Length == 0) {
    //         Console.WriteLine("Пустой файл. Нечего отправлять.");
    //         return;
    //     }
    //
    //     while (offset < totalBytes.Length) {
    //         int sizeToSend = Math.Min(blockSize, totalBytes.Length - offset);
    //     
    //         // Проверка на случай, если sizeToSend становился бы 0
    //         if (sizeToSend <= 0) {
    //             break; // Мы вышли за пределы, завершаем цикл
    //         }
    //
    //         byte[] packet = new byte[sizeToSend];
    //         Buffer.BlockCopy(totalBytes, offset, packet, 0, sizeToSend);
    //
    //         await udpSender.SendAsync(packet, new IPEndPoint(broadcastAddress, Int32.Parse(PortText.Text)));
    //         offset += sizeToSend;
    //         Console.WriteLine("Отправлен пакет# {0}", packetIndex);
    //         packetIndex++;
    //         await Task.Delay(Int32.Parse(Period.Text));
    //     }
    // } catch (Exception ex) {
    //     Console.WriteLine($"Ошибка при отправке пакета: {ex.Message}");
    // }
    
    // Деление с ffmeg
    // try {
    //     // Запускаем FFmpeg для извлечения кадров
    //     var process = new Process {
    //         StartInfo = new ProcessStartInfo {
    //             FileName = "ffmpeg",
    //             Arguments = $"-i \"{path}\" -f image2pipe -vcodec mjpeg -",
    //             RedirectStandardOutput = true,
    //             UseShellExecute = false,
    //             CreateNoWindow = true
    //         }
    //     };
    //
    //     process.Start();
    //     int blockSize = Int32.Parse(BlockSize.Text);
    //
    //     byte[] buffer = new byte[blockSize];
    //     int packetIndex = 0;
    //
    //     while (true) {
    //         // Читаем кадры из стандартного вывода FFmpeg
    //         int bytesRead = process.StandardOutput.BaseStream.Read(buffer, 0, buffer.Length);
    //         if (bytesRead == 0) {
    //             break; // Конец видео
    //         }
    //
    //         // Конвертируем прочитанные байты в изображение
    //         using (var ms = new MemoryStream(buffer, 0, bytesRead)) {
    //             // Здесь можно добавить проверку формата изображения
    //             byte[] frameBytes = ms.ToArray(); // Сохраняем кадр в виде байтов
    //             await udpSender.SendAsync(frameBytes, new IPEndPoint(broadcastAddress, Int32.Parse(PortText.Text)));
    //             Console.WriteLine("Отправлен кадр# {0}", packetIndex);
    //             packetIndex++;
    //             await Task.Delay(Int32.Parse(Period.Text)); // Задержка между кадрами
    //         }
    //     }
    //
    //     process.WaitForExit();
    //     Console.WriteLine("Отправка завершена.");
    // } catch (Exception ex) {
    //     Console.WriteLine($"Ошибка при отправке видео: {ex.Message}");
    // }
}
    else if (choose == "F" || choose == "f") {
        try {
            String path = TextPath.Text;
            byte[] allBytes = File.ReadAllBytes(path);
            String fileName = Path.GetFileName(path);
            var header = Encoding.UTF8.GetBytes(fileName + "$$");
            var commandBytes = Encoding.UTF8.GetBytes("stop$$");
            var totalBytes = new byte[header.Length + allBytes.Length + commandBytes.Length];

            Buffer.BlockCopy(header, 0, totalBytes, 0, header.Length);
            Buffer.BlockCopy(allBytes, 0, totalBytes, header.Length, allBytes.Length);
            Buffer.BlockCopy(commandBytes, 0, totalBytes, header.Length + allBytes.Length, commandBytes.Length);

            int blockSize = Int32.Parse(BlockSize.Text);
            int offset = 0;
            int packetIndex = 0;

            while (offset < totalBytes.Length) {
                int sizeToSend = Math.Min(blockSize, totalBytes.Length - offset);
                byte[] packet = new byte[sizeToSend];
                Buffer.BlockCopy(totalBytes, offset, packet, 0, sizeToSend);

                await udpSender.SendAsync(packet, new IPEndPoint(broadcastAddress, Int32.Parse(PortText.Text)));
                offset += sizeToSend;
                Console.WriteLine("Отправлен пакет# {0}", packetIndex);
                packetIndex++;
                await Task.Delay(Int32.Parse(Period.Text));
            }
        } catch (Exception ex) {
            Console.WriteLine($"Ошибка при отправке пакета: {ex.Message}");
        }
    } else if (choose == "W" || choose == "w") {
        try {
            byte[] allStr = Encoding.UTF8.GetBytes(WriteString);
            byte[] commandBytes = Encoding.UTF8.GetBytes("stop$$");
            var allBytes = new byte[allStr.Length + commandBytes.Length];

            Buffer.BlockCopy(allStr, 0, allBytes, 0, allStr.Length);
            Buffer.BlockCopy(commandBytes, 0, allBytes, allStr.Length, commandBytes.Length);

            await udpSender.SendAsync(allBytes, new IPEndPoint(broadcastAddress, Int32.Parse(PortText.Text)));
            StatusOfSend.Text += " done...";
        } catch (Exception ex) {
            Console.WriteLine($"Ошибка при отправке текста: {ex.Message}");
        }
    }
}


	}
}
