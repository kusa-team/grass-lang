using System;
using System.Collections.Generic;

namespace grasslang
{
    public class ArgumentParser
    {
        public ArgumentParser(string[] args)
        {
            this.args = args;
        }


        private string[] args;
        private int pos = 0;
        private string current
        {
            get
            {
                if (args.Length < pos + 1)
                {
                    return null;
                }
                return args[pos];
            }
        }
        private string peek
        {
            get
            {
                if (args.Length == pos + 1)
                {
                    return null;
                }
                return args[pos + 1];
            }
        }
        private string next()
        {
            string result = peek;
            pos++;
            return result;
        }


        public Dictionary<string, object> Arguments = new Dictionary<string, object>();
        public List<string> Others = new List<string>();


        enum PrepareType
        {
            Switch, Value
        }
        struct Prepare
        {

            public PrepareType type;
            public string tag;
            public string[] name;
            public object value;
            public Prepare(PrepareType type, string tag, string[] name, object value)
            {
                this.type = type;
                this.tag = tag;
                this.name = name;
                this.value = value;
            }
        }
        private List<Prepare> prepares = new List<Prepare>();

        public object this[string key]
        {
            get
            {
                if(Arguments.ContainsKey(key))
                {
                    return Arguments[key];
                }
                return null;
            }
        }

        public void AddSwitch(string tag, string[] name, bool value = false)
        {
            prepares.Add(new Prepare(PrepareType.Switch, tag, name, value));
        }
        public void AddValue(string tag, string[] name, string value = "")
        {
            prepares.Add(new Prepare(PrepareType.Value, tag, name, value));
        }


        private void parsePrepare()
        {
            bool found = false;
            foreach (Prepare prepare in prepares)
            {
                if (Array.IndexOf(prepare.name, current) != -1)
                {
                    if (prepare.type == PrepareType.Switch)
                    {
                        Arguments[prepare.tag] = !(bool)prepare.value;
                    }
                    else
                    {
                        Arguments[prepare.tag] = peek == null ? prepare.value : peek;
                    }
                    found = true;
                }
            }
            if (!found)
            {
                Others.Add(current);
            }
            next();
        }
        public void Parse()
        {
            foreach (Prepare prepare in prepares)
            {
                Arguments[prepare.tag] = prepare.value;
            }
            while (current != null)
            {
                parsePrepare();
            }
        }
    }
}
