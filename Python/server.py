import asyncio
import socket
import struct
import threading
from tkinter import ttk

import numpy as np
import cv2
import tkinter as tk


def UDPServer(app):
    UDP_IP = app.text_ip_server.get("1.0", tk.END).replace('\n', '')
    UDP_PORT = int(app.text_port_UDP.get("1.0", tk.END).replace('\n', ''))

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    count = 0
    print("Ожидание инфорации...")

    while app.is_working:
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
        except Exception as e:
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
        # self.start_button_QUIC = ttk.Button(text="Запустить сервер QUIC", command=self.start_video_stream_QUIC)
        self.ip_adr_label = ttk.Label(
            text="IP адрес сервера")
        self.label_server_QUIC = ttk.Label(text="Работает сервер на QUIC")
        self.label_server_UDP = ttk.Label(text="Работает сервер на UDP")
        self.text_ip_server = tk.Text(root, height=1, width=20)
        self.text_ip_server.insert(tk.END, "127.0.0.1")
        self.QUIC_port_label = ttk.Label(text="Порт для QUIC")
        self.text_port_QUIC = tk.Text(root, height=1, width=20)
        self.text_port_QUIC.insert(tk.END, "6000")

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

        # self.start_button_QUIC.pack(pady=10)
        self.start_button_UPD.pack(pady=10)

        self.stop_button.pack(pady=10)
        self.quit_button.pack(pady=20)

    def stop_server(self):
        self.label_server_UDP.pack_forget()
        self.is_working = False

    # def start_video_stream_QUIC(self):
    #     # self.start_button_QUIC.config(state=tk.DISABLED)  # Отключаем кнопку
    #     print("Начинается стрим на QUIC...")
    #     self.is_working = True
    #     threading.Thread(target=self.run_client_thread_QUIC).start()
    #
    # def run_client_thread_QUIC(self):
    #     asyncio.run(run_stream_QUIC(self.text_ip_server.get("1.0", tk.END).replace('\n', ''),
    #                                 int(self.text_port_QUIC.get("1.0", tk.END).replace('\n', ''))))

    def start_video_stream_UDP(self):
        # self.start_button_UPD.config(state=tk.DISABLED)  # Отключаем кнопку
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
