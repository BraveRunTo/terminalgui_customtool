using Terminal.Gui;

namespace FastTool_TerminalGUI;

public class BlogToolWindow : BaseToolWindow
{
    protected Dictionary<string, View> _toolDict = new Dictionary<string, View>()
    {
        ["MD转Jekyll格式文章"] = new MDConvertJekyllWindow(),
        ["MD图片链接转换"] = new PicxWindow()
    };

    public BlogToolWindow() : base("文章转换")
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