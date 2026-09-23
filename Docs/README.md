# Echoes of Steal ⚔️

**Echoes of Steal** adalah game 2D Top-Down Action Survival untuk Android di mana pemain bertarung bertahan hidup melawan gelombang musuh (*waves*) di dalam arena. Dikembangkan dengan **Unity (C#)**, dengan fokus pada kontrol yang presisi dan efek pertarungan yang dinamis (*game juice*).

> Dokumen requirement lengkap ada di [`PRD.md`](./PRD.md). Panduan konvensi kode & arsitektur ada di [`agents.md`](./agents.md).

---

## 🎯 Fitur Utama

- **Virtual Joystick Control** — pergerakan karakter 360° yang responsif di layar sentuh.
- **Action & Knockback System** — sabetan pedang memberikan dampak dorongan (*knockback*) pada musuh.
- **Wave Spawner** — gelombang musuh yang muncul secara berkala dengan tingkat kesulitan bertambah.
- **Game Juice Effects** — efek getaran kamera (*screen shake*) ringan saat serangan berhasil mengenai musuh.
- **Score & Health System** — indikator darah (HP Bar) serta sistem poin berbasis jumlah musuh yang dikalahkan.

---

## 🛠️ Tech Stack

| Kategori | Detail |
|----------|--------|
| Engine | Unity (2D) |
| Bahasa | C# |
| Target Platform | Android (Min. SDK 24 / Nougat 7.0) |
| Aset Grafis & Audio | Pixel Art Asset Pack (Itch.io / Kenney.nl), SFX (OpenGameArt.org) |
| Plugin / Package | Unity Joystick Pack, Cinemachine, `UnityEngine.Pool` |

---

## 📁 Struktur Proyek

```
Assets/
├── Scripts/
│   ├── Player/         # PlayerController, PlayerHealth
│   ├── Enemy/          # EnemyAI, EnemyHealth
│   ├── Combat/         # AttackArea, Knockback
│   ├── Systems/        # WaveSpawner, GameManager, ObjectPool
│   └── UI/             # HUDController, GameOverPanel
├── Prefabs/            # Player, Enemy variants, Attack VFX
├── Art/                # Sprite & tilemap pixel art
├── Audio/              # SFX & musik
├── ScriptableObjects/  # WaveData, EnemyStats
└── Scenes/             # MainMenu, Gameplay
```

---

## 🕹️ Skema Kontrol & UI

| Elemen UI | Kontrol / Fungsi |
|-----------|-------------------|
| Virtual Joystick (Kiri) | Mengarahkan pergerakan karakter |
| Tombol Attack (Kanan) | Memicu tebasan pedang (*Area Damage*) |
| Health Bar (Kiri Atas) | Menampilkan sisa HP pemain |
| Score Text (Kanan Atas) | Menampilkan jumlah musuh yang mati |

---

## 📐 Arsitektur Script Utama

- **`PlayerController.cs`** — mengelola pergerakan pemain via input joystick dan memicu animasi serangan.
- **`AttackArea.cs`** — mengatur hitbox tebasan, memberikan damage, serta menerapkan impuls gaya knockback ke musuh.
- **`EnemyAI.cs`** — mengatur logika pergerakan musuh untuk mengejar koordinat pemain secara real-time.
- **`WaveSpawner.cs`** — mengontrol interval waktu spawn dan kalkulasi jumlah musuh per gelombang.
- **`GameManager.cs`** — mengatur alur permainan (Start, Game Over), sistem skor, dan restart level.

Konvensi coding & prinsip arsitektur (event-driven, object pooling, ScriptableObject) dijelaskan lebih detail di [`agents.md`](./agents.md).

---

## ⚙️ Requirement Setup

- Unity Hub + Unity Editor (versi LTS terbaru yang mendukung 2D & Android Build Support).
- Module **Android Build Support** (termasuk Android SDK & NDK, OpenJDK) terpasang lewat Unity Hub.
- Perangkat Android fisik (opsional, untuk testing) dengan **USB Debugging** aktif.

---

## ▶️ Menjalankan di Unity Editor

1. Clone/download repo ini.
2. Buka **Unity Hub** → `Add` → pilih folder proyek.
3. Buka proyek, load scene `Assets/Scenes/Gameplay.unity`.
4. Klik tombol **Play** di Editor untuk testing cepat (gunakan mouse sebagai simulasi touch).

---

## 🚀 Cara Build ke Android (.APK)

1. Buka proyek pada **Unity Editor**.
2. Masuk ke menu `File` → `Build Settings`.
3. Pindahkan target platform ke **Android**, lalu klik `Switch Platform`.
4. Cek `Player Settings` — pastikan Package Name, Min API Level (24), dan Icon sudah sesuai.
5. Hubungkan HP Android (USB Debugging aktif) untuk `Build and Run`, atau klik `Build` saja untuk menghasilkan file `.apk`.
6. Install file `.apk` pada perangkat Android (aktifkan "Install from Unknown Sources" jika diperlukan).

---

## 🗺️ Roadmap

- [ ] Fase 1 — Core Movement & Attack
- [ ] Fase 2 — Enemy & Wave System
- [ ] Fase 3 — Systems & UI (HP, Score, HUD)
- [ ] Fase 4 — Juice & Polish
- [ ] Fase 5 — Build & Submission

---

## 📜 Kredit Aset

- **Sprite Karakter & Musuh:** Ninja Adventure Asset Pack by Pixel-boy and AAA, via [itch.io](https://pixel-boy.itch.io/ninja-adventure-asset-pack), licensed CC0 1.0 Universal
- **Tilemap Arena:** Kenney.nl (CC0 / Public Domain)
- **Audio & SFX:** OpenGameArt.org

---

## 📄 Lisensi

Proyek ini dibuat untuk keperluan tugas akademik dan tidak ditujukan untuk distribusi komersial.
