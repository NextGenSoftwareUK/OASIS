using System;
using System.Collections.Generic;
using System.Text;

namespace NextGenSoftware.OASIS.API.Core
{
    public class Holon
    {
        private Holon _parent = null;
        private List<Holon> _children = null;
        public Guid Id { get; set; }
        public Guid ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public HolonType HolonType { get; set; }

        public List<Holon> Children
        {
            get
            {
                if (_children == null)
                {
                    _children = new List<Holon>();
                    //TODO: Get Holon Children from graph here.
                }

                return _children;
            }
        }

        public Holon Parent
        {
            get
            {
                if (_parent == null)
                {
                  //  _parent = new Holon();
                    //TODO: Get parent holon from graph here.
                }

                return _parent;
            }
        }
    }
}
