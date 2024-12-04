import asyncio
import struct

import cv2
import numpy as np
from aioquic.asyncio import serve
from aioquic.asyncio.protocol import QuicConnectionProtocol
from aioquic.quic.configuration import QuicConfiguration
from aioquic.quic.events import HandshakeCompleted, StreamDataReceived
from pathlib import Path


class EchoServerProtocol(QuicConnectionProtocol):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.buffer = b""  # Буфер для данных, полученных из потока

    def quic_event_received(self, event):
        if isinstance(event, HandshakeCompleted):
            print("Handshake completed.")
        elif isinstance(event, StreamDataReceived):
            self.buffer += event.data  # Копируем данные в буфер

            # Проверяем, достаточно ли данных для извлечения размера изображения
            while len(self.buffer) >= 4:
                # Извлекаем размер данных
                data_size = struct.unpack("!I", self.buffer[:4])[0]

                # Убедимся, что у нас достаточно данных для полного изображения
                if len(self.buffer) < data_size + 4:
                    break  # Ждем дополнительные данные

                # Извлекаем и обрабатываем изображение
                jpg_as_text = self.buffer[4:4 + data_size]
                self.buffer = self.buffer[4 + data_size:]  # Очищаем буфер

                # Конвертируем данные в массив чисел
                jpg_as_np = np.frombuffer(jpg_as_text, dtype=np.uint8)
                frame = cv2.imdecode(jpg_as_np, flags=cv2.IMREAD_COLOR)

                if frame is not None:
                    # Отображение кадра
                    cv2.imshow('Receiver', frame)
                    if cv2.waitKey(1) & 0xFF == ord('q'):  # Нажмите 'q' для выхода
                        break


async def run_server(host='localhost', port=4433):
    # Настройки сертификата и ключа
    configuration = QuicConfiguration(is_client=False)
    certfile = Path('tests/ssl_cert.pem')  # Замените на ваш путь к SSL сертификату
    keyfile = Path('tests/ssl_key.pem')  # Замените на ваш путь к SSL ключу
    configuration.load_cert_chain(certfile=certfile, keyfile=keyfile)
    session_ticket_store = {}  # Пример: используем простой словарь для хранения session tickets

    # Запуск сервера на порту 4433
    await serve(
        host,
        port,
        configuration=configuration,
        create_protocol=EchoServerProtocol,
        session_ticket_fetcher=session_ticket_store.pop if session_ticket_store else None,
        session_ticket_handler=session_ticket_store.setdefault if session_ticket_store else None,
        retry=3  # Или любое значение retry, которое вам нужно
    )
    while True:
        await asyncio.sleep(3600)  # Позволяет серверу работать вечно (или около того)


if __name__ == "__main__":
    asyncio.run(run_server())
