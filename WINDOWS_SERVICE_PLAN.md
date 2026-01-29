# Mochi Windows Companion Service - Implementation Plan

## Project Overview

A .NET 10 Windows service that monitors PC activity and controls the Mochi desk companion via USB serial communication. Mochi retains autonomous behaviors (auto-blink, idle movement) while reacting to PC events like music playback, system load, time of day, and active applications.

**Last Updated:** 2026-01-29
**Status:** .NET Service ✅ Complete | Arduino Dual Communication 🔄 In Progress

---

## Project Goals

✅ **Ambient awareness** - Passively reflects PC state
✅ **Active notifications** - Alerts for important events
✅ **Autonomous companion** - Own personality + PC reactions
✅ **Multi-event monitoring** - Audio, system load, time of day, active apps

---

## Architecture Overview

### System Diagram

```
┌─────────────────────────────────────────┐
│         Windows PC (.NET 10)            │
│  ┌───────────────────────────────────┐  │
│  │  MochiCompanion Service           │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │ Monitors (Parallel):        │  │  │
│  │  │ - AudioMonitor (Pri 8)      │  │  │
│  │  │ - SystemMonitor (Pri 4-6)   │  │  │
│  │  │ - TimeMonitor (Pri 1-3)     │  │  │
│  │  │ - ApplicationMonitor (5-7)  │  │  │
│  │  └─────────────────────────────┘  │  │
│  │           ↓                        │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │ MoodManager                 │  │  │
│  │  │ (Priority Handler)          │  │  │
│  │  └─────────────────────────────┘  │  │
│  │           ↓                        │  │
│  │  ┌─────────────────────────────┐  │  │
│  │  │ SerialConnection            │  │  │
│  │  │ "MOOD:HAPPY:8"             │  │  │
│  │  └─────────────────────────────┘  │  │
│  └───────────────────────────────────┘  │
│              ↓ USB Serial (COM3)        │
└──────────────┼──────────────────────────┘
               ↓
    ┌──────────────────────┐
    │  ESP32-C3 Mochi      │
    │  ┌────────────────┐  │
    │  │ Command Parser │  │
    │  │ (Priority)     │  │
    │  └────────────────┘  │
    │         ↓            │
    │  ┌────────────────┐  │
    │  │  Autonomous    │  │ ← Always running
    │  │  - Auto-blink  │  │   (baseline)
    │  │  - Idle mode   │  │
    │  └────────────────┘  │
    │         ↓            │
    │  ┌────────────────┐  │
    │  │  RoboEyes      │  │
    │  │  Display       │  │
    │  └────────────────┘  │
    └──────────────────────┘
```

### Priority-Based Mood System

Mochi behavior is layered with priority levels. Higher priority events temporarily override lower ones.

```
┌────────────────────────────────────────┐
│ Layer 4: Events (Priority 7-10)        │ ← Highest
│ - Music playing: Happy/dancing         │   Interrupts
│ - Notifications: Surprised/alert       │   everything
│ - Build failed: Sad                    │
├────────────────────────────────────────┤
│ Layer 3: System State (Priority 4-6)   │
│ - High CPU: Tired/stressed             │
│ - Low activity: Calm                   │
│ - Active app context                   │
├────────────────────────────────────────┤
│ Layer 2: Time-Based (Priority 1-3)     │
│ - Morning: Sleepy/waking up            │
│ - Day: Alert/active                    │
│ - Evening: Relaxed                     │
│ - Night: Very sleepy                   │
├────────────────────────────────────────┤
│ Layer 1: Autonomous (Priority 0)       │ ← Baseline
│ - Auto-blink every 3±2s                │   Always active
│ - Idle eye movement every 2±2s         │
│ - Default mood                         │
└────────────────────────────────────────┘
```

**Behavior Rules:**
1. Mochi **always** maintains autonomous behaviors (Layer 1)
2. PC service sends mood suggestions with priority levels
3. Higher priority overrides lower priority
4. When high-priority event ends, reverts to next active layer
5. Timed events auto-revert after duration expires

---

## Communication Protocol

### Serial Command Format

**Text-based protocol over USB Serial (115200 baud):**

```
Command Format: COMMAND:PARAM1:PARAM2\n

Available Commands:
  MOOD:<mood>:<priority>      Set mood with priority level
  POS:<position>:<priority>   Set eye position with priority
  ANIM:<animation>            Play animation (always interrupts)
  IDLE:<on|off>               Enable/disable idle mode
  BLINK:<on|off>              Enable/disable auto-blink
  RESET                       Reset to autonomous baseline
  COLOR:<r>,<g>,<b>           Future: RGB color control
```

### Priority Levels

```
Priority Range    Layer           Example Events
─────────────────────────────────────────────────────
0                 Autonomous      Baseline (always active)
1-3               Time-based      Morning/Day/Evening/Night
4-6               System state    CPU usage, RAM, active app
7-10              Events          Music, notifications, alerts
```

### Command Examples

```
MOOD:MOOD_HAPPY:8          // Set happy mood (priority 8)
POS:E:5                    // Look right (priority 5)
ANIM:LAUGH                 // Play laugh animation
MOOD:MOOD_TIRED:3          // Set tired mood (priority 3)
IDLE:OFF                   // Disable idle eye movement
BLINK:ON                   // Enable auto-blinking
RESET                      // Reset to default autonomous state
```

### Response Format (Future)

```
OK:<command>               // Command accepted
ERROR:<message>            // Command failed
STATE:<mood>:<priority>    // Current state (on request)
```

---

## Phase 1: Arduino Dual Communication (USB + WiFi)

### Objectives
- Add serial command parser (USB mode)
- Add WiFi HTTP/WebSocket server (WiFi mode)
- Auto-detect connection mode (USB vs WiFi)
- Implement priority-based mood system
- Maintain autonomous behaviors
- Test both communication modes

### Communication Mode Selection

**Strategy:** Auto-detect which mode to use
```
On Startup:
1. Check if USB Serial is active (PC connected)
2. If USB active → Use Serial mode
3. If USB not active → Start WiFi mode
4. Periodically check and switch modes if needed
```

**Arduino Communication Modes:**

| Mode | When | Protocol | Port | Use Case |
|------|------|----------|------|----------|
| USB Serial | PC connected | Serial | 115200 baud | Development, PC service |
| WiFi HTTP | Standalone | HTTP REST | Port 80 | Mobile app, web control |
| WiFi WebSocket | Standalone | WebSocket | Port 81 | Real-time bidirectional |

### Phase 1.1: Arduino WiFi Setup

**Add WiFi credentials (stored in separate file):**

