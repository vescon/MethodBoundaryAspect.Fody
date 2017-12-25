using System;
using System.Collections.Generic;
using System.Diagnostics;
using MethodBoundaryAspect.Fody.Ordering;
using NUnit.Framework;

namespace MethodBoundaryAspect.Fody.UnitTests
{
    public class AspectOrderTests
    {
        [Test]
        public void IfSorted_AfterAndBeforeAreEmpty_OrderShouldNotBeChanged()
        {
            // Arrange
            var afterOrders = new List<string>();
            var beforeOrders = new List<string>();

            var orders = new[] {"1", "2", "3", "4", "5", "6"};
            var expectedOrders = new[] {"1", "2", "3", "4", "5", "6"};

            // Act
            var comparer = new RoleOrderComparer(beforeOrders, afterOrders);
            Array.Sort(orders, comparer);

            // Arrange
            AssertArray(expectedOrders, orders);
        }

        [Test]
        public void IfSorted_BeforeIsInAfter_OrderShouldBeCorrect()
        {
            // Arrange
            var afterOrders = new List<string> {"A", "B"};
            var beforeOrders = new List<string> {"F"};

            var orders = new[] {"A", "F", "B", "4", "5", "6"};
            var expectedOrders = new[] {"A", "B", "4", "5", "6", "F"};

            // Act
            var comparer = new RoleOrderComparer(beforeOrders, afterOrders);
            Array.Sort(orders, comparer);

            // Arrange
            AssertArray(expectedOrders, orders);
        }

        [Test]
        public void IfUnsorted_BeforeIsInAfter_OrderShouldBeCorrect()
        {
            // Arrange
            var afterOrders = new List<string> {"A", "B"};
            var beforeOrders = new List<string> {"F"};

            var orders = new[] {"4", "F", "5", "6", "A", "B"};
            var expectedOrders = new[] {"A", "B", "4", "5", "6", "F"};

            // Act
            var comparer = new RoleOrderComparer(beforeOrders, afterOrders);
            Array.Sort(orders, comparer);

            // Arrange
            AssertArray(expectedOrders, orders);
        }

        private void AssertArray(string[] expectedArray, string[] actualArray)
        {
            Debug.WriteLine("Dump expected:");
            DumpArray(expectedArray);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("Dump actual:");
            DumpArray(actualArray);

            Assert.AreEqual(expectedArray.Length, actualArray.Length);
            for (var i = 0; i < expectedArray.Length; i++)
                Assert.AreEqual(expectedArray[i], actualArray[i]);
        }

        private static void DumpArray(string[] array)
        {
            for (var i = 0; i < array.Length; i++)
                Debug.WriteLine("{0}: {1}", i, array[i]);
        }
    }
}