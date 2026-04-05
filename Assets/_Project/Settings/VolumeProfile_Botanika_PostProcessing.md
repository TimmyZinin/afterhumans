# URP Volume Profile — Botanika

> Guide for configuring the Botanika Volume Profile in Unity Editor after install.
> Apply via Global Volume in Scene_Botanika.unity.

## Target mood
Warm, living oasis. Late golden hour light through glass ceiling.
Color palette: tangerine-amber + deep forest green + dusty wood.
Temperature: ~3200K (warm tungsten).

## Volume Components

### 1. Bloom
- Enabled: true
- Threshold: 1.0
- Intensity: 0.5
- Scatter: 0.7
- Tint: (255, 230, 180) — warm amber

### 2. Tonemapping
- Enabled: true
- Mode: ACES

### 3. White Balance
- Enabled: true
- Temperature: +15
- Tint: -5

### 4. Color Adjustments
- Enabled: true
- Post Exposure: +0.15
- Contrast: +5
- Color Filter: (255, 240, 215) — slight warm wash
- Hue Shift: 0
- Saturation: +10

### 5. Shadows, Midtones, Highlights
- Enabled: true
- Shadows color: (107, 122, 133) — cool teal (to balance warmth)
- Midtones color: (245, 216, 163) — warm amber
- Highlights color: (242, 192, 132) — rose-gold
- Shadows Range: 0.0 → 0.3
- Highlights Range: 0.55 → 1.0

### 6. Vignette
- Enabled: true
- Color: (30, 20, 10)
- Center: (0.5, 0.5)
- Intensity: 0.2
- Smoothness: 0.8
- Roundness: 0.8

### 7. Film Grain
- Enabled: true
- Type: Thin 1
- Intensity: 0.15
- Response: 1.0

### 8. Depth of Field
- Enabled: true
- Mode: Bokeh
- Focus Distance: 3
- Aperture: 5.6
- Focal Length: 50

### 9. Chromatic Aberration
- Disabled (not for Botanika — keep clean)

## Fog (Scene-level, not in Volume Profile)
- Enable Fog in Lighting window
- Fog Color: (245, 216, 163) — warm amber
- Fog Mode: Exponential
- Density: 0.015

## Lighting
- Directional Light:
  - Color: (255, 220, 170)
  - Color Temperature: 3200K
  - Intensity: 1.2
  - Indirect Multiplier: 1.0
  - Angle: 25° elevation (low sun through greenhouse glass)
  - Shadows: Soft, strength 0.6

## Ambient (Environment)
- Skybox: warm gradient or Poly Haven HDRI sunset
- Ambient Mode: Trilight
- Sky Color: (255, 210, 160)
- Equator Color: (245, 216, 163)
- Ground Color: (60, 40, 20)
- Ambient Intensity: 0.4

---

**Key feel:** walking into a friend's lab-apartment at 5pm golden hour, smelling coffee and something fermenting. You belong here. The tungsten lamps are all on even though sun is still up.