Create `WiFiConfig.h`:
```cpp
#ifndef WIFI_CONFIG_H
#define WIFI_CONFIG_H

// WiFi credentials
#define WIFI_SSID "YourWiFiSSID"
#define WIFI_PASSWORD "YourWiFiPassword"

// Optional: Static IP configuration
#define USE_STATIC_IP false
#define STATIC_IP     IPAddress(192, 168, 1, 100)
#define GATEWAY       IPAddress(192, 168, 1, 1)
#define SUBNET        IPAddress(255, 255, 255, 0)

// Server configuration
#define HTTP_PORT 80
#define WEBSOCKET_PORT 81

#endif
```

**Add to `.gitignore`:**
```
WiFiConfig.h
```

### Phase 1.2: Arduino Dependencies

**Required Libraries:**
- WiFi (built-in for ESP32)
- WebServer (built-in for ESP32)
- ArduinoJson (for JSON parsing)

**Install via Arduino Library Manager:**
```
Sketch → Include Library → Manage Libraries
Search: ArduinoJson
```

### Phase 1.3: Arduino Dual-Mode Implementation

**Complete Arduino code with auto-detection:**

Add to top of `.ino` file:
```cpp
#include <WiFi.h>
#include <WebServer.h>
#include "WiFiConfig.h"  // Your credentials

// Communication mode
enum CommMode { MODE_USB, MODE_WIFI };
CommMode currentMode = MODE_USB;

// WiFi server
WebServer httpServer(HTTP_PORT);
bool wifiStarted = false;
```

Add WiFi detection and setup:
```cpp
CommMode detectCommunicationMode() {
  // Check if USB Serial is active (PC connected)
  // On ESP32, Serial is always available, so check if DTR is active
  delay(100);

  if (Serial) {
    // Check if there's actual USB connection (not just Serial object)
    Serial.println("USB detected - using Serial mode");
    return MODE_USB;
  }

  Serial.println("No USB - starting WiFi mode");
  return MODE_WIFI;
}

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

Add HTTP handlers:
```cpp
void handleMoodHttp() {
  if (!httpServer.hasArg("mood") || !httpServer.hasArg("priority")) {
    httpServer.send(400, "application/json", "{\"error\":\"Missing parameters\"}");
    return;
  }

  String mood = httpServer.arg("mood");
  int priority = httpServer.arg("priority").toInt();
  int duration = httpServer.hasArg("duration") ? httpServer.arg("duration").toInt() : 0;

  // Build command string
  String cmd = "MOOD:" + mood + ":" + String(priority);
  if (duration > 0) {
    cmd += ":" + String(duration);
  }

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

  String cmd = "POS:" + pos + ":" + String(priority);
  processCommand(cmd);
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}

void handleAnimationHttp() {
  if (!httpServer.hasArg("animation")) {
    httpServer.send(400, "application/json", "{\"error\":\"Missing animation parameter\"}");
    return;
  }

  String anim = httpServer.arg("animation");
  String cmd = "ANIM:" + anim;
  processCommand(cmd);
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}

void handleStatusHttp() {
  String json = "{";
  json += "\"mood\":\"" + String(currentMood) + "\",";
  json += "\"priority\":" + String(currentMoodPriority) + ",";
  json += "\"mode\":\"" + String(currentMode == MODE_USB ? "USB" : "WiFi") + "\"";
  json += "}";
  httpServer.send(200, "application/json", json);
}

void handleResetHttp() {
  handleResetCommand();
  httpServer.send(200, "application/json", "{\"status\":\"ok\"}");
}
```

Update `setup()`:
```cpp
void setup() {
  Serial.begin(115200);
  delay(1000);

  // ... existing display setup ...

  // Detect communication mode
  currentMode = detectCommunicationMode();

  if (currentMode == MODE_WIFI) {
    setupWiFi();
  }

  Serial.println("Mochi ready for commands!");
  Serial.println("Format: COMMAND:PARAM1:PARAM2");
}
```

Update `loop()`:
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
}
```

### Phase 1.4: Arduino Implementation Steps

#### 1.4.1 Add Serial Command Parser

**Location:** `Desktop_Companion.ino`

Note: The `setup()` and `loop()` are already defined in Phase 1.3 above.

#### 1.4.2 Create Command Parser Function

**Add global variables:**
```cpp
// Current mood state
byte currentMood = MOOD_DEFAULT;
int currentMoodPriority = 0;
unsigned long moodExpiryTime = 0;  // 0 = no expiry

// Command buffer
#define CMD_BUFFER_SIZE 64
char cmdBuffer[CMD_BUFFER_SIZE];
```

**Add command parser:**
```cpp
void processCommand(String cmd) {
  // Split command by ':'
  int firstColon = cmd.indexOf(':');
  if (firstColon == -1) {
    Serial.println("ERROR:Invalid format");
    return;
  }

  String command = cmd.substring(0, firstColon);
  String params = cmd.substring(firstColon + 1);

  // Route to handlers
  if (command == "MOOD") {
    handleMoodCommand(params);
  } else if (command == "POS") {
    handlePositionCommand(params);
  } else if (command == "ANIM") {
    handleAnimationCommand(params);
  } else if (command == "IDLE") {
    handleIdleCommand(params);
  } else if (command == "BLINK") {
    handleBlinkCommand(params);
  } else if (command == "RESET") {
    handleResetCommand();
  } else {
    Serial.print("ERROR:Unknown command: ");
    Serial.println(command);
  }
}
```

#### 1.4.3 Implement Command Handlers

**Mood Command Handler:**
```cpp
void handleMoodCommand(String params) {
  // Format: MOOD_HAPPY:8 or MOOD_HAPPY:8:5000 (5s duration)
  int colon = params.indexOf(':');
  if (colon == -1) {
    Serial.println("ERROR:MOOD needs priority");
    return;
  }

  String moodStr = params.substring(0, colon);
  String rest = params.substring(colon + 1);

  int priority = rest.toInt();

  // Check priority
  if (priority < currentMoodPriority) {
    Serial.println("OK:MOOD_IGNORED (priority too low)");
    return;
  }

  // Parse mood
  byte mood = parseMood(moodStr);
  if (mood == 255) {  // Invalid
    Serial.print("ERROR:Unknown mood: ");
    Serial.println(moodStr);
    return;
  }

  // Apply mood
  currentMood = mood;
  currentMoodPriority = priority;
  roboEyes.setMood(mood);

  // Check for duration parameter
  int secondColon = rest.indexOf(':');
  if (secondColon != -1) {
    int duration = rest.substring(secondColon + 1).toInt();
    moodExpiryTime = millis() + (duration * 1000);
  } else {
    moodExpiryTime = 0;  // No expiry
  }

  Serial.print("OK:MOOD_");
  Serial.print(moodStr);
  Serial.print(" (pri:");
  Serial.print(priority);
  Serial.println(")");
}

byte parseMood(String moodStr) {
  if (moodStr == "MOOD_DEFAULT") return MOOD_DEFAULT;
  if (moodStr == "MOOD_HAPPY") return MOOD_HAPPY;
  if (moodStr == "MOOD_TIRED") return MOOD_TIRED;
  if (moodStr == "MOOD_ANGRY") return MOOD_ANGRY;
  return 255;  // Invalid
}
```

