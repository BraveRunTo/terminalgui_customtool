using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Terminal.Gui;

namespace FastTool_TerminalGUI;

public class CmdUtils
{
    public static Command ExecuteBat(Action<string> outputAction = null,
        Action<string> errorAction = null, Action exitAction = null, params string[] cmds)
    {
        Command command = new Command();
        command.Output += outputAction;
        command.Error += errorAction;
        command.Exited += exitAction;
        
        int index = 0;
        while (cmds.Length > index)
        {
            command.RunCMD(cmds[index]);
            index += 1;
        }

        return command;
    }
    
    public static void OpenUrl(string url)
    {
        using Process process = new Process();
        process.StartInfo.FileName = url;
        process.StartInfo.UseShellExecute = true;
        process.Start();
    }
    /// <summary>
    /// TODO
    /// </summary>
    /// <param name="picPath"></param>
    public static void OpenPic(string picPath)
    {
        //建立新的系统进程 
        using Process process = new Process();
        //设置文件名，此处为图片的真实路径+文件名 
        process.StartInfo.FileName = picPath;
        //此为关键部分。设置进程运行参数，此时为最大化窗口显示图片。 
        process.StartInfo.Arguments = "rundll32.exe C:\\WINDOWS\\system32\\shimgvw.dll,ImageView_Fullscreen";
        //此项为是否使用Shell执行程序，因系统默认为true，此项也可不设，但若设置必须为true 
        process.StartInfo.UseShellExecute = true;
        //此处可以更改进程所打开窗体的显示样式，可以不设 
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
        process.Start();
    }
}

/// <summary>
/// 控制台命令，由天蘩封装
/// </summary>
public class Command
{
    private const int ReadSize = 1024;
    private Process _cmd; //cmd进程
    private Encoding _outEncoding; //输出字符编码
    private Stream _outStream; //基础输出流
    private Stream _errorStream; //错误输出流
    public event Action<string> Output; //输出事件
    public event Action<string> Error; //错误事件
    public event Action Exited; //退出事件
    private bool _run; //循环控制
    private byte[] _tempBuffer; //临时缓冲
    
    private byte[] _readBuffer; //读取缓存区

    private byte[] _eTempBuffer; //临时缓冲
    private byte[] _errorBuffer; //错误读取缓存区

    public Command()
    {
        _cmd = new Process();
        _cmd.StartInfo.FileName = "cmd.exe";
        _cmd.StartInfo.UseShellExecute = false; //是否使用操作系统shell启动
        _cmd.StartInfo.RedirectStandardInput = true; //接受来自调用程序的输入信息
        _cmd.StartInfo.RedirectStandardOutput = true; //由调用程序获取输出信息
        _cmd.StartInfo.RedirectStandardError = true; //重定向标准错误输出
        _cmd.StartInfo.CreateNoWindow = true; //不显示程序窗口
        _cmd.Exited += _CMD_Exited;
        _readBuffer = new byte[ReadSize];
        _errorBuffer = new byte[ReadSize];
        Start();
    }

    /// <summary>
    /// 停止使用，关闭进程和循环线程
    /// </summary>
    public void Stop()
    {
        _run = false;
        _cmd.Close();
        _outStream.Close();
        _errorStream.Close();
    }


    /// <summary>
    /// 重新启用
    /// </summary>
    public void Start()
    {
        _cmd.Start();
        _outEncoding = _cmd.StandardOutput.CurrentEncoding;
        _outStream = _cmd.StandardOutput.BaseStream;
        _errorStream = _cmd.StandardError.BaseStream;
        _run = true;
        _cmd.StandardInput.AutoFlush = true;
        ReadResult();
        ErrorResult();
    }

    //退出事件
    private void _CMD_Exited(object sender, EventArgs e)
    {
        Exited?.Invoke();
        
    }

    /// <summary>
    /// 执行cmd命令
    /// </summary>
    /// <param name="cmd">需要执行的命令</param>
    public void RunCMD(string cmd)
    {
        if (!_run)
        {
            return;
        }

        if (_cmd.HasExited)
        {
            Stop();
            return;
        }

        _cmd.StandardInput.WriteLine(cmd);
    }


    //异步读取输出结果
    private void ReadResult()
    {
        if (!_run)
        {
            return;
        }

        _outStream.BeginRead(_readBuffer, 0, ReadSize, ReadEnd, null);
    }

    //一次异步读取结束
    private void ReadEnd(IAsyncResult ar)
    {
        try
        {
            if (!_run)
            {
                return;
            }
            int count = _outStream.EndRead(ar);

            if (count < 1)
            {
                if (_cmd.HasExited)
                {
                    Stop();
                }

                return;
            }

            if (_tempBuffer == null)
            {
                _tempBuffer = new byte[count];
                Buffer.BlockCopy(_readBuffer, 0, _tempBuffer, 0, count);
            }
            else
            {
                byte[] buff = _tempBuffer;
                _tempBuffer = new byte[buff.Length + count];
                Buffer.BlockCopy(buff, 0, _tempBuffer, 0, buff.Length);
                Buffer.BlockCopy(_readBuffer, 0, _tempBuffer, buff.Length, count);
            }

            if (count < ReadSize)
            {
                string str = _outEncoding.GetString(_tempBuffer);
                Output?.Invoke(str);
                _tempBuffer = null;
            }

            ReadResult();
        }
        catch (Exception e)
        {
            Output?.Invoke(e.Message);
            MessageBox.ErrorQuery(e.Message, e.StackTrace, "确定");
        }
    }


    //异步读取错误输出
    private void ErrorResult()
    {
        if (!_run)
        {
            return;
        }

        _errorStream.BeginRead(_errorBuffer, 0, ReadSize, ErrorCallback, null);
    }

    private void ErrorCallback(IAsyncResult ar)
    {
        try
        {
            if (!_run)
            {
                return;
            }
            int count = _errorStream.EndRead(ar);

            if (count < 1)
            {
                if (_cmd.HasExited)
                {
                    Stop();
                }

                return;
            }

            if (_eTempBuffer == null)
            {
                _eTempBuffer = new byte[count];
                Buffer.BlockCopy(_errorBuffer, 0, _eTempBuffer, 0, count);
            }
            else
            {
                byte[] buff = _eTempBuffer;
                _eTempBuffer = new byte[buff.Length + count];
                Buffer.BlockCopy(buff, 0, _eTempBuffer, 0, buff.Length);
                Buffer.BlockCopy(_errorBuffer, 0, _eTempBuffer, buff.Length, count);
            }

            if (count < ReadSize)
            {
                string str = _outEncoding.GetString(_eTempBuffer);
                Error?.Invoke(str);
                _eTempBuffer = null;
            }

            ErrorResult();
        }
        catch (Exception e)
        {
            Error?.Invoke(e.Message);
            MessageBox.ErrorQuery(e.Message, e.StackTrace, "确定");
        }
    }

    ~Command()
    {
        _run = false;
        _cmd?.Close();
        _cmd?.Dispose();
        _outStream?.Close();
        _errorStream?.Close();
    }
}