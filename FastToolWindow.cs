using System.Data;
using Terminal.Gui;
using YamlDotNet.Serialization;

namespace FastTool_TerminalGUI;

public class FastToolWindow : Toplevel
{
    private Toplevel _content;
    private MenuBar _menuBar;
    private BlogToolWindow _blogToolWindow;
    private UnityToolWindow _unityToolWindow;
    public FastToolWindow()
    {
        _menuBar = new MenuBar
        (
            new MenuBarItem[]
            {
                new MenuBarItem("_文件", new MenuItem[]
                {
                    new MenuItem("_退出", "", Quit)
                }),
                new MenuBarItem("_工具", new MenuItem[]
                {
                    new MenuItem("_博客工具相关", "", ShowBlogToolWindow),
                    new MenuItem("_Unity工具相关", "", ShowUnityToolWindow)
                }),
            }
        );

        _content = new Toplevel()
        {
            X = Pos.Left(_menuBar),
            Y = Pos.Bottom(_menuBar),
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };


        Add(_menuBar, _content);
    }
    
    public void Quit()
    {
        Application.RequestStop();
    }
    
    public void ShowBlogToolWindow()
    {
        _content.RemoveAll();
        _blogToolWindow ??= new BlogToolWindow();
        _content.Add(_blogToolWindow);
    }

    public void ShowUnityToolWindow()
    {
        _content.RemoveAll();
        _unityToolWindow ??= new UnityToolWindow();
        _content.Add(_unityToolWindow);
    }
}