**Position Command Handler:**
```cpp
void handlePositionCommand(String params) {
  // Format: E:5 (position:priority)
  int colon = params.indexOf(':');
  if (colon == -1) {
    Serial.println("ERROR:POS needs priority");
    return;
  }

  String posStr = params.substring(0, colon);
  int priority = params.substring(colon + 1).toInt();

  // Check priority
  if (priority < currentMoodPriority) {
    Serial.println("OK:POS_IGNORED (priority too low)");
    return;
  }

  // Parse position
  byte pos = parsePosition(posStr);
  if (pos == 255) {
    Serial.print("ERROR:Unknown position: ");
    Serial.println(posStr);
    return;
  }

  roboEyes.setPosition(pos);
  Serial.print("OK:POS_");
  Serial.println(posStr);
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
  return 255;  // Invalid
}
```

**Animation Command Handler:**
```cpp
void handleAnimationCommand(String params) {
  // Animations always interrupt (highest priority)
  if (params == "BLINK") {
    roboEyes.blink();
  } else if (params == "CONFUSED") {
    roboEyes.anim_confused();
  } else if (params == "LAUGH") {
    roboEyes.anim_laugh();
  } else {
    Serial.print("ERROR:Unknown animation: ");
    Serial.println(params);
    return;
  }

  Serial.print("OK:ANIM_");
  Serial.println(params);
}
```

**Idle/Blink Command Handlers:**
```cpp
void handleIdleCommand(String params) {
  bool enable = (params == "ON" || params == "1");
  roboEyes.setIdleMode(enable);
  Serial.print("OK:IDLE_");
  Serial.println(enable ? "ON" : "OFF");
}

void handleBlinkCommand(String params) {
  bool enable = (params == "ON" || params == "1");
  roboEyes.setAutoblinker(enable);
  Serial.print("OK:BLINK_");
  Serial.println(enable ? "ON" : "OFF");
}
```

**Reset Command Handler:**
```cpp
void handleResetCommand() {
  currentMood = MOOD_DEFAULT;
  currentMoodPriority = 0;
  moodExpiryTime = 0;

  roboEyes.setMood(MOOD_DEFAULT);
  roboEyes.setPosition(0);  // Center
  roboEyes.setIdleMode(ON);
  roboEyes.setAutoblinker(ON);

  Serial.println("OK:RESET");
}
```

#### 1.4.4 Add Mood Expiry Check

**Add to `loop()`:**
```cpp
void loop() {
  roboEyes.update();

  // Check for mood expiry
  if (moodExpiryTime > 0 && millis() >= moodExpiryTime) {
    // Revert to baseline
    currentMoodPriority = 0;
    moodExpiryTime = 0;
    roboEyes.setMood(MOOD_DEFAULT);
    Serial.println("INFO:Mood expired, reverted to default");
  }

  // Check for serial commands
  if (Serial.available() > 0) {
    String command = Serial.readStringUntil('\n');
    command.trim();
    processCommand(command);
  }
}
```

#### 1.4.5 Testing Serial Commands

**Test via Arduino Serial Monitor (115200 baud):**

**Test via Arduino Serial Monitor (115200 baud):**

```
> MOOD:MOOD_HAPPY:8
OK:MOOD_HAPPY (pri:8)

> POS:E:5
OK:POS_IGNORED (priority too low)

> POS:E:10
OK:POS_E

> ANIM:LAUGH
OK:ANIM_LAUGH

> MOOD:MOOD_TIRED:3
OK:MOOD_IGNORED (priority too low)

> RESET
OK:RESET

> IDLE:OFF
OK:IDLE_OFF
```

**Expected Behavior:**
- ✅ Lower priority commands are ignored
- ✅ Higher priority commands override current state
- ✅ Animations always play
- ✅ Autonomous behaviors keep running (auto-blink, idle if enabled)
- ✅ Reset returns to default state

---

## Phase 2: .NET 10 Windows Service

### Objectives
- Create .NET 10 Windows Service project
- Implement serial communication
- Add basic MoodManager with priority handling
- Test communication with Mochi

### Tech Stack

**.NET 10 Service:**
- **.NET 10** (latest LTS)
- **Microsoft.Extensions.Hosting** - Windows Service hosting
- **System.IO.Ports** - USB Serial communication
- **Microsoft.Extensions.DependencyInjection** - DI container
- **Microsoft.Extensions.Configuration** - Configuration management
- **Microsoft.Extensions.Logging** - Structured logging
- **Serilog** - File and console logging
- **MediatR** - CQRS/Mediator pattern (optional, for clean separation)
- **FluentValidation** - Input validation

**Testing:**
- **xUnit** - Test framework
- **Moq** - Mocking framework
- **FluentAssertions** - Fluent assertion library
- **Microsoft.Extensions.Hosting.Testing** - Service testing utilities
- **Testcontainers** (optional) - Integration testing

**Communication:**
- **System.IO.Ports** - USB Serial
- **System.Net.Http** - HTTP client (WiFi fallback)
- **System.Net.WebSockets** - WebSocket client (WiFi fallback, optional)

### Clean Architecture Structure

Following **Clean Architecture** (Uncle Bob) with clear separation of concerns:

