using Terminal.Gui;

namespace FastTool_TerminalGUI;

public abstract class BaseToolWindow : FrameView
{
    private FrameView _left;
    private ListView _toolList;
    private Toplevel _right;
    
    public BaseToolWindow(string title)
    {
        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        Title = title;

        _left = new FrameView("工具")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(20),
            Height = Dim.Fill()
        };
        _toolList = new ListView(GetToolList())
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (0),
            Height = Dim.Fill (0),
            AllowsMarking = false,
            CanFocus = true,
        };
        _left.Add(_toolList);
        _toolList.OpenSelectedItem += (a) => {
            _right.SetFocus ();
        };
        _toolList.SelectedItemChanged += OnToolSelected;
        
        _right = new Toplevel()
        {
            X = Pos.Right(_left),
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
        };
        Add(_left, _right);
        _toolList.SelectedItem = 0;
    }
    public void OnToolSelected(ListViewItemEventArgs e)
    {
        var item = e.Value.ToString();
        if (string.IsNullOrEmpty(item)) return;
        _right.RemoveAll();
        _right.Add(GetTool(item));
    }

    public abstract View GetTool(string toolName);

    public abstract List<string> GetToolList();
}