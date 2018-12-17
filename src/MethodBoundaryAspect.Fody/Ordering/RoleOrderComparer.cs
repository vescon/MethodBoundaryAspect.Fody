using System;
using System.Collections.Generic;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public class RoleOrderComparer : IComparer<string>
    {
        public List<string> BeforeRoles { get; set; }
        public List<string> AfterRoles { get; set; }

        public RoleOrderComparer(AspectInfo aspectInfo)
        {
            BeforeRoles = aspectInfo.Order.BeforeRoles;
            AfterRoles = aspectInfo.Order.AfterRoles;
        }

        public RoleOrderComparer(List<string> beforeRoles, List<string> afterRoles)
        {
            BeforeRoles = beforeRoles;
            AfterRoles = afterRoles;
        }

        public int Compare(string x, string y)
        {
            if (AfterRoles.Contains(x))
            {
                if (AfterRoles.Contains(y))
                    return String.Compare(x, y, StringComparison.Ordinal);

                return -1;
            }

            if (AfterRoles.Contains(y))
            {
                if (AfterRoles.Contains(x))
                    return String.Compare(x, y, StringComparison.Ordinal);

                return 1;
            }

            if (BeforeRoles.Contains(x))
            {
                if (BeforeRoles.Contains(y))
                    return String.Compare(x, y, StringComparison.Ordinal);

                return 1;
            }

            if (BeforeRoles.Contains(y))
            {
                if (BeforeRoles.Contains(x))
                    return String.Compare(x, y, StringComparison.Ordinal);

                return -1;
            }

            return x.CompareTo(y);
        }
    }
}