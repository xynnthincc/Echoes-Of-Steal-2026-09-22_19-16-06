# agents.md — Panduan untuk AI Coding Agent & Kontributor
## Proyek: Echoes of Steal

Dokumen ini memberi konteks ke AI coding agent (Claude Code, Cursor, Copilot, dll) atau kontributor manusia lain yang mengerjakan kode di repo ini. Baca dulu sebelum menulis atau mengubah kode.

---

## 1. Tentang Proyek

Echoes of Steal adalah game Android 2D Top-Down Action Survival, dibangun di Unity dengan C#. Requirement lengkap ada di `PRD.md`. Ini proyek tugas dengan scope yang sengaja dijaga kecil (MVP-first) — jangan menambah fitur di luar `PRD.md` tanpa diminta eksplisit.

---

## 2. Tech Stack

- Unity 2D (LTS), C#
- Target: Android, Min SDK 24 (Nougat)
- Package: Input System / Joystick Pack, Cinemachine, `UnityEngine.Pool`

---

## 3. Struktur Folder

```
Assets/
├── Scripts/
│   ├── Player/
│   ├── Enemy/
│   ├── Combat/
│   ├── Systems/
│   └── UI/
├── Prefabs/
├── Art/
├── Audio/
├── ScriptableObjects/
└── Scenes/
```

Jangan ubah struktur folder ini tanpa alasan jelas — script baru masuk ke subfolder yang sesuai kategori, bukan ditaruh langsung di `Scripts/`.

---

## 4. Konvensi Coding

- **Naming:** `PascalCase` untuk class, method, dan public property. `camelCase` untuk parameter dan variabel lokal. Prefix `_` untuk private field (`_currentHealth`).
- **Satu class = satu file**, nama file harus sama dengan nama class.
- **Namespace:** gunakan `EchoesOfSteal.<Kategori>` (misal `EchoesOfSteal.Player`, `EchoesOfSteal.Systems`) untuk menghindari konflik nama.
- Tambahkan XML doc comment (`/// <summary>`) untuk method public yang logikanya tidak trivial.
- Hindari *magic number* — gunakan `const`, `[SerializeField]` field, atau ScriptableObject (lihat poin 5).

---

## 5. Prinsip Arsitektur (WAJIB diikuti)

1. **Event-driven, bukan referensi langsung.** Sistem seperti `AttackArea`, `EnemyAI`, `GameManager` sebaiknya berkomunikasi lewat C# event atau `UnityEvent` (mis. `OnEnemyKilled`, `OnPlayerDamaged`), bukan saling `GetComponent`/referensi hardcoded. Ini menjaga sistem tetap independen dan gampang diuji.
2. **Object Pooling wajib untuk entitas yang sering di-spawn.** Musuh dan efek serangan/VFX **tidak boleh** pakai `Instantiate`/`Destroy` langsung saat runtime — pakai `ObjectPool<T>` dari `UnityEngine.Pool` atau pool manager custom. Ini krusial untuk performa di Android low-end.
3. **Data desain lewat ScriptableObject.** Konfigurasi wave (jumlah musuh, interval spawn, tipe musuh) dan stats musuh (HP, speed, damage) disimpan sebagai ScriptableObject asset, bukan di-hardcode di script. Tujuannya supaya balancing game bisa diubah tanpa sentuh kode.
4. **Cache referensi di `Awake()`/`Start()`.** Jangan pernah panggil `GetComponent`, `FindObjectOfType`, atau `GameObject.Find` di dalam `Update()`/`FixedUpdate()` — ini penyebab umum lag di mobile.
5. **Physics 2D dengan layer & collision matrix yang jelas.** Gunakan `Rigidbody2D` + `Collider2D` dengan layer terpisah (Player, Enemy, AttackHitbox) dan atur Physics2D collision matrix di Project Settings, bukan filter manual lewat tag/nama di kode.
6. **Hindari Singleton berlebihan.** `GameManager` boleh pakai pola static-instance sederhana, tapi jangan jadikan semua sistem singleton yang saling bergantung — ini menyulitkan testing dan menambah coupling.

---

## 6. Daftar Script Utama & Tanggung Jawab

| Script | Tanggung Jawab | Catatan |
|--------|-----------------|---------|
| `PlayerController.cs` | Input joystick, pergerakan, trigger animasi serangan | Cache `Rigidbody2D` di `Awake()` |
| `AttackArea.cs` | Hitbox serangan, damage, impuls knockback | Gunakan `OverlapCircle`/`OverlapBox`, bukan collision event untuk hit detection area |
| `EnemyAI.cs` | Logika kejar pemain | State sederhana (`enum`) jika nanti ada >1 tipe musuh |
| `WaveSpawner.cs` | Interval spawn, jumlah musuh per wave | Ambil data dari `WaveData` (ScriptableObject), gunakan object pool untuk instansiasi |
| `GameManager.cs` | Alur game (Start/Game Over), skor, restart | Terima event dari sistem lain, jangan query langsung ke tiap musuh |
| `ObjectPool.cs` | Pool generik untuk enemy & VFX | Wajib dipakai oleh `WaveSpawner` dan `AttackArea` |

---

## 7. Testing & Build

- Jalankan gameplay langsung dari Unity Editor sebelum commit; pastikan tidak ada error/warning kritis di Console.
- Untuk perubahan yang menyangkut performa (spawn musuh, efek VFX), test juga di perangkat Android fisik — performa Editor tidak merepresentasikan performa device sebenarnya.
- Build APK lewat `File → Build Settings → Android → Build`. Detail lengkap ada di `README.md`.

---

## 8. Git Workflow

- `.gitignore` wajib mengecualikan: `Library/`, `Temp/`, `Obj/`, `Build/`, `Logs/`, `.vs/`, `*.csproj`, `*.sln` (gunakan template `.gitignore` resmi Unity).
- Format commit message: `[Scope] Deskripsi singkat`
  Contoh: `[EnemyAI] Fix musuh tetap mengejar saat pemain di luar arena`

---

## 9. Batasan untuk AI Agent

- **Jangan** menambah fitur di luar `PRD.md` tanpa diminta eksplisit oleh pengguna.
- **Jangan** menambah dependency/package Unity baru tanpa mencatatnya di `README.md`.
- **Jangan** hardcode angka balance (damage, HP, spawn rate, cooldown) — selalu taruh di ScriptableObject atau `[SerializeField]` yang jelas namanya.
- **Jangan** mengubah struktur folder `Assets/` tanpa alasan yang dijelaskan.
- Jika ragu soal scope suatu fitur, cek `PRD.md` (Functional Requirements) sebagai acuan sebelum menulis kode.

---

## 10. Referensi

- `PRD.md` — requirement detail & acceptance criteria.
- `README.md` — instruksi setup, struktur proyek, dan cara build.
