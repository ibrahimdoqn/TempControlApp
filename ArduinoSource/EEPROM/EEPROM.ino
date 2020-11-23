#include <EEPROM.h>
void EEPROMWritelong(int address, long value) {
  byte four = (value & 0xFF);
  byte three = ((value >> 8) & 0xFF);
  byte two = ((value >> 16) & 0xFF);
  byte one = ((value >> 24) & 0xFF);
  EEPROM.write(address, four);
  EEPROM.write(address + 1, three);
  EEPROM.write(address + 2, two);
  EEPROM.write(address + 3, one);
}

void setup() {
  EEPROMWritelong(0,1);
  EEPROM.write(4, 15);
  EEPROM.write(5, 60);
  EEPROM.write(6, 65);
  EEPROM.write(7, 65);
  EEPROM.write(8, 88);
  EEPROM.write(9, 88);
}

void loop() {
  // put your main code here, to run repeatedly:

}
