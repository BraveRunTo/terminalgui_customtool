using System.Collections;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using FastTool_Lib;
using Terminal.Gui;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Serialization;

namespace FastTool_TerminalGUI;

public class MDConvertJekyllWindow : FrameView
{
    public class Config
    {
        [YamlMember(Alias = "Version")] public string Version { get; set; }
        [YamlMember(Alias = "AuthorName")] public string AuthorName { get; set; }
        [YamlMember(Alias = "JekyllLocation")] public string JekyllLocation { get; set; }
        [YamlMember(Alias = "BlogLocation")] public string BlogLocation { get; set; }

        [YamlMember(Alias = "PictureBedWebsite")]
        public string PictureBedWebsite { get; set; }
    }

    public class JekyllBlogTitleData
    {
        public string Title { get; set; }
        public string Author { get; set; }
        public string Date { get; set; }
        public string Categories { get; set; }
        public string Tags { get; set; }
    }

    //输入路径选择按钮
    private Button _inputSelectButton;

    //输出路径显示
    private Label _outputPathLabel;

    //输入路径显示
    private Label _inputPathLabel;

    //输出路径选择按钮
    private Button _outputPathSelectButton;

    //转换按钮
    private Button _convertButton;

    //将要转换的文件路径显示组件
    private ListView _inputFilePathView;

    //将要转换的文件路径
    private List<string> _inputFilePaths;

    private Config _config;

    //转换后的文件存放路径
    private string _outputPath;

    public MDConvertJekyllWindow()
    {
        _config = DataUtils.YamlDeserialize<Config>(Environment.CurrentDirectory +
                                                    "/Configs/md_convert_jekyll_config.yml");

        X = 0;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill();
        Title = "MD转Jekyll格式文章";
        _outputPathSelectButton = new Button("选择输出路径")
        {
            X = 0,
            Y = 0,
            Width = 4,
            Height = 1,
        };
        _outputPathLabel = new Label()
        {
            X = Pos.Right(_outputPathSelectButton),
            Y = Pos.Top(_outputPathSelectButton),
            Width = Dim.Fill(),
            Height = Dim.Height(_outputPathSelectButton),
        };
        _outputPathSelectButton.Clicked += OnOutputPathSelectButtonClicked;

        _inputSelectButton = new Button("选择输入路径")
        {
            X = 0,
            Y = 1,
            Width = 4,
            Height = 1,
        };
        _inputPathLabel = new Label()
        {
            X = Pos.Right(_inputSelectButton),
            Y = Pos.Top(_inputSelectButton),
            Width = Dim.Fill(),
            Height = Dim.Height(_inputSelectButton),
        };
        _inputSelectButton.Clicked += OnFileSelectButtonClicked;

        _inputFilePathView = new ListView()
        {
            X = Pos.Left(_inputSelectButton),
            Y = Pos.Bottom(_inputSelectButton) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
        };
        _convertButton = new Button("转换")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(_inputFilePathView),
            Width = 4,
            Height = 1,
        };
        _convertButton.Clicked += OnConvertButtonClicked;
        Add(_inputSelectButton, _inputPathLabel, _outputPathSelectButton, _outputPathLabel, _inputFilePathView,
            _convertButton);

