using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BetterConsoleTables;
using NextGenSoftware.ErrorHandling;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;

namespace NextGenSoftware.CLI.Engine
{
    public static class CLIEngine
    {
        const char _block = '■';
        const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        const string _twirl = "-\\|/";

        private static string[] _columnHeaders;
        private static string[][] _rows;
        private static int _currentRow = 0;
        private static ProgressBar _progressBar = null;
        //private static ShellProgressBar.ProgressBar _progressBar = null;
        private static int _lastPercentage = 0;
        private static int _workingMessageLength = 0;
        private static bool _updatingProgressBar = false;
        private static bool _nextMessageOnSameLine = false;
        public static Spinner Spinner = new Spinner();
        // public static Colorful.Console ColorfulConsole;

        public static ConsoleColor SuccessMessageColour { get; set; } = ConsoleColor.Green;
        public static ConsoleColor ErrorMessageColour { get; set; } = ConsoleColor.Red;
        public static ConsoleColor WarningMessageColour { get; set; } = ConsoleColor.DarkYellow;
        public const ConsoleColor MessageColour = ConsoleColor.Yellow;
        public static ConsoleColor WorkingMessageColour { get; set; } = ConsoleColor.Yellow;
        public static bool SupressConsoleLogging { get; set; } = false;

        //public static ErrorHandlingBehaviour ErrorHandlingBehaviour { get; set; } = ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent;
        public static ErrorHandlingBehaviour ErrorHandlingBehaviour
        {
            get
            {
                return ErrorHandling.ErrorHandling.ErrorHandlingBehaviour;
            }
            set
            {
                ErrorHandling.ErrorHandling.ErrorHandlingBehaviour = value;
            }
        }

        public delegate void Error(object sender, CLIEngineErrorEventArgs e);
        public static event Error OnError;

        public static void WriteAsciMessage(string message, Color color)
        {
            Colorful.Console.WriteAscii(message, color);
        }

        public static void ShowColoursAvailable()
        {
            try
            {
                ShowMessage("", false);
                ConsoleColor oldColour = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write(" Sorry, that colour is not valid. Please try again. The colour needs to be one of the following: ");

                string[] values = Enum.GetNames(typeof(ConsoleColor));

                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] != "Black")
                    {
                        PrintColour(values[i]);

                        if (i < values.Length - 2)
                            Console.Write(", ");

                        else if (i == values.Length - 2)
                            Console.Write(" or ");
                    }
                }

