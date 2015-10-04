using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YoutubeExtractor {
    public static class Decipherer {
        public static event Action<RetryableProcessFailed> FailedDownload;

        public static string DecipherWithVersion(VideoInfo vidinfo, string cipher, string cipherVersion) {
            var jsUrl = $"http://s.ytimg.com/yts/jsbin/html5player-{cipherVersion}.js";

            string js;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = vidinfo};
            retry:try {
                js = HttpHelper.DownloadString(jsUrl);
            } catch (Exception e) {
                rpf.Defaultize(e);
                FailedDownload?.Invoke(rpf);
                if (rpf.ShouldRetry && rpf.NumberOfTries<=10)
                    goto retry;
                return null;
            }


            //Find "C" in this: var A = B.sig||C (B.s)
            var functNamePattern = @"\.sig\s*\|\|([a-zA-Z0-9\$]+)\("; //Regex Formed To Find Word or DollarSign

            var funcName = Regex.Match(js, functNamePattern).Groups[1].Value;

            if (funcName.Contains("$"))
                funcName = "\\" + funcName; //Due To Dollar Sign Introduction, Need To Escape

            var funcBodyPattern = @"(?<brace>{([^{}]| ?(brace))*})"; //Match nested angle braces
            var funcPattern = string.Format(@"{0}\(\w+\){1}", @funcName, funcBodyPattern); //Escape funcName string
            var funcBody = Regex.Match(js, funcPattern).Groups["brace"].Value; //Entire sig function
            var lines = funcBody.Split(';'); //Each line in sig function

            string idReverse = "", idSlice = "", idCharSwap = ""; //Hold name for each cipher method
            var functionIdentifier = "";
            var operations = "";

            foreach (var line in lines.Skip(1).Take(lines.Length - 2)) //Matches the funcBody with each cipher method. Only runs till all three are defined.
            {
                if (!string.IsNullOrEmpty(idReverse) && !string.IsNullOrEmpty(idSlice) &&
                    !string.IsNullOrEmpty(idCharSwap))
                    break; //Break loop if all three cipher methods are defined

                functionIdentifier = GetFunctionFromLine(line);
                var reReverse = string.Format(@"{0}:\bfunction\b\(\w+\)", functionIdentifier); //Regex for reverse (one parameter)
                var reSlice = string.Format(@"{0}:\bfunction\b\([a],b\).(\breturn\b)?.?\w+\.", functionIdentifier); //Regex for slice (return or not)
                var reSwap = string.Format(@"{0}:\bfunction\b\(\w+\,\w\).\bvar\b.\bc=a\b", functionIdentifier); //Regex for the char swap.

                if (Regex.Match(js, reReverse).Success)
                    idReverse = functionIdentifier; //If def matched the regex for reverse then the current function is a defined as the reverse

                if (Regex.Match(js, reSlice).Success)
                    idSlice = functionIdentifier; //If def matched the regex for slice then the current function is defined as the slice.

                if (Regex.Match(js, reSwap).Success)
                    idCharSwap = functionIdentifier; //If def matched the regex for charSwap then the current function is defined as swap.
            }

            foreach (var line in lines.Skip(1).Take(lines.Length - 2)) {
                Match m;
                functionIdentifier = GetFunctionFromLine(line);

                if ((m = Regex.Match(line, @"\(\w+,(?<index>\d+)\)")).Success && functionIdentifier == idCharSwap)
                    operations += "w" + m.Groups["index"].Value + " "; //operation is a swap (w)

                if ((m = Regex.Match(line, @"\(\w+,(?<index>\d+)\)")).Success && functionIdentifier == idSlice)
                    operations += "s" + m.Groups["index"].Value + " "; //operation is a slice

                if (functionIdentifier == idReverse) //No regex required for reverse (reverse method has no parameters)
                    operations += "r "; //operation is a reverse
            }

            operations = operations.Trim();

            return DecipherWithOperations(cipher, operations);
        }

         public static async Task<string> DecipherWithVersionAsync(VideoInfo vidinfo, string cipher, string cipherVersion) {
            var jsUrl = $"http://s.ytimg.com/yts/jsbin/html5player-{cipherVersion}.js";

            string js;
            var rpf = new RetryableProcessFailed("LoadUrls") {Tag = vidinfo};
            retry:try {
                js = await HttpHelper.DownloadStringAsync(jsUrl);
            } catch (Exception e) {
                rpf.Defaultize(e);
                FailedDownload?.Invoke(rpf);
                if (rpf.ShouldRetry && rpf.NumberOfTries<=10)
                    goto retry;
                return null;
            }


            //Find "C" in this: var A = B.sig||C (B.s)
            var functNamePattern = @"\.sig\s*\|\|([a-zA-Z0-9\$]+)\("; //Regex Formed To Find Word or DollarSign

            var funcName = Regex.Match(js, functNamePattern).Groups[1].Value;

            if (funcName.Contains("$"))
                funcName = "\\" + funcName; //Due To Dollar Sign Introduction, Need To Escape

            var funcBodyPattern = @"(?<brace>{([^{}]| ?(brace))*})"; //Match nested angle braces
            var funcPattern = string.Format(@"{0}\(\w+\){1}", @funcName, funcBodyPattern); //Escape funcName string
            var funcBody = Regex.Match(js, funcPattern).Groups["brace"].Value; //Entire sig function
            var lines = funcBody.Split(';'); //Each line in sig function

            string idReverse = "", idSlice = "", idCharSwap = ""; //Hold name for each cipher method
            var functionIdentifier = "";
            var operations = "";

            foreach (var line in lines.Skip(1).Take(lines.Length - 2)) //Matches the funcBody with each cipher method. Only runs till all three are defined.
            {
                if (!string.IsNullOrEmpty(idReverse) && !string.IsNullOrEmpty(idSlice) &&
                    !string.IsNullOrEmpty(idCharSwap))
                    break; //Break loop if all three cipher methods are defined

                functionIdentifier = GetFunctionFromLine(line);
                var reReverse = string.Format(@"{0}:\bfunction\b\(\w+\)", functionIdentifier); //Regex for reverse (one parameter)
                var reSlice = string.Format(@"{0}:\bfunction\b\([a],b\).(\breturn\b)?.?\w+\.", functionIdentifier); //Regex for slice (return or not)
                var reSwap = string.Format(@"{0}:\bfunction\b\(\w+\,\w\).\bvar\b.\bc=a\b", functionIdentifier); //Regex for the char swap.

                if (Regex.Match(js, reReverse).Success)
                    idReverse = functionIdentifier; //If def matched the regex for reverse then the current function is a defined as the reverse

                if (Regex.Match(js, reSlice).Success)
                    idSlice = functionIdentifier; //If def matched the regex for slice then the current function is defined as the slice.

                if (Regex.Match(js, reSwap).Success)
                    idCharSwap = functionIdentifier; //If def matched the regex for charSwap then the current function is defined as swap.
            }

            foreach (var line in lines.Skip(1).Take(lines.Length - 2)) {
                Match m;
                functionIdentifier = GetFunctionFromLine(line);

                if ((m = Regex.Match(line, @"\(\w+,(?<index>\d+)\)")).Success && functionIdentifier == idCharSwap)
                    operations += "w" + m.Groups["index"].Value + " "; //operation is a swap (w)

                if ((m = Regex.Match(line, @"\(\w+,(?<index>\d+)\)")).Success && functionIdentifier == idSlice)
                    operations += "s" + m.Groups["index"].Value + " "; //operation is a slice

                if (functionIdentifier == idReverse) //No regex required for reverse (reverse method has no parameters)
                    operations += "r "; //operation is a reverse
            }

            operations = operations.Trim();

            return DecipherWithOperations(cipher, operations);
        }


        private static string ApplyOperation(string cipher, string op) {
            switch (op[0]) {
                case 'r':
                    return new string(cipher.ToCharArray().Reverse().ToArray());

                case 'w': {
                    var index = GetOpIndex(op);
                    return SwapFirstChar(cipher, index);
                }

                case 's': {
                    var index = GetOpIndex(op);
                    return cipher.Substring(index);
                }

                default:
                    throw new NotImplementedException("Couldn't find cipher operation.");
            }
        }

        private static string DecipherWithOperations(string cipher, string operations) {
            return operations.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries)
                .Aggregate(cipher, ApplyOperation);
        }

        private static string GetFunctionFromLine(string currentLine) {
            var matchFunctionReg = new Regex(@"\w+\.(?<functionID>\w+)\("); //lc.ac(b,c) want the ac part.
            var rgMatch = matchFunctionReg.Match(currentLine);
            var matchedFunction = rgMatch.Groups["functionID"].Value;
            return matchedFunction; //return 'ac'
        }

        private static int GetOpIndex(string op) {
            var parsed = new Regex(@".(\d+)").Match(op).Result("$1");
            var index = int.Parse(parsed);

            return index;
        }

        private static string SwapFirstChar(string cipher, int index) {
            var builder = new StringBuilder(cipher);
            builder[0] = cipher[index];
            builder[index] = cipher[0];

            return builder.ToString();
        }
    }
}