# ⚔️ The Little Adventurers

[![Unity](https://img.shields.io/badge/Unity-6000.5.3f1-blue.svg?style=flat&logo=unity)](https://unity.com/)
[![Render Pipeline](https://img.shields.io/badge/Render%20Pipeline-URP-purple.svg)](https://unity.com/universal-render-pipeline)
[![Git LFS](https://img.shields.io/badge/Git%20LFS-Enabled-orange.svg)](https://git-lfs.github.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**The Little Adventurers** is a 3D RPG action-adventure game created in **Unity 6** featuring real-time combat, exploration, dynamic enemy AI, inventory & progression systems, customizable settings, and persistent scene navigation.

---

## 🚀 Key Features

* **⚔️ Real-Time Combat System**: Smooth third-person directional combat, hit-detection, damage popups, knockback effects, player & enemy health management.
* **🤖 Dynamic Enemy AI & Spawning**: State-machine driven enemy behavior (Wander, Aggro Patrol, Attack, Death) with an automated spawner system for monster prefabs.
* **🎒 RPG & Progression**: Inventory management system, collectible item pickups, XP/Level progression, and interactive NPC dialogue engine.
* **🗺️ Exploration & Environment**: Multi-scene architecture (Village & Big Island) connected by scene portals with seamless loading screens and persistent game state (`GameManager`).
* **🧭 Navigation & UI**: Dynamic circular minimap, customized URP visual effects, comprehensive pause menu, and animated UI controls.
* **⚙️ Advanced Settings & Controls**: Custom keybinding remapper with reset functionality, audio volume mixers (`MusicManager`), graphic quality presets, and custom resolution settings.

---

## 🛠️ Tech Stack & Requirements

* **Engine**: Unity `6000.5.3f1` (Unity 6)
* **Render Pipeline**: Universal Render Pipeline (URP)
* **Input System**: Unity New Input System (`InputReader`)
* **Large File Storage**: **Git LFS** (tracks textures, 3D FBX models, WAV audio, and baked binary assets)

---

## 📦 Cloning & Git LFS Setup

This repository uses **Git LFS** to manage high-resolution textures, 3D models (`.fbx`), and high-fidelity audio assets (`.wav`).

### Prerequisites
Make sure you have [Git LFS](https://git-lfs.github.com/) installed before cloning:
```bash
git lfs install
```

### Cloning the Repository
Clone the repository using Git:
```bash
git clone https://github.com/jpmasangkay/the-little-adventures.git
cd the-little-adventures
git lfs pull
```

---

## 📂 Project Architecture

```
Assets/
├── Assets/                 # 3D Models, Materials, Audio & UI Bundles
├── Editor/                 # Unity Editor Custom Tools & Scripts
├── Resources/              # Runtime Resources & Minimap Assets
├── Scenes/                 # Main Game Scenes (Village, BigIsland, MainMenu)
├── Scripts/                # C# Gameplay Logic & Systems
│   ├── Core/               # GameManager, GlobalUIManager, LoadingScreenManager
│   ├── DialogueSystem/     # NPC Dialogue & Text Rendering
│   ├── Environment/        # Portals & Scene Transitions
│   ├── InventorySystem/    # Inventory, Items & Collectibles
│   └── UI/                 # Menus, Minimap, Keybindings & Audio Controllers
├── Settings/               # URP Assets & Input Configuration
└── TextMesh Pro/           # Font Assets & UI Text Styles
```

---

## 🎮 Controls

| Action | Default Key |
| :--- | :--- |
| **Movement** | `W`, `A`, `S`, `D` |
| **Attack** | `Left Mouse Button` |
| **Interact / Talk** | `E` |
| **Inventory** | `I` / `Tab` |
| **Pause Menu** | `Escape` |

*Note: All keybindings can be customized dynamically via the in-game Settings menu.*

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.