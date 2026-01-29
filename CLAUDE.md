# Desktop Companion Project - Development Notes

## Project Overview
A **Mochi-style reactive desk companion** using an ESP32 microcontroller and a 2.0" 240x320 pixel TFT display. Inspired by the [Mini Mochi Robot](https://www.hackster.io/news/this-tiny-robot-wants-to-live-on-your-desk-ed7931a13f5a), this companion sits on your desk and expresses personality through facial animations and reactions.

**Key Concept**: Unlike a Tamagotchi (which you care for), this is a **reactive companion** that responds to its environment with emotions and personality - like having a little friend on your desk!

**Current Phase**: Building with display only (facial expressions & animations). Sensors (sound, touch) and servo motor can be added later.

## Development Environment

### Operating System: Windows
This project is being developed on a **Windows machine**.

**IMPORTANT WARNING - Windows Command Line:**
- **NEVER use `2>nul` in bash/Git Bash/WSL** - This is Windows CMD syntax that will create a file called `nul`
- The file `nul` is a **reserved name in Windows** and is extremely difficult to remove
- **Correct alternatives:**
  - **Git Bash/WSL**: Use `2>/dev/null` for stderr redirection
  - **PowerShell**: Use `2>$null` for stderr redirection
  - **CMD**: `2>nul` works in CMD, but avoid mixing with bash scripts
- **General rule**: Use PowerShell for Windows-specific commands, Git Bash for cross-platform scripts

### Development Tools
- **Arduino IDE** - For compiling and uploading to ESP32 boards
- **Git Bash / WSL** - For command-line operations
- **Board**: XIAO ESP32-C3 selected in Arduino IDE

## Hardware Configuration

### Current Hardware
- **Microcontroller**: Seeed Studio XIAO ESP32-C3 (soldered headers)
- **Display**: 2.0" TFT LCD, 240x320 pixels, ST7789 driver (rectangular)
- **Input**: TBD (buttons, touch, tilt sensor)
- **Power**: USB (3.3V) or battery

### Display Specifications
- **Size**: 2.0 inches
- **Resolution**: 240 x 320 pixels (portrait orientation)
- **Driver**: ST7789 (likely)
- **Interface**: SPI
- **Difference from D20**: Rectangular display vs round, no BLK (backlight) pin

### Wiring Diagram - XIAO ESP32-C3

**ST7789 Display to XIAO ESP32-C3:**
- VCC → 3V3
- GND → GND
- SCL → D8 (GPIO8 - Hardware SPI SCK)
- SDA → D10 (GPIO10 - Hardware SPI MOSI)
- RES → D1 (GPIO3 - Reset)
- DC → D2 (GPIO4 - Data/Command)
- CS → D3 (GPIO5 - Chip Select)
- ~~BLK~~ → Not present on this display (backlight always on)

**Available GPIO Pins After Display:**
- D0 (GPIO2) - Strapping pin, should be avoided for critical inputs
- D4 (GPIO6) - Available
- D5 (GPIO7) - Available
- D6 (GPIO21) - Available
- D7 (GPIO20) - Available
- D9 (GPIO9) - Hardware SPI MISO, keep free

**Total Available for Inputs**: 4-5 GPIO pins (D4, D5, D6, D7, and optionally D0)

**Important Notes for XIAO:**
- D9 (GPIO9) is the hardware SPI MISO pin - kept free (not used by display but reserved for SPI)
- **Hardware SPI default pins:** D8=GPIO8 (SCK), D10=GPIO10 (MOSI)
- Display uses 5 pins: D1, D2, D3, D8 (SCK), D10 (MOSI)
- **Pin headers MUST be soldered** - un-soldered pins cause unreliable SPI communication
- **CRITICAL: Use GPIO numbers in code, not D-numbers** - Arduino library expects GPIO pin numbers (e.g., GPIO5 for D3)
- **CRITICAL: Wire to correct hardware SPI pins** - Must use D8/D10 for hardware SPI
- **Using HARDWARE SPI @ 40MHz** - Works perfectly with correct wiring and GPIO numbers

### Software Libraries
- **Adafruit GFX Library** - Graphics primitives
- **Adafruit ST7789** - ST7789 display driver (recommended based on D20 experience)
- Alternative: **Adafruit_ST7735** if display uses ST7735 driver

## Display Pin Labeling (Important!)

