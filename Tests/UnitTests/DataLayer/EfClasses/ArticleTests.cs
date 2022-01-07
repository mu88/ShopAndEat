﻿using System.Diagnostics.CodeAnalysis;
using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
public class ArticleTests
{
    [Test]
    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalse")]
    public void CreateArticle()
    {
        var name = "Salat";
        var ingredientGroup = new ArticleGroup("Gemüse");
        var isInventory = true;

        var testee = new Article{Name =name, ArticleGroup = ingredientGroup,IsInventory = isInventory};

        testee.Name.Should().Be(name);
        testee.ArticleGroup.Should().Be(ingredientGroup);
        testee.IsInventory.Should().Be(isInventory);
    }
}