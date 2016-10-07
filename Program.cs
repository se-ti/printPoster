using System;
using System.Reflection;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;
using System.Linq;

/// todo
/// icon 
/// инструмент линейка
/// контуры под словами Page 1 - Page 2  drawstring outline

/// ++- localization -- дожать в 1 файл -- подгружать нужные ресурсы    http://stackoverflow.com/questions/10137937/merge-dll-into-exe  https://www.microsoft.com/en-us/download/details.aspx?id=17630
/// check for update
/// win32, определяться c имеющимся .Net Framework -- а надо?
/// installer или дожать в 1 файл?

/// чистка кода
/// правильный подсчет страниц при смене принтера
/// +- mark native resolution
/// 
/// rotate image
/// плоттеры и печать на рулоне
/// +- about
/// +- command line params
/// +- и таки корректная работа с рамками  // http://stackoverflow.com/questions/8761633/how-to-find-the-actual-printable-area-printdocument
/// +- разобраться, какие поля тащим из настроек принтера!   

/// ++ проверять допустимость перекрытия при смене printArea
/// ++ отрисовка перекрытия
/// ++ версию в заголовок
/// ++ overlap
/// ++ правильный подсчет страниц при смене разрешения и т.п. (было 5, стало больше)
/// ++ рассинхронизация скролла и картинки
/// ++ память на больших зумах
/// ++ git -- доделать дистрибутивы
/// ++ изменять разрешение по табу и т.п.
/// ++ размер зоны выделения в px и см
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
