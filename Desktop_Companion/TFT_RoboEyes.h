/*
 * TFT RoboEyes - RoboEyes for TFT Color Displays
 *
 * Custom implementation inspired by FluxGarage RoboEyes V1.1.1
 * Adapted for TFT displays (ST7789, ST7735, ILI9341, etc.)
 *
 * Based on FluxGarage RoboEyes by Dennis Hoelscher
 * Adapted for TFT by Desktop Companion Project
 *
 * Key differences from original:
 * - Works with TFT displays (direct rendering, no buffer)
 * - Uses RGB565 colors instead of monochrome
 * - Optimized for color displays
 * - Mood enums prefixed to avoid ESP32 conflicts
 */

#ifndef _TFT_ROBOEYES_H
#define _TFT_ROBOEYES_H

#include <Adafruit_GFX.h>

// For turning things on or off
#define ON  true
#define OFF false

// Mood definitions (prefixed to avoid ESP32 Arduino.h conflicts with DEFAULT)
#define MOOD_DEFAULT  0
#define MOOD_TIRED    1
#define MOOD_ANGRY    2
#define MOOD_HAPPY    3

// Position definitions (cardinal directions)
#define N   1  // North (top center)
#define NE  2  // Northeast (top right)
#define E   3  // East (middle right)
#define SE  4  // Southeast (bottom right)
#define S   5  // South (bottom center)
#define SW  6  // Southwest (bottom left)
#define W   7  // West (middle left)
#define NW  8  // Northwest (top left)
// For middle center, use any value outside 1-8 (or 0)

// Template class for TFT RoboEyes
template<typename AdafruitDisplay>
class TFT_RoboEyes {
private:
  AdafruitDisplay* display;
  GFXcanvas16* sprite;  // Sprite buffer for double-buffering
  bool useSprite;       // Whether sprite is initialized

  // Screen properties
  int screenWidth;
  int screenHeight;
  int frameInterval;  // Milliseconds between frames
  unsigned long fpsTimer;

  // Eye geometry
  byte eyeLwidthDefault;
  byte eyeLheightDefault;
  byte eyeRwidthDefault;
  byte eyeRheightDefault;
  byte eyeLborderRadius;
  byte eyeRborderRadius;
  int spaceBetween;

  // Current eye state
  int eyeLwidthCurrent;
  int eyeLheightCurrent;
  int eyeRwidthCurrent;
  int eyeRheightCurrent;

  // Target eye state (for smooth transitions)
  int eyeLwidthNext;
  int eyeLheightNext;
  int eyeRwidthNext;
  int eyeRheightNext;

  // Eye absolute coordinates (matching original)
  int eyeLx, eyeLy;          // Left eye current position
  int eyeRx, eyeRy;          // Right eye current position
  int eyeLxNext, eyeLyNext;  // Left eye target position
  int eyeRxNext, eyeRyNext;  // Right eye target position
  int eyeLxDefault, eyeLyDefault;  // Default center positions
  int eyeRxDefault, eyeRyDefault;

  // Eye open/closed
  bool eyeL_open;
  bool eyeR_open;

  // Mood and mode flags
  byte currentMood;
  bool curious;
  bool cyclops;

  // Auto features
  bool autoBlinkerEnabled;
  int autoBlinkerInterval;
  int autoBlinkerVariation;
  unsigned long lastBlink;
  unsigned long nextBlinkTime;

  bool idleModeEnabled;
  int idleModeInterval;
  int idleModeVariation;
  unsigned long lastIdle;
  unsigned long nextIdleTime;

  // Animation state
  bool isBlinking;
  int blinkPhase;  // 0=open, 1=closing, 2=closed, 3=opening

  // Previous eye positions (for efficient clearing)
  int prevLeftEyeX, prevLeftEyeY;
  int prevRightEyeX, prevRightEyeY;
  int prevLeftWidth, prevLeftHeight;
  int prevRightWidth, prevRightHeight;
  bool firstDraw;

