using System;
using System.Windows.Forms;

/// todo
/// icon 
/// git
/// ++- localization -- дожать в 1 файл
/// память на больших зумах
/// и таки корректная работа с рамками  // // http://stackoverflow.com/questions/8761633/how-to-find-the-actual-printable-area-printdocument
/// +- разобраться, какие поля тащим из настроек принтера!   

/// чистка кода
/// правильный подсчет страниц при смене разрешения и т.п. (было 5, стало больше)
/// правильный подсчет страниц при смене принтера
/// rotate image
/// плоттеры и печать на рулоне
/// win32, определяться c имеющимся .Net Framework
/// installer
/// -+ about
/// +- command line params

/// ++ подбор названия printPoster -- printLarge printImage panoPrint 
/// ++ автоскроллинг: partial deselect 
/// +- zoom по центру экрана
/// ++ картинка при автоскроллинге при выделении
/// -- переделать селект на https://support.microsoft.com/en-us/kb/314945 ускорили по-другому
/// ++ горизонтальный скроллинг и mouse zoom
/// ++ 86.7 и 86,7 in resolution
/// ++ keyboard shortcuts (Ctrl-+ Ctrl-- )
/// ++ номера страниц на разграфке
/// ++ print area
/// ++ auto zoom on open
/// ++ open non image
/// ++ нижняя и правая границы разграфки
/// ++ автоскроллинг при выделении зоны
/// ++ enter in resolution

namespace printPoster
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            var mf = new CMainForm();

            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length > 1 && !String.IsNullOrEmpty(args[1]))
                mf.LoadImage(args[1], args[1]);

            Application.Run(mf);
        }
    }
}
