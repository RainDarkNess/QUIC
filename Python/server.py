import socket
import threading
from tkinter import ttk
import tkinter as tk
import asyncio
import struct
import cv2
import numpy as np
from aioquic.asyncio import serve
from aioquic.asyncio.protocol import QuicConnectionProtocol
from aioquic.quic.configuration import QuicConfiguration
from aioquic.quic.events import HandshakeCompleted, StreamDataReceived
from pathlib import Path
import time

from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure

class EchoServerProtocol(QuicConnectionProtocol):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.buffer = b""

    def quic_event_received(self, event):
        if isinstance(event, HandshakeCompleted):
            print("Handshake completed.")
        elif isinstance(event, StreamDataReceived):
            self.buffer += event.data

            while len(self.buffer) >= 4:
                data_size = struct.unpack("!I", self.buffer[:4])[0]

                if len(self.buffer) < data_size + 4:
                    break
                jpg_as_text = self.buffer[4:4 + data_size]
                self.buffer = self.buffer[4 + data_size:]  # Очищаем буфер

                jpg_as_np = np.frombuffer(jpg_as_text, dtype=np.uint8)
                frame = cv2.imdecode(jpg_as_np, flags=cv2.IMREAD_COLOR)

                if frame is not None:
                    cv2.imshow('Receiver', frame)
                    if cv2.waitKey(1) & 0xFF == ord('q'):  # Нажмите 'q' для выхода
                        break


def UDPServer(app):
    UDP_IP = app.text_ip_server.get("1.0", tk.END).replace('\n', '')
    UDP_PORT = int(app.text_port_UDP.get("1.0", tk.END).replace('\n', ''))

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    count = 0
    print("Ожидание инфорации...")

    app.update_count = 0
    start_time = time.time()

    app.time_graph = [0, 0, 0, 0, 0]
    app.count_pack_graph = [0, 0, 0, 0, 0]

    app.fig = Figure(figsize=(10, 4), dpi=100)
    app.ax = app.fig.add_subplot(111)
    app.line, = app.ax.plot(app.time_graph, app.count_pack_graph)
    app.canvas = FigureCanvasTkAgg(app.fig, master=app.root)
    app.canvas.get_tk_widget().pack()
    loss_count = 0
    while app.is_working:
        # count = count + 1
        # elapsed_time = time.time() - start_time
        # app.update_count = app.update_count + 1
        try:
            header = sock.recv(4)
            if not header:
                break
            data_size = struct.unpack("!I", header)[0]
            jpg_as_text = b""
            while len(jpg_as_text) < data_size:
                part = sock.recv(4096)
                if not part:
                    break
                jpg_as_text += part

            jpg_as_np = np.frombuffer(jpg_as_text, dtype=np.uint8)
            frame = cv2.imdecode(jpg_as_np, flags=1)

            if frame is not None:
                # Отображение кадра
                cv2.imshow('Receiver', frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):  # Нажмите 'q' для выхода
                break
            count = count + 1

            # if elapsed_time >= float(app.graph_speed_text.get("1.0", tk.END).replace('\n', '')):
            #     threading.Thread(target=app.update_graph).start()
            #     app.seconds = app.seconds + 1
            #     app.update_count = 0
            #     start_time = time.time()

        except Exception as e:
            app.update_count = app.update_count + 1
            print(f"Потеря пакета (фрейма) №{count}.")

    if not app.is_working:
        cv2.destroyAllWindows()
        sock.close()


class VideoStreamApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Сервер UPD/QUIC")
        self.root.geometry("1200x720")
        root.title("Сервер UPD/QUIC")
        self.start_button_QUIC = ttk.Button(text="Запустить сервер QUIC", command=self.start_video_stream_QUIC)
        self.ip_adr_label = ttk.Label(
            text="IP адрес сервера")
        self.label_server_QUIC = ttk.Label(text="Работает сервер на QUIC")
        self.label_server_UDP = ttk.Label(text="Работает сервер на UDP")
        self.text_ip_server = tk.Text(root, height=1, width=20)
        self.text_ip_server.insert(tk.END, "127.0.0.1")
        self.QUIC_port_label = ttk.Label(text="Порт для QUIC")
        self.text_port_QUIC = tk.Text(root, height=1, width=20)
        self.text_port_QUIC.insert(tk.END, "6000")

        self.graph_speed_label = ttk.Label(text="Скорость отрисовки графика")
        self.graph_speed_text = tk.Text(root, height=1, width=20)
        self.graph_speed_text.insert(tk.END, "1")

        self.UDP_port_label = ttk.Label(text="Порт для UDP")
        self.text_port_UDP = tk.Text(root, height=1, width=20)
        self.text_port_UDP.insert(tk.END, "6001")

        self.start_button_UPD = ttk.Button(text="Запустить сервер UDP", command=self.start_video_stream_UDP)
        self.stop_button = tk.Button(root, text="Остановить сервер", command=self.stop_server)
        self.is_working = True
        self.quit_button = tk.Button(root, text="Quit", command=root.quit)

        self.ip_adr_label.pack(pady=5)
        self.text_ip_server.pack(pady=10)

        self.QUIC_port_label.pack(pady=5)
        self.text_port_QUIC.pack(pady=10)

        self.UDP_port_label.pack(pady=5)
        self.text_port_UDP.pack(pady=10)

        self.start_button_QUIC.pack(pady=10)

        self.start_button_UPD.pack(pady=10)
        self.graph_speed_label.pack(pady=5)
        self.graph_speed_text.pack(pady=10)

        # self.stop_button.pack(pady=10)
        self.quit_button.pack(pady=20)

        self.time_graph = [0, 0, 0, 0, 0]
        self.count_pack_graph = [0, 0, 0, 0, 0]
        self.seconds = 0
        self.update_count = 0

    def stop_server(self):
        self.label_server_UDP.pack_forget()
        self.label_server_QUIC.pack_forget()
        self.is_working = False

    def start_video_stream_QUIC(self):
        print("Начинается стрим на QUIC...")
        self.is_working = True
        threading.Thread(target=self.run_client_thread_QUIC).start()

    async def run_stream_QUIC(self):
        # Настройки сертификата и ключа
        configuration = QuicConfiguration(is_client=False)
        self.label_server_QUIC.pack(pady=10)
        certfile = Path('tests/ssl_cert.pem')
        keyfile = Path('tests/ssl_key.pem')
        configuration.load_cert_chain(certfile=certfile, keyfile=keyfile)
        session_ticket_store = {}
        await serve(
            self.text_ip_server.get("1.0", tk.END).replace('\n', ''),
            int(self.text_port_QUIC.get("1.0", tk.END).replace('\n', '')),
            configuration=configuration,
            create_protocol=EchoServerProtocol,
            session_ticket_fetcher=session_ticket_store.pop if session_ticket_store else None,
            session_ticket_handler=session_ticket_store.setdefault if session_ticket_store else None,
            retry=True
        )
        while True:
            await asyncio.sleep(3600)

    def update_graph(self):
        self.time_graph.pop(0)
        self.time_graph.append(self.seconds)
        self.count_pack_graph.pop(0)
        self.count_pack_graph.append(self.update_count)
        self.line, = self.ax.plot(self.time_graph, self.count_pack_graph)
        self.line.set_xdata(self.time_graph)
        self.line.set_ydata(self.count_pack_graph)
        self.canvas.draw()

    def run_client_thread_QUIC(self):
        asyncio.run(self.run_stream_QUIC())

    def start_video_stream_UDP(self):
        print("Начинается стрим на UDP...")
        self.label_server_UDP.pack(pady=10)
        self.is_working = True
        threading.Thread(target=self.run_client_thread_UDP).start()

    def run_client_thread_UDP(self):
        asyncio.run(UDPServer(self))


if __name__ == "__main__":
    root = tk.Tk()
    app = VideoStreamApp(root)
    root.mainloop()
