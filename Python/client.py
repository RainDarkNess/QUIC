import asyncio
import threading
import time
from tkinter import ttk
import socket
import cv2
import struct

import numpy as np
from aioquic.asyncio import connect
from aioquic.quic.configuration import QuicConfiguration
from aioquic.asyncio.protocol import QuicConnectionProtocol
from aioquic.quic.events import HandshakeCompleted, StreamDataReceived
import tkinter as tk

from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg
from matplotlib.figure import Figure


class ClientProtocol(QuicConnectionProtocol):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self._stream_id = None  # Идентификатор потока

    def quic_event_received(self, event):
        if isinstance(event, HandshakeCompleted):
            print("Handshake completed.")
            self.start_video_stream()  # Начинаем потоковое видео

        elif isinstance(event, StreamDataReceived):
            data = event.data
            print(f"Received: {data}")

    def start_video_stream(self, video_path='1.mp4'):
        video_path = video_path
        cap = cv2.VideoCapture(video_path)

        if not cap.isOpened():
            print("Error: Could not open video file.")
            exit()

        self._stream_id = self._quic.get_next_available_stream_id()  # Получаем новый поток
        print(f"Using stream ID: {self._stream_id}")

        asyncio.create_task(self.send_video_frames(cap))  # Запускаем задачу отправки видео

    async def send_video_frames(self, cap):
        MAX_UDP_SIZE = 1400

        while True:
            ret, frame = cap.read()
            if not ret:
                print("End of video stream or error reading frame.")
                break

            # Кодируем кадр в формат JPEG
            success, buffer = cv2.imencode('.jpg', frame)
            if not success:
                print("Failed to encode frame.")
                continue

            # Преобразуем в байты
            data_bytes = buffer.tobytes()

            # Отправляем размер данных
            data_size = struct.pack("!I", len(data_bytes))
            self._quic.send_stream_data(self._stream_id, data_size)

            # Отправляем данные в сегментах
            for i in range(0, len(data_bytes), MAX_UDP_SIZE):
                segment = data_bytes[i:i + MAX_UDP_SIZE]
                self._quic.send_stream_data(self._stream_id, segment)

        # Закрываем поток
        self._quic.send_stream_data(self._stream_id, b'', end_stream=True)
        cap.release()
        print("Video stream has ended.")


async def run_stream_QUIC(ip='127.0.0.1', port=6000):
    configuration = QuicConfiguration(is_client=True)
    configuration.verify_mode = False  # Отключение проверки сертификата

    async with connect(ip, port, configuration=configuration, create_protocol=ClientProtocol) as protocol:
        # Ожидаем, пока данные не будут получены
        await asyncio.sleep(10)  # Долгое ожидание для получения ответа


async def run_stream_UDP(app):
    UDP_IP = app.text_ip_server.get("1.0", tk.END).replace('\n', '')
    UDP_PORT = int(app.text_port_UDP.get("1.0", tk.END).replace('\n', ''))

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    video_path = app.path_text.get("1.0", tk.END).replace('\n', '')
    delay_seconds = float(app.delay_text.get("1.0", tk.END).replace('\n', ''))
    cap = cv2.VideoCapture(video_path)

    count = 0
    if not cap.isOpened():
        print("Видео не найдено.")
        exit()

    app.update_count = 0
    start_time = time.time()

    app.time_graph = [0, 0, 0, 0, 0]
    app.count_pack_graph = [0, 0, 0, 0, 0]

    app.fig = Figure(figsize=(10, 4), dpi=100)
    app.ax = app.fig.add_subplot(111)
    app.line, = app.ax.plot(app.time_graph, app.count_pack_graph)
    app.canvas = FigureCanvasTkAgg(app.fig, master=app.root)
    app.canvas.get_tk_widget().pack()

    app.seconds = 0
    while app.is_working:
        ret, frame = cap.read()
        if not ret:
            print("End of video stream or error reading frame.")
            break

        _, buffer = cv2.imencode('.jpg', frame)

        data_size = struct.pack("!I", len(buffer))
        sock.sendto(data_size, (UDP_IP, UDP_PORT))

        MAX_UDP_SIZE = 1400

        for i in range(0, len(buffer), MAX_UDP_SIZE):
            elapsed_time = time.time() - start_time
            app.update_count = app.update_count + 1
            count = count + 1
            try:
                if elapsed_time >= float(app.graph_speed_text.get("1.0", tk.END).replace('\n', '')):
                    app.label_technical_UDP.config(text=f"Пакетов в {app.graph_speed_text.get("1.0", tk.END).replace('\n', '')} от одной секунды: {app.update_count}")
                    app.seconds = app.seconds + 1
                    threading.Thread(target=app.update_graph).start()
                    app.update_count = 0
                    start_time = time.time()
            except Exception:
                pass
            sock.sendto(buffer[i:i + MAX_UDP_SIZE], (UDP_IP, UDP_PORT))
            if app.delay_on:
                await asyncio.sleep(0)
                print("UDP отправло сообщение: ", buffer[i])
                print("Номер сообщения: ", count)
        await asyncio.sleep(delay_seconds)
    cap.release()