**Critical Discovery from D20 project**: TFT displays from AliExpress often have misleading pin labels:
- Pin labeled "SDA" is actually SPI **MOSI** (Master Out Slave In)
- Pin labeled "SCL" is actually SPI **SCLK** (Serial Clock)
- This is **NOT** an I2C interface despite the labeling suggesting otherwise
- **Common Mistake**: Easy to swap SCL/SDA on breadboard - double-check wiring if display initializes but shows nothing

### Pin Order Confusion
Display PCB shows pins in reverse order when viewed from front vs back. When connecting from the back (solder side), the order is reversed. Always verify pinout carefully.

## Library Compatibility (Based on D20 Experience)

### ✅ Recommended: Adafruit Libraries
- **Adafruit_ST7789** or **Adafruit_ST7735**
- **Status**: Should work reliably (Adafruit GC9A01A worked perfectly on D20)
- Known for excellent ESP32 compatibility
- Reliable initialization and display output
- Works with hardware SPI

### ❌ Avoid: TFT_eSPI
- **Status**: Has known issues with ESP32-C3 and newer ESP32 boards
- Can cause crashes: `assert failed: xQueueSemaphoreTake queue.c:1545`
- May show blank screens even with correct wiring
- Not recommended based on D20 testing

## Code Configuration Template

### Pin Definitions (Use GPIO Numbers!)
```cpp
// Display pins - IMPORTANT: Use GPIO numbers, NOT D-numbers!
#define TFT_CS    5   // D3 board label = GPIO5
#define TFT_DC    4   // D2 board label = GPIO4
#define TFT_RST   3   // D1 board label = GPIO3
// Hardware SPI automatically uses:
// SCK  = GPIO8  (D8 board label)
// MOSI = GPIO10 (D10 board label)

// Example input pins (adjust as needed)
#define BUTTON_A_PIN  20  // D7 = GPIO20
#define BUTTON_B_PIN  21  // D6 = GPIO21
#define BUTTON_C_PIN  6   // D4 = GPIO6
```

### Display Initialization
```cpp
#include <Adafruit_GFX.h>
#include <Adafruit_ST7789.h>

// 3-parameter constructor = hardware SPI
Adafruit_ST7789 tft = Adafruit_ST7789(TFT_CS, TFT_DC, TFT_RST);

void setup() {
  // Initialize with 40MHz hardware SPI
  tft.init(240, 320);  // Width x Height
  tft.setRotation(0);  // 0=portrait, 1=landscape
  tft.fillScreen(ST77XX_BLACK);
}
```

## Mochi-Style Companion Features