```
MochiCompanion/
├── MochiCompanion.sln
├── src/
│   ├── Core/                           ← Inner layer (no dependencies)
│   │   ├── MochiCompanion.Domain/      ← Entities, Value Objects
│   │   │   ├── Entities/
│   │   │   │   ├── MoodState.cs
│   │   │   │   └── MonitorResult.cs
│   │   │   ├── Enums/
│   │   │   │   ├── MoodType.cs
│   │   │   │   ├── PositionType.cs
│   │   │   │   └── AnimationType.cs
│   │   │   └── ValueObjects/
│   │   │       ├── Priority.cs
│   │   │       └── Duration.cs
│   │   │
│   │   └── MochiCompanion.Application/ ← Use Cases, Interfaces
│   │       ├── Interfaces/
│   │       │   ├── ICommunication/
│   │       │   │   ├── IMochiConnection.cs
│   │       │   │   └── ICommandBuilder.cs
│   │       │   ├── IMonitors/
│   │       │   │   └── ISystemMonitor.cs
│   │       │   └── IServices/
│   │       │       ├── IMoodService.cs
│   │       │       └── IMonitoringService.cs
│   │       ├── Services/
│   │       │   ├── MoodService.cs
│   │       │   └── MonitoringService.cs
│   │       ├── DTOs/
│   │       │   ├── MoodSuggestion.cs
│   │       │   └── MonitorState.cs
│   │       └── Exceptions/
│   │           ├── ConnectionException.cs
│   │           └── MoodException.cs
│   │
│   ├── Infrastructure/                  ← Outer layer (external dependencies)
│   │   └── MochiCompanion.Infrastructure/
│   │       ├── Communication/
│   │       │   ├── SerialConnection.cs      ← USB implementation
│   │       │   ├── HttpConnection.cs        ← WiFi HTTP implementation
│   │       │   ├── WebSocketConnection.cs   ← WiFi WebSocket implementation
│   │       │   ├── CommandBuilder.cs
│   │       │   └── ConnectionFactory.cs     ← Factory pattern
│   │       ├── Monitors/
│   │       │   ├── AudioMonitor.cs
│   │       │   ├── SystemMonitor.cs
│   │       │   ├── TimeMonitor.cs
│   │       │   └── ApplicationMonitor.cs
│   │       ├── Logging/
│   │       │   └── SerilogConfiguration.cs
│   │       └── Persistence/
│   │           └── ConfigurationRepository.cs
│   │
│   └── Presentation/                    ← UI/Service layer
│       └── MochiCompanion.Service/
│           ├── Program.cs
│           ├── MochiWorker.cs           ← Background service
│           ├── appsettings.json
│           ├── appsettings.Development.json
│           └── Extensions/
│               ├── ServiceCollectionExtensions.cs
│               └── HostBuilderExtensions.cs
│
└── tests/
    ├── MochiCompanion.UnitTests/        ← Unit tests (fast, isolated)
    │   ├── Application/
    │   │   └── Services/
    │   │       ├── MoodServiceTests.cs
    │   │       └── MonitoringServiceTests.cs
    │   ├── Infrastructure/
    │   │   ├── Communication/
    │   │   │   ├── SerialConnectionTests.cs
    │   │   │   └── HttpConnectionTests.cs
    │   │   └── Monitors/
    │   │       ├── AudioMonitorTests.cs
    │   │       └── SystemMonitorTests.cs
    │   └── TestHelpers/
    │       ├── Builders/
    │       │   └── MoodSuggestionBuilder.cs
    │       └── Mocks/
    │           └── MockMochiConnection.cs
    │
    └── MochiCompanion.IntegrationTests/ ← Integration tests (slower, real dependencies)
        ├── Communication/
        │   ├── SerialCommunicationTests.cs
        │   └── HttpCommunicationTests.cs
        ├── EndToEnd/
        │   └── MoodWorkflowTests.cs
        ├── Fixtures/
        │   └── ServiceFixture.cs
        └── TestHelpers/
            └── MockSerialPort.cs
```

**Layer Dependencies:**
```
Presentation → Infrastructure → Application → Domain
     ↓              ↓                ↓            ↓
   Service    Implementations    Interfaces   Entities
```

**Dependency Rule:** Inner layers NEVER depend on outer layers!

### Implementation Steps

#### 2.1 Create .NET 10 Service Project

**Via Visual Studio:**
1. File → New → Project
2. Select "Worker Service" template
3. Name: `MochiCompanion.Service`
4. Framework: .NET 10.0

**Via CLI:**
```bash
dotnet new worker -n MochiCompanion.Service -f net10.0
cd MochiCompanion.Service
dotnet add package Microsoft.Extensions.Hosting.WindowsServices
dotnet add package System.IO.Ports
```

#### 2.2 Configure as Windows Service

**Program.cs:**
```csharp
using MochiCompanion.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

// Register as Windows Service
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = "Mochi Companion Service";
});

// Register services
builder.Services.AddHostedService<MochiService>();
builder.Services.AddSingleton<ISerialConnection, SerialConnection>();
builder.Services.AddSingleton<IMoodManager, MoodManager>();

// Register monitors
builder.Services.AddSingleton<IMonitor, TimeMonitor>();
builder.Services.AddSingleton<IMonitor, AudioMonitor>();
builder.Services.AddSingleton<IMonitor, SystemMonitor>();
builder.Services.AddSingleton<IMonitor, ApplicationMonitor>();

var host = builder.Build();
await host.RunAsync();
```

#### 2.3 Implement Serial Connection

**ISerialConnection.cs:**
```csharp
public interface ISerialConnection
{
    Task ConnectAsync(string portName, int baudRate);
    void Disconnect();
    Task SendCommandAsync(string command);
    bool IsConnected { get; }
    event EventHandler<string>? DataReceived;
}
```

**SerialConnection.cs:**
```csharp
using System.IO.Ports;
using Microsoft.Extensions.Logging;

public class SerialConnection : ISerialConnection, IDisposable
{
    private readonly ILogger<SerialConnection> _logger;
    private SerialPort? _serialPort;

    public bool IsConnected => _serialPort?.IsOpen ?? false;
    public event EventHandler<string>? DataReceived;

    public SerialConnection(ILogger<SerialConnection> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(string portName, int baudRate)
    {
        try
        {
            _serialPort = new SerialPort(portName, baudRate);
            _serialPort.DataReceived += OnDataReceived;
            _serialPort.Open();

            _logger.LogInformation("Connected to {Port} at {BaudRate} baud", portName, baudRate);

            // Wait for Arduino to initialize
            await Task.Delay(2000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Port}", portName);
            throw;
        }
    }

    public void Disconnect()
    {
        if (_serialPort?.IsOpen == true)
        {
            _serialPort.Close();
            _logger.LogInformation("Disconnected from serial port");
        }
    }

    public async Task SendCommandAsync(string command)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send command - not connected");
            return;
        }

        try
        {
            await _serialPort!.BaseStream.WriteAsync(
                System.Text.Encoding.ASCII.GetBytes(command + "\n"));

            _logger.LogDebug("Sent: {Command}", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send command: {Command}", command);
        }
    }

    private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var data = _serialPort!.ReadLine();
            _logger.LogDebug("Received: {Data}", data);
            DataReceived?.Invoke(this, data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading serial data");
        }
    }

    public void Dispose()
    {
        Disconnect();
        _serialPort?.Dispose();
    }
}
```

#### 2.3b Implement HTTP Connection (WiFi Fallback)

