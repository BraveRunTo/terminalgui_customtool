using Terminal.Gui;

namespace FastTool_TerminalGUI;

public class UnityToolWindow : BaseToolWindow
{
    protected Dictionary<string, View> _toolDict = new Dictionary<string, View>()
    {
        ["Unity资产包相关"] = new UnityPackageWindow()
    };

    public UnityToolWindow() : base("Unity工具")
    {

    }

    public override View GetTool(string toolName)
    {
        return _toolDict[toolName];
    }

    public override List<string> GetToolList()
    {
        return _toolDict.Keys.ToList();
    }
}