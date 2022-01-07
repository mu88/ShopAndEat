﻿using DataLayer.EfClasses;
using DTO;
using DTO.Unit;
using FluentAssertions;
using NUnit.Framework;
using ServiceLayer.Concrete;
using Tests.Doubles;

namespace Tests.UnitTests.ServiceLayer
{
    [TestFixture]
    public class UnitServiceTests
    {
        [Test]
        public void CreateUnit()
        {
            using var context = new InMemoryDbContext();
            var testee = new UnitService(new SimpleCrudHelper(context, TestMapper.Create()));
            var newUnitDto = new NewUnitDto("Piece");

            testee.CreateUnit(newUnitDto);

            context.Units.Should().Contain(x => x.Name == "Piece");
        }

        [Test]
        public void DeleteUnit()
        {
            using var context = new InMemoryDbContext();
            var existingUnit = context.Units.Add(new Unit("Piece"));
            context.SaveChanges();
            var testee = new UnitService(new SimpleCrudHelper(context, TestMapper.Create()));
            var deleteUnitDto = new DeleteUnitDto(existingUnit.Entity.UnitId);

            testee.DeleteUnit(deleteUnitDto);

            context.Units.Should().NotContain(x => x.Name == "Piece");
        }

        [Test]
        public void GetAllUnits()
        {
            using var context = new InMemoryDbContext();
            context.Units.Add(new Unit("Piece"));
            context.Units.Add(new Unit("Bag"));
            context.SaveChanges();
            var testee = new UnitService(new SimpleCrudHelper(context, TestMapper.Create()));

            var results = testee.GetAllUnits();

            results.Should().Contain(x => x.Name == "Piece").And.Contain(x => x.Name == "Bag");
        }
    }
}