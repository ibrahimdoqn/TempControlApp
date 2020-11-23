#include <EEPROM.h>
//Setup App
byte millisTimer;//Sıcaklık listesinin uzunluğu(Kontrol edilen).
byte switchTemp;//12V'a geçiş sıcaklığı
byte minTempCpu;
byte minTempGpu;
byte maxTempCpu;
byte maxTempGpu;
//Relay Pins
byte Pump=5;//Relay pin
byte Relay=6;//Relay pin
//SerialPortData
byte index = 0;//Serial port data index'i
byte buf[8];//Serial port data buffer
//Timers
byte Time = 0;//Timer
unsigned long highTime1;
unsigned long lowTime1;
unsigned long highTime2;
unsigned long lowTime2;
//Relay State
bool pumpSwitchMode = false;//Relay'nin açık veya kapalı olduğunu hafıza'da tutar
bool RelaySwitchMode = true;//Relay'nin açık veya kapalı olduğunu hafıza'da tutar
long RelayCounts = 0;//Relay geçiş sayısı
//BoardConfig
bool Config = false;//Yapılandırma kontrolü
bool turboMode = false;

void setupTimer(){
    cli();
    TCCR1A = 0;
    TCCR1B = 0;
    TCNT1  = 0;
    OCR1A = 15624;// 1 Second
    TCCR1B |= (1 << WGM12);
    TCCR1B |= (1 << CS12) | (1 << CS10);  
    TIMSK1 |= (1 << OCIE1A);
    sei();
}

long EEPROMReadlong(long address) {
  long four = EEPROM.read(address);
  long three = EEPROM.read(address + 1);
  long two = EEPROM.read(address + 2);
  long one = EEPROM.read(address + 3);
  return ((four << 0) & 0xFF) + ((three << 8) & 0xFFFF) + ((two << 16) & 0xFFFFFF) + ((one << 24) & 0xFFFFFFFF);
}

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

void boardWrite(){
  millisTimer = buf[0];
  switchTemp = buf[1];
  minTempCpu = buf[2];
  minTempGpu = buf[3];
  maxTempCpu = buf[4];
  maxTempGpu = buf[5];
  EEPROM.write(4, millisTimer);
  EEPROM.write(5, switchTemp);
  EEPROM.write(6, minTempCpu);
  EEPROM.write(7, minTempGpu);
  EEPROM.write(8, maxTempCpu);
  EEPROM.write(9, maxTempGpu);
}

void boardRead(){
  RelayCounts = EEPROMReadlong(0);
  millisTimer = EEPROM.read(4);
  switchTemp = EEPROM.read(5);
  minTempCpu = EEPROM.read(6);
  minTempGpu = EEPROM.read(7);
  maxTempCpu = EEPROM.read(8);
  maxTempGpu = EEPROM.read(9);
}

void RomWrite(){
  RelayCounts++;
  Serial.write((byte)201);
  Serial.println(RelayCounts);
  EEPROMWritelong(0, RelayCounts);
}

void pumpSwitch(bool Switch){
  if( Switch ){//Relayyi 12V konumuna ayarla
      highTime1 = millis();
      if( ((highTime1 - lowTime1) > 3000) && !pumpSwitchMode ){
        digitalWrite(Pump, LOW);
        pumpSwitchMode = true;
      }
    }
    else{//Relayyi 5V konumuna ayarla
      lowTime1 = millis();
      if( ((lowTime1 - highTime1) > (millisTimer * 1000)) && pumpSwitchMode ){
        digitalWrite(Pump, HIGH);
        pumpSwitchMode = false;
        }
    }
}

void RelayCountSwitch(bool Switch){
  if( Switch ){//Relayyi 12V konumuna ayarla
      highTime2 = millis();
      if( ((highTime2 - lowTime2) > 3000) && !RelaySwitchMode ){
        digitalWrite(Relay, LOW);
        RelaySwitchMode = true;
        RomWrite();
      }
    }
    else{//Relayyi 5V konumuna ayarla
      lowTime2 = millis();
      if( ((lowTime2 - highTime2) > (millisTimer * 1000)) && RelaySwitchMode ){
        digitalWrite(Relay, HIGH);
        RelaySwitchMode = false;
        RomWrite();
        }
    }
}

void setup() 
{ 
  boardRead();
  Serial.begin(19200, SERIAL_8O1); 
  setupTimer();
  pinMode(Pump, OUTPUT);
  pinMode(Relay, OUTPUT);
  digitalWrite(Pump, HIGH);
  digitalWrite(Relay, LOW);
} 

ISR(TIMER1_COMPA_vect){
  if(Time < 255){
    if(Time > 5 && Config){
      controlOff();//Eğer seriporttan 2 sn gecikme olursa değerleri sıfırla
    }
    Time++;
  }
}

void TempControl(){
  //Variables
    byte CalculatedTemp = 0;
  //Calculate
    for (byte x = 0; x < 2; x++){
      if(CalculatedTemp < buf[x]) CalculatedTemp = buf[x]; //Buffer'dan yüksek sıcaklığı bul
    }
    Serial.write((byte)208);
    Serial.write(CalculatedTemp);
    
  //Fan Control
    if( CalculatedTemp > switchTemp || turboMode){//Relayyi 12V konumuna ayarla
      RelayCountSwitch(true);
    }
    else{//Relayyi 5V konumuna ayarla
      RelayCountSwitch(false);
    }
    //Pump Control
    if( CalculatedTemp > 70 || turboMode){//Relayyi 12V konumuna ayarla
      pumpSwitch(true);
    }
    else{//Relayyi 5V konumuna ayarla
      pumpSwitch(false);
    }
}

void controlOff(){//Değerleri sıfırlar
  Config = false;
  digitalWrite(Pump, HIGH);
  digitalWrite(Relay, LOW);
  RomWrite();
  index = 0;
}

//BoardConfig
void boardConfig(){//C# konfigürasyonunu ayarlar ve devir kontrolüne hazırlar.
  //Setup Board
  index = 0;
  highTime1 = millis();
  lowTime1 = millis();
  highTime2 = millis();
  lowTime2 = millis();
  //Setup Desktop App
  Serial.write((byte)207);
  Serial.write(millisTimer);
  Serial.write(switchTemp);
  Serial.write(minTempCpu);
  Serial.write(minTempGpu);
  Serial.write(maxTempCpu);
  Serial.write(maxTempGpu);
  if(turboMode){
    Serial.write((byte)205);
  }
  else{
    Serial.write((byte)206);
  }
  Serial.write((byte)201);
  Serial.println(RelayCounts);
  Serial.write((byte)202);
}

//Seriport değerler
void serialPort(){
  byte b = Serial.read();
  if(b == 255){//Bir paket gelmeye başladı
    index = 0;
    }
  else if(b == 254){//Sıcaklık bilgisi geldi
    if(Config){
      TempControl();
    }
    else{
      Serial.write((byte)200);
    }
    }
  else if( b == 253){
    boardConfig();//C# read board config
  }
  else if( b == 252){
    Serial.write((byte)252);//Serialporttan gelen mesajı aldığını iletir ve bağlantıyı kurar.
  }
  else if( b == 251){
    turboMode = true;
  }
  else if( b == 250){
    turboMode = false;
  }
  else if( b == 249){
    boardWrite();
  }
  else if( b == 248){
    Config = true;
  }
  else{//Paket gelmeye devam ediyor
    if(index < 8)buf[index] = b;
    index++;
    }
}

void serialEvent() {//Seriport Kesmesi
  while (Serial.available()) {
    serialPort();
  }
  Time = 0;//Timer değerini sıfırla
}

void loop()
{ 
  
}
