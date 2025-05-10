#include <Arduino.h>
#include <Encoder.h>
#include <Bounce.h>

// Pin definitions - UPDATED
const int ENCODER1_PIN_A = 2;  // Encoder 1 (Left/Right) - Pin A
const int ENCODER1_PIN_B = 3;  // Encoder 1 (Left/Right) - Pin B
const int BUTTON_PIN = 6;      // Spacebar button

Encoder encoder1(ENCODER1_PIN_A, ENCODER1_PIN_B);
Bounce button = Bounce(BUTTON_PIN, 10);

// Variable to store encoder positions
long encoder1Position = 0; 
long lastEncoder1Position = 0; 

int encoder1Accum = 0; // Tick accumulation for joystick sensitivit                y

// Float joystick state and tuning
float joystickXFloat = 0.0; // -1.0 (left) to 1.0 (right)
const float incrementStep = 0.05;   // change per encoder1 tick
const float decayRate = 0.8;       // auto-centering multiplier
const float zeroThreshold = 0.01;   // snap-to-zero threshold

void setup() {
  pinMode(BUTTON_PIN, INPUT_PULLUP);
  Serial.begin(9600);
  Serial.println("Single Encoder (X-axis) Controller Ready:"); 

  // Initialize Joystick
  Joystick.useManualSend(true); 
  Joystick.X(512); // Center X
  Joystick.Y(512); // Center Y
  Joystick.send_now();
}

void loop() {
  button.update();
  bool joystickNeedsUpdate = false; 

  // --- Read encoder 1 and update float directly ---
  encoder1Position = encoder1.read();
  if (encoder1Position != lastEncoder1Position) {
    int delta = encoder1Position - lastEncoder1Position;
    lastEncoder1Position = encoder1Position;
    
    float effectiveIncrement = incrementStep; 
    joystickXFloat = constrain(
      joystickXFloat - delta * effectiveIncrement,  
      -1.0, 1.0
    );
    joystickNeedsUpdate = true;
    Serial.print("Enc1 delta="); Serial.print(delta);
    Serial.print(" -> float X="); Serial.println(joystickXFloat);
  }

  // --- Auto-centering decay for X-axis ---
  if (joystickXFloat != 0.0) {
    float oldX = joystickXFloat;
    joystickXFloat *= decayRate;
    if (abs(joystickXFloat) < zeroThreshold) joystickXFloat = 0.0;
    if (joystickXFloat != oldX) {
      joystickNeedsUpdate = true;
      Serial.print("Decay X, float = "); Serial.println(joystickXFloat);
    }
  }

  // --- Update Joystick HID output ---
  if (joystickNeedsUpdate) {
    int xVal = round(512.0 + joystickXFloat * 511.0);
    int yVal = 512;
    xVal = constrain(xVal, 0, 1023);
    // yVal is already fixed at 512, constrain not strictly necessary but harmless
    // yVal = constrain(yVal, 0, 1023); 

    Joystick.X(xVal);
    Joystick.Y(yVal);
    Joystick.send_now();

    Serial.print("Joystick HID Sent: X="); Serial.print(xVal);
    Serial.print(" (float "); Serial.print(joystickXFloat); Serial.print(")");
    Serial.print(", Y="); Serial.print(yVal); 
    Serial.println(" (fixed center)");
  }
  
  // --- Button handling (Space) ---
  if (button.fallingEdge()) {
    Keyboard.press(' ');
    Serial.println("SPACE pressed");
  }
  if (button.risingEdge()) {
    Keyboard.release(' ');
    Serial.println("SPACE released");
  }

  // Small delay to avoid flooding and allow processing
  delay(5);
}