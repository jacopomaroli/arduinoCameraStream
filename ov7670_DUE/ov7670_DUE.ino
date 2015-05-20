#include <Wire.h>
#include "ov7670.h"

//#define ADDRESS         0x21 //Define i2c address of OV7670
//#define REGISTERS       0xC9 //Define total numbers of registers on OV7076

#define XCLK_FREQ 10 * 1000000 //10.5 Mhz slow but ok.
//#define XCLK_FREQ 20 * 1000000 //21 Mhz doubles the image; too fast?
//#define XCLK_FREQ 24 * 1000000 //42 Mhz

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

#define VSYNC_BIT       (REG_PIOC_PDSR & (1 << 29))
#define HREF_BIT        (REG_PIOC_PDSR & (1 << 21))
#define PCLK_BIT        (REG_PIOC_PDSR & (1 << 22)) //pin8 is port C22 as stated here -> http://www.arduino.cc/en/Hacking/PinMappingSAM3X

#define WIDTH           640
#define HEIGHT          480

volatile boolean onFrame = false;
volatile boolean onRow   = false;
volatile boolean onPixel = false;

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

void vsync_falling() //frame stop
{
	onFrame = false;
}

void href_rising() //row start
{
	onRow = true;
}

void href_falling() //row stop
{
	onRow = false;
}

void pclk_rising() //pixel start
{
	onPixel = true;
}

void pclk_falling() //pixel stop
{
	onPixel = false;
}

void setup()
{
	Serial.begin(250000);
	SerialUSB.begin(0);
	while (!Serial);
	while (!SerialUSB);

	Serial.print("here");
	SerialUSB.print("there");

	setupXCLK();
	Wire1.begin();
	//Wire1.setClock(400000); //should work but needs some delay after every read/write. buggy?

	//some registers to debug and test
	//wrReg(0x12, 0x80); //Reset all the values
	camInit();
	setRes(VGA);
	setColorSpace(BAYER_RGB);

	//wrReg(0x12, 0x02); //ColorBar semitransparent overlay of the image
	//wrReg(0x42, 0x08); //ColorBar (DSP color bars at COM17)

	//wrReg(0x15, 0x02); //VSYNC inverted
	//wrReg(0x11, 0x82); //Prescaler x3 (10 fps)
	wrReg(0x11, 60); //slow divider because of slow serial limit
	//wrReg(0x11, 1 << 6);

	//F(internal clock) = F(input clock) / (Bit[0 - 5] + 1)
	/*uint8_t tmpReg = 0;
	tmpReg |= 1 << 0;
	tmpReg |= 1 << 1;
	tmpReg |= 1 << 2;*/
	//tmpReg |= 1 << 3;
	//tmpReg |= 1 << 4;
	//tmpReg |= 1 << 5;
	//wrReg(0x11, tmpReg);
	//wrReg(0x11, 20);

	// default value gives 5.25MHz pixclock
	// wrReg(0x11, 20); //slow divider because of slow serial limit 125kHz
	// wrReg(0x11, 10); //slow divider because of slow serial limit 238kHz
	// wrReg(OV_CLKRC, 30); //slow divider because of slow serial limit 84kHz
	// wrReg(0x11, 60); //slow divider because of slow serial limit 43kHz

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
	//attachInterrupt(VSYNC_PIN, vsync_falling, FALLING); //not needed (?)
	attachInterrupt(HREF_PIN, href_rising, RISING);
	//attachInterrupt(HREF_PIN, href_falling, FALLING); // not working
	//attachInterrupt(PCLK_PIN, pclk_rising, RISING); // code hang here. Occur too fast?
	//attachInterrupt(PCLK_PIN, pclk_falling, FALLING); // code hang here. Occur too fast?

}

bool singleFrame = true;
uint8_t buf[WIDTH];

void loop()
{
	//We take only one frame for testing purposes.
	//You're free to comment next line to take multiple frames.
	//if (!singleFrame) return; singleFrame = false;

	SerialUSB.print(F("*FRAME_START*"));
	onFrame = false;
	while (!onFrame);
	for (int i = 0; i < HEIGHT; i++)
	{
		onRow = false;
		while (!onRow); //not working w/interrupt on href
		for (int j = 0; j < WIDTH; j++)
		{
			while (PCLK_BIT); //wait for low
			//while (!onPixel); // non working interrupt driven pclk
			buf[j] = REG_PIOD_PDSR & 0xFF;
			//while (onPixel); // non working interrupt driven pclk
			while (!PCLK_BIT); //wait for high
		}
		//while (onRow); //not working w/interrupt on href
		SerialUSB.write(buf, WIDTH);
	}
	SerialUSB.print(F("*FRAME_STOP*"));
}