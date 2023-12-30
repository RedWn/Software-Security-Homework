public enum LogType // Types of log messages
{
    info1,
    info2,
    warning,
    error,
    commandFeedback
}

public class Logger
{
    /// <summary>The color that the logger sets the console's text color to after writing a message.</summary>
    public static ConsoleColor defaultColor = ConsoleColor.Gray;
    /// <summary>The color that the logger uses when writing info1 messages to the console.</summary>
    public static ConsoleColor info1Color = ConsoleColor.DarkCyan;
    /// <summary>The color that the logger uses when writing info2 messages to the console.</summary>
    public static ConsoleColor info2Color = ConsoleColor.DarkGreen;
    /// <summary>The color that the logger uses when writing warnings to the console.</summary>
    public static ConsoleColor warningColor = ConsoleColor.Yellow;
    /// <summary>The color that the logger uses when writing errors to the console.</summary>
    public static ConsoleColor errorColor = ConsoleColor.Red;
    /// <summary>The color that the logger uses when writing command output to the console.</summary>
    public static ConsoleColor commandColor = ConsoleColor.White;

    private static List<Action> executeOnConsoleThread = new List<Action>();
    private static List<Action> executeCopiedOnConsoleThread = new List<Action>();
    private static bool logToWriteOnConsoleThread = false;

    /// <summary>Performs any necessary setup for the Logger.</summary>
    /// <param name="_consoleColor">The default console text color. All user input will be displayed in this color.</param>
    public static void Initialize(ConsoleColor _consoleColor)
    {
        defaultColor = _consoleColor;
    }

    /// <summary>Log a timestamped message to the console.</summary>
    /// <param name="_logType">The type of log message. Determines formatting.</param>
    /// <param name="_message">The message to log to the console.</param>
    /// <param name="_indent">Whether or not to indent the message if its length exceeds one line.</param>
    public static void Log(LogType _logType, string _message, bool _indent = true)
    {
        lock (executeOnConsoleThread)
        {
            executeOnConsoleThread.Add(() =>
            {
                WriteLog(_logType, _message, _indent);
            });
            logToWriteOnConsoleThread = true;
        }
    }

    /// <summary>Checks if there are unwritten messages and writes them. This should be called in an update loop.</summary>
    public static void WriteLogs()
    {
        if (logToWriteOnConsoleThread)
        {
            // If there's a log waiting to be written from the console thread
            executeCopiedOnConsoleThread.Clear(); // Clear the old logs from the copied queue
            lock (executeOnConsoleThread)
            {
                executeCopiedOnConsoleThread.AddRange(executeOnConsoleThread); // Copy actions from the queue to the copied queue
                executeOnConsoleThread.Clear(); // Clear the actions from the queue
                logToWriteOnConsoleThread = false;
            }

            // Write all logs from the copied queue
            for (int i = 0; i < executeCopiedOnConsoleThread.Count; i++)
            {
                executeCopiedOnConsoleThread[i]();
            }
        }
    }

