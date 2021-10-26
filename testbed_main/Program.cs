using ICSharpCode.SharpZipLib.Zip;

namespace testbed_main
{
    class Program
    {
        static void Main(string[] args)
        {
            FastZip zipper = new FastZip();

            zipper.ExtractZip(@"C:\Users\abruss\Downloads\simpsons_map\simpsons.pk3", @"c:\temp", "levelshots");
        }
    }
}
