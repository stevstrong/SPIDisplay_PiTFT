#include "pch.h"
#include <gpio.h>
#include "PiTFT.h"

using namespace PiTFT;
using namespace Platform;
using namespace Windows::Foundation;
using namespace Windows::Devices::Enumeration;
using namespace Windows::Devices::Gpio;
using namespace Windows::Devices::Spi;
using namespace Windows::System::Threading;
using namespace concurrency;

Display::Display() // char* sclk)
{
//     _sclk = sclk;
}

void Display::writecommand(uint8_t c)
{
    // Debug.Writeln( "Data 0x" + c );
    dc_->Write(GpioPinValue::Low);
    _spiDisplay->Write( c );
}

void Display::writedata(uint8_t d)
 {
    // Debug.Writeln( "Data 0x" + c );
    dc_->Write(GpioPinValue::High);
    _spiDisplay->Write(d);
}

void Display::begin()
{
    auto gpio = GpioController::GetDefault();
    
    if (gpio == nullptr)
    {
        dc_ = nullptr;
        return;
    }
    // Initialize a pin as output for the Data/Command line on the display
    dc_ = gpio->OpenPin(DC_PIN);
    dc_->Write(GpioPinValue::High);
    dc_->SetDriveMode(GpioPinDriveMode::Output);

    SpiConnectionSettings ^settings = ref new SpiConnectionSettings(CS_PIN);
    String^ querySyntax = SpiDevice::GetDeviceSelector("SPI0");
    auto asyncop = DeviceInformation::FindAllAsync(querySyntax);
    while (asyncop->Status != AsyncStatus::Completed)
    {
        if (asyncop->Status == AsyncStatus::Error)
        {
            // Debug.WriteLine ( "Could not find information for device '%S': %d", name, asyncop->E
            return;
        }
        Sleep(50);
    }

    auto info = asyncop->GetResults();
    if (info == nullptr || info->Size == 0)
    {
        return;
    }

    String^ id = info->GetAt(0)->Id;

    settings->ClockFrequency = 30000000;
    settings->Mode = SpiMode::Mode0;

    auto spideviceop = SpiDevice::FromIdAsync(id, settings);
    while (spideviceop->Status != AsyncStatus::Completed)
    {
        if (spideviceop->Status == AsyncStatus::Error)
        {
            // PyErr_Format(PyExc_RuntimeError, "Could get SPI device: %d", spideviceop->ErrorCode);
            return;
        }
        Sleep(50);
    }
    
    auto spidevice = spideviceop->GetResults();
    if (spidevice == nullptr)
    {
        return;
    }
    _spiDisplay = spidevice;


    writecommand(HX8357_SWRESET);

    // setextc
    writecommand(HX8357D_SETC);
    writedata(0xFF);
    writedata(0x83);
    writedata(0x57);
    
    Sleep( 300 );

    // setRGB which also enables SDO
    writecommand(HX8357_SETRGB);
    writedata(0x80);  //enable SDO pin!
                        //    writedata(0x00);  //disable SDO pin!
    writedata(0x0);
    writedata(0x06);
    writedata(0x06);

    writecommand(HX8357D_SETCOM);
    writedata(0x25);  // -1.52V

    writecommand(HX8357_SETOSC);
    writedata(0x68);  // Normal mode 70Hz, Idle mode 55 Hz

    writecommand(HX8357_SETPANEL); //Set Panel
    writedata(0x05);  // BGR, Gate direction swapped

    writecommand(HX8357_SETPWR1);
    writedata(0x00);  // Not deep standby
    writedata(0x15);  //BT
    writedata(0x1C);  //VSPR
    writedata(0x1C);  //VSNR
    writedata(0x83);  //AP
    writedata(0xAA);  //FS

    writecommand(HX8357D_SETSTBA);
    writedata(0x50);  //OPON normal
    writedata(0x50);  //OPON idle
    writedata(0x01);  //STBA
    writedata(0x3C);  //STBA
    writedata(0x1E);  //STBA
    writedata(0x08);  //GEN

    writecommand(HX8357D_SETCYC);
    writedata(0x02);  //NW 0x02
    writedata(0x40);  //RTN
    writedata(0x00);  //DIV
    writedata(0x2A);  //DUM
    writedata(0x2A);  //DUM
    writedata(0x0D);  //GDON
    writedata(0x78);  //GDOFF

    writecommand(HX8357D_SETGAMMA);
    writedata(0x02);
    writedata(0x0A);
    writedata(0x11);
    writedata(0x1d);
    writedata(0x23);
    writedata(0x35);
    writedata(0x41);
    writedata(0x4b);
    writedata(0x4b);
    writedata(0x42);
    writedata(0x3A);
    writedata(0x27);
    writedata(0x1B);
    writedata(0x08);
    writedata(0x09);
    writedata(0x03);
    writedata(0x02);
    writedata(0x0A);
    writedata(0x11);
    writedata(0x1d);
    writedata(0x23);
    writedata(0x35);
    writedata(0x41);
    writedata(0x4b);
    writedata(0x4b);
    writedata(0x42);
    writedata(0x3A);
    writedata(0x27);
    writedata(0x1B);
    writedata(0x08);
    writedata(0x09);
    writedata(0x03);
    writedata(0x00);
    writedata(0x01);

    writecommand(HX8357_COLMOD);
    writedata(0x55);  // 16 bit

    writecommand(HX8357_MADCTL);
    writedata(0xC0);

    writecommand(HX8357_TEON);  // TE off
    writedata(0x00);

    writecommand(HX8357_TEARLINE);  // tear line
    writedata(0x00);
    writedata(0x02);

    writecommand(HX8357_SLPOUT); //Exit Sleep
    Sleep( 150 );

    writecommand(HX8357_DISPON);  // display on
    Sleep( 50 );
}

