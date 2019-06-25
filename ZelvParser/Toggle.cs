using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ZelvParser
{
    class Toggle
    {
        public static string Navigate(string url)
        {
            //опции хрома - новый экземпляр
            ChromeOptions chromeOptions = new ChromeOptions();

            //без отображения браузера
            chromeOptions.AddArgument("--headless");
            
            //драйвер браузера
            OpenQA.Selenium.Chrome.ChromeDriver odr = new OpenQA.Selenium.Chrome.ChromeDriver(chromeOptions);
            start:
            try
            {
                //переходим по странице
                odr.Navigate().GoToUrl(url);
            }
            catch (Exception e)
            {
                if (e.HResult == -2146233088) goto start;
            }
            //берем внутренний html
            string dom = odr.PageSource;

            //выходим из браузера(он автоматом делает dispose, но я подстраховался еще)
            odr.Quit();
            //подстраховка
            odr.Dispose();
            //возврат кода страницы
            return dom;
        }
    }
}