**HttpConnection.cs:**
```csharp
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

public class HttpConnection : ISerialConnection
{
    private readonly ILogger<HttpConnection> _logger;
    private readonly HttpClient _httpClient;
    private string _baseUrl = "";
    private bool _isConnected;

    public bool IsConnected => _isConnected;
    public event EventHandler<string>? DataReceived;

    public HttpConnection(ILogger<HttpConnection> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task ConnectAsync(string baseUrl, int port = 80)
    {
        _baseUrl = $"http://{baseUrl}:{port}";

        try
        {
            // Test connection
            var response = await _httpClient.GetAsync($"{_baseUrl}/status");
            response.EnsureSuccessStatusCode();

            _isConnected = true;
            _logger.LogInformation("Connected to Mochi via HTTP at {Url}", _baseUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to {Url}", _baseUrl);
            _isConnected = false;
            throw;
        }
    }

    public void Disconnect()
    {
        _isConnected = false;
        _logger.LogInformation("Disconnected from HTTP endpoint");
    }

    public async Task SendCommandAsync(string command)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send command - not connected");
            return;
        }

        try
        {
            // Parse command and convert to HTTP request
            var parts = command.Split(':');
            var endpoint = parts[0].ToLower();

            HttpResponseMessage response = endpoint switch
            {
                "mood" => await _httpClient.PostAsync($"{_baseUrl}/mood?mood={parts[1]}&priority={parts[2]}" +
                    (parts.Length > 3 ? $"&duration={parts[3]}" : ""), null),
                "pos" => await _httpClient.PostAsync($"{_baseUrl}/position?position={parts[1]}&priority={parts[2]}", null),
                "anim" => await _httpClient.PostAsync($"{_baseUrl}/animation?animation={parts[1]}", null),
                "reset" => await _httpClient.PostAsync($"{_baseUrl}/reset", null),
                _ => throw new ArgumentException($"Unknown command: {endpoint}")
            };

            response.EnsureSuccessStatusCode();
            _logger.LogDebug("Sent HTTP: {Command}", command);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send HTTP command: {Command}", command);
        }
    }

    Task ISerialConnection.ConnectAsync(string portName, int baudRate)
    {
        // Treat portName as IP address for HTTP
        return ConnectAsync(portName, baudRate == 115200 ? 80 : baudRate);
    }
}
```

#### 2.3c Connection Factory

**IConnectionFactory.cs:**
```csharp
public interface IConnectionFactory
{
    Task<ISerialConnection> CreateConnectionAsync(string address);
}
```

**ConnectionFactory.cs:**
```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

public class ConnectionFactory : IConnectionFactory
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ConnectionFactory> _logger;

    public ConnectionFactory(IServiceProvider services, ILogger<ConnectionFactory> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<ISerialConnection> CreateConnectionAsync(string address)
    {
        // Try Serial (USB) first
        if (address.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation("Attempting USB Serial connection on {Port}", address);
            var serialConn = _services.GetRequiredService<SerialConnection>();
            try
            {
                await serialConn.ConnectAsync(address, 115200);
                return serialConn;
            }
            catch
            {
                _logger.LogWarning("USB connection failed, will try HTTP");
            }
        }

        // Try HTTP (WiFi) fallback
        _logger.LogInformation("Attempting HTTP connection to {Address}", address);
        var httpConn = _services.GetRequiredService<HttpConnection>();
        await httpConn.ConnectAsync(address, 80);
        return httpConn;
    }
}
```

**CommandBuilder.cs:**
```csharp
public static class CommandBuilder
{
    public static string BuildMoodCommand(string mood, int priority, int? durationSeconds = null)
    {
        var cmd = $"MOOD:{mood}:{priority}";
        if (durationSeconds.HasValue)
        {
            cmd += $":{durationSeconds.Value}";
        }
        return cmd;
    }

    public static string BuildPositionCommand(string position, int priority)
        => $"POS:{position}:{priority}";

    public static string BuildAnimationCommand(string animation)
        => $"ANIM:{animation}";

    public static string BuildIdleCommand(bool enabled)
        => $"IDLE:{(enabled ? "ON" : "OFF")}";

    public static string BuildBlinkCommand(bool enabled)
        => $"BLINK:{(enabled ? "ON" : "OFF")}";

    public static string BuildResetCommand()
        => "RESET";
}
```

#### 2.4 Implement Mood Manager

**MoodSuggestion.cs:**
```csharp
public class MoodSuggestion
{
    public required string Mood { get; init; }
    public required int Priority { get; init; }
    public string? Position { get; init; }
    public string? Animation { get; init; }
    public TimeSpan? Duration { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

**IMoodManager.cs:**
```csharp
public interface IMoodManager
{
    Task ApplySuggestionAsync(MoodSuggestion suggestion);
    MoodSuggestion? CurrentMood { get; }
    void Reset();
}
```

**MoodManager.cs:**
```csharp
using Microsoft.Extensions.Logging;

public class MoodManager : IMoodManager
{
    private readonly ISerialConnection _serial;
    private readonly ILogger<MoodManager> _logger;
    private MoodSuggestion? _currentMood;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public MoodSuggestion? CurrentMood => _currentMood;

    public MoodManager(ISerialConnection serial, ILogger<MoodManager> logger)
    {
        _serial = serial;
        _logger = logger;
    }

