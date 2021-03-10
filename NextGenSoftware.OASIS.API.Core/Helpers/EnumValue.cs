using System;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public class EnumValue<T>
    {
        public EnumValue(T value)
        {
            Value = value;
        }

        private string _name = "";
        public T Value { get; set; }
      //  public Type Type { get; set; }
        public string Name
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_name))
                    _name = Enum.GetName(typeof(T), Value);

                return _name;
            }
        }
    }
}