  // Colors (RGB565)
  uint16_t bgColor;
  uint16_t fgColor;

public:
  // Constructor
  TFT_RoboEyes(AdafruitDisplay& disp) : display(&disp) {
    sprite = nullptr;
    useSprite = false;

    // Default eye geometry
    eyeLwidthDefault = 36;
    eyeLheightDefault = 36;
    eyeRwidthDefault = 36;
    eyeRheightDefault = 36;
    eyeLborderRadius = 8;
    eyeRborderRadius = 8;
    spaceBetween = 10;

    // Initialize current state
    eyeLwidthCurrent = eyeLwidthDefault;
    eyeLheightCurrent = eyeLheightDefault;
    eyeRwidthCurrent = eyeRwidthDefault;
    eyeRheightCurrent = eyeRheightDefault;

    eyeLwidthNext = eyeLwidthDefault;
    eyeLheightNext = eyeLheightDefault;
    eyeRwidthNext = eyeRwidthDefault;
    eyeRheightNext = eyeRheightDefault;

    // Initialize eye positions (will be calculated in begin())
    eyeLx = 0;
    eyeLy = 0;
    eyeRx = 0;
    eyeRy = 0;
    eyeLxNext = 0;
    eyeLyNext = 0;
    eyeRxNext = 0;
    eyeRyNext = 0;
    eyeLxDefault = 0;
    eyeLyDefault = 0;
    eyeRxDefault = 0;
    eyeRyDefault = 0;

    eyeL_open = true;
    eyeR_open = true;

    currentMood = MOOD_DEFAULT;
    curious = false;
    cyclops = false;

    autoBlinkerEnabled = false;
    idleModeEnabled = false;

    isBlinking = false;
    blinkPhase = 0;

    bgColor = 0x0000;  // Black
    fgColor = 0xFFFF;  // White

    frameInterval = 33;  // ~30 FPS default
    fpsTimer = 0;
  }

  // Destructor - free sprite memory
  ~TFT_RoboEyes() {
    if (sprite) {
      delete sprite;
    }
  }

  // Initialize display
  void begin(int width, int height, byte maxFramerate) {
    screenWidth = width;
    screenHeight = height;
    frameInterval = 1000 / maxFramerate;
    fpsTimer = millis();

    // Calculate default centered eye positions (matching original)
    eyeLxDefault = ((screenWidth) - (eyeLwidthDefault + spaceBetween + eyeRwidthDefault)) / 2;
    eyeLyDefault = ((screenHeight - eyeLheightDefault) / 2);
    eyeRxDefault = eyeLxDefault + eyeLwidthDefault + spaceBetween;
    eyeRyDefault = eyeLyDefault;

    // Set initial positions to defaults
    eyeLx = eyeLxDefault;
    eyeLy = eyeLyDefault;
    eyeRx = eyeRxDefault;
    eyeRy = eyeRyDefault;
    eyeLxNext = eyeLxDefault;
    eyeLyNext = eyeLyDefault;
    eyeRxNext = eyeRxDefault;
    eyeRyNext = eyeRyDefault;

    // Clear screen once
    display->fillScreen(bgColor);

    // Create sprite buffer for double-buffering (covers entire screen)
    sprite = new GFXcanvas16(width, height);
    if (sprite) {
      useSprite = true;
      sprite->fillScreen(bgColor);
    }
  }

  // Set eye dimensions
  void setWidth(byte leftEye, byte rightEye) {
    eyeLwidthDefault = leftEye;
    eyeRwidthDefault = rightEye;
    eyeLwidthNext = leftEye;
    eyeRwidthNext = rightEye;
  }

  void setHeight(byte leftEye, byte rightEye) {
    eyeLheightDefault = leftEye;
    eyeRheightDefault = rightEye;
    eyeLheightNext = leftEye;
    eyeRheightNext = rightEye;
  }

  void setBorderradius(byte leftEye, byte rightEye) {
    eyeLborderRadius = leftEye;
    eyeRborderRadius = rightEye;
  }

  void setSpacebetween(int space) {
    spaceBetween = space;
  }

  // Set mood
  void setMood(byte mood) {
    currentMood = mood;
  }

