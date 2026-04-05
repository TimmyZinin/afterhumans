# URP Volume Profile — City

> Guide for configuring the City Volume Profile in Unity Editor after install.
> Apply via Global Volume in Scene_City.unity.

## Target mood
Sterile, cold, too clean. Clinical blue-white neutrals, no warmth anywhere.
Color palette: pale blue-white + steel gray + graphite shadows.
Temperature: ~7500K (cool daylight).

## Volume Components

### 1. Bloom
- Enabled: true
- Threshold: 1.2
- Intensity: 0.4
- Scatter: 0.5
- Tint: (230, 240, 250) — cool white

### 2. Tonemapping
- Enabled: true
- Mode: ACES

### 3. White Balance
- Enabled: true
- Temperature: -20
- Tint: 0

### 4. Color Adjustments
- Enabled: true
- Post Exposure: 0
- Contrast: +10
- Color Filter: (240, 245, 250) — faint cool wash
- Hue Shift: 0
- Saturation: -30 (heavy desaturation — near b&w with blue cast)

### 5. Shadows, Midtones, Highlights
- Enabled: true
- Shadows color: (26, 37, 48) — deep cold
- Midtones color: (200, 210, 220) — neutral cool
- Highlights color: (250, 252, 253) — pale
- Shadows Range: 0.0 → 0.3
- Highlights Range: 0.55 → 1.0

### 6. Vignette
- Enabled: true
- Color: (10, 20, 30)
- Intensity: 0.3 (stronger than Botanika — pressing)
- Smoothness: 0.8
- Roundness: 0.8

### 7. Film Grain
- Enabled: true
- Type: Thin 1
- Intensity: 0.12
- Response: 1.0

### 8. Chromatic Aberration
- Enabled: true
- Intensity: 0.1 (subtle glitch hint)

### 9. Lens Distortion
- Enabled: true
- Intensity: -0.05 (barely perceptible)
- X Multiplier: 1
- Y Multiplier: 1
- Scale: 1

### 10. Depth of Field
- Disabled (flat, clean, all in focus — adds to sterility)

## Fog
- Fog Color: (216, 224, 232) — cool dist
- Mode: Exponential
- Density: 0.02

## Lighting
- Directional Light:
  - Color: (220, 235, 255)
  - Color Temperature: 7500K
  - Intensity: 0.8
  - Angle: 45° (neutral, not sunset)
  - Shadows: Hard, strength 1.0

## Ambient
- Skybox: cool neutral or Poly Haven overcast HDRI
- Ambient Mode: Flat
- Ambient Color: (224, 232, 239)
- Ambient Intensity: 0.7 (high — fills dark corners, no mystery)

---

**Key feel:** walking into a ward that's too clean. Everything works. Everyone is polite. Nobody is here.
