using Godot;
using System;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xianxia.Client.Battle;
using Xianxia.Client.BattleWindows;

namespace Xianxia.Client.UI;

/// <summary>后台子进程拥有一个完整世界。管道只有控制和观察值，模拟始终在子进程的主线程推进。</summary>
internal partial class ObserverWorker : Node
{
    private readonly BattleObserverSession _session = new();
    private ObserverSessionOutput? _output;
    private NamedPipeClientStream? _pipe;
    private StreamReader? _reader;
    private StreamWriter? _writer;
    private Task<string?>? _command;
    private int _cursor;
    private bool _stopping;

    public static void Start(Node bootstrap) => bootstrap.AddChild(new ObserverWorker());

    public override async void _Ready()
    {
        SetProcess(false);
        try
        {
            string pipeName = ObserverSessionOutput.Argument("--observer-worker=") ?? throw new InvalidDataException("缺少本机会话管道。");
            _pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
            await _pipe.ConnectAsync(15000);
            if (_stopping) return;
            _reader = new StreamReader(_pipe, new UTF8Encoding(false), false, 4096, true);
            _writer = new StreamWriter(_pipe, new UTF8Encoding(false), 65536, true) { AutoFlush = true };
            string input = ObserverSessionOutput.Argument("--observer-case-input=") ?? throw new InvalidDataException("缺少冻结输入。");
            string data = ObserverSessionOutput.Argument("--observer-data-root=") ?? GameBootstrap.ResolveDataRoot();
            _output = new ObserverSessionOutput();
            _output.Attach(_session);
            _session.PresentationCommitted += OnFrame;
            _session.Initialize(BattleCase.FromJson(File.ReadAllText(input)), data);
            _output.SaveInput(_session.Case);
            if (_session.Diagnostic != null) throw new InvalidOperationException(_session.Diagnostic);
            _command = _reader.ReadLineAsync();
            SetProcess(true);
        }
        catch (Exception error) { Fail(error); }
    }

    private void OnFrame(BattlePresentationFrame frame)
    {
        var records = _session.Records.Skip(_cursor).ToArray();
        _cursor += records.Length;
        _output!.AppendRecords(_session);
        // 初始包带回实际校验的数据身份。其余包只发新增记录和当前值。
        new ObserverPacket("frame", _session.SessionId, frame, records, frame.IsInitialization ? _session.Case : null,
            frame.IsInitialization ? _output.SessionDirectory : null, _session.IsPlaying, _session.CanAdvance).Write(_writer!);
    }

    public override void _Process(double delta)
    {
        if (_stopping) return;
        try
        {
            if (_command?.IsCompleted == true)
            {
                string? command = _command.GetAwaiter().GetResult();
                switch (command)
                {
                    case null:
                    case "close": Stop(0); return;
                    case "play": _session.Play(); break;
                    case "pause": _session.Pause(); break;
                    case "step": _session.Step(); break;
                    default: throw new InvalidDataException("未知观察器命令：" + command);
                }
                new ObserverPacket("ack", _session.SessionId, Playing: _session.IsPlaying, CanAdvance: _session.CanAdvance).Write(_writer!);
                _command = _reader!.ReadLineAsync();
            }
            _session.Update(delta);
            if (_session.Diagnostic != null) throw new InvalidOperationException(_session.Diagnostic);
        }
        catch (Exception error) { Fail(error); }
    }

    private void Fail(Exception error)
    {
        if (_stopping) return;
        try
        {
            _output?.AppendRecords(_session);
            if (_output != null) File.WriteAllText(Path.Combine(_output.SessionDirectory, "failure.txt"), error.ToString());
            if (_writer != null) new ObserverPacket("error", _session.SessionId, Error: error.ToString()).Write(_writer);
        }
        catch (IOException) { /* 父端已离开，释放本次会话即可。 */ }
        GD.PushError("[ObserverWorker] " + error.Message);
        Stop(1);
    }

    private void Stop(int code)
    {
        _stopping = true;
        SetProcess(false);
        GetTree().Quit(code);
    }

    public override void _ExitTree()
    {
        _stopping = true;
        _session.PresentationCommitted -= OnFrame;
        _session.Dispose();
        _output?.Dispose();
        _pipe?.Dispose();
        _reader?.Dispose();
    }
}
