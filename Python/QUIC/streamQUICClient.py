import asyncio
import cv2
import struct
from aioquic.asyncio import connect
from aioquic.quic.configuration import QuicConfiguration
from aioquic.asyncio.protocol import QuicConnectionProtocol
from aioquic.quic.events import HandshakeCompleted, StreamDataReceived


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

    def start_video_stream(self):
        video_path = "1.mp4"  # Замените на путь к вашему видео
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


async def run_client():
    configuration = QuicConfiguration(is_client=True)
    configuration.verify_mode = False  # Отключение проверки сертификата

    async with connect('localhost', 4433, configuration=configuration, create_protocol=ClientProtocol) as protocol:
        # Ожидаем, пока данные не будут получены
        await asyncio.sleep(10)  # Долгое ожидание для получения ответа


if __name__ == "__main__":
    asyncio.run(run_client())