    public async Task ApplySuggestionAsync(MoodSuggestion suggestion)
    {
        await _lock.WaitAsync();
        try
        {
            // Check priority
            if (_currentMood != null && suggestion.Priority < _currentMood.Priority)
            {
                _logger.LogDebug(
                    "Ignoring {Mood} (pri {Priority}) - current mood {CurrentMood} has higher priority {CurrentPriority}",
                    suggestion.Mood, suggestion.Priority, _currentMood.Mood, _currentMood.Priority);
                return;
            }

            // Apply new mood
            _currentMood = suggestion;

            // Send commands to Mochi
            var moodCmd = CommandBuilder.BuildMoodCommand(
                suggestion.Mood,
                suggestion.Priority,
                (int?)suggestion.Duration?.TotalSeconds);

            await _serial.SendCommandAsync(moodCmd);

            if (suggestion.Position != null)
            {
                var posCmd = CommandBuilder.BuildPositionCommand(
                    suggestion.Position,
                    suggestion.Priority);
                await _serial.SendCommandAsync(posCmd);
            }

            if (suggestion.Animation != null)
            {
                var animCmd = CommandBuilder.BuildAnimationCommand(suggestion.Animation);
                await _serial.SendCommandAsync(animCmd);
            }

            _logger.LogInformation(
                "Applied mood: {Mood} (priority {Priority})",
                suggestion.Mood, suggestion.Priority);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Reset()
    {
        _currentMood = null;
        _serial.SendCommandAsync(CommandBuilder.BuildResetCommand()).Wait();
        _logger.LogInformation("Reset to baseline");
    }
}
```

#### 2.5 Implement Main Service

**MochiService.cs:**
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class MochiService : BackgroundService
{
    private readonly ILogger<MochiService> _logger;
    private readonly IConfiguration _config;
    private readonly ISerialConnection _serial;
    private readonly IMoodManager _moodManager;
    private readonly IEnumerable<IMonitor> _monitors;

    public MochiService(
        ILogger<MochiService> logger,
        IConfiguration config,
        ISerialConnection serial,
        IMoodManager moodManager,
        IEnumerable<IMonitor> monitors)
    {
        _logger = logger;
        _config = config;
        _serial = serial;
        _moodManager = moodManager;
        _monitors = monitors;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Mochi Companion Service starting...");

        try
        {
            // Connect to Mochi
            var portName = _config["Mochi:SerialPort"] ?? "COM3";
            var baudRate = _config.GetValue<int>("Mochi:BaudRate", 115200);

            await _serial.ConnectAsync(portName, baudRate);

            // Start monitors
            var monitorTasks = _monitors.Select(m => RunMonitorAsync(m, stoppingToken));

            await Task.WhenAll(monitorTasks);
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Service failed to start");
            throw;
        }
    }

    private async Task RunMonitorAsync(IMonitor monitor, CancellationToken ct)
    {
        _logger.LogInformation("Starting monitor: {Monitor}", monitor.GetType().Name);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var suggestion = await monitor.CheckAsync();
                if (suggestion != null)
                {
                    await _moodManager.ApplySuggestionAsync(suggestion);
                }

                await Task.Delay(monitor.CheckInterval, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in monitor {Monitor}", monitor.GetType().Name);
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }

        _logger.LogInformation("Stopped monitor: {Monitor}", monitor.GetType().Name);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Mochi Companion Service stopping...");
        _serial.Disconnect();
        await base.StopAsync(cancellationToken);
    }
}
```

#### 2.6 Configuration

**appsettings.json:**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MochiCompanion": "Debug"
    }
  },
  "Mochi": {
    "SerialPort": "COM3",
    "BaudRate": 115200,
    "ReconnectInterval": 5000
  }
}
```

#### 2.7 Testing

**Build and run:**
```bash
dotnet build
dotnet run
```

**Expected output:**
```
info: Mochi Companion Service starting...
info: Connected to COM3 at 115200 baud
info: Starting monitor: TimeMonitor
info: Starting monitor: AudioMonitor
info: Applied mood: MOOD_DEFAULT (priority 1)
```

**Test manual command:**
```csharp
// In Program.cs or test console app
var serial = new SerialConnection(logger);
await serial.ConnectAsync("COM3", 115200);
await serial.SendCommandAsync("MOOD:MOOD_HAPPY:8");
await serial.SendCommandAsync("ANIM:LAUGH");
```

---

## Phase 3: Implement Monitors

### 3.1 Base Monitor Interface

**IMonitor.cs:**
```csharp
public interface IMonitor
{
    Task<MoodSuggestion?> CheckAsync();
    TimeSpan CheckInterval { get; }
    string Name { get; }
}
```

### 3.2 Time Monitor (Priority 1-3)

**TimeMonitor.cs:**
```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

public class TimeMonitor : IMonitor
{
    private readonly ILogger<TimeMonitor> _logger;
    private readonly IConfiguration _config;

    public string Name => "TimeMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromMinutes(1);

    public TimeMonitor(ILogger<TimeMonitor> logger, IConfiguration config)
    {
        _logger = logger;
        _config = config;
    }

    public Task<MoodSuggestion?> CheckAsync()
    {
        var now = DateTime.Now;
        var hour = now.Hour;

        var suggestion = hour switch
        {
            >= 6 and < 9 => new MoodSuggestion
            {
                Mood = "MOOD_TIRED",
                Priority = 2,
                Position = null,
                Animation = null
            },
            >= 9 and < 18 => new MoodSuggestion
            {
                Mood = "MOOD_DEFAULT",
                Priority = 1
            },
            >= 18 and < 22 => new MoodSuggestion
            {
                Mood = "MOOD_HAPPY",
                Priority = 1
            },
            _ => new MoodSuggestion
            {
                Mood = "MOOD_TIRED",
                Priority = 3,
                Position = "S"  // Look down (sleepy)
            }
        };

        _logger.LogDebug("Time check: {Hour}:00 → {Mood}", hour, suggestion.Mood);
        return Task.FromResult<MoodSuggestion?>(suggestion);
    }
}
```

### 3.3 Audio Monitor (Priority 8)

**Dependencies:**
```bash
dotnet add package NAudio
```

**AudioMonitor.cs:**
```csharp
using NAudio.CoreAudioApi;
using Microsoft.Extensions.Logging;

public class AudioMonitor : IMonitor
{
    private readonly ILogger<AudioMonitor> _logger;
    private readonly MMDeviceEnumerator _deviceEnum;
    private MMDevice? _defaultDevice;
    private bool _wasPlayingMusic = false;

    public string Name => "AudioMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(2);

