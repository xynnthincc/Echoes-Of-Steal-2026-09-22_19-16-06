# Product Requirements Document (PRD)
## Echoes of Steal — 2D Top-Down Action Survival (Android)

**Versi:** 1.0
**Status:** Draft — Tugas Software Engineering
**Platform:** Android (Unity 2D, C#)

---

## 1. Ringkasan Produk

Echoes of Steal adalah game mobile 2D top-down action survival di mana pemain bertahan hidup melawan gelombang (*wave*) musuh yang terus meningkat kesulitannya di dalam sebuah arena tertutup. Fokus produk ini adalah pada **kontrol yang presisi** (virtual joystick) dan **umpan balik pertarungan yang terasa "hidup"** (*game juice*: knockback, screen shake).

Sesi bermain didesain singkat (2–5 menit per *run*), cocok untuk gameplay mobile kasual berbasis *high score*.

---

## 2. Latar Belakang & Tujuan

**Tujuan akademik:**
- Menerapkan konsep pengembangan game 2D di Unity: input handling, physics 2D, spawning system, state management, dan UI mobile.
- Melatih struktur arsitektur kode yang bersih dan scalable (event-driven, data-driven design).

**Tujuan produk:**
- Menghasilkan game arena-survival yang *playable*, stabil di perangkat Android kelas menengah-bawah, dan terasa responsif.

---

## 3. Target Pengguna

- Pemain kasual mobile, sesi bermain singkat.
- Tidak memerlukan pengalaman gaming yang mendalam — kontrol sederhana (1 stick + 1 tombol).

---

## 4. Ruang Lingkup (Scope)

### 4.1 In Scope (MVP)
- Pergerakan karakter 360° via virtual joystick.
- Serangan area (*melee slash*) dengan efek knockback ke musuh.
- Sistem wave musuh dengan kesulitan meningkat bertahap.
- Sistem HP pemain & kondisi Game Over.
- Sistem skor berbasis jumlah musuh yang dikalahkan.
- Efek screen shake saat serangan berhasil mengenai musuh.
- Alur game dasar: Start → Play → Game Over → Restart.

### 4.2 Out of Scope (v1)
- Multiplayer / mode co-op.
- In-app purchase / monetisasi.
- Multiple weapon / skill tree / upgrade system.
- Leaderboard online / integrasi cloud.
- Lebih dari satu tipe arena/map.

---

## 5. Functional Requirements

| ID | Requirement | Acceptance Criteria |
|----|-------------|----------------------|
| FR-1 | Virtual Joystick Movement | Karakter bergerak 360° mengikuti arah drag joystick; berhenti saat joystick dilepas. |
| FR-2 | Attack & Knockback | Tombol attack memicu hitbox area di depan/sekitar karakter; musuh yang terkena menerima damage dan gaya dorong (knockback) sesuai arah serangan. |
| FR-3 | Wave Spawner | Musuh muncul dalam gelombang bertahap; jumlah & kecepatan spawn meningkat tiap wave; ada jeda antar wave. |
| FR-4 | Enemy AI (Chase) | Musuh bergerak mengejar posisi pemain secara real-time; berhenti mengejar jika pemain di luar jangkauan (opsional). |
| FR-5 | Health System | HP pemain berkurang saat kontak dengan musuh; Game Over dipicu saat HP mencapai 0. |
| FR-6 | Score System | Skor bertambah setiap musuh dikalahkan; ditampilkan real-time di UI. |
| FR-7 | Screen Shake (Juice) | Kamera bergetar singkat saat serangan pemain mengenai musuh, intensitas proporsional terhadap jumlah musuh yang kena. |
| FR-8 | Game Flow | Ada state Start Screen, Gameplay, Game Over Screen dengan opsi Restart. |
| FR-9 | HUD | Health Bar (kiri atas) dan Score Text (kanan atas) selalu terlihat dan ter-update real-time selama gameplay. |

---

## 6. Non-Functional Requirements

- **Performa:** Berjalan stabil di kisaran 30–60 FPS pada perangkat Android kelas menengah-bawah (min. Android 7.0 / API 24).
- **Responsivitas kontrol:** Delay input joystick/tombol serendah mungkin (tidak terasa "lag" oleh pemain).
- **Ukuran build:** Dijaga ringan dengan aset pixel art (bukan aset 3D/high-res).
- **Stabilitas:** Tidak ada crash saat transisi antar wave atau saat jumlah musuh di layar banyak (perlu object pooling — lihat `agents.md`).

---

## 7. Technical Requirements

- **Engine:** Unity 2D (versi LTS terbaru yang tersedia).
- **Bahasa:** C#.
- **Target Platform:** Android, Min SDK 24 (Nougat).
- **Package/Plugin:** Unity Input System / Joystick Pack, Cinemachine (kamera & shake), `UnityEngine.Pool` (object pooling).
- **Prinsip arsitektur:** event-driven antar sistem, data desain (wave & enemy stats) berbasis ScriptableObject. Detail lengkap ada di `agents.md`.

---

## 8. UX / UI Requirements

| Elemen UI | Posisi | Fungsi |
|-----------|--------|--------|
| Virtual Joystick | Kiri bawah | Mengarahkan pergerakan karakter |
| Tombol Attack | Kanan bawah | Memicu serangan area |
| Health Bar | Kiri atas | Menampilkan sisa HP pemain |
| Score Text | Kanan atas | Menampilkan jumlah musuh yang dikalahkan |
| Game Over Panel | Tengah layar | Muncul saat HP habis, berisi skor akhir & tombol Restart |

---

## 9. Milestone (berbasis fase, bukan tanggal)

1. **Fase 1 — Core Movement & Attack:** Joystick, pergerakan karakter, hitbox serangan dasar.
2. **Fase 2 — Enemy & Wave System:** EnemyAI chase, WaveSpawner, object pooling.
3. **Fase 3 — Systems & UI:** HP, Score, HUD, Game Over flow.
4. **Fase 4 — Juice & Polish:** Knockback tuning, screen shake, animasi, SFX.
5. **Fase 5 — Build & Submission:** Build APK, testing di device fisik, dokumentasi akhir.

---

## 10. Success Metrics

- Semua Functional Requirements (FR-1 s/d FR-9) terpenuhi dan bisa didemokan.
- Build APK terpasang dan berjalan tanpa crash di minimal satu perangkat fisik.
- Gameplay loop lengkap bisa dijalankan dari Start hingga Game Over tanpa bug penghambat (*blocker*).

---

## 11. Risiko & Asumsi

- **Asumsi:** Aset gratis (pixel art, audio) yang dipakai berlisensi CC0/bebas royalti dan tersedia sesuai jadwal.
- **Risiko performa:** Spawn musuh berlebih tanpa object pooling berpotensi menyebabkan frame drop di device low-end.
- **Risiko waktu:** Scope perlu dijaga ketat (MVP dulu) mengingat ini tugas dengan tenggat terbatas.

---

## 12. Referensi

- `README.md` — instruksi setup, struktur proyek, dan cara build.
- `agents.md` — konvensi arsitektur kode dan panduan kontribusi/coding.
