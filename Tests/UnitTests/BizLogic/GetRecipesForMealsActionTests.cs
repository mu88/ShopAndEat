﻿using System;
using BizLogic.Concrete;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.BizLogic
{
    [TestFixture]
    public class GetRecipesForMealsActionTests
    {
        [Test]
        public void GetRecipesForMeals()
        {
            var mealType = new MealType("Lunch", 1);
            var meal1 = new Meal(new DateTime(), mealType, new Recipe("Recipe 1", 3, new Ingredient[] { }));
            var meal2 = new Meal(new DateTime(), mealType, new Recipe("Recipe 2", 3, new Ingredient[] { }));
            var meals = new[] { meal1, meal2 };
            var testee = new GetRecipesForMealsAction();

            var results = testee.GetRecipesForMeals(meals);

            results.Should().HaveCount(2);
        }
    }
}