        _outputPath = _config.JekyllLocation;
        _outputPathLabel.Text = _outputPath;
    }

    private void OnOutputPathSelectButtonClicked()
    {
        var dialog = new SaveDialog("选择输出路径", "选择转化后的文件存放路径")
        {
            DirectoryPath = _config.JekyllLocation
        };
        Application.Run(dialog);
        if (!dialog.Canceled)
        {
            _outputPathLabel.Text = dialog.FilePath;
            _outputPath = dialog.FilePath.ToString();
        }
    }

    private void OnFileSelectButtonClicked()
    {
        var dialog = new OpenDialog("选择文件或文件夹", "选择需要转化的MD文件或者需要批量转换的文件夹")
        {
            CanChooseFiles = true,
            CanChooseDirectories = true,
            AllowsMultipleSelection = true,
            DirectoryPath = _config.BlogLocation
        };
        Application.Run(dialog);
        if (!dialog.Canceled)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string path in dialog.FilePaths)
            {
                sb.Append(path).Append(';');
            }

            _inputPathLabel.Text = sb.ToString();

            List<FileSystemInfo> filePaths = dialog.FilePaths.Select<string, FileSystemInfo>((filePath) =>
            {
                if (Directory.Exists(filePath))
                {
                    return new DirectoryInfo(filePath);
                }

                //可能和文件夹同名
                if (File.Exists(filePath))
                {
                    return new FileInfo(filePath);
                }

                return null;
            }).ToList();
            _inputFilePaths = RefreshFilePathView(filePaths);
            _inputFilePathView.Source = new ListWrapper(_inputFilePaths);
        }
    }

    private List<string> RefreshFilePathView(List<FileSystemInfo> filePaths, string patten = ".*\\.md$")
    {
        List<string> pathList = new List<string>(filePaths.Count);
        foreach (FileSystemInfo filePath in filePaths)
        {
            if (filePath is DirectoryInfo dInfo)
            {
                pathList.AddRange(RefreshFilePathView(dInfo.GetFileSystemInfos().ToList()));
            }

            if (filePath is FileInfo fInfo)
            {
                Regex regex = new Regex(patten);
                if (!regex.IsMatch(fInfo.Name))
                {
                    continue;
                }

                pathList.Add(fInfo.FullName);
            }
        }

        return pathList;
    }

    private void OnConvertButtonClicked()
    {
        if (_inputFilePaths == null || _inputFilePaths.Count == 0)
        {
            MessageBox.ErrorQuery("错误", "请先选择文件", "确定");
            return;
        }

        if (string.IsNullOrEmpty(_outputPath))
        {
            MessageBox.ErrorQuery("错误", "请先选择输出路径", "确定");
            return;
        }

        Convert();
        BuildJekyll();
        GitPush();
    }

    private void Convert()
    {
        StringBuilder sb = new StringBuilder();
        foreach (string filePath in _inputFilePaths)
        {
            FileInfo fInfo = new FileInfo(filePath);

            var jekyllBlogTitleDataSetting = new JekyllBlogTitleDataSetDialog();
            jekyllBlogTitleDataSetting.SetData(new JekyllBlogTitleData()
            {
                Author = _config.AuthorName,
                Date = $"{fInfo.CreationTime:yyyy-MM-dd} +0800",
                Title = fInfo.Name.Replace(".md", ""),
                Categories = $"[{(fInfo.Directory != null ? fInfo.Directory.Name : "")}]",
                Tags = "[]"
            });
            Application.Run(jekyllBlogTitleDataSetting);
            if (jekyllBlogTitleDataSetting.Data == null)
            {
                continue;
            }

            string content = File.ReadAllText(filePath);
            string newContent = ConvertMDToJekyll(content, jekyllBlogTitleDataSetting.Data);
            newContent = ConvertImage(newContent, filePath);
            var newFilePath = Path.Combine(_outputPath, $"{fInfo.CreationTime:yyyy-MM-dd}-{fInfo.Name}");
            sb.Append(newFilePath + "\n");
            File.WriteAllText(newFilePath, newContent);
        }

        MessageBox.Query("成功", sb.ToString(), "确定");
    }

    private string ConvertMDToJekyll(string content, JekyllBlogTitleData data)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("---\n");
        sb.Append("title: ").Append(data.Title).Append('\n');
        sb.Append("author: ").Append(data.Author).Append('\n');
        sb.Append("date: ").Append(data.Date).Append('\n');
        sb.Append("categories: ").Append(data.Categories).Append('\n');
        sb.Append("tags: ").Append(data.Tags).Append('\n');
        sb.Append("---\n");

        sb.Append(content);
        return sb.ToString();
    }

    private string ConvertImage(string content, string filePath)
    {
        if (!CheckImagePathLocal(content, filePath))
        {
            return content;
        }

        int iChose = MessageBox.Query("注意", "当前文件中包含本地图片路径，请先将图片上传到图床", "确定", "无视");
        if (iChose == 1)
        {
            return content;
        }
        
        CmdUtils.OpenUrl(_config.PictureBedWebsite);

        Regex regex = new Regex(@"!\[.*?\]\((.*?)\)");
        var localImageList = regex.Matches(content).ToList();
        //本地图片路径和图片名字的对应关系
        var localImageNameMap = new Dictionary<string, string>();
        //本地图片可能分布在多个文件夹中，需要将这些文件夹和其中的图片关联起来
        var localImageDirShowMap = new Dictionary<string, List<string>>();
        foreach (var match in localImageList)
        {
            var localImageRelativePath = Path.GetDirectoryName(match.Groups[1].Value)!.Replace("/", "\\").Replace(".\\", "");
            if (localImageRelativePath.StartsWith("http:\\") || localImageRelativePath.StartsWith("https:\\"))
            {
                continue;
            }
            var localImageKey = Path.Combine(Path.GetDirectoryName(filePath)!, localImageRelativePath);
            if (!localImageDirShowMap.TryGetValue(localImageKey, out List<string> value))
            {
                value = new List<string>();
                localImageDirShowMap.Add(localImageKey, value);
            }
            value.Add("\"" + match.Groups[1].Value.Replace("/", "\\").Split("\\").Last() + "\"");
            localImageNameMap.Add(match.Value, match.Groups[1].Value.Replace("/", "\\").Split("\\").Last().Split(".").First());
        }
        
        var messageShowDialog = new ConvertImageMessageShowDialog("图床上传", localImageDirShowMap);
        using ClipboardListener listener = new ClipboardListener();
        listener.OnReceive += (str) =>
        {
            if (!regex.IsMatch(str))
            {
                return;
            }
            var uploadImageList = regex.Matches(str).ToList();
            //上传图片名字和图片路径的对应关系
            var uploadImageNameMap = new Dictionary<string, string>();
            foreach (var match in uploadImageList)
            {
                var uploadImageName = match.Groups[1].Value.Replace("/", "\\").Split("\\").Last()
                    .Split(".").First();
                uploadImageNameMap.Add(uploadImageName, match.Value);
            }
            
            var localReplacedImageList = new List<string>();
            foreach (var key in localImageNameMap.Keys)
            {
                if (uploadImageNameMap.ContainsKey(localImageNameMap[key]))
                {
                    content = content.Replace(key, uploadImageNameMap[localImageNameMap[key]]);
                    localReplacedImageList.Add(key);
                }
            }
            //将已经成功替换的图片路径从本地图片路径中移除
            foreach (var path in localReplacedImageList)
            {
                localImageNameMap.Remove(path);
            }
            
            if (localImageNameMap.Count == 0)
            {
                listener.SetEmpty();
                listener.Stop();
                Application.RequestStop();
            }
        };
        listener.Start();
        Application.Run(messageShowDialog);
        MessageBox.Query("提示", "图片替换完成", "确定");
        return content;
    }

    private bool CheckImagePathLocal(string content, string filePath)
    {
        Regex regex = new Regex(@"!\[.*?\]\((.*?)\)");
        return regex.IsMatch(content);
    }

    private void BuildJekyll()
    {
        if (MessageBox.Query("是否进行Jekyll构建？", "勾选确定则将自动进行Jekyll构建", "确定", "取消") == 0)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var consoleDialog = new ConsoleDialog("Jekyll构建", "正在构建Jekyll，请稍等...\n");
            CmdUtils.ExecuteBat((args) => { consoleDialog.AddContent(args); },
                (args) => { consoleDialog.AddContent(args); }, () => { Application.RequestStop(); },
                "cd H:\\Data\\githubblog", "H:", "bundle exec jekyll b", "exit");

            Application.Run(consoleDialog);
            sw.Stop();
            MessageBox.Query("提示", "Jekyll构建完成，耗时：" + sw.ElapsedMilliseconds + "ms", "确定");
        }
    }

    private void GitPush()
    {
        if (MessageBox.Query("是否进行Git提交？", "勾选确定则将自动进行Git提交", "确定", "取消") == 0)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            var consoleDialog = new ConsoleDialog("Git提交", "正在提交Git，请稍等...\n");
            CmdUtils.ExecuteBat((args) => { consoleDialog.AddContent(args); },
                (args) => { consoleDialog.AddContent(args); }, () => { Application.RequestStop(); },
                "cd H:\\Data\\githubblog", "H:", "git add .", "git commit -am \"update\"", "git push origin main",
                "exit");

            Application.Run(consoleDialog);
            sw.Stop();
            MessageBox.Query("提示", "Git提交完成，耗时：" + sw.ElapsedMilliseconds + "ms", "确定");
        }
    }
}

