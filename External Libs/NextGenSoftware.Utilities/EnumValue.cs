using System;

namespace NextGenSoftware.Utilities
{
    public class EnumValue<T>
    {
        private string _name = "";
        private T _value;

        public EnumValue(T value)
        {
            Value = value;
        }

        public EnumValue(T value, double score)
        {
            Value = value;
            Score = score;
        }

        
        public T Value
        {
            get { return _value; }
            set
            {
                _value = value;
                _name = null;
            }
        }

        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                    _name = Enum.GetName(typeof(T), Value);

                return _name;
            }
        }

        /// <summary>
        /// AI-driven score for provider recommendations (0.0 to 1.0)
        /// Higher scores indicate better performance/recommendation
        /// </summary>
        public double Score { get; set; } = 0.0;
    }
}