    /// <summary>Writes the log message to the console with proper formatting.</summary>
    /// <param name="_logType">The type of log message. Determines formatting.</param>
    /// <param name="_message">The message to log to the console.</param>
    /// <param name="_indent">Whether or not to indent the message if its length exceeds one line.</param>
    private static void WriteLog(LogType _logType, string _message, bool _indent = true)
    {
        string _line = "";
        string _string = "";

        switch (_logType)
        {
            // Set _line, _string, and console text colour appropriately based on the log type
            case LogType.info1:
                Console.ForegroundColor = info1Color;
                _line = "[" + GetTimeStamp(DateTime.Now) + "] [INFO]  ";
                _string = _message + ".";
                break;
            case LogType.info2:
                Console.ForegroundColor = info2Color;
                _line = "[" + GetTimeStamp(DateTime.Now) + "] [INFO]  ";
                _string = _message + ".";
                break;
            case LogType.warning:
                Console.ForegroundColor = warningColor;
                _line = "[" + GetTimeStamp(DateTime.Now) + "] [WARN]  ";
                _string = _message + "!";
                break;
            case LogType.error:
                Console.ForegroundColor = errorColor;
                _line = "[" + GetTimeStamp(DateTime.Now) + "] [ERROR] ";
                _string = _message + "!";
                _indent = false;
                break;
            case LogType.commandFeedback:
                Console.ForegroundColor = commandColor;
                _line = "[" + GetTimeStamp(DateTime.Now) + "] [CMD]   ";
                _string = _message;
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
            // If we want to indent and keep words whole
            string[] _words = _string.Split(' '); // Split _string on spaces
            for (int i = 0; i < _words.Length; i++)
            {
                // Loop through all words in array of words
                int outputPlusWord = _line.Length + _words[i].Length; // Calculate length of current line + next word
                if (outputPlusWord < Console.WindowWidth)
                {
                    // If length of current line + next word is less than width of console window
                    _line += _words[i]; // Add the word to current line
                    if (outputPlusWord + 1 < Console.WindowWidth)
                    {
                        // If length of current line + next word + space is less than width of console window
                        _line += " "; // Add a space to current line
                    }
                }
                else
                {
                    // If length of current line + next word is greater or equal to width of console window
                    _lines.Add(_line); // Add the current line to the output
                    _line = "                   " + _words[i] + " "; // Indent next line and add next word
                }

                if (i == _words.Length - 1)
                {
                    // If this is the last word to be added
                    _lines.Add(_line); // Add the current line to the output
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.WriteLine(string.Join(Environment.NewLine, _lines)); // Write the output to the console
                }
            }
        }
        else
        {
            // If we are logging an error/exception or preformatted messages
            Console.WriteLine(_line + _string);
        }

        Console.ForegroundColor = defaultColor; // Reset console text color
    }

    private static void WriteLog2(LogType _logType, string _message, bool _indent = true)
    {
        string _output = "";
        string _string = "";

        switch (_logType)
        {
            // Set _output, _string, and console text colour appropriately based on the log type
            case LogType.info1:
                Console.ForegroundColor = ConsoleColor.Gray;
                _output = "[" + GetTimeStamp(DateTime.Now) + "] [INFO]  ";
                _string = _message + ".";
                break;
            case LogType.info2:
                Console.ForegroundColor = ConsoleColor.Green;
                _output = "[" + GetTimeStamp(DateTime.Now) + "] [INFO]  ";
                _string = _message + ".";
                break;
            case LogType.warning:
                Console.ForegroundColor = ConsoleColor.Yellow;
                _output = "[" + GetTimeStamp(DateTime.Now) + "] [WARN]  ";
                _string = _message + "!";
                break;
            case LogType.error:
                Console.ForegroundColor = ConsoleColor.Red;
                _output = "[" + GetTimeStamp(DateTime.Now) + "] [ERROR] ";
                _string = _message + "!";
                _indent = false;
                break;
            case LogType.commandFeedback:
                Console.ForegroundColor = ConsoleColor.White;
                _output = "[" + GetTimeStamp(DateTime.Now) + "] [CMD]   ";
                _string = _message;
                break;
        }

        if (_indent)
        {
            // If we want to indent and keep words whole
            string[] _words = _string.Split(' '); // Split _string on spaces
            for (int i = 0; i < _words.Length; i++)
            {
                // Loop through all words in array of words
                int outputPlusWord = _output.Length + _words[i].Length; // Calculate length of current line + next word
                if (outputPlusWord < Console.WindowWidth)
                {
                    // If length of current line + next word is less than width of console window
                    _output += _words[i]; // Add the word to current line
                    if (outputPlusWord + 1 < Console.WindowWidth)
                    {
                        // If length of current line + next word + space is less than width of console window
                        _output += " "; // Add a space to current line
                    }
                }
                else
                {
                    // If length of current line + next word is greater or equal to width of console window
                    Console.WriteLine(_output); // Write current line to console
                    _output = "                   " + _words[i] + " "; // Indent next line and add next word
                }

                if (i == _words.Length - 1)
                {
                    // If this is the last word to be added
                    Console.WriteLine(_output); // Write current line to console
                }
            }
        }
        else
        {
            // If we are logging an error/exception or preformatted messages
            Console.WriteLine(_output + _string);
        }

        Console.ForegroundColor = ConsoleColor.Cyan; // Reset foreground color
    }

    /// <summary>Returns a timestamp formatted as "HH:mm:ss".</summary>
    /// <param name="_time">The DateTime value to format.</param>
    private static string GetTimeStamp(DateTime _time)
    {
        return _time.ToString("HH:mm:ss"); // Return the formatted timestamp
    }
}