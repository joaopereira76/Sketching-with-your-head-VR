# Sketching-with-your-head-VR

This is an experimental Unity-based painting application developed for Meta Quest 2. It allows users to draw in a grid canvas using only their head movements and voice commands—no controllers required.

## 🎨 Project Overview

The application supports multiple drawing modes (free draw, lines, color picking), brush sizes, and mirror effects. It also includes a UI system for menus and color selection that appears in front of the user. Input is driven by:

- **Head movement**: Used to change modes, control UI interactions, and perform specific gestures in different modes.
- **Voice commands**: Used to trigger modes, select tools, and interact.

## ✨ Features

- **Head Gesture Recognition**
  - Tilting head **right** opens the menu.
  - Tilting head **left** cycles through drawing modes.
  - Tilting **up** = Redo, Tilting **down** = Undo.
  - Circular gesture detection (clockwise/anticlockwise) to navigate drawing history, pixel by pixel.

- **Game States**
  - `Draw` – Enables painting actions
  - `UndoRedo` – Interacts with gesture-based history
  - `ChooseColor` – Opens color picker canvas
  - `Navigation` – Moves the canvas through head tilt
  - `Menu` – Opens the floating menu in front of the user
  
- **Voice Command Support**
  - `free` – Free drawing mode
  - `line` – Line drawing mode
  - `picker` – Color picker tool
  - `size` – Switch to brush size adjustment mode
  - `rewind` – Enables rewind gesture mode
  - `forwards` / `backwards` – Move forward/back
  - `click` – Simulates click
  - `draw`, `menu`, `undo`, `navigation`, `color` – Switches GameState
  - `stop` – Resets to default mode


- **UI & Interaction**
  - Floating menus appear in front of the user
  - Visual feedback for drawing and selection
  - Voice-based command input system with sound feedback
  - Save painted texture as PNG

## 🚀 Getting Started

1. Clone the repository
2. Open the project in Unity (tested with Unity 2022.x or higher)
3. Configure for Android + XR Plugin (Meta Quest 2)
4. Build and run to Meta Quest 2 using `adb install`

## 🧠 Requirements

- Meta Quest 2 with developer mode enabled
- Unity XR Plugin Management + Oculus XR Plugin
- Microphone access for speech recognition

