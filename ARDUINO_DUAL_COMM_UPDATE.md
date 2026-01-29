# Arduino Dual Communication Update Instructions

## Overview
Add USB Serial and WiFi HTTP dual communication to Desktop_Companion.ino

## Required Changes

### 1. Add WiFi Configuration
1. Copy `WiFiConfig.h.template` to `WiFiConfig.h`
2. Update with your WiFi credentials

### 2. Add Required Includes (top of .ino file)
```cpp
#include <WiFi.h>
#include <WebServer.h>
#include "WiFiConfig.h"  // Your credentials
```

### 3. Add Global Variables (after includes)
```cpp
// Communication mode
enum CommMode { MODE_USB, MODE_WIFI };
CommMode currentMode = MODE_USB;

// WiFi server
WebServer httpServer(HTTP_PORT);
bool wifiStarted = false;

// Current mood state (for command parsing)
byte currentMood = MOOD_DEFAULT;
int currentMoodPriority = 0;
unsigned long moodExpiryTime = 0;  // 0 = no expiry
```

### 4. Add Mode Detection Function (before setup())
```cpp
CommMode detectCommunicationMode() {
  // Check if USB Serial has active connection
  delay(100);

  if (Serial) {
    Serial.println("USB detected - using Serial mode");
    return MODE_USB;
  }

  Serial.println("No USB - starting WiFi mode");
  return MODE_WIFI;
}
```

### 5. Add WiFi Setup Function
```cpp
void setupWiFi() {
  if (wifiStarted) return;

  Serial.println("Starting WiFi...");
  WiFi.mode(WIFI_STA);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  int attempts = 0;
  while (WiFi.status() != WL_CONNECTED && attempts < 20) {
    delay(500);
    Serial.print(".");
    attempts++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("\nWiFi connected!");
    Serial.print("IP address: ");
    Serial.println(WiFi.localIP());

    // Setup HTTP routes
    httpServer.on("/mood", HTTP_POST, handleMoodHttp);
    httpServer.on("/position", HTTP_POST, handlePositionHttp);
    httpServer.on("/animation", HTTP_POST, handleAnimationHttp);
    httpServer.on("/status", HTTP_GET, handleStatusHttp);
    httpServer.on("/reset", HTTP_POST, handleResetHttp);

    httpServer.begin();
    wifiStarted = true;
    Serial.println("HTTP server started on port 80");
  } else {
    Serial.println("\nWiFi connection failed!");
  }
}
```

### 6. Add HTTP Handler Functions
```cpp
void handleMoodHttp() {
  if (!httpServer.hasArg("mood") || !httpServer.hasArg("priority")) {
    httpServer.send(400, "application/json", "{\"error\":\"Missing parameters\"}");
    return;
  }

  String mood = httpServer.arg("mood");
  int priority = httpServer.arg("priority").toInt();
  int duration = httpServer.hasArg("duration") ? httpServer.arg("duration").toInt() : 0;

  String cmd = "MOOD:" + mood + ":" + String(priority);
  if (duration > 0) cmd += ":" + String(duration);

  processCommand(cmd);
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}

void handlePositionHttp() {
  if (!httpServer.hasArg("position") || !httpServer.hasArg("priority")) {
    httpServer.send(400, "application/json", "{\"error\":\"Missing parameters\"}");
    return;
  }

  String pos = httpServer.arg("position");
  int priority = httpServer.arg("priority").toInt();

  processCommand("POS:" + pos + ":" + String(priority));
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}

void handleAnimationHttp() {
  if (!httpServer.hasArg("animation")) {
    httpServer.send(400, "application/json", "{\"error\":\"Missing animation\"}");
    return;
  }

  processCommand("ANIM:" + httpServer.arg("animation"));
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}

void handleStatusHttp() {
  String json = "{";
  json += "\"mood\":" + String(currentMood) + ",";
  json += "\"priority\":" + String(currentMoodPriority) + ",";
  json += "\"mode\":\"" + String(currentMode == MODE_USB ? "USB" : "WiFi") + "\"";
  json += "}";
  httpServer.send(200, "application/json", json);
}

void handleResetHttp() {
  processCommand("RESET");
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}
```

