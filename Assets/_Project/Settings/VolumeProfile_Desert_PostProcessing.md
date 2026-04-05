# URP Volume Profile — Desert

> Guide for configuring the Desert Volume Profile in Unity Editor after install.
> Apply via Global Volume in Scene_Desert.unity.

## Target mood
Dune-inspired eternal sunset. Dramatic, vast, warm and melancholic.
Colors: blood-orange sun + violet horizon + ochre sand.
Time frozen: sunset never moves.
Temperature: ~2400K (very warm).

## Volume Components

### 1. Bloom
- Enabled: true
- Threshold: 0.9
- Intensity: **1.2** (strongest — the sun must burn)
- Scatter: 0.85
- Tint: (255, 180, 100) — warm amber sun

### 2. Tonemapping
- Enabled: true
- Mode: ACES

### 3. White Balance
- Enabled: true
- Temperature: +30 (very warm)
- Tint: +10 (push magenta)

### 4. Color Adjustments
- Enabled: true
- Post Exposure: +0.3 (bright HDR sun)
- Contrast: +20
- Color Filter: (255, 200, 150) — sunset wash
- Hue Shift: 0
- Saturation: +20

### 5. Shadows, Midtones, Highlights
- Enabled: true
- Shadows color: (61, 26, 28) — deep burgundy
- Midtones color: (210, 96, 47) — orange
- Highlights color: (232, 118, 86) — rose-red
- Shadows Range: 0.0 → 0.25
- Highlights Range: 0.55 → 1.0

### 6. Vignette
- Enabled: true
- Color: (20, 8, 10)
- Intensity: 0.25
- Smoothness: 0.9
- Roundness: 0.85

### 7. Film Grain
- Enabled: true
- Type: Medium 1 (slightly stronger — cinematic)
- Intensity: 0.18
- Response: 1.0

### 8. Depth of Field
- Enabled: true
- Mode: Bokeh
- Focus Distance: 15 (far focus for epic landscape)
- Aperture: 2.8
- Focal Length: 85 (telephoto — compresses distance)

### 9. Motion Blur
- Enabled: true
- Intensity: 0.3 (subtle — walking through sand)
- Clamp: 0.05

### 10. Lens Distortion
- Disabled

### 11. Chromatic Aberration
- Enabled: true
- Intensity: 0.05 (subtle — hint at reality-bending)

## Fog
- Fog Color: (201, 118, 72) — dusty ochre
- Mode: Exponential
- Density: 0.01
- Start: 50m

## Lighting
- Directional Light (the sun):
  - Color: (255, 140, 80)
  - Color Temperature: 2400K
  - Intensity: **2.5** (HDR — intense sunset)
  - Angle: **5° elevation** (almost touching horizon)
  - Shadows: Very Soft, Long, strength 0.9
  - Cookie (optional): sun flare cookie

## Ambient
- Skybox: **Poly Haven HDRI** (suggested: `rogland_sunset_16k` or `sunset_in_the_chalk_quarry_16k`)
- Ambient Mode: Skybox (let HDRI drive ambient)
- Ambient Intensity: 1.0

## Player Camera
- FOV: gradually reduce from 65° → 50° as player approaches Cursor (handled by CursorFinale.cs)
- Head bob: strongest here (uneven sand)

## Sand shader hint (for terrain material)
- Use Megascans photoscanned sand textures
- Normal map intensity: 1.5 (make dunes feel sculpted by wind)
- Smoothness: 0.15 (matte, not shiny)
- Subtle height-based color gradient: darker ochre in valleys, lighter gold on crests

## Volumetric effects (VFX Graph if time)
- Wind particles: low density, large particles, slow drift from -X to +X
- Near the ruined server (key prop): occasional red-shifted glitch particles in 3m radius

---

**Key feel:** time stopped at perfect sunset. You walk slowly. The wind is warm. You are not alone, but you are small. Kafka's paws make quiet sand sounds beside you. In the distance, a small blinking `> _` hovers above the horizon. You know you will reach it.
