# Play Mode Camera Sync for Unity

Sync your Scene view camera with your game camera during play mode.

## Features

- **Camera following** - Scene view automatically follows your main camera in play mode
- **Transform following** - Lock Scene view to any GameObject's position
- **View presets** - Quick access to Front, Back, Left, Right, Top, and Bottom views
- **Distance control** - Mouse scroll to adjust viewing distance
- **Floating overlay** - Draggable control window
- **Click to follow** - Ctrl+Shift+Click objects to track them
- **Persistent settings** - Remembers your preferences across sessions

## Installation

1. Import the package into your Unity project
2. The tool activates automatically - no setup required

## Quick Start

1. Enter **Play Mode**
2. Open the overlay: **Tools > Play Mode Camera Sync > Toggle Overlay**
3. Scene view now follows your main camera
4. Click **lock icon** to toggle on/off
5. Click **camera/transform icon** to switch modes

## Settings

Access via **Tools > Play Mode Camera Sync**

- **Toggle Overlay** - Show/hide control window

## Overlay Controls

- **Lock Icon** - Enable/disable sync (green = camera mode, blue = transform mode)
- **Mode Icon** - Switch between Camera and Transform modes
- **Target Field** - Set GameObject to follow (Transform mode only)
- **View Dropdown** - Choose camera angle (Front, Back, Left, Right, Top, Bottom, Free)

## Keyboard Shortcuts

- **Mouse Scroll** - Adjust viewing distance
- **Ctrl+Shift+Click** - Set follow target
- **Right-click Drag** - Rotate view (auto-switches to Free mode)

## Requirements

- Unity 2021.3 LTS or later
- Works with all render pipelines (Built-in, URP, HDRP)

## Support

For issues or feature requests: cheekychopslabs@gmail.com

## License

© Copyright 2026 Cheeky Chops Labs