### 7. Add Command Processing Functions
```cpp
void processCommand(String cmd) {
  int firstColon = cmd.indexOf(':');
  if (firstColon == -1) {
    Serial.println("ERROR:Invalid format");
    return;
  }

  String command = cmd.substring(0, firstColon);
  String params = cmd.substring(firstColon + 1);

  if (command == "MOOD") handleMoodCommand(params);
  else if (command == "POS") handlePositionCommand(params);
  else if (command == "ANIM") handleAnimationCommand(params);
  else if (command == "IDLE") handleIdleCommand(params);
  else if (command == "BLINK") handleBlinkCommand(params);
  else if (command == "RESET") handleResetCommand();
  else Serial.println("ERROR:Unknown command: " + command);
}

void handleMoodCommand(String params) {
  // Parse: MOOD_HAPPY:8 or MOOD_HAPPY:8:5
  int colon = params.indexOf(':');
  if (colon == -1) {
    Serial.println("ERROR:MOOD needs priority");
    return;
  }

  String moodStr = params.substring(0, colon);
  String rest = params.substring(colon + 1);
  int priority = rest.toInt();

  if (priority < currentMoodPriority) {
    Serial.println("OK:MOOD_IGNORED (priority too low)");
    return;
  }

  byte mood = parseMood(moodStr);
  if (mood == 255) {
    Serial.println("ERROR:Unknown mood: " + moodStr);
    return;
  }

  currentMood = mood;
  currentMoodPriority = priority;
  roboEyes.setMood(mood);

  // Check for duration
  int secondColon = rest.indexOf(':');
  if (secondColon != -1) {
    int duration = rest.substring(secondColon + 1).toInt();
    moodExpiryTime = millis() + (duration * 1000);
  } else {
    moodExpiryTime = 0;
  }

  Serial.println("OK:MOOD_" + moodStr + " (pri:" + String(priority) + ")");
}

byte parseMood(String moodStr) {
  if (moodStr == "MOOD_DEFAULT") return MOOD_DEFAULT;
  if (moodStr == "MOOD_HAPPY") return MOOD_HAPPY;
  if (moodStr == "MOOD_TIRED") return MOOD_TIRED;
  if (moodStr == "MOOD_ANGRY") return MOOD_ANGRY;
  return 255;
}

void handlePositionCommand(String params) {
  int colon = params.indexOf(':');
  if (colon == -1) {
    Serial.println("ERROR:POS needs priority");
    return;
  }

  String posStr = params.substring(0, colon);
  int priority = params.substring(colon + 1).toInt();

  if (priority < currentMoodPriority) {
    Serial.println("OK:POS_IGNORED (priority too low)");
    return;
  }

  byte pos = parsePosition(posStr);
  if (pos == 255) {
    Serial.println("ERROR:Unknown position: " + posStr);
    return;
  }

  roboEyes.setPosition(pos);
  Serial.println("OK:POS_" + posStr);
}

byte parsePosition(String posStr) {
  if (posStr == "N") return N;
  if (posStr == "NE") return NE;
  if (posStr == "E") return E;
  if (posStr == "SE") return SE;
  if (posStr == "S") return S;
  if (posStr == "SW") return SW;
  if (posStr == "W") return W;
  if (posStr == "NW") return NW;
  if (posStr == "0") return 0;
  return 255;
}

void handleAnimationCommand(String params) {
  if (params == "BLINK") roboEyes.blink();
  else if (params == "CONFUSED") roboEyes.anim_confused();
  else if (params == "LAUGH") roboEyes.anim_laugh();
  else {
    Serial.println("ERROR:Unknown animation: " + params);
    return;
  }
  Serial.println("OK:ANIM_" + params);
}

void handleIdleCommand(String params) {
  bool enable = (params == "ON" || params == "1");
  roboEyes.setIdleMode(enable);
  Serial.println("OK:IDLE_" + String(enable ? "ON" : "OFF"));
}

void handleBlinkCommand(String params) {
  bool enable = (params == "ON" || params == "1");
  roboEyes.setAutoblinker(enable);
  Serial.println("OK:BLINK_" + String(enable ? "ON" : "OFF"));
}

void handleResetCommand() {
  currentMood = MOOD_DEFAULT;
  currentMoodPriority = 0;
  moodExpiryTime = 0;

  roboEyes.setMood(MOOD_DEFAULT);
  roboEyes.setPosition(0);
  roboEyes.setIdleMode(ON);
  roboEyes.setAutoblinker(ON);

  Serial.println("OK:RESET");
}
```

### 8. Update setup()
```cpp
void setup() {
  Serial.begin(115200);
  delay(1000);

  // ... existing display/RoboEyes setup ...

  // Detect communication mode
  currentMode = detectCommunicationMode();

  if (currentMode == MODE_WIFI) {
    setupWiFi();
  }

  Serial.println("Mochi ready for commands!");
  Serial.println("Format: COMMAND:PARAM1:PARAM2");
}
```

### 9. Update loop()
```cpp
void loop() {
  roboEyes.update();

  // Handle mood expiry
  if (moodExpiryTime > 0 && millis() >= moodExpiryTime) {
    currentMoodPriority = 0;
    moodExpiryTime = 0;
    roboEyes.setMood(MOOD_DEFAULT);
    Serial.println("INFO:Mood expired, reverted to default");
  }

  // Handle commands based on mode
  if (currentMode == MODE_USB) {
    if (Serial.available() > 0) {
      String command = Serial.readStringUntil('\n');
      command.trim();
      processCommand(command);
    }
  } else {
    // WiFi mode - handle HTTP requests
    httpServer.handleClient();
  }

  // Remove or comment out the old test sequence code
}
```

## Testing

### USB Mode Testing
1. Connect via USB
2. Open Serial Monitor (115200 baud)
3. Send commands:
   - `MOOD:MOOD_HAPPY:8`
   - `POS:E:5`
   - `ANIM:LAUGH`
   - `RESET`

### WiFi Mode Testing
1. Disconnect USB
2. Power Mochi independently
3. Find IP address from serial output
4. Test with curl or browser:
   ```bash
   curl -X POST "http://192.168.1.xxx/mood?mood=MOOD_HAPPY&priority=8"
   curl -X GET "http://192.168.1.xxx/status"
   ```

## Integration with .NET Service

The .NET service will automatically use:
- **SerialConnection** when `ConnectionType: "Serial"` in appsettings.json
- **HttpConnection** when `ConnectionType: "Http"` in appsettings.json

Both send the same command format, just over different transports.
