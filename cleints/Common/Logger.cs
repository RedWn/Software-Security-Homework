public enum LogType
{
    info1,
    info2,
    warning,
    error,
    commandFeedback
}

public class Logger
{
    public static ConsoleColor defaultColor = ConsoleColor.Gray;
    public static ConsoleColor info1Color = ConsoleColor.DarkCyan;
    public static ConsoleColor info2Color = ConsoleColor.DarkGreen;
    public static ConsoleColor warningColor = ConsoleColor.Yellow;
    public static ConsoleColor errorColor = ConsoleColor.Red;
    public static ConsoleColor commandColor = ConsoleColor.White;

    private static List<Action> executeOnConsoleThread = new List<Action>();
    private static List<Action> executeCopiedOnConsoleThread = new List<Action>();
    private static bool logToWriteOnConsoleThread = false;

    public static void Initialize(ConsoleColor _consoleColor)
    {
        defaultColor = _consoleColor;
    }

    public static void Log(LogType _logType, string _message, bool newline = true, bool _indent = true)
    {
        lock (executeOnConsoleThread)
        {
            executeOnConsoleThread.Add(() =>
            {
                WriteLog(_logType, _message, newline, _indent);
            });
            logToWriteOnConsoleThread = true;
        }
    }


    public static void WriteLogs()
    {
        if (logToWriteOnConsoleThread)
        {

            executeCopiedOnConsoleThread.Clear();
            lock (executeOnConsoleThread)
            {
                executeCopiedOnConsoleThread.AddRange(executeOnConsoleThread);
                executeOnConsoleThread.Clear();
                logToWriteOnConsoleThread = false;
            }

            for (int i = 0; i < executeCopiedOnConsoleThread.Count; i++)
            {
                executeCopiedOnConsoleThread[i]();
            }
        }
    }

    private static void WriteLog(LogType _logType, string _message, bool newline = true, bool _indent = true)
    {
        string _line = "[" + GetTimeStamp(DateTime.Now) + "] ";
        string _string = _message;

        switch (_logType)
        {
            case LogType.info1:
                Console.ForegroundColor = info1Color;
                break;
            case LogType.info2:
                Console.ForegroundColor = info2Color;
                break;
            case LogType.warning:
                Console.ForegroundColor = warningColor;
                break;
            case LogType.error:
                Console.ForegroundColor = errorColor;
                _indent = false;
                break;
            case LogType.commandFeedback:
                Console.ForegroundColor = commandColor;
                break;
            default:
                Console.ForegroundColor = warningColor;
                Console.WriteLine("[" + GetTimeStamp(DateTime.Now) + "] [WARN]  Could not write message to console - Unknown LogType!");
                Console.ForegroundColor = defaultColor;
                return;
        }

        List<string> _lines = new List<string>();

        if (_indent)
        {

            string[] _words = _string.Split(' ');
            for (int i = 0; i < _words.Length; i++)
            {

                int outputPlusWord = _line.Length + _words[i].Length;
                if (outputPlusWord < Console.WindowWidth)
                {

                    _line += _words[i];
                    if (outputPlusWord + 1 < Console.WindowWidth)
                    {
                        _line += " ";
                    }
                }
                else
                {
                    _lines.Add(_line);
                    _line = "                   " + _words[i] + " ";
                }

                if (i == _words.Length - 1)
                {

                    _lines.Add(_line);
                    Console.SetCursorPosition(0, Console.CursorTop);

                    if (newline) Console.WriteLine(string.Join(Environment.NewLine, _lines));
                    else Console.Write(string.Join(Environment.NewLine, _lines));
                }
            }
        }
        else
        {
            if (newline) Console.WriteLine(_line + _string);
            else Console.Write(_line + _string);
        }

        Console.ForegroundColor = defaultColor;
    }

    private static string GetTimeStamp(DateTime _time)
    {
        return _time.ToString("HH:mm:ss");
    }
}