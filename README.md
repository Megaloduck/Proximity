# Proximity – Offline LAN Communication App

Proximity is a **fully offline, peer-to-peer communication system** designed for local area networks. It enables **instant text messaging**, **real-time voice chat**, and **automatic peer discovery** — all without internet, servers, or cloud services.

Built using **.NET MAUI**, it runs on **Windows, macOS, iOS, and Android**.

---

## 📌 Key Features

- **100% Offline** — works entirely within local WiFi/LAN
- **Automatic Peer Discovery** — no manual setup
- **Instant Text Chat** — low-latency TCP messaging
- **Real-Time Voice Communication** — Opus-powered audio streaming
- **Cross-Platform** — MAUI desktop + mobile
- **Privacy-First** — data never leaves the local network

---

## 🏗️ Technical Architecture

### Framework
| Component | Technology |
|----------|------------|
| Framework | .NET MAUI (net10.0) |
| Language | C# 12 |
| UI | XAML |
| Audio Engine | NAudio 2.2.1 |
| Voice Codec | Concentus Opus 2.2.2 |
| Architecture | MVVM with Dependency Injection |

---

## 🌐 Network Protocols

### 1. Discovery Protocol — UDP (Port 9001)

Used to detect peers on the LAN.

**Broadcast Payload**
DISCOVER_CHAT|peerId|ipAddress|chatPort|voicePort

- Broadcast Interval: **2 seconds**
- Peer Timeout: **10 seconds**

---

### 2. Chat Protocol — TCP (Port 9002)

Reliable TCP communication for messages.

**Handshake**
HELLO|peerId|userName
WELCOME|peerId|userName

**Message Format**
MSG|senderId|senderName|content|isPrivate|recipientId

---

### 3. Voice Protocol — UDP (Port 9003)

Real-time audio streaming.

| Property | Value |
|----------|-------|
| Codec | Opus (24 kbps) |
| Sample Rate | 48 kHz |
| Channels | Mono |
| Frame Duration | 20 ms |
| Samples/Frame | 960 |
| Expected Latency | 40–60 ms |


---

## ⚙️ How It Works

### Peer Discovery
Devices broadcast their presence every **2 seconds**.  
A peer is removed if no heartbeat is received for **10 seconds**.

### Text Messaging
Messages are sent over persistent **TCP** connections for reliable delivery.

### Voice Transmission
1. Capture audio  
2. Encode using **Opus**  
3. Send via **UDP**  
4. Decode on receiver  
5. Playback with jitter buffer  

---

## 🚀 Running the App

### Requirements
- .NET 10 SDK  
- MAUI workloads installed  

### Build & Run
```bash
dotnet build
dotnet run

Required LAN Ports

UDP 9001 — Discovery

TCP 9002 — Text chat

UDP 9003 — Voice chat

Ensure your firewall allows these ports.