public class JekyllBlogTitleDataSetDialog : Dialog
{
    public MDConvertJekyllWindow.JekyllBlogTitleData Data { get; set; }
    private Label _titleLabel;
    private TextField _titleTextField;
    private Label _authorLabel;
    private TextField _authorTextField;
    private Label _dateLabel;
    private TextField _dateTextField;
    private Label _categoriesLabel;
    private TextField _categoriesTextField;
    private Label _tagsLabel;
    private TextField _tagsTextField;
    private Button _confirmButton;
    private Button _cancelButton;

    public JekyllBlogTitleDataSetDialog() : base()
    {
        Title = "Jekyll博客标题设置";

        _titleLabel = new Label("标题")
        {
            X = 0,
            Y = 1,
            Width = 4,
            Height = 1,
        };
        _titleTextField = new TextField()
        {
            X = Pos.Right(_titleLabel),
            Y = Pos.Top(_titleLabel),
            Width = Dim.Fill(),
            Height = 1,
        };

        _authorLabel = new Label("作者")
        {
            X = 0,
            Y = 3,
            Width = 4,
            Height = 1,
        };
        _authorTextField = new TextField()
        {
            X = Pos.Right(_authorLabel),
            Y = Pos.Top(_authorLabel),
            Width = Dim.Fill(),
            Height = 1,
        };

        _dateLabel = new Label("日期")
        {
            X = 0,
            Y = 5,
            Width = 4,
            Height = 1,
        };
        _dateTextField = new TextField()
        {
            X = Pos.Right(_dateLabel),
            Y = Pos.Top(_dateLabel),
            Width = Dim.Fill(),
            Height = 1,
        };

        _categoriesLabel = new Label("分类")
        {
            X = 0,
            Y = 7,
            Width = 4,
            Height = 1,
        };
        _categoriesTextField = new TextField()
        {
            X = Pos.Right(_categoriesLabel),
            Y = Pos.Top(_categoriesLabel),
            Width = Dim.Fill(),
            Height = 1,
        };

        _tagsLabel = new Label("标签")
        {
            X = 0,
            Y = 9,
            Width = 4,
            Height = 1,
        };
        _tagsTextField = new TextField()
        {
            X = Pos.Right(_tagsLabel),
            Y = Pos.Top(_tagsLabel),
            Width = Dim.Fill(),
            Height = 1,
        };

        _confirmButton = new Button("确定")
        {
            X = Pos.Center() - 4,
            Y = Pos.Bottom(this),
            Width = 4,
            Height = 1,
        };

        _cancelButton = new Button("取消")
        {
            X = Pos.Center() + 4,
            Y = Pos.Bottom(this),
            Width = 4,
            Height = 1,
        };

        _confirmButton.Clicked += OnConfirmButtonClicked;
        _cancelButton.Clicked += () => { Application.RequestStop(); };

        AddButton(_confirmButton);
        AddButton(_cancelButton);
        Add(_titleTextField, _authorTextField, _dateTextField, _categoriesTextField, _tagsTextField,
            _titleLabel, _authorLabel, _dateLabel, _categoriesLabel, _tagsLabel);
    }

