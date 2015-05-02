#include <Wire.h>
#include <Arduino.h>

#define ADDRESS         0x21 //Define i2c address of OV7670
#define REGISTERS       0xC9 //Define total numbers of registers on OV7076

#define XCLK_FREQ 10 * 1000000

//3v3					3v3
//gnd                   gnd
//reset                 3v3
//pwdn                  gnd
//SIOD					SDA1 + 1k pullup
//SIOC					SCL1 + 1k pullup
#define VSYNC_PIN		10
#define HREF_PIN 		9
#define PCLK_PIN		8
#define XCLK_PIN		7
#define D0_PIN			25
#define D1_PIN			26
#define D2_PIN			27
#define D3_PIN			28
#define D4_PIN			14
#define D5_PIN			15
#define D6_PIN			29
#define D7_PIN			11

#define WIDTH           640
#define HEIGHT          480

volatile boolean onFrame = false;
volatile boolean onPixel = false;

void wrReg(byte reg, byte dat)
{
	delay(5);
	Wire1.beginTransmission(ADDRESS);  //Start communication
	Wire1.write(reg);				   //Set the register
	Wire1.write(dat);				   //Set the value
	Wire1.endTransmission();		   //Send data and close communication
}

byte rdReg(byte reg)
{
	delay(1);
	Wire1.beginTransmission(ADDRESS);	//Start communication
	Wire1.write(reg);					//Set the register to read
	Wire1.endTransmission();			//Send data and close communication
	Wire1.requestFrom(ADDRESS, 1);		//Set the channel to read
	while (Wire1.available() < 1);		//Wait for all data to arrive
	return Wire1.read();                //Read the data and return them
}

void printRegister(byte reg, int mode)
{
	char tmpStr[80];
	byte highByte = rdReg(reg); //Read the byte as an integer

	if (mode == 0)
	{
		Serial.print(F("0x")); if (reg < 0x10) Serial.print(0, HEX); Serial.print(reg, HEX);
		Serial.print(F(" : "));
		Serial.print(F("0x")); if (highByte < 0x10) Serial.print(0, HEX); Serial.print(highByte, HEX);
	}
	if (mode == 1)
	{
		Serial.print("Register ");
		sprintf(tmpStr, "%03d", reg); Serial.print(tmpStr);
		Serial.print(" ");
		itoa(reg, tmpStr, 16); sprintf(tmpStr, "0x%02d", atoi(tmpStr)); Serial.print(tmpStr);
		Serial.print(" ");
		itoa(reg, tmpStr, 2); sprintf(tmpStr, "0b%08d", atoi(tmpStr)); Serial.print(tmpStr);
		Serial.print(": ");
		sprintf(tmpStr, "%03d", highByte); Serial.print(tmpStr);
		Serial.print(" ");
		itoa(highByte, tmpStr, 16); sprintf(tmpStr, "0x%02d", atoi(tmpStr)); Serial.print(tmpStr);
		Serial.print(" ");
		itoa(highByte, tmpStr, 2); sprintf(tmpStr, "0b%08d", atoi(tmpStr)); Serial.print(tmpStr);
	}
	Serial.print("\r\n");
}

void printAllRegisters(int mode)
{
	for (byte reg = 0x00; reg <= REGISTERS; reg++)
	{
		printRegister(reg, mode);
	}
}

void setupXCLK()
{
	pmc_enable_periph_clk(PWM_INTERFACE_ID);
	PWMC_ConfigureClocks(XCLK_FREQ * 2, 0, VARIANT_MCK); //freq * period
	PIO_Configure(
		g_APinDescription[XCLK_PIN].pPort,
		g_APinDescription[XCLK_PIN].ulPinType,
		g_APinDescription[XCLK_PIN].ulPin,
		g_APinDescription[XCLK_PIN].ulPinConfiguration);
	uint32_t channel = g_APinDescription[XCLK_PIN].ulPWMChannel;
	PWMC_ConfigureChannel(PWM_INTERFACE, channel, PWM_CMR_CPRE_CLKA, 0, 0);
	PWMC_SetPeriod(PWM_INTERFACE, channel, 2);
	PWMC_SetDutyCycle(PWM_INTERFACE, channel, 1);
	PWMC_EnableChannel(PWM_INTERFACE, channel);
	//pmc_mck_set_prescaler(2);
}

void vsync_rising() //frame start
{
	onFrame = true;
}

void pclk_rising() //pixel start
{
	onPixel = true;
}

void setup()
{
	delay(5000);
	Serial.begin(250000); //can't go any faster otherwise DUE would output garbage

	setupXCLK();
	Wire1.begin();
	//Wire1.setClock(400000); //should work but needs some delay after every read/write. buggy?

	//some registers to debug and test
	wrReg(0x12, 0x80); //Reset all the values

	wrReg(0x12, 0x02); //ColorBar
	wrReg(0x42, 0x08); //ColorBar

	//wrReg(0x15, 0x02); //VSYNC inverted
	//wrReg(0x11, 0x82); //Prescaler x3 (10 fps)
	wrReg(0x11, 60); //slow divider because of slow serial limit

	//code to read registers and check if they were written ok
	/*printRegister(0x01, 1);
	printRegister(0x12, 1);
	printRegister(0x15, 1);
	printRegister(0x11, 1);
	Serial.print(F("\n"));
	printAllRegisters(1);*/

	pinMode(D0_PIN, INPUT);
	pinMode(D1_PIN, INPUT);
	pinMode(D2_PIN, INPUT);
	pinMode(D3_PIN, INPUT);
	pinMode(D4_PIN, INPUT);
	pinMode(D5_PIN, INPUT);
	pinMode(D6_PIN, INPUT);
	pinMode(D7_PIN, INPUT);

	attachInterrupt(VSYNC_PIN, vsync_rising, RISING);
	//attachInterrupt(PCLK_PIN, pclk_rising, RISING); // code hang here. Occur too fast?
}

bool singleFrame = true;

void loop()
{
	//We take only one frame for testing purposes.
	//You're free to comment next line to take multiple frames.
	if (!singleFrame) return; singleFrame = false;

	onFrame = false;
	Serial.print(F("*FRAME_START*"));
	while (!onFrame);
	for (int i = 0; i < WIDTH * HEIGHT; i++)
	{
		//pin8 is port C22 as stated here -> http://www.arduino.cc/en/Hacking/PinMappingSAM3X
		while (REG_PIOC_PDSR & (1 << 22)); //wait for low
		Serial.write(REG_PIOD_PDSR & 0xFF);
		while (!(REG_PIOC_PDSR & (1 << 22))); //wait for high
	}
	Serial.print(F("*FRAME_STOP*"));
}