  // Set position (cardinal directions) - MATCHING ORIGINAL
  void setPosition(byte pos) {
    switch(pos) {
      case N:
        // North, top center
        eyeLxNext = getScreenConstraint_X() / 2;
        eyeLyNext = 0;
        break;
      case NE:
        // North-east, top right
        eyeLxNext = getScreenConstraint_X();
        eyeLyNext = 0;
        break;
      case E:
        // East, middle right
        eyeLxNext = getScreenConstraint_X();
        eyeLyNext = getScreenConstraint_Y() / 2;
        break;
      case SE:
        // South-east, bottom right
        eyeLxNext = getScreenConstraint_X();
        eyeLyNext = getScreenConstraint_Y();
        break;
      case S:
        // South, bottom center
        eyeLxNext = getScreenConstraint_X() / 2;
        eyeLyNext = getScreenConstraint_Y();
        break;
      case SW:
        // South-west, bottom left
        eyeLxNext = 0;
        eyeLyNext = getScreenConstraint_Y();
        break;
      case W:
        // West, middle left
        eyeLxNext = 0;
        eyeLyNext = getScreenConstraint_Y() / 2;
        break;
      case NW:
        // North-west, top left
        eyeLxNext = 0;
        eyeLyNext = 0;
        break;
      default:
        // Middle center
        eyeLxNext = getScreenConstraint_X() / 2;
        eyeLyNext = getScreenConstraint_Y() / 2;
        break;
    }

    // Right eye follows left eye with spacing
    eyeRxNext = eyeLxNext + eyeLwidthCurrent + spaceBetween;
    eyeRyNext = eyeLyNext;
  }

private:
  // Get maximum X position for left eye (matching original)
  int getScreenConstraint_X() {
    return screenWidth - eyeLwidthCurrent - spaceBetween - eyeRwidthCurrent;
  }

  // Get maximum Y position for eyes (matching original)
  int getScreenConstraint_Y() {
    return screenHeight - eyeLheightDefault;  // Use default height (doesn't change with blink)
  }

public:

  // Cyclops mode
  void setCyclops(bool enabled) {
    cyclops = enabled;
  }

  // Curiosity mode
  void setCuriosity(bool enabled) {
    curious = enabled;
  }

  // Auto blinker
  void setAutoblinker(bool active, int interval = 3, int variation = 2) {
    autoBlinkerEnabled = active;
    autoBlinkerInterval = interval * 1000;
    autoBlinkerVariation = variation * 1000;

    if (active) {
      lastBlink = millis();
      nextBlinkTime = lastBlink + autoBlinkerInterval + random(-autoBlinkerVariation, autoBlinkerVariation);
    }
  }

  // Idle mode
  void setIdleMode(bool active, int interval = 2, int variation = 2) {
    idleModeEnabled = active;
    idleModeInterval = interval * 1000;
    idleModeVariation = variation * 1000;

    if (active) {
      lastIdle = millis();
      nextIdleTime = lastIdle + idleModeInterval + random(-idleModeVariation, idleModeVariation);
    }
  }

  // Manual eye control
  void blink() {
    if (!isBlinking) {
      isBlinking = true;
      blinkPhase = 1;  // Start closing
    }
  }

  void open() {
    eyeL_open = true;
    eyeR_open = true;
    eyeLheightNext = eyeLheightDefault;
    eyeRheightNext = eyeRheightDefault;
  }

  void close() {
    eyeL_open = false;
    eyeR_open = false;
    eyeLheightNext = 0;
    eyeRheightNext = 0;
  }

  // Animations (using absolute coordinates)
  void anim_confused() {
    // Quick left-right shake
    int centerX = getScreenConstraint_X() / 2;
    for (int i = 0; i < 3; i++) {
      eyeLxNext = centerX + 20;
      eyeRxNext = eyeLxNext + eyeLwidthCurrent + spaceBetween;
      for (int j = 0; j < 5; j++) { update(); delay(20); }

      eyeLxNext = centerX - 20;
      eyeRxNext = eyeLxNext + eyeLwidthCurrent + spaceBetween;
      for (int j = 0; j < 5; j++) { update(); delay(20); }
    }
    eyeLxNext = centerX;
    eyeRxNext = eyeLxNext + eyeLwidthCurrent + spaceBetween;
  }

  void anim_laugh() {
    // Up-down bounce
    int centerY = getScreenConstraint_Y() / 2;
    for (int i = 0; i < 3; i++) {
      eyeLyNext = centerY - 15;
      eyeRyNext = eyeLyNext;
      for (int j = 0; j < 5; j++) { update(); delay(20); }

      eyeLyNext = centerY + 15;
      eyeRyNext = eyeLyNext;
      for (int j = 0; j < 5; j++) { update(); delay(20); }
    }
    eyeLyNext = centerY;
    eyeRyNext = eyeLyNext;
  }

