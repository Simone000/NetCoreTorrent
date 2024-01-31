using System;
using System.Linq;

namespace BEncoding
{
    /// <summary>
    /// https://wiki.theory.org/BitTorrentSpecification#Bencoding
    /// </summary>
    public static class BEncoding2
    {
        public static BItem Decode(byte[] BEncodedBytes)
        {
            if (BEncodedBytes.Length == 0)
                return null;

            var BEncodedChars = BEncodedBytes.Select(p => (char)p).ToArray();

            char firstChar = (char)BEncodedBytes[0];
            switch (firstChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    //string 
                    string firstNumber = new String(BEncodedChars.TakeWhile(Char.IsDigit).ToArray());
                    int parsedFirstNumber = int.Parse(firstNumber);
                    string bstring = new String(BEncodedChars.SubArray(firstNumber.Length + 1, parsedFirstNumber)); //+1 cause there is ":" to consider
                    return new BString() { Value = bstring, SourceBytes = BEncodedBytes.SubArray(firstNumber.Length + 1, parsedFirstNumber) };

                case 'i':
                    var integerText = BEncodedBytes.SubArray(1, BEncodedBytes.ToList().IndexOf((byte)'e') - 1);
                    int parsedBInt = int.Parse(new string(integerText.Select(p => (char)p).ToArray()));
                    return new BInteger() { Value = parsedBInt };

                case 'l':
                    var blist = new BList();
                    int startReadingIndex = 1;
                    while (BEncodedChars[startReadingIndex] != 'e')
                    {
                        var item = Decode(BEncodedBytes.SubArray(startReadingIndex, BEncodedBytes.Length - 1 - startReadingIndex));
                        blist.Values.Add(item);
                        startReadingIndex += item.GetOccupiedSize();
                    }
                    return blist;

                case 'd':
                    var bdict = new BDictionary();
                    int startReadingIndex2 = 1;
                    while (BEncodedChars[startReadingIndex2] != 'e')
                    {
                        var propName = Decode(BEncodedBytes.SubArray(startReadingIndex2, BEncodedBytes.Length - startReadingIndex2));
                        var propValue = Decode(BEncodedBytes.SubArray(startReadingIndex2 + propName.GetOccupiedSize(), BEncodedBytes.Length - (startReadingIndex2 + propName.GetOccupiedSize())));
                        bdict.Values.Add(propName, propValue);
                        startReadingIndex2 = bdict.GetOccupiedSize() - 1;
                    }
                    bdict.SourceBytes = BEncodedBytes.SubArray(0, bdict.GetOccupiedSize());
                    return bdict;
            }

            throw new Exception("First char not recognized as a valid BEncoding value");
        }


        [Obsolete("Use the one with byte[], otherwise you will not able to calculate the infohash (in order to read you need an Encoding)")]
        public static BItem Decode(string BEncodedText)
        {
            if (string.IsNullOrWhiteSpace(BEncodedText))
                return null;

            char firstChar = BEncodedText[0];
            switch (firstChar)
            {
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    string firstNumber = new String(BEncodedText.TakeWhile(Char.IsDigit).ToArray());
                    int parsedFirstNumber = int.Parse(firstNumber);
                    string bstring = BEncodedText.Substring(firstNumber.Length + 1, parsedFirstNumber); //+1 cause there is ":" to consider
                    return new BString() { Value = bstring };

                case 'i':
                    var integerText = BEncodedText.Substring(1, BEncodedText.IndexOf('e') - 1);
                    int parsedBInt = int.Parse(integerText);
                    return new BInteger() { Value = parsedBInt };

                case 'l':
                    var blist = new BList();
                    int startReadingIndex = 1;
                    while (BEncodedText[startReadingIndex] != 'e')
                    {
                        var item = Decode(BEncodedText.Substring(startReadingIndex, BEncodedText.Length - 1 - startReadingIndex));
                        blist.Values.Add(item);
                        startReadingIndex += item.GetOccupiedSize();
                    }
                    return blist;

                case 'd':
                    var bdict = new BDictionary();
                    int startReadingIndex2 = 1;
                    while (BEncodedText[startReadingIndex2] != 'e')
                    {
                        var propName = Decode(BEncodedText.Substring(startReadingIndex2, BEncodedText.Length - startReadingIndex2));
                        var propValue = Decode(BEncodedText.Substring(startReadingIndex2 + propName.GetOccupiedSize(), BEncodedText.Length - (startReadingIndex2 + propName.GetOccupiedSize())));
                        bdict.Values.Add(propName, propValue);
                        startReadingIndex2 = bdict.GetOccupiedSize() - 1;
                    }
                    //bdict.Text = BEncodedText.Substring(0, bdict.GetOccupiedSize());
                    return bdict;
            }

            throw new Exception("First char not recognized as a valid BEncoding value");
        }
    }
}