void Display::setAddrWindow(uint16_t x0, uint16_t y0, uint16_t x1, uint16_t y1)
{
    writecommand(HX8357_CASET); // Column addr set
    writedata(x0 >> 8);
    writedata(x0 & 0xFF);     // XSTART 
    writedata(x1 >> 8);
    writedata(x1 & 0xFF);     // XEND

    writecommand(HX8357_PASET); // Row addr set
    writedata(y0 >> 8);
    writedata(y0);     // YSTART
    writedata(y1 >> 8);
    writedata(y1);     // YEND

    writecommand(HX8357_RAMWR); // write to RAM
}

void Display::drawPixel(int16_t x, int16_t y, uint16_t color) {

    if ((x < 0) || (x >= HX8357_TFTWIDTH) || (y < 0) || (y >= HX8357_TFTHEIGHT)) return;

    setAddrWindow(x, y, x + 1, y + 1);

    //digitalWrite(_dc, HIGH);
    //digitalWrite(_cs, LOW);

    writedata(color >> 8);
    writedata(color);

    //digitalWrite(_cs, HIGH);
}

void Display::fillScreen(uint16_t color) {
    fillRect(0, 0, HX8357_TFTWIDTH, HX8357_TFTHEIGHT, color);
}

// fill a rectangle
void Display::fillRect(int16_t x, int16_t y, int16_t w, int16_t h,
    uint16_t color) {

    // rudimentary clipping (drawChar w/big text requires this)
    if ((x >= HX8357_TFTWIDTH) || (y >= HX8357_TFTHEIGHT)) return;
    if ((x + w - 1) >= HX8357_TFTWIDTH)  w = HX8357_TFTWIDTH - x;
    if ((y + h - 1) >= HX8357_TFTHEIGHT) h = HX8357_TFTHEIGHT - y;

    setAddrWindow(x, y, x + w - 1, y + h - 1);

    uint8_t hi = color >> 8, lo = color;

    //digitalWrite(_dc, HIGH);
    //digitalWrite(_cs, LOW);

    for (y = h; y>0; y--)
    {
        for (x = w; x>0; x--)
        {
            writedata(hi);
            writedata(lo);
        }
    }
    //digitalWrite(_cs, HIGH);
}

/*
void Display::pushColor(uint16_t color) {
    //digitalWrite(_dc, HIGH);
    // *dcport |= dcpinmask;
    //digitalWrite(_cs, LOW);
    // *csport &= ~cspinmask;

    spiwrite(color >> 8);
    spiwrite(color);

    // *csport |= cspinmask;
    //digitalWrite(_cs, HIGH);
}

void Display::drawFastVLine(int16_t x, int16_t y, int16_t h,
    uint16_t color) {

    // Rudimentary clipping
    if ((x >= _width) || (y >= _height)) return;

    if ((y + h - 1) >= _height)
        h = _height - y;

    setAddrWindow(x, y, x, y + h - 1);

    uint8_t hi = color >> 8, lo = color;

    //digitalWrite(_dc, HIGH);
    //digitalWrite(_cs, LOW);

    while (h--) {
        spiwrite(hi);
        spiwrite(lo);
    }
    //digitalWrite(_cs, HIGH);
}

void Display::drawFastHLine(int16_t x, int16_t y, int16_t w,
    uint16_t color) {

    // Rudimentary clipping
    if ((x >= _width) || (y >= _height)) return;
    if ((x + w - 1) >= _width)  w = _width - x;
    setAddrWindow(x, y, x + w - 1, y);

    uint8_t hi = color >> 8, lo = color;
    //digitalWrite(_dc, HIGH);
    //digitalWrite(_cs, LOW);
    while (w--) {
        spiwrite(hi);
        spiwrite(lo);
    }
    //digitalWrite(_cs, HIGH);
}

// Pass 8-bit (each) R,G,B, get back 16-bit packed color
uint16_t Display::color565(uint8_t r, uint8_t g, uint8_t b) {
    return ((r & 0xF8) << 8) | ((g & 0xFC) << 3) | (b >> 3);
}


#define MADCTL_MY  0x80
#define MADCTL_MX  0x40
#define MADCTL_MV  0x20
#define MADCTL_ML  0x10
#define MADCTL_RGB 0x00
#define MADCTL_BGR 0x08
#define MADCTL_MH  0x04

void Display::setRotation(uint8_t m) {

    writecommand(HX8357_MADCTL);
    rotation = m % 4; // can't be higher than 3
    switch (rotation) {
    case 0:
        writedata(MADCTL_MX | MADCTL_MY | MADCTL_RGB);
        _width = HX8357_TFTWIDTH;
        _height = HX8357_TFTHEIGHT;
        break;
    case 1:
        writedata(MADCTL_MV | MADCTL_MY | MADCTL_RGB);
        _width = HX8357_TFTHEIGHT;
        _height = HX8357_TFTWIDTH;
        break;
    case 2:
        writedata(MADCTL_RGB);
        _width = HX8357_TFTWIDTH;
        _height = HX8357_TFTHEIGHT;
        break;
    case 3:
        writedata(MADCTL_MX | MADCTL_MV | MADCTL_RGB);
        _width = HX8357_TFTHEIGHT;
        _height = HX8357_TFTWIDTH;
        break;
    }
}


void Display::invertDisplay(boolean i) {
    writecommand(i ? HX8357_INVON : HX8357_INVOFF);
}
*/