namespace Apify.Services;

using System;
using System.Timers;
using Timer = System.Timers.Timer;

class SpinnerAnimation
{
    private readonly string[] _frames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];
    private int _index;
    private Timer? _timer;
    private readonly int _column;
    private readonly int _row;

    public SpinnerAnimation(Timer? timer = null, int column = 8, int row = 0)
    {
        timer ??= new Timer(100);
        _timer = timer;
        _column = column;
        _row = row;
        Console.OutputEncoding = System.Text.Encoding.UTF8; // Enable Unicode for spinner
    }

    public void Start()
    {
        _timer = new Timer(100); // Frame every 100ms
        _timer!.Elapsed += OnElapsed!;
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer?.Dispose();
    }

    private void OnElapsed(object sender, ElapsedEventArgs e)
    {
        _index++;
        try
        {
            Console.SetCursorPosition(_column, _row);
            Console.Write(_frames[_index % _frames.Length]);
        }
        catch
        {
            // Ignore console resize errors
        }
    }
}