using System;
using System.Collections.Generic;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.Holons
{
    public class SemanticHolon : Holon, ISemanticHolon
    {
        private IHolon _parent = null;
        private List<IHolon> _children = null;
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public List<IHolon> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<IHolon>();
                    //TODO: Get Holon Children from graph here.
                }

                return _children;
            }
        }

        public IHolon Parent
        {
            get
            {
                if (_parent == null)
                {
                    //TODO: Get parent holon from graph here.
                }

                return _parent;
            }
        }
    }
}
