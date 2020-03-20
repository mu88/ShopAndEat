﻿using System;
using System.Collections.ObjectModel;
using DataLayer.Core;
using DataLayer.EfClasses;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Tests.DataLayer.EfClasses
{
    [TestFixture]
    public class PurchaseTests
    {
        [Test]
        public void CreatePurchase()
        {
            var from = new DateTime();
            var to = new DateTime();
            var components = new Collection<(IIngredient, int)> { (new Mock<IIngredient>().Object, 3) };

            var testee = new Purchase(from, to, components);

            testee.From.Should().Be(from);
            testee.To.Should().Be(to);
            testee.Components.Should().BeEquivalentTo(components);
        }
    }
}