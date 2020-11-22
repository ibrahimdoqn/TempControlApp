#include <EEPROM.h>
//Setup Array
byte listLenght = 30;//Sıcaklık listesinin uzunluğu(Kontrol edilen).
//Setup Temp
byte switchTemp = 60;//12V'a geçiş sıcaklığı
//Global
byte role=5;//Role pin
byte index;//Serial port data index'i
byte buf[4];//Serial port data buffer
byte tempList[255];//Sıcaklık listesi
byte tempIndex = 0;//Sıcaklık listesi için tutulan yazma index'i
byte Time = 0;//Timer
bool Config = false;//Yapılandırma kontrolü
long roleCounts = 0;//Role geçiş sayısı
bool roleSwitchMode = true;//Role'nin açık veya kapalı olduğunu hafıza'da tutar
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

void RomWrite(){
  roleCounts++;
  EEPROMWritelong(0, roleCounts);
  Serial.write((byte)201);
  Serial.println(roleCounts);
}

void roleCountswitch(bool Switch){
  if( Switch && !roleSwitchMode ){//Roleyi 12V konumuna ayarla
      digitalWrite(role, LOW);
      roleSwitchMode = true;
      Serial.write((byte)202);
      RomWrite();
    }
    else if(!Switch && roleSwitchMode){//Roleyi 5V konumuna ayarla
      digitalWrite(role, HIGH);
      roleSwitchMode = false;
      Serial.write((byte)203);
      RomWrite();
    }
}

void setup() 
{ 
  roleCounts = EEPROMReadlong(0);
  Serial.begin(19200); 
  setupTimer();
  pinMode(role, OUTPUT);
    for(byte x = 0; x < 255; x++){
      tempList[x] = 0;
    }
} 

ISR(TIMER1_COMPA_vect){
  if(Time < 255){
    if(Time > 2){
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
    tempList[tempIndex] = CalculatedTemp;//Sıcaklık listesine ekle
    tempIndex++;//Index'i artır
    if(tempIndex >= listLenght) tempIndex = 0;//Config'de belirlenen değere ulaşınca Index'i sıfırla
    for(byte x = 0; x < listLenght; x++){
      if(CalculatedTemp < tempList[x]) CalculatedTemp = tempList[x];//Listedeki en büyük değeri bul
    }
    Serial.write((byte)204);
    Serial.write(CalculatedTemp);
    
  //Control
    if( CalculatedTemp > switchTemp || turboMode){//Roleyi 12V konumuna ayarla
      roleCountswitch(true);
    }
    else{//Roleyi 5V konumuna ayarla
      roleCountswitch(false);
    }
}

void controlOff(){//Değerleri sıfırlar
  roleCountswitch(true);//Roleyi 12V'a ayarla
  for(byte x = 0; x < 255; x++){
      tempList[x] = 0;//Listeyi sıfırla
    }
  Config = false;
  tempIndex = 0;
  index = 0;
}

//BoardConfig
void boardConfig(){//C# konfigürasyonunu ayarlar ve devir kontrolüne hazırlar.
  tempIndex = 0;
  index = 0;
  listLenght = buf[0];
  switchTemp= buf[1];
  if(turboMode){
    Serial.write((byte)205);
  }
  else{
    Serial.write((byte)206);
  }
  Serial.write((byte)201);
  Serial.println(roleCounts);
  Serial.write((byte)202);
  Config = true;
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
    boardConfig();//Program yapılandırması geldiği iletir
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
  else{//Paket gelmeye devam ediyor
    if(index < 4)buf[index] = b;
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
  //Bu yazılım Timer ve Serial kesmeleri ile çalışmaktadır.
}
