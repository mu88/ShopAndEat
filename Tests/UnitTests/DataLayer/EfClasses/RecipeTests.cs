﻿using System.Collections.ObjectModel;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses
{
    [TestFixture]
    public class RecipeTests
    {
        [Test]
        public void CreateRecipe()
        {
            var name = "Suppe";
            var numberOfDays = 3;
            var ingredients = new Collection<Ingredient>
                              {
                                  new Ingredient(new Article{Name="Tomato", ArticleGroup = new ArticleGroup("Vegetables"), IsInventory = false}, 3, new Unit("Bag"))
                              };

            var testee = new Recipe(name, numberOfDays, ingredients);

            testee.Name.Should().Be(name);
            testee.NumberOfDays.Should().Be(numberOfDays);
            testee.Ingredients.Should().BeEquivalentTo(ingredients);
        }
    }
}