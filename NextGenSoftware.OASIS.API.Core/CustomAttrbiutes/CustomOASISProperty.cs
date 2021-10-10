using NextGenSoftware.OASIS.API.Core.Interfaces;
using System;

namespace NextGenSoftware.OASIS.API.Core.CustomAttrbiutes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class CustomOASISProperty : Attribute
    {
        public CustomOASISProperty()
        {
           
        }

        //private string propertyName;
        //public double version;

        //public CustomOASISProperty(string propertyName)
        //{
        //    this.propertyName = propertyName;
        //    version = 1.0;
        //}
    }
}