  // Main update function
  void update() {
    unsigned long currentTime = millis();

    // Framerate limiting
    if (currentTime - fpsTimer < frameInterval) {
      return;
    }
    fpsTimer = currentTime;

    // Auto-blinker
    if (autoBlinkerEnabled && currentTime >= nextBlinkTime && !isBlinking) {
      blink();
      nextBlinkTime = currentTime + autoBlinkerInterval + random(-autoBlinkerVariation, autoBlinkerVariation);
    }

    // Idle mode - random eye movement (MATCHING ORIGINAL - anywhere on screen!)
    if (idleModeEnabled && currentTime >= nextIdleTime) {
      eyeLxNext = random(getScreenConstraint_X());
      eyeLyNext = random(getScreenConstraint_Y());
      eyeRxNext = eyeLxNext + eyeLwidthCurrent + spaceBetween;
      eyeRyNext = eyeLyNext;
      nextIdleTime = currentTime + idleModeInterval + random(-idleModeVariation, idleModeVariation);
    }

    // Handle blinking animation
    if (isBlinking) {
      switch (blinkPhase) {
        case 1:  // Closing
          eyeLheightNext = 0;
          eyeRheightNext = 0;
          if (eyeLheightCurrent <= 2) {
            blinkPhase = 2;
          }
          break;
        case 2:  // Closed (brief pause)
          blinkPhase = 3;
          break;
        case 3:  // Opening
          eyeLheightNext = eyeLheightDefault;
          eyeRheightNext = eyeRheightDefault;
          if (eyeLheightCurrent >= eyeLheightDefault - 2) {
            blinkPhase = 0;
            isBlinking = false;
          }
          break;
      }
    }

    // Smooth transitions
    smoothTransitions();

    // Apply mood modifications
    applyMood();

    // Draw eyes
    drawEyes();
  }

private:
  void smoothTransitions() {
    float smoothFactor = 0.2;

    // Smooth eye positions (absolute coordinates, matching original)
    eyeLx += (eyeLxNext - eyeLx) * smoothFactor;
    eyeLy += (eyeLyNext - eyeLy) * smoothFactor;
    eyeRx += (eyeRxNext - eyeRx) * smoothFactor;
    eyeRy += (eyeRyNext - eyeRy) * smoothFactor;

    // Smooth eye size changes
    eyeLwidthCurrent += (eyeLwidthNext - eyeLwidthCurrent) * smoothFactor;
    eyeLheightCurrent += (eyeLheightNext - eyeLheightCurrent) * smoothFactor;
    eyeRwidthCurrent += (eyeRwidthNext - eyeRwidthCurrent) * smoothFactor;
    eyeRheightCurrent += (eyeRheightNext - eyeRheightCurrent) * smoothFactor;
  }

  void applyMood() {
    // Apply mood-specific modifications
    // These are applied as offsets to the next values
    switch (currentMood) {
      case MOOD_HAPPY:
        // Squinted eyes
        eyeLheightNext = eyeLheightDefault - 10;
        eyeRheightNext = eyeRheightDefault - 10;
        break;
      case MOOD_TIRED:
        // Very squinted
        eyeLheightNext = eyeLheightDefault - 15;
        eyeRheightNext = eyeRheightDefault - 15;
        break;
      case MOOD_ANGRY:
        // Normal height but could add other effects
        break;
      case MOOD_DEFAULT:
      default:
        if (!isBlinking) {
          eyeLheightNext = eyeLheightDefault;
          eyeRheightNext = eyeRheightDefault;
        }
        break;
    }

    // Curiosity mode - widen eyes when looking near edges (matching original)
    if (curious) {
      int centerX = getScreenConstraint_X() / 2;
      // If eyes are far from center (near edges), make them wider
      if (eyeLxNext <= 10 || eyeLxNext >= (getScreenConstraint_X() - 10)) {
        eyeLwidthNext = eyeLwidthDefault + 8;
        eyeRwidthNext = eyeRwidthDefault + 8;
      } else {
        eyeLwidthNext = eyeLwidthDefault;
        eyeRwidthNext = eyeRwidthDefault;
      }
    } else {
      eyeLwidthNext = eyeLwidthDefault;
      eyeRwidthNext = eyeRwidthDefault;
    }
  }

