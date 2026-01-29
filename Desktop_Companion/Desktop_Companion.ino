/*
 * Desktop Companion - TFT RoboEyes
 *
 * Hardware: XIAO ESP32-C3 + 2.0" ST7789 240x320 Display
 * Library: TFT_RoboEyes (custom implementation for TFT displays)
 *
 * Custom RoboEyes implementation adapted for TFT color displays
 * Based on FluxGarage RoboEyes library
 */

#include <Adafruit_GFX.h>
#include <Adafruit_ST7789.h>
#include <SPI.h>
#include "TFT_RoboEyes.h"  // Our custom RoboEyes for TFT

// Pin definitions - Use GPIO numbers!
#define TFT_CS    5   // D3 = GPIO5
#define TFT_DC    4   // D2 = GPIO4
#define TFT_RST   3   // D1 = GPIO3

// Screen dimensions (landscape)
#define SCREEN_WIDTH    320
#define SCREEN_HEIGHT   240

// Display object - hardware SPI
Adafruit_ST7789 display(TFT_CS, TFT_DC, TFT_RST);

// Create RoboEyes instance
TFT_RoboEyes<Adafruit_ST7789> roboEyes(display);

// Test state
int testMode = 0;
unsigned long lastChange = 0;

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("Desktop Companion - TFT RoboEyes");
  Serial.println("=================================");
  Serial.println();

  // Initialize display in LANDSCAPE mode
  Serial.print("Initializing display... ");
  display.init(240, 320);
  display.setRotation(1);  // Landscape (320x240)
  Serial.println("OK");

  // Initialize RoboEyes
  Serial.print("Initializing RoboEyes... ");
  roboEyes.begin(SCREEN_WIDTH, SCREEN_HEIGHT, 30);  // 30 FPS
  Serial.println("OK");

  // Configure eyes (tall minimalist style)
  roboEyes.setWidth(64, 64);
  roboEyes.setHeight(64, 64);
  roboEyes.setBorderradius(10, 10);
  roboEyes.setSpacebetween(36);

  // Enable features
  roboEyes.setAutoblinker(ON, 3, 2);  // Blink every 3±2 seconds
  roboEyes.setIdleMode(ON, 2, 2);     // Move every 2±2 seconds
  roboEyes.setCuriosity(ON);          // Wider eyes when looking sideways

  Serial.println();
  Serial.println("RoboEyes configured!");
  Serial.println("Eye size: 40x80 (minimalist style)");
  Serial.println("Auto-blink: ON (every 3±2s)");
  Serial.println("Idle mode: ON (every 2±2s)");
  Serial.println("Curiosity: ON");
  Serial.println();
  Serial.println("Cycling through test sequence...");
  Serial.println();

  lastChange = millis();
}

void loop() {
  // CRITICAL: Update eyes every loop for smooth animations
  roboEyes.update();

  // Test sequence (every 5 seconds)
  if (millis() - lastChange > 5000) {
    testMode++;

    switch(testMode) {
      case 0:
        Serial.println("Mood: DEFAULT, Position: Center");
        roboEyes.setMood(MOOD_DEFAULT);
        roboEyes.setPosition(0);  // Center
        break;

      case 1:
        Serial.println("Mood: HAPPY");
        roboEyes.setMood(MOOD_HAPPY);
        break;

      case 2:
        Serial.println("Mood: TIRED");
        roboEyes.setMood(MOOD_TIRED);
        break;

      case 3:
        Serial.println("Mood: ANGRY");
        roboEyes.setMood(MOOD_ANGRY);
        break;

      case 4:
        Serial.println("Looking E (right)");
        roboEyes.setMood(MOOD_DEFAULT);
        roboEyes.setPosition(E);
        break;

      case 5:
        Serial.println("Looking W (left)");
        roboEyes.setPosition(W);
        break;

      case 6:
        Serial.println("Looking N (up)");
        roboEyes.setPosition(N);
        break;

      case 7:
        Serial.println("Looking S (down)");
        roboEyes.setPosition(S);
        break;

      case 8:
        Serial.println("Looking NE");
        roboEyes.setPosition(NE);
        break;

      case 9:
        Serial.println("Animation: CONFUSED");
        roboEyes.anim_confused();
        break;

      case 10:
        Serial.println("Animation: LAUGH");
        roboEyes.anim_laugh();
        break;

      case 11:
        Serial.println("CYCLOPS mode ON");
        roboEyes.setCyclops(ON);
        break;

      case 12:
        Serial.println("CYCLOPS mode OFF");
        roboEyes.setCyclops(OFF);
        break;

      default:
        // Reset
        testMode = -1;
        Serial.println("Reset to DEFAULT");
        roboEyes.setMood(MOOD_DEFAULT);
        roboEyes.setPosition(0);  // Center
        break;
    }

    lastChange = millis();
  }

  // Don't use delay() here - breaks smooth animations!
}
