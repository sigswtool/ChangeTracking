﻿using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace ChangeTracking.Tests
{
    public class IChangeTrackableCollectionTests
    {
        [Fact]
        public void AsTrackable_On_Collection_Should_Make_Object_Implement_IChangeTrackableCollection()
        {
            var orders = Helper.GetOrdersIList();

            ICollection<Order> trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackableCollection<Order>>();
        }

        [Fact]
        public void When_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Not_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            IList<Order> trackable = orders.AsTrackable();

            trackable.Invoking(o => o.CastToIChangeTrackableCollection()).ShouldNotThrow<InvalidCastException>();
        }

        [Fact]
        public void When_Not_AsTrackable_On_Collection_CastToIChangeTrackableCollection_Should_Throw_InvalidCastException()
        {
            var orders = Helper.GetOrdersIList();

            orders.Invoking(o => o.CastToIChangeTrackableCollection()).ShouldThrow<InvalidCastException>();
        }

        [Fact]
        public void When_Calling_AsTrackable_On_Collection_Already_Tracking_Should_Throw()
        {
            var orders = Helper.GetOrdersIList();

            orders[0] = orders[0].AsTrackable();
            orders[0].CustomerNumber = "Test1";

            orders.Invoking(list => list.AsTrackable()).ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void When_Calling_AsTrackable_On_Collection_All_Items_Should_Become_Trackable()
        {
            var orders = Helper.GetOrdersIList();

            orders.AsTrackable();

            orders.Should().ContainItemsAssignableTo<IChangeTrackable<Order>>();
        }

        [Fact]
        public void When_Adding_To_Collection_Should_Be_IChangeTracableTrackable()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustomerNumber = "Customer" });

            trackable.Single(o => o.Id == 999999999).Should().BeAssignableTo<IChangeTrackable<Order>>();
        }

        [Fact]
        public void When_Adding_To_Collection_Status_Should_Be_Added()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            trackable.Add(new Order { Id = 999999999, CustomerNumber = "Customer" });

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Adding_Trackable_To_Collection_Status_Should_Be_Added()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var tOrder = new Order {Id = 999999999, CustomerNumber = "Customer"}.AsTrackable();
            trackable.Add(tOrder);

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
            tOrder.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }
        
        [Fact]
        public void When_Adding_To_Collection_Via_Indexer_Status_Should_Be_Added()
        {
            IList<Order> list = Helper.GetOrdersIList();

            var trackable = list.AsTrackable();
            trackable[0] = new Order { Id = 999999999, CustomerNumber = "Customer" };

            trackable.Single(o => o.Id == 999999999).CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Added);
        }

        [Fact]
        public void When_Deleting_From_Collection_Status_Should_Be_Deleted()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            first.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Deleted);
        }

        [Fact]
        public void When_Deleting_From_Collection_Should_Be_Added_To_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(1)
                .And.OnlyContain(o => o.Id == first.Id && o.CustomerNumber == first.CustomerNumber);
        }
        
        [Fact]
        public void When_Deleting_From_Collection_And_Re_Adding_Manually_At_Same_Index_Should_Be_Set_To_Unchanged()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);

            // Act
            trackable.Insert(0, first);

            // Assert
            var fTrack = first.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Unchanged);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);
        }

        [Fact]
        public void When_Deleting_From_Collection_And_Re_Adding_Manually_At_Different_Index_Should_Be_Set_To_Changed()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.CustomerNumber = "12345";
            trackable.Remove(first);

            // Act
            trackable.Add(first);

            // Assert
            var fTrack = first.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Changed);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);
        }

        [Fact]
        public void When_Deleting_From_Collection_And_Re_Adding_Manually_Into_Different_Collection_Should_Be_Set_To_Added()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable[0];
            var second = trackable[1];

            var orderDetails = first.OrderDetails[0];

            first.OrderDetails.Remove(orderDetails);

            // Act
            second.OrderDetails.Add(orderDetails);

            // Assert
            var fTrack = orderDetails.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Added);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);



            var change = trackable.CastToIChangeTrackableCollection();
            change.AcceptChanges();


            trackable[0].OrderDetails.Count.ShouldBeEquivalentTo(1);
            trackable[1].OrderDetails.Count.ShouldBeEquivalentTo(3);

            change.IsChanged.ShouldBeEquivalentTo(false);
        }

        [Fact]
        public void When_Deleting_From_Collection_And_Re_Adding_Manually_Into_Different_Collection_And_Later_Reverted_Should_Be_Removed()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable[0];
            var second = trackable[1];

            var orderDetails = first.OrderDetails[0];

            first.OrderDetails.Remove(orderDetails);

            // Act
            second.OrderDetails.Add(orderDetails);

            // Assert
            var fTrack = orderDetails.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Added);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);


            var odTrack = second.OrderDetails.CastToIChangeTrackableCollection();
            odTrack.AddedItems.Count().ShouldBeEquivalentTo(1);

            var change = trackable.CastToIChangeTrackableCollection();
            change.RejectChanges();


            trackable[0].OrderDetails.Count.ShouldBeEquivalentTo(2);
            trackable[1].OrderDetails.Count.ShouldBeEquivalentTo(2);


            change.IsChanged.ShouldBeEquivalentTo(false);
        }
        
        [Fact]
        public void When_Adding_To_Collection_And_Then_Deleting_Collection_Has_No_Changes()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable[0];

            var trackableDetails = first.OrderDetails;
            var orderDetails = first.OrderDetails[0];
            first.OrderDetails.Remove(orderDetails);

            // Act
            first.OrderDetails.Insert(0, orderDetails);

            // Assert
            var fTrack = orderDetails.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Unchanged);


            trackableDetails.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);
            trackableDetails.CastToIChangeTrackableCollection().AddedItems.Should().HaveCount(0);
            var change = trackable.CastToIChangeTrackableCollection();
            change.IsChanged.ShouldBeEquivalentTo(false);
        }

        [Fact]
        public void When_Removing_From_Collection_And_Then_Adding_Collection_Has_No_Changes()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable[0];

            var trackableDetails = first.OrderDetails;
            var orderDetails = first.OrderDetails[0];
            first.OrderDetails.Insert(1, new OrderDetail { ItemNo = "3333", OrderDetailId = 3333 });

            // Act
            first.OrderDetails.RemoveAt(1);

            // Assert
            var fTrack = orderDetails.CastToIChangeTrackable();
            fTrack.ChangeTrackingStatus.ShouldBeEquivalentTo(ChangeStatus.Unchanged);


            trackableDetails.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(0);
            trackableDetails.CastToIChangeTrackableCollection().AddedItems.Should().HaveCount(0);
            var change = trackable.CastToIChangeTrackableCollection();
            change.IsChanged.ShouldBeEquivalentTo(false);
        }

        [Fact]
        public void When_Deleting_From_Collection_Item_That_Status_Is_Added_Should_Not_Be_Added_To_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);
            var order = Helper.GetOrder();
            order.Id = 999;
            trackable.Add(order);
            trackable.Remove(trackable.Single(o => o.Id == 999));

            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().HaveCount(1)
                .And.OnlyContain(o => o.Id == first.Id && o.CustomerNumber == first.CustomerNumber);
        }

        [Fact]
        public void When_Using_Not_On_List_Of_T_Or_Collection_Of_T_Should_Throw()
        {
            var orders = Helper.GetOrdersIList().ToArray();

            orders.Invoking(o => o.AsTrackable()).ShouldThrow<InvalidOperationException>();
        }

        [Fact]
        public void AsTrackable_On_Collection_Should_Make_It_IRevertibleChangeTracking()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();

            trackable.Should().BeAssignableTo<System.ComponentModel.IRevertibleChangeTracking>();
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            collectionintf.AcceptChanges();

            trackable.All(o => o.CastToIChangeTrackable().ChangeTrackingStatus == ChangeStatus.Unchanged).Should().BeTrue();
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var itemIntf = first.CastToIChangeTrackable();
            var collectionintf = trackable.CastToIChangeTrackableCollection();
            int oldChangeStatusCount = collectionintf.ChangedItems.Count();
            collectionintf.AcceptChanges();

            itemIntf.GetOriginalValue(c => c.CustomerNumber).Should().Be("Testing");
            itemIntf.GetOriginalValue(c => c.Id).Should().Be(963);
            oldChangeStatusCount.Should().Be(1);
            collectionintf.ChangedItems.Count().Should().Be(0);
        }

        [Fact]
        public void AcceptChanges_On_Collection_Should_Clear_DeletedItems()
        {
            var orders = Helper.GetOrdersIList();

            var trackable = orders.AsTrackable();
            var first = trackable.First();
            trackable.Remove(first);
            var intf = trackable.CastToIChangeTrackableCollection();
            int oldDeleteStatusCount = intf.DeletedItems.Count();
            intf.AcceptChanges();


            oldDeleteStatusCount.Should().Be(1);
            intf.DeletedItems.Count().Should().Be(0);
            trackable.All(o => o.CastToIChangeTrackable().ChangeTrackingStatus == ChangeStatus.Unchanged).Should().BeTrue();
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_All_Items_Status_Be_Unchanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var intf = trackable.CastToIChangeTrackableCollection();
            var oldAnythingUnchanged = intf.ChangedItems.Any();
            intf.RejectChanges();

            oldAnythingUnchanged.Should().BeTrue();
            intf.ChangedItems.Count().Should().Be(0);
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_RejectChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = trackable.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var newOrder = Helper.GetOrder();
            newOrder.Id = 999;
            trackable.Add(newOrder);
            trackable.RemoveAt(5);
            var intf = trackable.CastToIChangeTrackableCollection();
            intf.RejectChanges();
            var ordersToMatch = Helper.GetOrdersIList().AsTrackable();

            intf.UnchangedItems.Should().Contain(i => ordersToMatch.SingleOrDefault(o =>
                o.Id == i.Id &&
                i.CustomerNumber == o.CustomerNumber &&
                i.CastToIChangeTrackable().ChangeTrackingStatus == o.CastToIChangeTrackable().ChangeTrackingStatus) != null);
            intf.UnchangedItems.Count().Should().Be(ordersToMatch.Count);
            intf.UnchangedItems.Count().Should().Be(intf.Count());
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_Move_DeletedItems_Back_To_Unchanged()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders.First();
            trackable.Remove(first);
            var intf = trackable.CastToIChangeTrackableCollection();
            intf.RejectChanges();

            trackable.Count.Should().Be(10);
            intf.UnchangedItems.Count().Should().Be(10);
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_Move_DeletedItems_Back_To_Unchanged_And_ReInsert_Them_At_Correct_Index()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders[4];
            trackable.Remove(first);
            var intf = trackable.CastToIChangeTrackableCollection();

            // Act
            intf.RejectChanges();

            // Assert
            trackable.Count.Should().Be(10);
            intf.UnchangedItems.Count().Should().Be(10);
            orders.IndexOf(first).ShouldBeEquivalentTo(4, "item was not re-inserted at original index");
        }

        [Fact]
        public void RejectChanges_On_Collection_Should_RejectChanges_Only_After_Last_AcceptChanges()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            var first = orders.First();
            first.Id = 963;
            first.CustomerNumber = "Testing";
            var collectionIntf = trackable.CastToIChangeTrackableCollection();
            collectionIntf.AcceptChanges();
            first.Id = 999;
            first.CustomerNumber = "Testing 123";
            collectionIntf.RejectChanges();
            var intf = first.CastToIChangeTrackable();
            var orderToMatch = Helper.GetOrder();
            orderToMatch.Id = 963;
            orderToMatch.CustomerNumber = "Testing";

            intf.GetOriginal().ShouldBeEquivalentTo(orderToMatch);
            intf.GetOriginalValue(o => o.Id).Should().Be(963);
        }

        [Fact]
        public void UnDelete_Should_Move_Back_Item_From_DeletedItems_And_Change_Back_Status()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            Order first = trackable.First();
            trackable.Remove(first);
            trackable.CastToIChangeTrackableCollection().UnDelete(first);

            trackable.Should().Contain(first);
            trackable.CastToIChangeTrackableCollection().DeletedItems.Should().NotContain(first).And.BeEmpty();
            first.CastToIChangeTrackable().ChangeTrackingStatus.Should().Be(ChangeStatus.Unchanged);
        }

        [Fact]
        public void Can_Enumerate_IChangeTrackableCollection()
        {
            var orders = Helper.GetOrdersIList();
            var trackable = orders.AsTrackable();

            trackable.Invoking(t => t.FirstOrDefault()).ShouldNotThrow();
        }

        [Fact]
        public void AsTrackable_On_ICollection_Should_Convert_ToIList_Internally()
        {
            IList<Order> orders = Helper.GetOrdersIList();
            ICollection<Order> ordersSet = new System.Collections.ObjectModel.Collection<Order>(orders);

            ICollection<Order> trackable = ordersSet.AsTrackable();

            trackable.Should().BeAssignableTo<IChangeTrackableCollection<Order>>();
        }

        [Fact]
        public void Can_AddProxy_ToProxyCollection()
        {
            // Arrange
            var orders = Helper.GetOrdersIList();
            var tOrders = orders.AsTrackable();

            var order = Helper.GetOrder(2222, custumerNumber: "2222");
            var tOrder = order.AsTrackable();

            Exception exception = null;
            try
            {
                // Act
                tOrders.Add(tOrder);
            }
            catch (Exception x)
            {
                exception = x;
            }

            // Assert
            Assert.Null(exception);
        }
    }
}