  void drawEyes() {
    // Use absolute coordinates (matching original)
    if (useSprite) {
      // DOUBLE-BUFFERED RENDERING (fast!)
      sprite->fillScreen(bgColor);

      if (cyclops) {
        // Single centered eye
        int centerX = screenWidth / 2 - (eyeLwidthCurrent / 2);
        int centerY = screenHeight / 2 - (eyeLheightCurrent / 2);
        drawSingleEye(sprite, centerX, centerY,
                      eyeLwidthCurrent, eyeLheightCurrent, eyeLborderRadius);
      } else {
        // Two eyes at absolute positions
        drawSingleEye(sprite, eyeLx, eyeLy,
                      eyeLwidthCurrent, eyeLheightCurrent, eyeLborderRadius);
        drawSingleEye(sprite, eyeRx, eyeRy,
                      eyeRwidthCurrent, eyeRheightCurrent, eyeRborderRadius);
      }

      // Push sprite to display ONCE (only one slow operation!)
      display->drawRGBBitmap(0, 0, sprite->getBuffer(), screenWidth, screenHeight);

    } else {
      // Fallback: direct rendering (slower but works without sprite)
      display->fillScreen(bgColor);

      if (cyclops) {
        int centerX = screenWidth / 2 - (eyeLwidthCurrent / 2);
        int centerY = screenHeight / 2 - (eyeLheightCurrent / 2);
        drawSingleEye(display, centerX, centerY,
                      eyeLwidthCurrent, eyeLheightCurrent, eyeLborderRadius);
      } else {
        drawSingleEye(display, eyeLx, eyeLy,
                      eyeLwidthCurrent, eyeLheightCurrent, eyeLborderRadius);
        drawSingleEye(display, eyeRx, eyeRy,
                      eyeRwidthCurrent, eyeRheightCurrent, eyeRborderRadius);
      }
    }
  }

  void drawSingleEye(Adafruit_GFX* gfx, int x, int y, int width, int height, int borderRadius) {
    // Don't draw if eye is closed
    if (height <= 0) {
      return;
    }

    // Draw eye as rounded rectangle (matching original RoboEyes)
    gfx->fillRoundRect(x, y, width, height, borderRadius, fgColor);

    // Apply mood eyelids (overlays)
    int eyelidHeight = 0;

    switch (currentMood) {
      case MOOD_TIRED:
        // Tired: Triangle eyelids from top (droopy eyes)
        eyelidHeight = height / 2;
        if (cyclops) {
          // Cyclops: two triangle halves
          gfx->fillTriangle(x, y-1, x+(width/2), y-1, x, y+eyelidHeight-1, bgColor);
          gfx->fillTriangle(x+(width/2), y-1, x+width, y-1, x+width, y+eyelidHeight-1, bgColor);
        } else {
          // Left eye: triangle from left
          gfx->fillTriangle(x, y-1, x+width, y-1, x, y+eyelidHeight-1, bgColor);
        }
        break;

      case MOOD_ANGRY:
        // Angry: Triangle eyelids from top (angry eyebrows)
        eyelidHeight = height / 2;
        if (cyclops) {
          // Cyclops: two triangle halves angled inward
          gfx->fillTriangle(x, y-1, x+(width/2), y-1, x+(width/2), y+eyelidHeight-1, bgColor);
          gfx->fillTriangle(x+(width/2), y-1, x+width, y-1, x+(width/2), y+eyelidHeight-1, bgColor);
        } else {
          // Left/right eyes: triangles angled differently
          // This creates an angry "brow" effect
          gfx->fillTriangle(x, y-1, x+width, y-1, x+width, y+eyelidHeight-1, bgColor);
        }
        break;

      case MOOD_HAPPY:
        // Happy: Rounded rectangle from bottom (smiling eyes)
        eyelidHeight = height / 2;
        gfx->fillRoundRect(x-1, (y+height)-eyelidHeight+1, width+2, height, borderRadius, bgColor);
        break;

      case MOOD_DEFAULT:
      default:
        // No eyelid overlay
        break;
    }
  }
};

#endif // _TFT_ROBOEYES_H
