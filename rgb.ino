#include <Adafruit_NeoPixel.h>
#define LEDPIN             4
#define NUMPIXELS       13
#define delayTime       1
#define minDelay        600

char inData[64];
char inChar=-1;
bool newData = true;
bool enableTap = true;
int val;
int tickCount = 0;
int color[] = {0, 75, 75, 1};
int manual_colors[3][3] = {{100,0,0},{0,100,0},{0,0,0}};
int manual_index = 0;

char *token, *str, *tofree;

Adafruit_NeoPixel pixels = Adafruit_NeoPixel(NUMPIXELS, LEDPIN, NEO_GRB + NEO_KHZ800);

void setup()
{
  Serial.begin(9600);
  pixels.begin(); // This initializes the NeoPixel library.
  for( int i = 1; i<(NUMPIXELS); i++)
  {
      pixels.setPixelColor(i, pixels.Color(color[1], color[0], color[2])); // Moderately brightn white color.
  }   
  getData();
  procData();
  setColor(color);
}

void loop()
{
getData();
val = analogRead(A3);
if(enableTap)
  {
  if(tickCount>minDelay)
    if(val>150)
    {
      for( int i = 1; i<(NUMPIXELS); i++)
      {
        pixels.setPixelColor(i, pixels.Color(manual_colors[manual_index][1], manual_colors[manual_index][0], manual_colors[manual_index][2])); // 
      } 
      pixels.show();
      if(manual_index>1)
        manual_index=0;
      else
        manual_index++;
      if(val>150)
        delay(minDelay);
      tickCount=0;  
    }
  tickCount++;
  }
}


void getData()
{
  byte numBytesAvailable= Serial.available();
    // if there is something to read
    if (numBytesAvailable > 0)
    {
        // store everything into "inData"
        int i;
        for (i=0;i<numBytesAvailable;i++)
        {
            inChar= Serial.read();
            if(inChar='m')
            {
              enableTap=(!enableTap);
              return;
            }
            inData[i] = inChar;
        }

        inData[i] = '\0';

        //Serial.print("Arduino Received: ");
        //Serial.println(inData);
        procData();
        delay(delayTime);
  }
}

void procData()
{
  int i=0;
  tofree = str = strdup(inData);  // We own str's memory now.
  while (token = strsep(&str, " "))
  {
    color[i]=atoi(token);
    //Serial.println(color[i]);
    i++;
  }
  free(tofree);
  memset(inData, 0, sizeof(inData));
  setColor(color);
}

void setColor(int color[])
{
    //for( int i = 1; i<(NUMPIXELS); i++){
      pixels.setPixelColor(color[3], pixels.Color(color[1], color[0], color[2])); // Moderately brightn white color.
    //}   
  pixels.show();
}