class VideoStreamApp:
    def __init__(self, root):
        self.root = root
        self.root.title("Клиент UDP/QUIC")
        self.root.geometry("1200x720")

        self.start_button_QUIC = ttk.Button(text="Начать стрим на QUIC", command=self.start_video_stream_QUIC)
        self.ip_adr_label = ttk.Label(
            text="IP адрес сервера")
        self.label_server_UDP = ttk.Label(text="Идет отправка на UDP")
        self.label_server_QUIC = ttk.Label(text="Идет отправка на QUIC")

        self.label_technical_UDP = ttk.Label(text="Техническая строка")

        self.text_ip_server = tk.Text(root, height=1, width=20)
        self.text_ip_server.insert(tk.END, "127.0.0.1")
        self.QUIC_port_label = ttk.Label(text="Порт для QUIC")
        self.text_port_QUIC = tk.Text(root, height=1, width=20)
        self.text_port_QUIC.insert(tk.END, "6000")

        self.UDP_port_label = ttk.Label(text="Порт для UDP")
        self.text_port_UDP = tk.Text(root, height=1, width=20)
        self.text_port_UDP.insert(tk.END, "6001")

        self.delay_label = ttk.Label(text="Скорость чтения файла (обновляется при перезапуске)")
        self.delay_text = tk.Text(root, height=1, width=20)
        self.delay_text.insert(tk.END, "0")

        self.path_label = ttk.Label(text="Путь до видео")
        self.path_text = tk.Text(root, height=1, width=20)
        self.path_text.insert(tk.END, "./1.mp4")

        self.start_button_UPD = ttk.Button(text="Начать стрим на UDP", command=self.start_video_stream_UDP)
        self.stop_button = tk.Button(root, text="Остановить поток", command=self.stop_stream)
        self.is_working = True
        self.quit_button = tk.Button(root, text="Quit", command=root.quit)

        self.toggle_button = tk.Button(root, text="Выключить", bg="green", fg="white", command=self.toggle_function)
        self.delay_on = True
        self.delay_on_off_label = ttk.Label(text=f"Задержка между отправкой пакетов: включена")

        self.graph_speed_label = ttk.Label(text="Скорость отрисовки графика")
        self.graph_speed_text = tk.Text(root, height=1, width=20)
        self.graph_speed_text.insert(tk.END, "1")

        self.ip_adr_label.pack(pady=5)
        self.text_ip_server.pack(pady=10)

        self.QUIC_port_label.pack(pady=5, side='right', padx=50)
        self.text_port_QUIC.pack(pady=10, side='right', padx=10)
        self.start_button_QUIC.pack(pady=10, side='right', padx=10)

        self.UDP_port_label.pack(pady=5, side='left', padx=50)
        self.text_port_UDP.pack(pady=10, side='left', padx=10)
        self.start_button_UPD.pack(pady=10, side='left', padx=10)

        self.delay_label.pack(pady=5)
        self.delay_text.pack(pady=10)

        self.graph_speed_label.pack(pady=5)
        self.graph_speed_text.pack(pady=10)

        self.path_label.pack(pady=5)
        self.path_text.pack(pady=10)

        self.delay_on_off_label.pack(pady=5)
        self.toggle_button.pack(pady=10)
        self.stop_button.pack(pady=10)
        self.quit_button.pack(pady=20)

        self.time_graph = [0, 0, 0, 0, 0]
        self.count_pack_graph = [0, 0, 0, 0, 0]
        self.seconds = 0
        self.update_count = 0

    def update_graph(self):
        self.time_graph.pop(0)
        self.time_graph.append(self.seconds)

        self.count_pack_graph.pop(0)
        self.count_pack_graph.append(self.update_count)

        self.line, = self.ax.plot(self.time_graph, self.count_pack_graph)
        self.line.set_xdata(self.time_graph)
        self.line.set_ydata(self.count_pack_graph)
        self.canvas.draw()

    def toggle_function(self):
        if self.toggle_button.config('bg')[-1] == 'red':
            self.toggle_button.config(bg='green',
                                      text='Выключить')
            self.delay_on = True

        else:
            self.toggle_button.config(bg='red', text='Включить')
            self.delay_on = False
        delay_label = "включена" if self.delay_on else "выключена"
        self.delay_on_off_label.config(text=f"Задержка между отправкой пакетов: {delay_label}")

    def start_video_stream_QUIC(self):
        # self.start_button_QUIC.config(state=tk.DISABLED)  # Отключаем кнопку
        print("Начинается стрим на QUIC...")
        self.is_working = True
        threading.Thread(target=self.run_client_thread_QUIC).start()

    def stop_stream(self):
        self.is_working = False
        self.label_server_UDP.pack_forget()
        self.label_server_QUIC.pack_forget()
        self.label_technical_UDP.pack_forget()

    def run_client_thread_QUIC(self):
        asyncio.run(run_stream_QUIC(self.text_ip_server.get("1.0", tk.END).replace('\n', ''),
                                    int(self.text_port_QUIC.get("1.0", tk.END).replace('\n', ''))))

    def start_video_stream_UDP(self):
        # self.start_button_UPD.config(state=tk.DISABLED)  # Отключаем кнопку
        print("Начинается стрим на UDP...")
        self.label_server_UDP.pack(pady=10)
        self.label_technical_UDP.pack(pady=5)
        self.is_working = True

        self.time_graph = [0, 0, 0, 0, 0]
        self.count_pack_graph = [0, 0, 0, 0, 0]
        self.seconds = 0
        self.update_count = 0
        try:
            self.canvas.get_tk_widget().pack_forget()
        except:
            print("e")
        threading.Thread(target=self.run_client_thread_UDP).start()

    def run_client_thread_UDP(self):
        asyncio.run(run_stream_UDP(self))


if __name__ == "__main__":
    root = tk.Tk()
    app = VideoStreamApp(root)
    root.mainloop()
