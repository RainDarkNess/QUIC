import cv2
import socket
import struct

if __name__ == "__main__":
    UDP_IP = "172.18.192.1"  # IP-адрес получателя
    UDP_PORT = 5005  # Порт для передачи видео

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    video_path = "1.mp4"  # Замените на путь к вашему видео
    cap = cv2.VideoCapture(video_path)

    if not cap.isOpened():
        print("Error: Could not open video file.")
        exit()

    while True:
        ret, frame = cap.read()
        if not ret:
            print("End of video stream or error reading frame.")
            break

        _, buffer = cv2.imencode('.jpg', frame)

        data_size = struct.pack("!I", len(buffer))
        sock.sendto(data_size, (UDP_IP, UDP_PORT))

        MAX_UDP_SIZE = 1400
        for i in range(0, len(buffer), MAX_UDP_SIZE):
            sock.sendto(buffer[i:i + MAX_UDP_SIZE], (UDP_IP, UDP_PORT))

    cap.release()
