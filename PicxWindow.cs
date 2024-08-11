using FastTool_Lib;
using Terminal.Gui;

namespace FastTool_TerminalGUI;

public class PicxWindow : FrameView
{
    private Button _openButton;
    private ProcessReporter _reporter;
    private ConsoleDialog _consoleDialog;
    private Command _batCommand;

    public PicxWindow()
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        Title = "Picx图床";

        _openButton = new Button("打开Picx图床")
        {
            X = 0,
            Y = 0,
            Width = 4,
            Height = 1
        };
        Add(_openButton, _openButton);

        _openButton.Clicked += PicxControl;
    }

    private void PicxControl()
    {
    }
}