                ShowMessage("", false);
                Console.ForegroundColor = oldColour;
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowColoursAvailable method.", ex);
            }
        }

        public static void PrintColour(string colour)
        {
            Console.ForegroundColor = (ConsoleColor)Enum.Parse(typeof(ConsoleColor), colour);
            Console.Write(colour);
        }

        public static void ShowSuccessMessage(string message, bool lineSpace = true, bool noLineBreak = false, int intendBy = 1)
        {
            ShowMessage(message, SuccessMessageColour, lineSpace, noLineBreak, intendBy);
        }

        public static void ShowDivider(string character = "*", int dividerLength = 119, bool lineSpace = true, bool noLineBreaks = false, int intendBy = 1)
        {
            string divider = "";

            for(int i = 0; i < dividerLength; i++)
                divider = string.Concat(divider, character);

            ShowMessage(divider, MessageColour, lineSpace, noLineBreaks, intendBy);
        }

        public static void ShowMessage(string message, bool lineSpace = true, bool noLineBreaks = false, int intendBy = 1)
        {
            ShowMessage(message, MessageColour, lineSpace, noLineBreaks, intendBy);
        }

        public static void ShowMessage(string message, ConsoleColor colour, bool lineSpace = true, bool noLineBreaks = false, int intendBy = 1)
        {
            try
            {
                if (SupressConsoleLogging)
                    return;

                ConsoleColor existingColour = Console.ForegroundColor;
                Console.ForegroundColor = colour;
                //ShowMessage(message, lineSpace, noLineBreaks, intendBy);
                bool wasSpinnerActive = false;

                if (Spinner.IsActive)
                {
                    Spinner.Stop();
                    wasSpinnerActive = true;
                    
                    if (!_nextMessageOnSameLine)
                        Console.WriteLine("");
                }

                _nextMessageOnSameLine = false;

                if (lineSpace)
                    Console.WriteLine(" ");

                string indent = "";
                for (int i = 0; i < intendBy; i++)
                    indent = string.Concat(indent, " ");

                if (noLineBreaks)
                    Console.Write(string.Concat(indent, message));
                else
                {
                    // if (wasSpinnerActive)
                    //     Console.WriteLine("");

                   
                    Console.WriteLine(string.Concat(indent, message));
                    //Console.WriteLine("");
                }

                Console.ForegroundColor = existingColour;
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowMessage method.", ex);
            }
        }

        public static void ShowWorkingMessage(string message, bool lineSpace = true, int intendBy = 1, bool nextMessageOnSameLine = false)
        {
            try
            {
                if (SupressConsoleLogging)
                    return;

                ShowMessage(message, WorkingMessageColour, lineSpace, true, intendBy);
                Spinner.Start();
                _nextMessageOnSameLine = nextMessageOnSameLine;
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowWorkingMessage method.", ex);
            }
        }

        public static void ShowWorkingMessage(string message, ConsoleColor color, bool lineSpace = true, int intendBy = 1, bool nextMessageOnSameLine = false)
        {
            try
            {
                if (SupressConsoleLogging)
                    return;

                ShowMessage(message, color, lineSpace, true, intendBy);
                Spinner.Start();
                _nextMessageOnSameLine = nextMessageOnSameLine;
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowWorkingMessage method.", ex);
            }
        }

        public static void BeginWorkingMessage(string message, ConsoleColor consoleColour)
        {
            _workingMessageLength = message.Length;
            ShowWorkingMessage(message, consoleColour, false, 1, true);
        }

        public static void BeginWorkingMessage(string message)
        {
            _workingMessageLength = message.Length;
            ShowWorkingMessage(message, WorkingMessageColour, false, 1, true);
        }

        public static void UpdateWorkingMessage(string message, ConsoleColor consoleColour)
        {
            Spinner.Stop();
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(string.Concat(" ", message, " "), consoleColour);
            Spinner.Start();

            //string back = "";
            //for (int i = 0; i < _workingMessageLength; i++)
            //    back = string.Concat(back, "\b");

            //_workingMessageLength = message.Length;
            //ShowWorkingMessage(string.Concat(back, message), consoleColour, false, 1, true);
        }

        public static void UpdateWorkingMessageWithPercent(int percent, ConsoleColor consoleColour)
        {
            //Console.Write($"\b\b\b {percent}%");
            Console.Write($"\b\b\b\b\b {percent}%");
            //Console.Write($"\b\b\b\b\b\b {percent}%");

            if (percent >= 10 && _lastPercentage < 10)
                Spinner.Left++;

            _lastPercentage = percent;
        }

        public static void UpdateWorkingMessageWithPercent(int percent)
        {
            UpdateWorkingMessageWithPercent(percent, WorkingMessageColour);
        }

        public static void UpdateWorkingMessage(string message)
        {
            UpdateWorkingMessage(message, WorkingMessageColour);
        }

        public static void EndWorkingMessage(string message, ConsoleColor consoleColour)
        {
            ShowMessage(message, consoleColour, false, false, 0);
        }

        public static void EndWorkingMessage(string message)
        {
            ShowMessage(message, WorkingMessageColour, false, false, 0);
        }

        public static void ShowErrorMessage(string message, bool lineSpace = true, bool noLineBreak = false, int intendBy = 1)
        {
            try
            {
                ShowMessage(message, ErrorMessageColour, lineSpace, noLineBreak, intendBy);
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowErrorMessage method.", ex);
            }
        }

        public static void ShowWarningMessage(string message, bool lineSpace = true, bool noLineBreak = false, int intendBy = 1)
        {
            try
            {
                ShowMessage(message, WarningMessageColour, lineSpace, noLineBreak, intendBy);
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ShowWarningMessage method.", ex);
            }
        }

        public static void BeginTable(string[] columnHeaders)
        {
            _columnHeaders = columnHeaders;
        }

        public static void WriteTableRow(string[] columns)
        {
            _rows[_currentRow] = columns;
            _currentRow++;
        }

        public static void EndTable()
        {
            int width = Console.WindowWidth;

            for (int i = 0; i < _currentRow; i++)
            {
                for (int iCol = 0; iCol < _columnHeaders.Length; iCol++)
                    Console.Write(_rows[i][iCol]);
            }
        }

        public static string GetValidTitle(string message, ConsoleColor colour = MessageColour)
        {
            string title = GetValidInput(message, colour).ToUpper();
            //string[] validTitles = new string[5] { "Mr", "Mrs", "Ms", "Miss", "Dr" };
            string validTitles = "MR,MRS,MS,MISS,DR";

            try
            {
                bool titleValid = false;
                while (!titleValid)
                {
                    if (!validTitles.Contains(title))
                    {
                        ShowErrorMessage("Title invalid. Please try again. Valid values are: Mr, Mrs, Ms, Miss or Dr.");
                        title = GetValidInput(message).ToUpper();
                    }
                    else
                        titleValid = true;
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidTitle method.", ex);
            }

            return ExtensionMethods.ToPascalCase(title);
        }

        public static string GetValidInput(string message, ConsoleColor colour = MessageColour)
        {
            string input = "";
            message = string.Concat(message, " ");

            try
            {
                while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                {
                    ShowMessage(string.Concat("", message), colour, true, true);
                    input = Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInput method.", ex);
            }

            return input;
        }

        public static long GetValidInputForLong(string message, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            long result = 0;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                    {
                        result = -1;
                        break;
                    }

                    if (long.TryParse(input, out result))
                        valid = true;
                    else
                    {
                        ShowErrorMessage("Invalid Long Number.");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForLong method.", ex);
            }

            return result;
        }

        public static decimal GetValidInputForDecimal(string message, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            decimal result = 0;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                    {
                        result = -1;
                        break;
                    }

                    if (decimal.TryParse(input, out result))
                        valid = true;
                    else
                    {
                        ShowErrorMessage("Invalid Decimal.");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForDecimal method.", ex);
            }

            return result;
        }

        public static double GetValidInputForDouble(string message, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            double result = 0;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                    {
                        result = -1;
                        break;
                    }

                    if (double.TryParse(input, out result))
                        valid = true;
                    else
                    {
                        ShowErrorMessage("Invalid Double.");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForDouble method.", ex);
            }

            return result;
        }

        public static int GetValidInputForInt(string message, bool checkForLowestOrHighestRange = false, int lowest = 0, int highest = 0, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            int result = 0;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                        break;

                    if (int.TryParse(input, out result))
                    {
                        if (checkForLowestOrHighestRange && result < lowest)
                        {
                            ShowErrorMessage($"Number must be above {lowest}.");
                            input = "";
                        }

                        else if (checkForLowestOrHighestRange && result > highest)
                        {
                            ShowErrorMessage($"Number must be below {highest}.");
                            input = "";
                        }
                        else
                            valid = true;
                    }
                    else
                    {
                        ShowErrorMessage("Invalid Int (Number).");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForInt method.", ex);
            }

            return result;
        }

        public static Guid GetValidInputForGuid(string message, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            Guid result = Guid.Empty;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                        break;

                    if (Guid.TryParse(input, out result))
                        valid = true;
                    else
                    {
                        ShowErrorMessage("Invalid GUID.");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForGuid method.", ex);
            }

            return result;
        }

        public static object GetValidInputForEnum(string message, Type enumType, ConsoleColor colour = MessageColour)
        {
            string input = "";
            object objEnumValue = null;
            message = string.Concat(message, " ");

            try
            {
                bool valid = false;
               
                while (!valid)
                {
                    ShowMessage(string.Concat("", message), colour, true, true);
                    input = Console.ReadLine();

                    if (input == "exit")
                    {
                        objEnumValue = "exit";
                        break;
                    }

                    valid = Enum.TryParse(enumType, input, out objEnumValue);

                    if (!valid)
                        ShowMessage($"You need to enter one of the following: {EnumHelper.GetEnumValues(enumType, EnumHelperListType.ItemsSeperatedByComma)}");
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInputForEnum method.", ex);
            }

            return objEnumValue;
        }

        public static string GetValidFolder(string message, bool createIfDoesNotExist = true, string baseAddress = "", ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                        break;

                    string path = Path.Combine(baseAddress, input);

                    if (Directory.Exists(Path.Combine(baseAddress, input)))
                        valid = true;
                    else
                    {
                        if (createIfDoesNotExist)
                        {
                            if (GetConfirmation("The folder does not exist, do you wish to create it now?"))
                            {
                                Directory.CreateDirectory(Path.Combine(baseAddress, input));
                                valid = Directory.Exists(Path.Combine(baseAddress, input));
                            }
                            else
                                input = "";

                            //Console.WriteLine("");
                        }
                        else
                        {
                            ShowErrorMessage("The folder does not exist!");
                            input = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidFolder method.", ex);
            }

            return input;
        }

        //public static string GetValidFile(string message, string baseAddress = "", string defaultPath = "")
        public static string GetValidFile(string message, string baseAddress = "", ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;

            try
            {
                message = string.Concat(message, " ");

                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                        break;

                    if (File.Exists(Path.Combine(baseAddress, input)))
                        valid = true;
                    else
                    {
                        ShowErrorMessage("The file does not exist!");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidFile method.", ex);
            }

            return input;
        }

        public static byte[] GetValidFileAndUpload(string message, string baseAddress = "", ConsoleColor colour = MessageColour)
        {
            string path = GetValidFile(message, baseAddress, colour);

            if (!string.IsNullOrEmpty(path))
                return File.ReadAllBytes(path);

            return null;
        }

        public static async Task<Uri> GetValidURIAsync(string message, bool checkFileExists = true, ConsoleColor colour = MessageColour)
        {
            string input = "";
            bool valid = false;
            Uri uri = null;
            message = string.Concat(message, " ");

            try
            {
                while (!valid)
                {
                    while (string.IsNullOrEmpty(input) || string.IsNullOrWhiteSpace(input) && input != "exit")
                    {
                        ShowMessage(string.Concat("", message), colour, true, true);
                        input = Console.ReadLine();
                    }

                    if (input == "exit")
                        break;

                    if (Uri.TryCreate(input, UriKind.Absolute, out uri))
                    {
                        if (checkFileExists)
                        {
                            ShowWorkingMessage("Checking if the URI exists...", colour);

                            if (await URIHelper.ValidateUrlWithHttpClientAsync(uri.AbsoluteUri))
                            {
                                ShowSuccessMessage("The URI is valid!");
                                valid = true;
                            }
                            else
                            {
                                ShowErrorMessage("The URI is valid but the resource/file does not exist!");
                                input = "";
                            }
                        }
                        else
                            valid = true;
                    }
                    else
                    {
                        ShowErrorMessage("The URI is invalid!");
                        input = "";
                    }
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidURI method.", ex);
            }

            return uri;
        }

        //public static bool GetConfirmation(string message)
        //{
        //    return GetConfirmation(message, MessageColour);
        //}

        public static bool GetConfirmation(string message, ConsoleColor colour = MessageColour)
        {
            bool validKey = false;
            bool confirm = false;
            message = string.Concat(message, " ");

            try
            {
                while (!validKey)
                {
                    ShowMessage(message, colour, true, true);
                    ConsoleKey key = Console.ReadKey().Key;

                    if (key == ConsoleKey.Y)
                    {
                        confirm = true;
                        validKey = true;
                    }

                    if (key == ConsoleKey.N)
                    {
                        confirm = false;
                        validKey = true;
                    }
                }

            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidInput method.", ex);
            }

            return confirm;
        }

        public static string GetValidInputForEmail(string message)
        {
            bool emailValid = false;
            string email = "";

            while (!emailValid)
            {
                ShowMessage(string.Concat("", message), true, true);
                email = Console.ReadLine();

                if (!ValidationHelper.IsValidEmail(email))
                    ShowErrorMessage("That email is not valid. Please try again.");
                else
                    emailValid = true;
            }

            return email;
        }

        public static string GetValidPassword(string message = "What is the password you wish to use? ", string confirmMessage = "Please confirm password: ", ConsoleColor colour = MessageColour)
        {
            string password = "";
            string password2 = "";

            try
            {
                ShowMessage("", false);

                while ((string.IsNullOrEmpty(password) && string.IsNullOrEmpty(password2)) || password != password2)
                {
                    password = ReadPassword(message, colour);
                    password2 = ReadPassword(confirmMessage, colour);

                    if (password != password2)
                        ShowErrorMessage("The passwords do not match. Please try again.");
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidPassword method.", ex);
            }

            return password;
        }

        public static string ReadPassword(string message, ConsoleColor colour = MessageColour)
        {
            string password = "";
            ConsoleKey key;

            try
            {
                while (string.IsNullOrEmpty(password) && string.IsNullOrWhiteSpace(password))
                {
                    ShowMessage(string.Concat("", message), colour, true, true);

                    do
                    {
                        var keyInfo = Console.ReadKey(intercept: true);
                        key = keyInfo.Key;

                        if (key == ConsoleKey.Backspace && password.Length > 0)
                        {
                            Console.Write("\b \b");
                            password = password[0..^1];
                        }
                        else if (!char.IsControl(keyInfo.KeyChar))
                        {
                            Console.Write("*");
                            password += keyInfo.KeyChar;
                        }
                    } while (key != ConsoleKey.Enter);

                    ShowMessage("", false);
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.ReadPassword method.", ex);
            }

            return password;
        }

        public static void GetValidColour(ref ConsoleColor favColour, ref ConsoleColor cliColour, ConsoleColor messageColour = MessageColour)
        {
            try
            {
                bool colourSet = false;
                while (!colourSet)
                {
                    ShowMessage("What is your favourite colour? ", messageColour, true, true);
                    string colour = Console.ReadLine();
                    colour = ExtensionMethods.ToPascalCase(colour);
                    //object colourObj = null;
                    // ConsoleColor colourObj;

                    //if (Enum.TryParse(typeof(ConsoleColor), colour, out colourObj))
                    if (Enum.TryParse(colour, out favColour))
                    {
                        // favColour = (ConsoleColor)colourObj;
                        Console.ForegroundColor = favColour;
                        ShowMessage("Do you prefer to use your favourite colour? :) ", messageColour, true, true);

                        if (Console.ReadKey().Key != ConsoleKey.Y)
                        {
                            Console.WriteLine("");

                            while (!colourSet)
                            {
                                //Defaults
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                Spinner.Colour = ConsoleColor.Green;

                                ShowMessage("Which colour would you prefer? ", messageColour, true, true);
                                colour = Console.ReadLine();
                                colour = ExtensionMethods.ToPascalCase(colour);
                                //colourObj = null;

                                //if (Enum.TryParse(typeof(ConsoleColor), colour, out colourObj))
                                if (Enum.TryParse(colour, out cliColour))
                                {
                                    //cliColour = (ConsoleColor)colourObj;
                                    Console.ForegroundColor = cliColour;
                                    Spinner.Colour = cliColour;

                                    // ShowMessage("", false);
                                    ShowMessage("This colour ok? ", true, true);

                                    if (Console.ReadKey().Key == ConsoleKey.Y)
                                        colourSet = true;
                                    else
                                        ShowMessage("", false);
                                }
                                else
                                    ShowColoursAvailable();
                            }
                        }
                        else
                            colourSet = true;
                    }
                    else
                        ShowColoursAvailable();
                }
            }
            catch (Exception ex)
            {
                HandleError("Error occured in CLIEngine.GetValidColour method.", ex);
            }
        }

        public static void ShowProgressBar(double percent)
        {
            if (_progressBar == null)
                _progressBar = new ProgressBar();

            _progressBar.Report(percent);
        }

        public static void DisposeProgressBar(bool clearText = true)
        {
            if (_progressBar != null)
            {
                if (clearText)
                    _progressBar.Dispose();
                else
                    _progressBar.DisposeButKeepText();

                _progressBar = null;
            }
        }

        //public static void ShowProgressBar(string message, int maxTicks, ConsoleColor colour = ConsoleColor.Yellow)
        //{
        //    if (_progressBar == null)
        //        _progressBar = new ShellProgressBar.ProgressBar(maxTicks, message, colour);

        //    _progressBar.Tick()
        //}



        //public static void ShowProgressBar(int percent, bool update = false)
        //{
        //    if (_updatingProgressBar)
        //        return;

        //    _updatingProgressBar = true;

        //    if (update)
        //        Console.Write(_back);
        //    Console.Write("[");
        //    var p = (int)((percent / 10f) + .5f);
        //    for (var i = 0; i < 10; ++i)
        //    {
        //        if (i >= p)
        //            Console.Write(' ');
        //        else
        //            Console.Write(_block);
        //    }
        //    Console.Write("] {0,3:##0}%", percent);
        //    //Spinner.Start();

        //    _updatingProgressBar = false;
        //}

        //private static string WriteMessage(string message, bool lineSpace = true, int intendBy = 1)
        //{
        //    if (Spinner.IsActive)
        //    {
        //        Spinner.Stop();
        //        Console.WriteLine("");
        //    }

        //    ConsoleColor existingColour = Console.ForegroundColor;
        //    Console.ForegroundColor = ConsoleColor.Red;

        //    if (lineSpace)
        //        Console.WriteLine("");

        //    string indent = "";
        //    for (int i = 0; i < intendBy; i++)
        //        indent = string.Concat(indent, " ");

        //    Console.WriteLine(string.Concat(indent, message));
        //    Console.ForegroundColor = existingColour;
        //}

        private static void HandleError(string message, Exception exception)
        {
            message = string.Concat(message, exception != null ? $". Error Details: {exception}" : "");
            //Logging.Logging.Log(message, LogType.Error);

            OnError?.Invoke(null, new CLIEngineErrorEventArgs { Reason = message, ErrorDetails = exception });

            switch (ErrorHandlingBehaviour)
            {
                case ErrorHandlingBehaviour.AlwaysThrowExceptionOnError:
                    throw new CLIEngineException(message, exception);

                case ErrorHandlingBehaviour.OnlyThrowExceptionIfNoErrorHandlerSubscribedToOnErrorEvent:
                    {
                        if (OnError == null)
                            throw new CLIEngineException(message, exception);
                    }
                    break;
            }
        }
    }
}