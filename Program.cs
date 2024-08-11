// See https://aka.ms/new-console-template for more information

using System.Runtime.InteropServices;
using FastTool_Lib;
using FastTool_TerminalGUI;
using Terminal.Gui;

void Show()
{
    //Console.SetOut(new FileLogTextWriter());
    try
    {
         Application.Init();
         Application.Run<FastToolWindow>();
    }
    catch (Exception e)
    {
        Application.Shutdown();
        Console.WriteLine(e.Message + e.StackTrace);
    }
    finally
    {
        Application.Shutdown();
        Console.ReadKey(true);
        //Console.SetOut(new StreamWriter(Console.OpenStandardOutput()));
    }
    
    
}

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    
/*
 * 当前用户是管理员的时候，直接启动应用程序
 * 如果不是管理员，则使用启动对象启动程序，以确保使用管理员身份运行
 */
//获得当前登录的Windows用户标示
    System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
    System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
//判断当前登录用户是否为管理员
    if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
    {
        //如果是管理员，则直接运行
        Show();
    }
    else
    {
        //创建启动对象
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.UseShellExecute = true;
        startInfo.WorkingDirectory = Environment.CurrentDirectory;
        startInfo.FileName = "FastTool_TerminalGUI.exe";
        //设置启动动作,确保以管理员身份运行
        startInfo.Verb = "runas";
        try
        {
            System.Diagnostics.Process.Start(startInfo);
        }
        catch
        {
            return;
        }
    }
}
else
{
    Show();
}