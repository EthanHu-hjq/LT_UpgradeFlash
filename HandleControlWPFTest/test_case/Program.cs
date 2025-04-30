using System.Text.RegularExpressions;

namespace test_case
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string s1 = "LT6911";
            string s2 = @"D:\Project\Tymphany\LT_UpgradeFlash\HDMI Board\LT FW  Files\LT6911 UXE\lt6911_fw.bin";
            bool isMatch = s2.IndexOf(s1,StringComparison.OrdinalIgnoreCase)>=0;
            Console.WriteLine(isMatch);
        }
    }
}
