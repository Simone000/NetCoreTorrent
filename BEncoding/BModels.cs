using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BEncoding
{
    public abstract class BItem
    {
        public byte[] SourceBytes { get; set; }
        public abstract int GetOccupiedSize();
    }

    public class BString : BItem
    {
        //4:spam => spam
        public string Value { get; set; }
        public override int GetOccupiedSize()
        {
            int lengthSpace = Value.Length.ToString().Length;
            return lengthSpace + 1 + Value.Length;
        }
    }
    public class BInteger : BItem
    {
        //i3e => 3
        public int Value { get; set; }
        public override int GetOccupiedSize()
        {
            return 2 + Value.ToString().Length;
        }
    }
    public class BList : BItem
    {
        //l4:spam4:eggse => [ "spam", "eggs" ] 
        public List<BItem> Values = new List<BItem>();
        public override int GetOccupiedSize()
        {
            return 2 + Values.Sum(p => p.GetOccupiedSize());
        }
    }

    //todo: implement as a real dictionary
    public class BDictionary : BItem
    {
        //d4:spaml1:a1:bee => { "spam" => [ "a", "b" ] } 
        public Dictionary<BItem, BItem> Values = new Dictionary<BItem, BItem>();
        public override int GetOccupiedSize()
        {
            return 2 + Values.Sum(p => p.Key.GetOccupiedSize() + p.Value.GetOccupiedSize());
        }
    }
}
