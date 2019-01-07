using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodBoundaryAspect.Fody.Ordering
{
    public class AspectOrder
    {
        public AspectOrder(AspectInfo aspect)
        {
            Aspect = aspect;

            AfterRoles = new List<string>();
            AnyRoles = new List<string>();
            BeforeRoles = new List<string>();
        }

        public AspectInfo Aspect { get; set; }

        public List<string> AfterRoles { get; private set; }
        public List<string> AnyRoles { get; private set; }
        public List<string> BeforeRoles { get; private set; }

        public void AddRole(string role, int position)
        {
            if (ContainsRole(role))
                throw new InvalidOperationException(
                    string.Format("Aspect '{0}' already defined role '{1}'",
                        Aspect.AspectAttribute.AttributeType.Name,
                        role));

            switch (position)
            {
                case -1:
                    BeforeRoles.Add(role);
                    break;
                case 0:
                    AnyRoles.Add(role);
                    break;
                case 1:
                    AfterRoles.Add(role);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("position");
            }
        }

        public int GetOrderIndex(Dictionary<string, int> roleIndexMapping)
        {
            var minIndex = 0;
            if (AfterRoles.Count != 0)
            {
                var afterRolesIndexes = AfterRoles
                    .Where(roleIndexMapping.ContainsKey)
                    .Select(x => roleIndexMapping[x])
                    .ToList();

                if (afterRolesIndexes.Count != 0)
                    minIndex = afterRolesIndexes.Max() + 1;
            }

            var maxIndex = int.MaxValue;
            if (BeforeRoles.Count != 0)
            {
                var beforeRolesIndexes = BeforeRoles
                    .Where(roleIndexMapping.ContainsKey)
                    .Select(x => roleIndexMapping[x])
                    .ToList();

                if (beforeRolesIndexes.Count != 0)
                    maxIndex = beforeRolesIndexes.Min() - 1;
            }

            if (minIndex > maxIndex)
                throw new InvalidOperationException();

            return minIndex;
        }

        private bool ContainsRole(string role)
        {
            return BeforeRoles.Concat(AnyRoles).Concat(AfterRoles).Any(x => x == role);
        }
    }
}