    public void SetData(MDConvertJekyllWindow.JekyllBlogTitleData data)
    {
        _titleTextField.Text = data.Title;
        _authorTextField.Text = data.Author;
        _dateTextField.Text = data.Date;
        _categoriesTextField.Text = data.Categories;
        _tagsTextField.Text = data.Tags;
    }

    private void OnConfirmButtonClicked()
    {
        Data = new MDConvertJekyllWindow.JekyllBlogTitleData()
        {
            Title = _titleTextField.Text.ToString(),
            Author = _authorTextField.Text.ToString(),
            Date = _dateTextField.Text.ToString(),
            Categories = _categoriesTextField.Text.ToString(),
            Tags = _tagsTextField.Text.ToString(),
        };
        Application.RequestStop();
    }
}

public class ConsoleDialog : Dialog
{
    private TextView _textView;

    public ConsoleDialog(string title, string content) : base(title)
    {
        _textView = new TextView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill() - 1,
            ReadOnly = true,
            Text = content
        };
        Add(_textView);
    }

    public void AddContent(string content)
    {
        _textView.Text += "\n" + content;
    }
}

public class ConvertImageMessageShowDialog : Dialog
{
    private ListView _imageDirView;
    private ListView _imagePathsView;
    private Button _copyDirButton;
    private Button _closeButton;
    public ConvertImageMessageShowDialog(string title, Dictionary<string, List<string>> data) : base(title)
    {
        _imageDirView = new ListView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill() - 1,
            AllowsMarking = false,
            CanFocus = true,
        };
        _imagePathsView = new ListView()
        {
            X = Pos.Right(_imageDirView),
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill() - 1,
            AllowsMarking = false,
            CanFocus = true,
        };
        _copyDirButton = new Button("复制文件夹")
        {
            X = Pos.Left(_imagePathsView),
            Y = Pos.Bottom(_imagePathsView),
            Width = 4,
            Height = 1,
        };
        _copyDirButton.Clicked += () =>
        {
            var data = _imageDirView.Source as ListWrapper;
            if (data == null)
            {
                return;
            }

            var dir = data.ToList()[_imageDirView.SelectedItem]!.ToString();
            Clipboard.TrySetClipboardData(dir);
        };
        _closeButton = new Button("关闭")
        {
            X = Pos.Center(),
            Y = Pos.Bottom(this),
            Width = 4,
            Height = 1,
        };
        _closeButton.Clicked += () => { Application.RequestStop(); };
        Add(_imageDirView, _imagePathsView, _closeButton, _copyDirButton);
        _imageDirView.Source = new ListWrapper(data.Keys.ToList());
        _imageDirView.SelectedItemChanged += (a) =>
        {
            _imagePathsView.Source = new ListWrapper(data[a.Value.ToString()]);
        };
        _imageDirView.SelectedItem = 0;
    }
}