    public AudioMonitor(ILogger<AudioMonitor> logger)
    {
        _logger = logger;
        _deviceEnum = new MMDeviceEnumerator();
        _defaultDevice = _deviceEnum.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public Task<MoodSuggestion?> CheckAsync()
    {
        try
        {
            var isPlaying = IsMusicPlaying();

            if (isPlaying && !_wasPlayingMusic)
            {
                // Music just started
                _wasPlayingMusic = true;
                _logger.LogInformation("Music started playing");

                return Task.FromResult<MoodSuggestion?>(new MoodSuggestion
                {
                    Mood = "MOOD_HAPPY",
                    Priority = 8,
                    Animation = "LAUGH",
                    Duration = TimeSpan.FromSeconds(5)
                });
            }
            else if (!isPlaying && _wasPlayingMusic)
            {
                // Music stopped
                _wasPlayingMusic = false;
                _logger.LogInformation("Music stopped");
            }

            return Task.FromResult<MoodSuggestion?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking audio state");
            return Task.FromResult<MoodSuggestion?>(null);
        }
    }

    private bool IsMusicPlaying()
    {
        if (_defaultDevice == null)
            return false;

        var sessionManager = _defaultDevice.AudioSessionManager;
        var sessions = sessionManager.Sessions;

        for (int i = 0; i < sessions.Count; i++)
        {
            var session = sessions[i];
            var state = session.State;

            if (state == AudioSessionState.AudioSessionStateActive)
            {
                var processName = session.GetSessionIdentifier;
                // Filter out system sounds
                if (!string.IsNullOrEmpty(processName) &&
                    !processName.Contains("SystemSounds"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
```

### 3.4 System Monitor (Priority 4-6)

**Dependencies:**
```bash
dotnet add package System.Diagnostics.PerformanceCounter
```

**SystemMonitor.cs:**
```csharp
using System.Diagnostics;
using Microsoft.Extensions.Logging;

public class SystemMonitor : IMonitor
{
    private readonly ILogger<SystemMonitor> _logger;
    private readonly PerformanceCounter _cpuCounter;
    private float _lastCpuUsage = 0;

    public string Name => "SystemMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(5);

    public SystemMonitor(ILogger<SystemMonitor> logger)
    {
        _logger = logger;
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        // First call always returns 0, so prime it
        _cpuCounter.NextValue();
    }

    public Task<MoodSuggestion?> CheckAsync()
    {
        try
        {
            var cpuUsage = _cpuCounter.NextValue();

            // Only react to significant changes
            if (Math.Abs(cpuUsage - _lastCpuUsage) < 10)
                return Task.FromResult<MoodSuggestion?>(null);

            _lastCpuUsage = cpuUsage;

            if (cpuUsage > 80)
            {
                _logger.LogInformation("High CPU usage: {Cpu}%", cpuUsage);
                return Task.FromResult<MoodSuggestion?>(new MoodSuggestion
                {
                    Mood = "MOOD_TIRED",
                    Priority = 5,
                    Position = "S"  // Look down
                });
            }
            else if (cpuUsage < 20)
            {
                return Task.FromResult<MoodSuggestion?>(new MoodSuggestion
                {
                    Mood = "MOOD_DEFAULT",
                    Priority = 4
                });
            }

            return Task.FromResult<MoodSuggestion?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking system state");
            return Task.FromResult<MoodSuggestion?>(null);
        }
    }
}
```

### 3.5 Application Monitor (Priority 5-7)

**ApplicationMonitor.cs:**
```csharp
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Extensions.Logging;

public class ApplicationMonitor : IMonitor
{
    private readonly ILogger<ApplicationMonitor> _logger;
    private string _lastActiveWindow = "";

    public string Name => "ApplicationMonitor";
    public TimeSpan CheckInterval => TimeSpan.FromSeconds(3);

    public ApplicationMonitor(ILogger<ApplicationMonitor> logger)
    {
        _logger = logger;
    }

    public Task<MoodSuggestion?> CheckAsync()
    {
        try
        {
            var activeWindow = GetActiveWindowTitle();

            if (activeWindow == _lastActiveWindow)
                return Task.FromResult<MoodSuggestion?>(null);

            _lastActiveWindow = activeWindow;

            // Check for specific applications
            if (activeWindow.Contains("Visual Studio") || activeWindow.Contains("VS Code"))
            {
                _logger.LogInformation("Coding app detected: {App}", activeWindow);
                return Task.FromResult<MoodSuggestion?>(new MoodSuggestion
                {
                    Mood = "MOOD_DEFAULT",
                    Priority = 6,
                    Position = "N"  // Look up (focused)
                });
            }
            else if (activeWindow.Contains("Spotify") || activeWindow.Contains("YouTube"))
            {
                _logger.LogInformation("Media app detected: {App}", activeWindow);
                return Task.FromResult<MoodSuggestion?>(new MoodSuggestion
                {
                    Mood = "MOOD_HAPPY",
                    Priority = 7
                });
            }

            return Task.FromResult<MoodSuggestion?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking active application");
            return Task.FromResult<MoodSuggestion?>(null);
        }
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    private string GetActiveWindowTitle()
    {
        const int nChars = 256;
        var buff = new StringBuilder(nChars);
        var handle = GetForegroundWindow();

        if (GetWindowText(handle, buff, nChars) > 0)
        {
            return buff.ToString();
        }

        return string.Empty;
    }
}
```

---

## Phase 4: Configuration System

### Enhanced Configuration

**appsettings.json (Full Example):**
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "MochiCompanion": "Debug"
    }
  },
  "Mochi": {
    "SerialPort": "COM3",
    "BaudRate": 115200,
    "ReconnectInterval": 5000
  },
  "Monitors": {
    "Audio": {
      "Enabled": true,
      "CheckInterval": 2000,
      "Priority": 8,
      "Reactions": {
        "MusicStarted": {
          "Mood": "MOOD_HAPPY",
          "Animation": "LAUGH",
          "Duration": 5
        }
      }
    },
    "System": {
      "Enabled": true,
      "CheckInterval": 5000,
      "CpuThresholds": {
        "High": 80,
        "Low": 20
      },
      "Reactions": {
        "HighCpu": {
          "Mood": "MOOD_TIRED",
          "Priority": 5,
          "Position": "S"
        },
        "LowCpu": {
          "Mood": "MOOD_DEFAULT",
          "Priority": 4
        }
      }
    },
    "Time": {
      "Enabled": true,
      "CheckInterval": 60000,
      "Schedule": {
        "Morning": {
          "Start": "06:00",
          "End": "09:00",
          "Mood": "MOOD_TIRED",
          "Priority": 2
        },
        "Day": {
          "Start": "09:00",
          "End": "18:00",
          "Mood": "MOOD_DEFAULT",
          "Priority": 1
        },
        "Evening": {
          "Start": "18:00",
          "End": "22:00",
          "Mood": "MOOD_HAPPY",
          "Priority": 1
        },
        "Night": {
          "Start": "22:00",
          "End": "06:00",
          "Mood": "MOOD_TIRED",
          "Priority": 3,
          "Position": "S"
        }
      }
    },
    "Application": {
      "Enabled": true,
      "CheckInterval": 3000,
      "Rules": [
        {
          "Contains": "Visual Studio",
          "Mood": "MOOD_DEFAULT",
          "Position": "N",
          "Priority": 6
        },
        {
          "Contains": "VS Code",
          "Mood": "MOOD_DEFAULT",
          "Position": "N",
          "Priority": 6
        },
        {
          "Contains": "Spotify",
          "Mood": "MOOD_HAPPY",
          "Priority": 7
        },
        {
          "Contains": "YouTube",
          "Mood": "MOOD_HAPPY",
          "Priority": 7
        }
      ]
    }
  }
}
```

---

## Phase 5: Installation & Deployment

### Service Installation

**Install as Windows Service:**
```bash
# Publish the service
dotnet publish -c Release -r win-x64 --self-contained

# Install using sc.exe
sc create "MochiCompanion" binPath="C:\Path\To\MochiCompanion.Service.exe"
sc description "MochiCompanion" "Desktop companion service for Mochi device"
sc config "MochiCompanion" start=auto

# Start the service
sc start "MochiCompanion"

# Check status
sc query "MochiCompanion"
```

**Uninstall:**
```bash
sc stop "MochiCompanion"
sc delete "MochiCompanion"
```

### PowerShell Installation Script

**install-service.ps1:**
```powershell
param(
    [string]$ServicePath = ".\publish\MochiCompanion.Service.exe",
    [string]$ServiceName = "MochiCompanion"
)

# Stop and remove existing service
$service = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping existing service..."
    Stop-Service -Name $ServiceName
    sc.exe delete $ServiceName
    Start-Sleep -Seconds 2
}

# Install new service
Write-Host "Installing service..."
sc.exe create $ServiceName binPath=$ServicePath
sc.exe description $ServiceName "Desktop companion service for Mochi device"
sc.exe config $ServiceName start=auto

# Start service
Write-Host "Starting service..."
Start-Service -Name $ServiceName

# Check status
Get-Service -Name $ServiceName
Write-Host "Service installed and started successfully!"
```

---

## Development Timeline

### ✅ COMPLETED: .NET 10 Service Implementation
- [x] Created Clean Architecture solution structure
- [x] Implemented Domain layer (Entities, Enums, Value Objects)
- [x] Implemented Application layer (Interfaces, Services, DTOs)
- [x] Implemented Infrastructure layer (Communication + Monitors)
- [x] Implemented SerialConnection (USB)
- [x] Implemented HttpConnection (WiFi)
- [x] Implemented TimeMonitor
- [x] Implemented AudioMonitor (NAudio)
- [x] Implemented SystemMonitor (CPU)
- [x] Implemented ApplicationMonitor (Active Window)
- [x] Implemented MoodService with priority handling
- [x] Implemented MonitoringService
- [x] Implemented MochiWorker (BackgroundService)
- [x] Added Serilog logging to console and file
- [x] Added configuration support (appsettings.json)

### 🔄 IN PROGRESS: Testing & Arduino
- [ ] Create Unit Tests with mocks
- [ ] Create Integration Tests
- [ ] Update Arduino with dual communication (USB + WiFi)
- [ ] Test end-to-end communication

### 📦 TODO: Polish & Deploy
- [ ] Error handling & reconnection logic
- [ ] Service installation scripts
- [ ] User documentation
- [ ] Testing on fresh Windows install

---

## Testing Checklist

### Arduino Testing
- [ ] Commands parsed correctly
- [ ] Priority system works (lower priority ignored)
- [ ] Mood expiry works (timed moods revert)
- [ ] Autonomous behaviors stay active
- [ ] Invalid commands handled gracefully

### Service Testing
- [ ] Connects to Mochi on startup
- [ ] Reconnects if USB unplugged/replugged
- [ ] All monitors run in parallel
- [ ] Priority system works end-to-end
- [ ] Configuration hot-reload works
- [ ] Service survives PC restart
- [ ] Logs are useful for debugging

### Integration Testing
- [ ] Music playing triggers happy mood
- [ ] High CPU triggers tired mood
- [ ] Time-based moods change throughout day
- [ ] Active application affects mood
- [ ] Multiple events handled (priority wins)
- [ ] Events expire correctly

---

## Troubleshooting

### Arduino Issues

**"Commands not recognized"**
- Check Serial Monitor is set to "Newline" or "Both NL & CR"
- Verify baud rate is 115200
- Check command format (uppercase, colon-separated)

**"Mood doesn't change"**
- Check priority level is high enough
- Verify current mood priority in debug output
- Try RESET command first

**"Autonomous behaviors stopped"**
- Verify `roboEyes.update()` is still in `loop()`
- Check IDLE and BLINK weren't disabled
- Try RESET command

### .NET Service Issues

**"Cannot connect to COM port"**
- Check COM port number in Device Manager
- Close Arduino Serial Monitor (can't have two connections)
- Verify user has permissions to access COM ports

**"Service crashes on startup"**
- Check logs in Event Viewer (Windows → Application)
- Verify .NET 10 runtime is installed
- Run service in console mode first: `dotnet run`

**"Moods don't change"**
- Check serial connection is established
- Verify monitors are running (check logs)
- Test manual command via serial connection
- Check priority levels in configuration

---

## Future Enhancements

### Phase 6: Advanced Features
- [ ] Web dashboard for monitoring/control
- [ ] Custom event triggers (HTTP API)
- [ ] Notification system integration
- [ ] Bi-directional communication (buttons on Mochi)
- [ ] Machine learning for behavior prediction
- [ ] Multi-Mochi support (multiple devices)

### Phase 7: Community Features
- [ ] Plugin system for custom monitors
- [ ] Shared mood presets
- [ ] Integration with Discord/Slack
- [ ] Home automation integration (Home Assistant)

---

## Resources

### Documentation
- [.NET Windows Service Docs](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [System.IO.Ports Reference](https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport)
- [NAudio Documentation](https://github.com/naudio/NAudio)
- [Arduino Serial Reference](https://www.arduino.cc/reference/en/language/functions/communication/serial/)

### Tools
- [Arduino IDE](https://www.arduino.cc/en/software)
- [Visual Studio 2022](https://visualstudio.microsoft.com/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Serial Monitor Tools](https://freeware.the-meiers.org/) (CoolTerm, PuTTY, etc.)

---

## Appendix A: Full Command Reference

### Arduino Commands

| Command | Format | Example | Description |
|---------|--------|---------|-------------|
| MOOD | `MOOD:<mood>:<pri>[:duration]` | `MOOD:MOOD_HAPPY:8` | Set mood with priority |
| POS | `POS:<position>:<pri>` | `POS:E:5` | Set eye position |
| ANIM | `ANIM:<animation>` | `ANIM:LAUGH` | Play animation |
| IDLE | `IDLE:<on\|off>` | `IDLE:OFF` | Enable/disable idle mode |
| BLINK | `BLINK:<on\|off>` | `BLINK:ON` | Enable/disable auto-blink |
| RESET | `RESET` | `RESET` | Reset to baseline |

### Mood Types
- `MOOD_DEFAULT` - Neutral expression
- `MOOD_HAPPY` - Happy, squinted eyes
- `MOOD_TIRED` - Sleepy, droopy eyes
- `MOOD_ANGRY` - Intense, angry brows

### Position Types
- `N` - North (up)
- `NE` - Northeast
- `E` - East (right)
- `SE` - Southeast
- `S` - South (down)
- `SW` - Southwest
- `W` - West (left)
- `NW` - Northwest
- `0` or omit - Center

### Animation Types
- `BLINK` - Single blink
- `CONFUSED` - Left-right shake
- `LAUGH` - Up-down bounce

---

## Appendix B: Priority Level Guidelines

| Priority | Layer | Use Case | Examples |
|----------|-------|----------|----------|
| 0 | Baseline | Always active | Auto-blink, idle movement |
| 1-3 | Time-based | Background context | Time of day, calendar |
| 4-6 | System state | PC activity | CPU usage, active app |
| 7-10 | Events | Immediate reactions | Music, notifications |

**Priority Rules:**
- Lower numbers = lower priority
- Higher priority always overrides lower
- Same priority: most recent wins
- Priority 0 (baseline) never gets overridden, always runs

---

**End of Document**

*This document will be updated as the project progresses. All phases, code samples, and configurations are subject to refinement based on real-world testing.*
