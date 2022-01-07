﻿using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses
{
    [TestFixture]
    public class MealTypeTests
    {
        [Test]
        public void CreateMealType()
        {
            var name = "Lunch";

            var testee = new MealType(name, 1);

            testee.Name.Should().Be(name);
        }
    }
}