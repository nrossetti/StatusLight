#include <Adafruit_NeoPixel.h>
#define PIN            4
#define NUMPIXELS      1

bool newData = true;
int color[] = {255, 255, 255};
char inData[64];
char inChar=-1;

char *token, *str, *tofree;

Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, PIN, NEO_GRB + NEO_KHZ800);

void setup()
{
  Serial.begin(9600);
  pixels.begin(); // This initializes the NeoPixel library.
  setColor(color);
  getData();
  procData();
  setColor(color);
}

void loop()
{
  getData();
  procData();
  setColor(color);
}


char getData()
{
  byte numBytesAvailable= Serial.available();
    // if there is something to read
    if (numBytesAvailable > 0){
        // store everything into "inData"
        int i;
        for (i=0;i<numBytesAvailable;i++){
            inChar= Serial.read();
            inData[i] = inChar;
        }

        inData[i] = '\0';

        Serial.print("Arduino Received: ");
        Serial.print("RGB(");
        Serial.print(inData);
        Serial.println(")");
  }
}

void procData()
{
  int i=0;
  tofree = str = strdup(inData);  // We own str's memory now.
  while (token = strsep(&str, ","))
  {
    color[i]=atoi(token);
    //Serial.println(color[i]);
    i++;
  }
  free(tofree);
  memset(inData, 0, sizeof(inData));
}

void setColor(int color[])
{
  pixels.setPixelColor(0, pixels.Color(color[1], color[0], color[2])); // Moderately brightn white color.
  pixels.show(); // This sends the updated pixel color to the hardware. 
}

