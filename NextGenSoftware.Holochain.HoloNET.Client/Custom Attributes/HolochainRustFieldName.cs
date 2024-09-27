
using System;

namespace NextGenSoftware.Holochain.HoloNET.Client
{
    [AttributeUsage(AttributeTargets.Property)]
    public class HolochainRustFieldName : Attribute
    {
        private string _fieldName;
        private bool _isEnabled;

        /// <summary>
        /// Add this to any property you wish to map to your rust struct/entry in your hApp.
        /// </summary>
        /// <param name="fieldName">The name of the field (case sensitive) defined in your rust struct/entry in your hApp.</param>
        /// <param name="isEnabled">Set this to false if you to ommit sending this property/param/field to the zome function.</param>
        public HolochainRustFieldName(string fieldName, bool isEnabled = true)
        {
            _fieldName = fieldName;
            _isEnabled = isEnabled;
        }
    }
}
