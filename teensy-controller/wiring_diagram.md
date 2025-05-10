# Teensy Wiring Diagram

## Components
- 1x Teensy 4.0/4.1
- 2x Momentary Push Buttons
- 1x Rotary Encoder (with A and B outputs and push button)
- Jumper wires

## Connections

### Buttons
- Button 1:
  - One terminal → Teensy pin 2
  - Other terminal → GND
- Button 2:
  - One terminal → Teensy pin 3
  - Other terminal → GND

### Rotary Encoder
- Encoder A pin → Teensy pin 4
- Encoder B pin → Teensy pin 5
- Encoder Button pin → Teensy pin 6
- Encoder GND pin → Teensy GND
- Encoder VCC pin (if present) → Teensy 3.3V

## Notes
- The buttons are wired with internal pull-up resistors (enabled in software)
- No external resistors are needed for this configuration
- The rotary encoder should be a quadrature encoder compatible with the Encoder library
- Many rotary encoders include a push button built into the shaft
- Be careful not to apply more than 3.3V to any Teensy input pin
