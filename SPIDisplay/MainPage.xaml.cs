/*
    Copyright(c) Microsoft Open Technologies, Inc. All rights reserved.

    The MIT License(MIT)

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files(the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions :

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
    THE SOFTWARE.
*/

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using DisplayFont;
using Adafruit_HX8357;

namespace SPIDisplay
{

    public sealed partial class MainPage : Page
    {

        private Tft Tft = new Tft();

        public MainPage()
        {
            InitializeComponent();

            // Register for the unloaded event so we can clean up upon exit
            Unloaded += MainPage_Unloaded;

            InitAll();

            Task.Delay(1000);
            Tft.DrawColor(0xf800); //Red
            Tft.DrawColor(0x07e0); //Green
            //Tft.DrawColor(0x001f); //Blue
            //Tft.DrawColor(0x0000); //Black
            //Tft.DrawColor(0xffe0); //Yellow
            //Tft.DrawColor(0xffff); //White

            // Tft.FillRect(100, 100, 100, 100, 0xf800); // Red
            //Tft.FillRect(100, 100, 100, 100, 0x07e0); // Green
            //Tft.FillRect(100, 100, 100, 100, 0x001f); // Blue
            //Tft.FillRect(100, 100, 100, 100, 0x0000); // Black
            //Tft.FillRect(100, 100, 100, 100, 0xffe0); // Yellow
            //Tft.FillRect(100, 100, 100, 100, 0xffff); // White

        }

        private async void InitAll()
        {
            Debug.WriteLine("<-InitAll()");
            await Tft.Begin();
            Debug.WriteLine("<-InitAll()");
        }

        private void MainPage_Unloaded(object sender, object args)
        {
            // Cleanup
            Tft.Dispose();
        }
    }
}
