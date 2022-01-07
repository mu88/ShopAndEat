﻿using DataLayer.EfClasses;
using FluentAssertions;
using NUnit.Framework;

namespace Tests.UnitTests.DataLayer.EfClasses;

[TestFixture]
public class ArticleGroupTests
{
    [Test]
    public void CreateArticleGroup()
    {
        var name = "Gemüse";

        var testee = new ArticleGroup(name);

        testee.Name.Should().Be(name);
    }
}