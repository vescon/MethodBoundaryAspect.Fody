using MyApp.Business;
using System.Windows;

namespace MyApp.Presentation
{
    public partial class App : Application
    {
        public App()
        {
            BL class1 = new BL();
            class1.ClassService();
        }
    }
}