Inspired by the [Mini Mochi Robot](https://www.hackster.io/news/this-tiny-robot-wants-to-live-on-your-desk-ed7931a13f5a), this is a **reactive companion** that expresses personality through facial expressions and animations.

### Phase 1: Core Expressions (Display Only - NOW)

**1. Facial Expressions & Emotions**
   - 😊 Happy/Content (default idle state)
   - 😴 Sleeping (eyes closed, ZZZ animation)
   - 😲 Surprised (wide eyes, mouth open)
   - 🥰 Love/Affection (hearts, blushing)
   - 😢 Sad (tears, down-turned expression)
   - 😠 Annoyed (angry eyebrows)
   - 🤔 Curious (tilted head, question mark)
   - 🎵 Dancing/Vibing (music notes, bouncing)

**2. Autonomous Behaviors (No Input Needed)**
   - Random mood changes over time
   - Periodic animations (blink, look around, yawn)
   - Sleep/wake cycles (goes to sleep after inactivity)
   - Occasional "thoughts" (speech bubbles with icons)
   - Weather-based moods (if RTC + time of day)

**3. Simple Animations**
   - Blinking eyes (every 3-5 seconds)
   - Breathing motion (subtle scale/bounce)
   - Looking left/right randomly
   - Head bobbing to "internal music"
   - Stretching/yawning when waking up

**4. Button Interactions (Optional for Phase 1)**
   - Button 1: Pet/interact (trigger happy animation)
   - Button 2: Cycle moods manually (for testing)
   - Button 3: Sleep/wake toggle
   - Long press: Settings/brightness

### Phase 2: Sensor Integration (LATER - When You Get Sensors)

**1. Sound Sensor (Microphone Module)**
   - Reacts to loud noises (surprised face)
   - Dances when music detected (rhythm detection)
   - Falls asleep when quiet
   - Different reactions to volume levels

**2. Touch Sensor (Capacitive Touch)**
   - Happy face when petted
   - Affectionate response to gentle touch
   - Annoyed if touched too much
   - Purring/vibration animation

**3. Physical Movement (Servo Motor - Optional)**
   - Bobbing motion when happy
   - Dancing side-to-side with music
   - Nodding head when interacting
   - Wiggling when excited

### Phase 3: Advanced Features (Future)

**1. Personality & Memory**
   - Remember interaction patterns
   - Develop "personality" over time
   - Different reaction speeds based on mood
   - Persistent state in flash memory

**2. Time-Based Behaviors**
   - Morning: Waking up animation
   - Afternoon: Active and playful
   - Evening: Calmer, relaxed
   - Night: Sleepy, yawning

**3. Environmental Awareness**
   - Light sensor: Adjust brightness, sleep in dark
   - Temperature sensor: Show hot/cold reactions
   - Motion sensor: Wave hello when you approach

**4. Mini-Games (If adding buttons)**
   - Reaction time game (press when it blinks)
   - Pattern memory game
   - Dance-along rhythm game

## Display Layout Planning

### Portrait Mode (240 x 320) - Mochi Style

**Focus: Large expressive face, minimal UI clutter**

```
┌────────────────────┐ 240px wide
│                    │
│                    │
│    ┌──────────┐    │
│    │          │    │
│    │  ^    ^  │    │ Large face area
│    │   (oo)   │    │ 160x160+ pixels
│    │    ▿▿    │    │ Animated expressions
│    │   \__/   │    │
│    │          │    │
│    └──────────┘    │
│                    │
│       💭 ...       │ Optional: thought bubble
│                    │
│                    │
│   🎵 ♪  ♫  ♪      │ Context icons (music, zzz, etc)
│                    │
│   [Mood: Happy]    │ Optional: status text
└────────────────────┘
 320px tall
```

**Design Notes:**
- Companion face should be **large and centered** (128x128 to 160x160)
- Minimal UI - let the face be the main focus
- Emotions conveyed through facial features, not text
- Context icons appear around face when needed (music notes, hearts, ZZZ)
- Simple pixel art style (16x16, 32x32, or 64x64 base scaled up)

### Alternative: Full-Screen Face
```
┌────────────────────┐ 240px wide
│                    │
│      ^    ^        │ Eyes (can move, blink)
│       ○  ○         │
│                    │ Full screen = face
│         ▿          │ Nose
│                    │
│       \___/        │ Mouth (changes with emotion)
│                    │
│                    │
│    ♥  ♥  ♥  ♥     │ Bottom: emotion indicators
└────────────────────┘
 320px tall
```

## Color Palette (RGB565 Format)

### Mochi Companion Colors

**Background & Base:**
```cpp
#define COLOR_BG        0x0000  // Black background
#define COLOR_FACE      0xFFE0  // Yellow/cream (mochi color)
#define COLOR_OUTLINE   0x0000  // Black outlines
#define COLOR_SHADOW    0x7BEF  // Light gray (subtle shadows)
```

**Facial Features:**
```cpp
#define COLOR_EYES      0x0000  // Black eyes (default)
#define COLOR_BLUSH     0xF81F  // Pink/magenta (blushing, love)
#define COLOR_MOUTH     0x0000  // Black mouth
#define COLOR_HIGHLIGHT 0xFFFF  // White (eye sparkles, highlights)
```

**Emotion Colors:**
```cpp
#define COLOR_HAPPY     0x07E0  // Green (happy aura)
#define COLOR_LOVE      0xF81F  // Pink (hearts, affection)
#define COLOR_SAD       0x001F  // Blue (sad, tears)
#define COLOR_ANGRY     0xF800  // Red (angry, annoyed)
#define COLOR_SLEEPY    0x4208  // Purple/dark blue (sleepy, night)
#define COLOR_MUSIC     0x07FF  // Cyan (music, dancing)
#define COLOR_SURPRISE  0xFFE0  // Yellow (surprise, alert)
```

**Context Icons:**
```cpp
#define COLOR_HEART     0xF800  // Red hearts
#define COLOR_MUSIC_NOTE 0x07FF // Cyan music notes
#define COLOR_ZZZ       0x7BEF  // Gray ZZZ (sleeping)
#define COLOR_BUBBLE    0xFFFF  // White thought bubbles
```

## Common Problems & Solutions

### Problem: Display shows backlight but no content
**Causes**:
1. Wrong pin assignments (most common)
2. Incompatible library
3. Poor connections (un-soldered XIAO pins)
4. Wrong board selected in Arduino IDE
5. SCL/SDA swapped on breadboard

**Solution**:
- Use exact pin configuration documented above
- Use Adafruit_ST7789 library (not TFT_eSPI)
- Ensure solid connections (solder XIAO headers!)
- Select "XIAO_ESP32C3" in Tools > Board
- Double-check SCL goes to D8, SDA goes to D10

### Problem: Code uses wrong pin numbers
**Symptom**: Code compiles but display doesn't work, or works intermittently

**Cause**: Using D-numbers (board labels) instead of GPIO numbers in code

**Solution**:
```cpp
// ❌ WRONG - Don't use D-numbers
#define TFT_CS  3  // D3 board label

// ✅ CORRECT - Use GPIO numbers
#define TFT_CS  5  // D3 = GPIO5
```

### Problem: Display init success but nothing shows
**Cause**: Library sending commands but display not responding

**Solution**:
- Verify all pins are connected correctly
- Check VCC is 3.3V (not 5V)
- Verify using Adafruit library
- Check if SCL/SDA are swapped

## Development Phases

### Phase 1: Display Testing ✅ COMPLETE (2026-01-29)
- [x] Get basic display working
- [x] Test graphics primitives (pixels, lines, rectangles, text)
- [x] Test hardware SPI speed
- [x] Verify display orientation and resolution

**Confirmed Working:**
- ST7789 driver with Adafruit_ST7789 library
- Hardware SPI @ 40MHz (default)
- 240x320 resolution in portrait mode
- All color channels (RGB) working correctly
- Text rendering at multiple sizes
- Shapes (rectangles, circles, triangles, lines)
- Fast screen fills (~300ms for 4 full fills)
- 16x16 pixel art sprites scale beautifully @ 8x (128x128)

### Phase 2: Basic Face Display ⏳ NEXT
- [ ] Design simple companion face sprite (32x32 or 64x64 pixel art)
- [ ] Draw face on screen (centered, scaled up 2x-4x)
- [ ] Implement basic facial features (eyes, mouth, outline)
- [ ] Test different expressions (happy, sad, surprised)

### Phase 3: Simple Animations
- [ ] Create frame-based animation system
- [ ] Implement blinking animation (eyes open/close)
- [ ] Add breathing/idle motion (subtle bounce)
- [ ] Eye movement (look left/right)
- [ ] Mouth animation (talking, smiling)

### Phase 4: Emotion System
- [ ] Define emotion states (happy, sad, angry, sleepy, etc.)
- [ ] Create different face configurations for each emotion
- [ ] Implement smooth transitions between emotions
- [ ] Add autonomous mood changes (random + time-based)

### Phase 5: Context & Reactions
- [ ] Add context icons (hearts, music notes, ZZZ, thought bubbles)
- [ ] Implement sleep/wake cycles
- [ ] Random "thoughts" or actions
- [ ] Personality tweaks (reaction timing, preferences)

### Phase 6: Input System (Optional)
- [ ] Add 2-3 buttons for interaction
- [ ] Button 1: Pet/interact (trigger happy response)
- [ ] Button 2: Wake/sleep toggle
- [ ] Implement debouncing

### Phase 7: Sensor Integration (When You Get Hardware)
- [ ] Sound sensor: React to noise/music
- [ ] Touch sensor: Pet detection
- [ ] Servo motor: Physical bobbing/dancing
- [ ] Light sensor: Brightness adjustment

### Phase 8: Advanced Features
- [ ] Persistent memory (save mood/personality to flash)
- [ ] Time-based behaviors (morning/evening routines)
- [ ] Mini-games or interactive sequences
- [ ] Multiple companion "characters" to choose from

## Lessons Learned from D20 Project

1. **Don't trust display pin labels** - Verify actual protocol (I2C vs SPI)
2. **Soldered connections are CRITICAL for SPI** - Un-soldered pins fail for high-speed SPI
3. **Use GPIO numbers in code, not D-numbers** - Board labels (D0-D10) don't match GPIO numbers
4. **Hardware SPI requires specific pins** - Must use D8/D10 for XIAO ESP32-C3 hardware SPI
5. **Adafruit libraries are most reliable** - Better ESP32 support than TFT_eSPI
6. **Easy to swap SCL/SDA on breadboard** - Double-check if display initializes but shows nothing
7. **Test with known-working code first** - Use library examples before custom code
8. **D0 (GPIO2) is a strapping pin** - Avoid for critical inputs, can cause boot issues
9. **Read initial sensor states at startup** - Prevents false triggers on power-on
10. **Refactor early while code is fresh** - Modular structure prevents spaghetti code

## Bill of Materials

### Current Build (Phase 1-6)
| Item | Specification | Quantity | Status | Notes |
|------|---------------|----------|--------|-------|
| Microcontroller | Seeed Studio XIAO ESP32-C3 | 1 | ✅ Have | Ultra-compact ESP32-C3 |
| Display | ST7789 2.0" 240x320 | 1 | ✅ Have | Rectangular TFT LCD (no BLK pin) |
| Pin Headers | 2.54mm pitch | 1 set | ✅ Have | **Must be soldered to XIAO** |
| USB Cable | USB-C | 1 | ✅ Have | For programming/charging |
| Buttons | Tactile push buttons | 2-3 | 📋 Optional | For manual interaction (Phase 6) |

### Future Upgrades (Phase 7 - Mochi-Style Sensors)
| Item | Specification | Quantity | Status | Notes |
|------|---------------|----------|--------|-------|
| Sound Sensor | MAX4466 or similar | 1 | 🛒 Later | React to music/noise |
| Touch Sensor | TTP223 capacitive | 1 | 🛒 Later | Pet detection |
| Servo Motor | SG90 micro servo | 1 | 🛒 Later | Physical bobbing/dancing |
| LiPo Battery | 3.7V 100-500mAh | 1 | 🛒 Later | Portable power |
| Light Sensor | Photoresistor/BH1750 | 1 | 🛒 Later | Auto brightness, sleep in dark |
| Passive Buzzer | 5V buzzer | 1 | 🛒 Later | Sound effects (beeps, tones) |

**Current Focus**: Build core facial expressions and animations with display only!

## Related Documentation

- **[WINDOWS_SERVICE_PLAN.md](WINDOWS_SERVICE_PLAN.md)** - Complete plan for .NET Windows service integration (PC-to-Mochi communication)

## Project Status

### Current Phase
✅ **Phase 1 COMPLETE** - Display tested and working perfectly!
✅ **Phase 2 COMPLETE** - RoboEyes implementation (TFT-optimized, matches original API)
⏳ **Phase 3 NEXT** - Windows Service integration (optional)

**Project Style**: Mochi-style reactive companion (inspired by Mini Mochi Robot)

**Completed (2026-01-29)**:
1. ✅ Display initialization confirmed (ST7789 with Adafruit library)
2. ✅ Hardware SPI working at 40MHz
3. ✅ All graphics primitives tested (text, shapes, colors)
4. ✅ 240x320 portrait mode verified
5. ✅ Pixel art sprite scaling tested (16x16 @ 8x = 128x128)
6. ✅ Project direction clarified: Mochi-style companion, not Tamagotchi

**Next Steps (Phase 2)**:
1. Design simple companion face (32x32 or 64x64 pixel art base)
2. Draw face centered on screen (scaled 2x-4x for visibility)
3. Implement basic facial features (eyes, mouth, outline)
4. Test multiple expressions (happy, sad, surprised, sleepy)
5. Create simple animation (blinking eyes)

**Future Additions (When sensors arrive)**:
- Sound sensor for music reaction
- Touch sensor for petting detection
- Servo motor for physical movement
- Light sensor for auto-brightness

---

## Code Organization (Future)

When code grows beyond ~200 lines, use modular structure:

```
Desktop_Companion/
├─ Desktop_Companion.ino    - Main coordination & loop
├─ Config.h                 - Hardware pins & constants
├─ Face.h/cpp               - Face rendering (eyes, mouth, expressions)
├─ Emotions.h/cpp           - Emotion states & transitions
├─ Animations.h/cpp         - Animation system (blinking, breathing, etc)
├─ Sensors.h/cpp            - Sensor input (sound, touch, light)
├─ Behaviors.h/cpp          - Autonomous behaviors & reactions
└─ Persistence.h/cpp        - Save/load personality & state
```

**Module Responsibilities:**
- **Face.h/cpp**: Draw facial features, change expressions
- **Emotions.h/cpp**: Manage emotion states (happy, sad, angry, etc), mood changes
- **Animations.h/cpp**: Frame-based animations, transitions, motion
- **Sensors.h/cpp**: Read and process sensor data (Phase 7+)
- **Behaviors.h/cpp**: Autonomous actions, reactions, personality
- **Persistence.h/cpp**: Save companion state to flash memory

**Benefits**:
- Maintainability: Related code grouped together
- Testability: Each module tested in isolation
- Reusability: Face/Animation modules can be used in other projects
- Readability: Main file stays clean and short
- Expandability: Easy to add new emotions, animations, or sensors

---

*Last Updated: 2026-01-29*
*Claude Code Development Session*
*Based on D20 project display configuration*
