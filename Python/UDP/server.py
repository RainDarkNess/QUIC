import socket
import struct
import numpy as np
import cv2

if __name__ == "__main__":
    UDP_IP = "172.18.192.1"  # IP-адрес, на котором принимаем данные
    UDP_PORT = 5005  # Порт, который используется для передачи видео

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.bind((UDP_IP, UDP_PORT))

    print("Waiting for data...")

    while True:
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
        except Exception as e:
            print(f"Error: {e}")

    cv2.destroyAllWindows()